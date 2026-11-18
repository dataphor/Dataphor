/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Compiling
{
	using Alphora.Dataphor.DAE.Schema;
	
	/// <summary>Caches operator resolutions for the catalog. Shared by every session; safe for concurrent use.</summary>
	/// <remarks>
	/// Every member takes the private <c>_syncRoot</c> lock. The compiler also locks the cache instance itself
	/// around a whole resolution; that outer lock is separate, so the Clear overloads (called while compiling
	/// create/drop operator, from DDL and library nodes, and from the catalog device session when it changes an
	/// operator or conversion, often without any catalog lock held) never wait on a resolution in progress.
	/// <c>_syncRoot</c> is held only inside these members, which call nothing that takes another lock, so it
	/// cannot take part in a deadlock.
	/// </remarks>
	public class OperatorResolutionCache : System.Object
	{
		private readonly object _syncRoot = new object();
		private Dictionary<OperatorBindingContext, OperatorBindingContext> _resolutions = new Dictionary<OperatorBindingContext, OperatorBindingContext>();

		public OperatorBindingContext this[OperatorBindingContext context]
		{
			get
			{
				lock (_syncRoot)
				{
					OperatorBindingContext localContext = null;
					_resolutions.TryGetValue(context, out localContext);
					return localContext;
				}
			}
		}		
		
		public void Add(OperatorBindingContext context)
		{
			lock (_syncRoot)
			{
				if (!_resolutions.ContainsKey(context))
					_resolutions.Add(context, context);
			}
		}
		
		public void Clear()
		{
			lock (_syncRoot)
				_resolutions.Clear();
		}
		
		/// <summary>Removes cached resolutions for this operator, if any.</summary>
		public void Clear(Operator operatorValue)
		{
			lock (_syncRoot)
			{
				List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
				foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
					if (entry.Value.Operator == operatorValue)
						resolutions.Add(entry.Value);
			
				foreach (OperatorBindingContext context in resolutions)
					_resolutions.Remove(context);
			}
		}
		
		/// <summary>Removes cached resolutions for the given operator name, if any.</summary>
		public void Clear(string operatorName)
		{
			lock (_syncRoot)
			{
				string unqualifiedName = Schema.Object.Unqualify(operatorName);
				List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
				foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
					if (Schema.Object.NamesEqual(Schema.Object.EnsureUnrooted(entry.Value.OperatorName), unqualifiedName))
						resolutions.Add(entry.Value);
					
				foreach (OperatorBindingContext context in resolutions)
					_resolutions.Remove(context);
			}
		}
		
		/// <summary>Removes cached resolutions involving conversions referencing the given scalar type, if any.</summary>
		public void Clear(Schema.ScalarType sourceType, Schema.ScalarType targetType)
		{
			lock (_syncRoot)
			{
				List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
				foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
				{
					foreach (OperatorMatch match in entry.Value.Matches)
						foreach (ConversionContext conversionContext in match.ConversionContexts)
						{
							ScalarConversionContext scalarConversionContext = conversionContext as ScalarConversionContext;
							if (scalarConversionContext != null)
							{
								if (!scalarConversionContext.CanConvert && conversionContext.SourceType.Equals(sourceType) || conversionContext.TargetType.Equals(sourceType) || conversionContext.SourceType.Equals(targetType) || conversionContext.TargetType.Equals(targetType))
								{
									resolutions.Add(entry.Value);
									goto Continue;
								}
							
								foreach (Schema.ScalarConversionPath path in scalarConversionContext.Paths)
									if (path.Contains(sourceType) || path.Contains(targetType))
									{
										resolutions.Add(entry.Value);
										goto Continue;
									}
							}
						}
					
					Continue: continue;
				}
			
				foreach (OperatorBindingContext removeContext in resolutions)
					_resolutions.Remove(removeContext);
			}
		}
		
		/// <summary>Removes cached resolutions involving conversion paths using the given conversion, if any.</summary>
		public void Clear(Schema.Conversion conversion)
		{
			lock (_syncRoot)
			{
				List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
				foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
				{
					foreach (OperatorMatch match in entry.Value.Matches)
						foreach (ConversionContext conversionContext in match.ConversionContexts)
						{
							ScalarConversionContext scalarConversionContext = conversionContext as ScalarConversionContext;
							if (scalarConversionContext != null)
								foreach (Schema.ScalarConversionPath path in scalarConversionContext.Paths)
									if (path.Contains(conversion))
									{
										resolutions.Add(entry.Value);
										goto Continue;
									}
						}
					
					Continue: continue;
				}
			
				foreach (OperatorBindingContext removeContext in resolutions)
					_resolutions.Remove(removeContext);
			}
		}
		
		/// <summary>Removes cached resolutions involving the given name resolution path, if any.</summary>
		public void Clear(Schema.NameResolutionPath resolutionPath)
		{
			lock (_syncRoot)
			{
				List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
				foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
					if (entry.Value.ResolutionPath == resolutionPath)
						resolutions.Add(entry.Value);
					
				foreach (OperatorBindingContext context in resolutions)
					_resolutions.Remove(context);
			}
		}
	}
}
