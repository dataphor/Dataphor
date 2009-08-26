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
		private Hashtable FResolutions = new Hashtable();

		public OperatorBindingContext this[OperatorBindingContext AContext]
		{
			get
			{
				return FResolutions[AContext] as OperatorBindingContext;
			}
		}		
		
		public void Add(OperatorBindingContext AContext)
		{
			if (!FResolutions.Contains(AContext))
				FResolutions.Add(AContext, AContext);
		}
		
		public void Clear()
		{
			FResolutions.Clear();
		}
		
		/// <summary>Removes cached resolutions for this operator, if any.</summary>
		public void Clear(Operator AOperator)
		{
			ArrayList LResolutions = new ArrayList();
			foreach (DictionaryEntry LEntry in FResolutions)
				if (((OperatorBindingContext)LEntry.Value).Operator == AOperator)
					LResolutions.Add(LEntry.Value);
			
			foreach (OperatorBindingContext LContext in LResolutions)
				FResolutions.Remove(LContext);
		}
		
		/// <summary>Removes cached resolutions for the given operator name, if any.</summary>
		public void Clear(string AOperatorName)
		{
			string LUnqualifiedName = Schema.Object.Unqualify(AOperatorName);
			ArrayList LResolutions = new ArrayList();
			foreach (DictionaryEntry LEntry in FResolutions)
				if (Schema.Object.NamesEqual(Schema.Object.EnsureUnrooted(((OperatorBindingContext)LEntry.Value).OperatorName), LUnqualifiedName))
					LResolutions.Add(LEntry.Value);
					
			foreach (OperatorBindingContext LContext in LResolutions)
				FResolutions.Remove(LContext);
		}
		
		/// <summary>Removes cached resolutions involving conversions referencing the given scalar type, if any.</summary>
		public void Clear(Schema.ScalarType ASourceType, Schema.ScalarType ATargetType)
		{
			ArrayList LResolutions = new ArrayList();
			OperatorBindingContext LContext;
			foreach (DictionaryEntry LEntry in FResolutions)
			{
				LContext = (OperatorBindingContext)LEntry.Value;
				foreach (OperatorMatch LMatch in LContext.Matches)
					foreach (ConversionContext LConversionContext in LMatch.ConversionContexts)
					{
						ScalarConversionContext LScalarConversionContext = LConversionContext as ScalarConversionContext;
						if (LScalarConversionContext != null)
						{
							if (!LScalarConversionContext.CanConvert && LConversionContext.SourceType.Equals(ASourceType) || LConversionContext.TargetType.Equals(ASourceType) || LConversionContext.SourceType.Equals(ATargetType) || LConversionContext.TargetType.Equals(ATargetType))
							{
								LResolutions.Add(LEntry.Value);
								goto Continue;
							}
							
							foreach (Schema.ScalarConversionPath LPath in LScalarConversionContext.Paths)
								if (LPath.Contains(ASourceType) || LPath.Contains(ATargetType))
								{
									LResolutions.Add(LEntry.Value);
									goto Continue;
								}
						}
					}
					
				Continue: continue;
			}
			
			foreach (OperatorBindingContext LRemoveContext in LResolutions)
				FResolutions.Remove(LRemoveContext);
		}
		
		/// <summary>Removes cached resolutions involving conversion paths using the given conversion, if any.</summary>
		public void Clear(Schema.Conversion AConversion)
		{
			ArrayList LResolutions = new ArrayList();
			OperatorBindingContext LContext;
			foreach (DictionaryEntry LEntry in FResolutions)
			{
				LContext = (OperatorBindingContext)LEntry.Value;
				foreach (OperatorMatch LMatch in LContext.Matches)
					foreach (ConversionContext LConversionContext in LMatch.ConversionContexts)
					{
						ScalarConversionContext LScalarConversionContext = LConversionContext as ScalarConversionContext;
						if (LScalarConversionContext != null)
							foreach (Schema.ScalarConversionPath LPath in LScalarConversionContext.Paths)
								if (LPath.Contains(AConversion))
								{
									LResolutions.Add(LEntry.Value);
									goto Continue;
								}
					}
					
				Continue: continue;
			}
			
			foreach (OperatorBindingContext LRemoveContext in LResolutions)
				FResolutions.Remove(LRemoveContext);
		}
		
		/// <summary>Removes cached resolutions involving the given name resolution path, if any.</summary>
		public void Clear(Schema.NameResolutionPath AResolutionPath)
		{
			ArrayList LResolutions = new ArrayList();
			foreach (DictionaryEntry LEntry in FResolutions)
				if (((OperatorBindingContext)LEntry.Value).ResolutionPath == AResolutionPath)
					LResolutions.Add(LEntry.Value);
					
			foreach (OperatorBindingContext LContext in LResolutions)
				FResolutions.Remove(LContext);
		}
	}
}
