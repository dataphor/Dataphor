/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Server
{
	public class CachedPlanHeader
	{
		public CachedPlanHeader(string statement, string libraryName, int contextHashCode, bool inApplicationTransaction)
		{
			Statement = statement;
			LibraryName = libraryName;
			ContextHashCode = contextHashCode;
			InApplicationTransaction = inApplicationTransaction;
		}
		
		public string Statement;
		public string LibraryName;
		public int ContextHashCode; // Hash of the names of all types present on the context for the process
		public bool InApplicationTransaction;
		
		/// <summary>This flag will be set to true if the plan results in an error on open, indicating that it is invalid, and should not be returned to the plan cache.</summary>
		public bool IsInvalidPlan;
	
		public override int GetHashCode()
		{
			return Statement.GetHashCode() ^ LibraryName.GetHashCode() ^ ContextHashCode ^ InApplicationTransaction.GetHashCode();
		}
		
		public override bool Equals(object objectValue)
		{
			CachedPlanHeader cachedPlanHeader = objectValue as CachedPlanHeader;
			return 
				(cachedPlanHeader != null) 
					&& (cachedPlanHeader.Statement == Statement) 
					&& (cachedPlanHeader.LibraryName == LibraryName) 
					&& (cachedPlanHeader.ContextHashCode == ContextHashCode) 
					&& (cachedPlanHeader.InApplicationTransaction == InApplicationTransaction);
		}
	}
	
	public class CachedPlans : List
	{
		public new ServerPlan this[int index]
		{
			get { return (ServerPlan)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class PlanCache : System.Object
	{
		public PlanCache(int cacheSize) : base() 
		{
			_size = cacheSize;
			if (_size > 1)
				_plans = new FixedSizeCache<CachedPlanHeader, CachedPlans>(_size);
		}
		
		private int _size;
		public int Size { get { return _size; } }
		
		public int Count { get { return _plans == null ? 0 : _plans.Count; } }

		private FixedSizeCache<CachedPlanHeader, CachedPlans> _plans;
		
		private void DisposeCachedPlan(ServerProcess process, ServerPlan plan)
		{
			try
			{
				plan.BindToProcess(process.ServerSession.Server._systemProcess);
				plan.Dispose();
			}
			catch
			{
				// ignore disposal exceptions
			}
		}
		
		private void DisposeCachedPlans(ServerProcess process, CachedPlans plans)
		{
			foreach (ServerPlan plan in plans)
				DisposeCachedPlan(process, plan);
		}
		
		private CachedPlanHeader GetPlanHeader(ServerProcess process, string statement, int contextHashCode)
		{
			return new CachedPlanHeader(statement, process.ServerSession.CurrentLibrary.Name, contextHashCode, process.ApplicationTransactionID != Guid.Empty);
		}

		/// <summary>Gets a cached plan for the given statement, if available.</summary>
		/// <remarks>
		/// If a plan is found, it is referenced for the LRU, and disowned by the cache.
		/// The client must call Release to return the plan to the cache.
		/// If no plan is found, null is returned and the cache is unaffected.
		/// </remarks>
		public ServerPlan Get(ServerProcess process, string statement, int contextHashCode)
		{
			ServerPlan plan = null;
			CachedPlanHeader header = GetPlanHeader(process, statement, contextHashCode);
			CachedPlans bumped = null;
			lock (this)
			{
				if (_plans != null)
				{
					CachedPlans plans;
					if (_plans.TryGetValue(header, out plans))
					{
						for (int planIndex = plans.Count - 1; planIndex >= 0; planIndex--)
						{
							plan = plans[planIndex];
							plans.RemoveAt(planIndex);
							if (process.Catalog.PlanCacheTimeStamp > plan.PlanCacheTimeStamp)
							{
								DisposeCachedPlan(process, plan);
								plan = null;
							}
							else
							{
								bumped = _plans.Reference(header, plans);
								break;
							}
						}
					}
				}
			}
			
			if (bumped != null)
				DisposeCachedPlans(process, bumped);

			if (plan != null)
				plan.BindToProcess(process);

			return plan;
		}

		/// <summary>Adds the given plan to the plan cache.</summary>		
		/// <remarks>
		/// The plan is not contained within the cache after this call, it is assumed in use by the client.
		/// This call simply reserves storage and marks the plan as referenced for the LRU.
		/// </remarks>
		public void Add(ServerProcess process, string statement, int contextHashCode, ServerPlan plan)
		{
			CachedPlans bumped = null;
			CachedPlanHeader header = GetPlanHeader(process, statement, contextHashCode);
			plan.Header = header;
			plan.PlanCacheTimeStamp = process.Catalog.PlanCacheTimeStamp;

			lock (this)
			{
				if (_plans != null)
				{
					CachedPlans plans;
					if (!_plans.TryGetValue(header, out plans))
						plans = new CachedPlans();
					bumped = _plans.Reference(header, plans);
				}
			}
			
			if (bumped != null)
				DisposeCachedPlans(process, bumped);
		}
		
		/// <summary>Releases the given plan and returns whether or not it was returned to the cache.</summary>
		/// <remarks>
		/// If the plan is returned to the cache, the client is no longer responsible for the plan, it is owned by the cache.
		/// If the plan is not returned to the cache, the cache client is responsible for disposing the plan.
		///	</remarks>
		public bool Release(ServerProcess process, ServerPlan plan)
		{
			CachedPlanHeader header = plan.Header;

			lock (this)
			{
				if (_plans != null)
				{
					CachedPlans plans;
					if (_plans.TryGetValue(header, out plans))
					{
						plan.UnbindFromProcess();
						plans.Add(plan);
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>Clears the plan cache, disposing any plans it contains.</summary>		
		public void Clear(ServerProcess process)
		{
			lock (this)
			{
				if (_plans != null)
				{
					foreach (CachedPlans tempValue in (IEnumerable<CachedPlans>)_plans)
						DisposeCachedPlans(process, tempValue);
					
					_plans.Clear();
				}
			}
		}

		/// <summary>Resizes the cache to the specified size.</summary>
		/// <remarks>
		/// Resizing the cache has the effect of clearing the entire cache.
		/// </remarks>
		public void Resize(ServerProcess process, int size)
		{
			lock (this)
			{
				if (_plans != null)
				{
					Clear(process);
					_plans = null;
				}
				
				_size = size;
				if (_size > 1)
					_plans = new FixedSizeCache<CachedPlanHeader, CachedPlans>(_size);
			}
		}
	}
}
