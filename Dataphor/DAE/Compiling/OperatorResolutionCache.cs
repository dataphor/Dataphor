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
	
	public class OperatorResolutionCache : System.Object
	{
		private Dictionary<OperatorBindingContext, OperatorBindingContext> _resolutions = new Dictionary<OperatorBindingContext, OperatorBindingContext>();

		public OperatorBindingContext this[OperatorBindingContext context]
		{
			get
			{
				OperatorBindingContext localContext = null;
				_resolutions.TryGetValue(context, out localContext);
				return localContext;
			}
		}		
		
		public void Add(OperatorBindingContext context)
		{
			if (!_resolutions.ContainsKey(context))
				_resolutions.Add(context, context);
		}
		
		public void Clear()
		{
			_resolutions.Clear();
		}
		
		/// <summary>Removes cached resolutions for this operator, if any.</summary>
		public void Clear(Operator operatorValue)
		{
			List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
			foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
				if (entry.Value.Operator == operatorValue)
					resolutions.Add(entry.Value);
			
			foreach (OperatorBindingContext context in resolutions)
				_resolutions.Remove(context);
		}
		
		/// <summary>Removes cached resolutions for the given operator name, if any.</summary>
		public void Clear(string operatorName)
		{
			string unqualifiedName = Schema.Object.Unqualify(operatorName);
			List<OperatorBindingContext> resolutions = new List<OperatorBindingContext>();
			foreach (KeyValuePair<OperatorBindingContext, OperatorBindingContext> entry in _resolutions)
				if (Schema.Object.NamesEqual(Schema.Object.EnsureUnrooted(entry.Value.OperatorName), unqualifiedName))
					resolutions.Add(entry.Value);
					
			foreach (OperatorBindingContext context in resolutions)
				_resolutions.Remove(context);
		}
		
		/// <summary>Removes cached resolutions involving conversions referencing the given scalar type, if any.</summary>
		public void Clear(Schema.ScalarType sourceType, Schema.ScalarType targetType)
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
		
		/// <summary>Removes cached resolutions involving conversion paths using the given conversion, if any.</summary>
		public void Clear(Schema.Conversion conversion)
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
		
		/// <summary>Removes cached resolutions involving the given name resolution path, if any.</summary>
		public void Clear(Schema.NameResolutionPath resolutionPath)
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
