/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime
{
	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Collections.Generic;
	
	public struct LockID
	{
		/// <summary> Creates a lock ID. </summary>
		/// <param name="AOwner"> The object which owns this resource.  This object should implement a unique ToString(). </param>
		/// <param name="ALockName"> A lock name, which should be unique within an owner. </param>
		public LockID(object AOwner, string ALockName)
		{
			Error.AssertFail(AOwner != null, "LockID cannot have a null owner");
			Owner = AOwner;
			LockName = ALockName;
		}
		
		public readonly object Owner;
		public readonly string LockName;
		
		public override int GetHashCode()
		{
			return Owner.GetHashCode() ^ LockName.GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			if (AObject is LockID)
			{
				LockID LLockID = (LockID)AObject;
				return (Object.ReferenceEquals(LLockID.Owner, Owner)) && (LLockID.LockName == LockName);
			}
			return false;
		}
		
		public override string ToString()
		{
			return String.Format("{0}.{1}", Owner.ToString(), LockName);
		}
	}
	
	public struct LockHeader
	{
		public LockHeader(LockID ALockID, Semaphore ASemaphore)
		{
			LockID = ALockID;
			Semaphore = ASemaphore;
		}
		
		public LockID LockID;
		public Semaphore Semaphore;
		
		public override int GetHashCode()
		{
			return LockID.GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is LockHeader) && (LockID.Equals(((LockHeader)AObject).LockID));
		}
	}

	public class LockHeaderLink : System.Object
	{
		public LockHeaderLink() : base() {}

		public LockHeader LockHeader;		
		public LockHeaderLink NextLink;
	}
	
	public class LockHeaderLinkChain : System.Object
	{
		public LockHeaderLinkChain() : base() {}
		
		private LockHeaderLink FHead;

		public LockHeaderLink Add(LockHeaderLink ALink)
		{
			ALink.NextLink = FHead;
			FHead = ALink;
			return ALink;
		}
		
		public LockHeaderLink Remove()
		{
			LockHeaderLink LLink = FHead;
			FHead = FHead.NextLink;
			return LLink;
		}
		
		public bool IsEmpty()
		{
			return FHead == null;
		}
	}
	
	// TODO: LockHeader list should be a pre-allocated buffer pool with buffer replacement
	public class LockManager : System.Object
	{
		public const int CDefaultTimeout = 3000;
		
		public LockManager(){}

		private Dictionary<LockID, LockHeader> FLocks = new Dictionary<LockID, LockHeader>();
		internal Dictionary<LockID, LockHeader> Locks { get { return FLocks; } }
		
		private LockHeaderLinkChain FAvailable = new LockHeaderLinkChain();
		private LockHeaderLinkChain FLinkBuffer = new LockHeaderLinkChain();
		
		private int FMaxLocks = 1000;
		public int MaxLocks 
		{ 
			get { return FMaxLocks; } 
			set { FMaxLocks = value; }
		}
		
		private int FLockCount = 0;
		public int LockCount { get { return FLockCount; } }

		private List<string> FLockEvents;
		public List<string> LockEvents { get { return FLockEvents; } }
		
		public String LockEventsAsString()
		{
			if (FLockEvents == null)
				return String.Empty;
			
			StringBuilder LBuilder = new StringBuilder();
			foreach (String LString in FLockEvents)
				LBuilder.AppendFormat("{0}\r\n", LString);
			LBuilder.ToString();
			return LBuilder.ToString();
		}
		
		private bool FLockTracingEnabled;
		public bool LockTracingEnabled
		{
			get { return FLockTracingEnabled; }
			set
			{
				if (FLockTracingEnabled != value)
					FLockTracingEnabled = value;
				if (FLockTracingEnabled)
					FLockEvents = new List<string>();
				else
					FLockEvents = null;
			}
		}

		private LockHeader GetLockHeader(LockID ALockID)
		{
			lock (this)
			{
				LockHeader LLock;
				if (!FLocks.TryGetValue(ALockID, out LLock))
				{
					LLock = RequestLockHeader(ALockID);
					FLocks.Add(ALockID, LLock);
				}
				return LLock;
			}
		}
		
		private LockHeader AllocateLockHeader(LockID ALockID)
		{
			#if DEBUG
			if (FLockCount > FMaxLocks)
				throw new RuntimeException(RuntimeException.Codes.LockManagerOverflow);
			#endif
			
			FLockCount++;
			return new LockHeader(ALockID, new Semaphore());
		}
		
		private LockHeader RequestLockHeader(LockID ALockID)
		{
			if (FAvailable.IsEmpty())
				return AllocateLockHeader(ALockID);
			else
			{
				LockHeader LLockHeader = FLinkBuffer.Add(FAvailable.Remove()).LockHeader;
				LLockHeader.LockID = ALockID;
				return LLockHeader;
			}
		}
		
		private void ReleaseLockHeader(LockHeader ALockHeader)
		{
			LockHeaderLink LLink = GetLockHeaderLink();
			LLink.LockHeader = ALockHeader;
			FAvailable.Add(LLink);
		}
		
		private LockHeaderLink GetLockHeaderLink()
		{
			if (FLinkBuffer.IsEmpty())
				return new LockHeaderLink();
			else
				return FLinkBuffer.Remove();
		}
		
		public bool IsLocked(LockID ALockID)
		{
			LockHeader LLock;
			bool LFound;
			lock (this)
			{
				LFound = FLocks.TryGetValue(ALockID, out LLock);
			}
			
			return LFound && (LLock.Semaphore.Mode != LockMode.Free);
		}
		
		public void Lock(int AOwnerID, LockID ALockID, LockMode ALockMode, int ATimeout)
		{
			lock (this)
			{
				try
				{
					GetLockHeader(ALockID).Semaphore.Acquire(AOwnerID, ALockMode, ATimeout);
					if (FLockTracingEnabled)
						FLockEvents.Add(String.Format("Request LockID: {0}, Owner: {1}, Mode: {2}", ALockID.ToString(), AOwnerID.ToString(), ALockMode.ToString()));
				}
				catch (RuntimeException E)
				{
					if (E.Code == (int)RuntimeException.Codes.SemaphoreTimeout)
						throw new RuntimeException(RuntimeException.Codes.LockTimeout, ALockMode.ToString(), ALockID.LockName, ALockID.Owner.ToString());
					else
						throw;
				}
			}
		}
		
		public void Lock(int AOwnerID, LockID ALockID, LockMode ALockMode)
		{
			Lock(AOwnerID, ALockID, ALockMode, CDefaultTimeout);
		}
		
		public bool LockImmediate(int AOwnerID, LockID ALockID, LockMode ALockMode)
		{
			lock (this)
			{
				bool LResult = GetLockHeader(ALockID).Semaphore.AcquireImmediate(AOwnerID, ALockMode);
				if (FLockTracingEnabled)
					FLockEvents.Add(String.Format("Request LockID: {0}, Owner: {1}, Mode: {2}", ALockID.ToString(), AOwnerID.ToString(), ALockMode.ToString()));
				return LResult;
			}
		}
		
		public void Unlock(int AOwnerID, LockID ALockID)
		{
			lock (this)
			{
				LockHeader LLockHeader = GetLockHeader(ALockID);
				LLockHeader.Semaphore.Release(AOwnerID);
				if (LLockHeader.Semaphore.Mode == LockMode.Free)
				{
					FLocks.Remove(ALockID);
					ReleaseLockHeader(LLockHeader);
				}
				if (FLockTracingEnabled)
					FLockEvents.Add(String.Format("Release LockID: {0}, Owner: {1}", ALockID.ToString(), AOwnerID.ToString()));
			}
		}
		
		public string ListAllOpenLocks()
		{
			return ListLocks(-1, -1, LockMode.Shared);
		}
		
		public string ListLocks(int AOwner, int AOwnerID, LockMode AMode)
		{
			StringBuilder LResult = new StringBuilder();
			foreach (KeyValuePair<LockID, LockHeader> LEntry in FLocks)
			{
				LockHeader LLock = LEntry.Value;
				if ((AOwner == null) || Object.ReferenceEquals(LLock.LockID.Owner, AOwner))
					if (LLock.Semaphore.Mode >= AMode)
						if ((AOwnerID == -1) || ((LLock.Semaphore.Mode > LockMode.Free) && (LLock.Semaphore.IsSemaphoreOwned(AOwnerID))))
							LResult.AppendFormat
							(
								"Owner: {0} Lock Name: {1} Current Lock Mode: {2} Grant Count: {3} Wait Count: {4}\r\n", 
								LLock.LockID.Owner.ToString(), 
								LLock.LockID.LockName,
								LLock.Semaphore.Mode.ToString(),
								LLock.Semaphore.GrantCount().ToString(),
								LLock.Semaphore.WaitCount().ToString()
							);
			}
			return LResult.ToString();
		}
		
		public int Count()
		{
			lock (this)
			{
				return FLocks.Count;
			}
		}
	}
}
