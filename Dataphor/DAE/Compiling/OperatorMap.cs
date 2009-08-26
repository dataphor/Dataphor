/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Compiling
{
	using Alphora.Dataphor.DAE.Schema;
	
	public class OperatorMap : Schema.Object
    {
		public OperatorMap(string AName) : base(AName)
		{
			FSignatures = new OperatorSignatures(null);
		}
		
		protected OperatorSignatures FSignatures;
		
		public OperatorSignatures Signatures { get { return FSignatures; } }
		
		public int SignatureCount
		{
			get { return FSignatures.Count; }
		}
		
		public void AddSignature(Operator AOperator)
		{
			FSignatures.Add(new OperatorSignature(AOperator));
		}
		
		public void RemoveSignature(Signature ASignature)
		{
			FSignatures.Remove(ASignature);
		}
		
		public bool ContainsSignature(Signature ASignature)
		{
			return FSignatures.Contains(ASignature);
		}
		
		public void ResolveSignature(Plan APlan, OperatorBindingContext AContext)
		{
			FSignatures.Resolve(APlan, AContext);
		}

		#if USEVIRTUAL		
		public Operator ResolveInheritedSignature(Signature ASignature)
		{
			OperatorSignature LSignature = FSignatures.ResolveInherited(ASignature);
			return LSignature != null ? LSignature.Operator : null;
		}
		#endif
		
		public string ShowMap()
		{
			StringBuilder LString = new StringBuilder(Name);
			LString.Append(":\n");
			LString.Append(FSignatures.ShowSignatures(1));
			return LString.ToString();
		}
    }
    
	public class OperatorMaps : Schema.Objects
    {		
		public new OperatorMap this[int AIndex]
		{
			get { return (OperatorMap)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public new OperatorMap this[string AName]
		{
			get { return (OperatorMap)base[AName]; }
			set { base[AName] = value; }
		}
		
		#if SINGLENAMESPACE
		public new OperatorMap this[string AName, string ANameSpace]
		{
			get { return (OperatorMap)base[AName, ANameSpace]; }
			set { base[AName, ANameSpace] = value; }
		}
		#endif
		
		public void ResolveCall(Plan APlan, OperatorBindingContext AContext)
		{
			lock (this)
			{
				bool LDidResolve = false;
				IntegerList LIndexes = InternalIndexesOf(AContext.OperatorName);
				OperatorBindingContext LContext = new OperatorBindingContext(AContext.Statement, AContext.OperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
				for (int LIndex = 0; LIndex < LIndexes.Count; LIndex++)
				{
					LContext.OperatorNameContext.Names.Add(this[LIndexes[LIndex]].Name);
					this[LIndexes[LIndex]].ResolveSignature(APlan, LContext);
				}
				
				foreach (Schema.LoadedLibraries LLevel in APlan.NameResolutionPath)
				{
					OperatorBindingContext LLevelContext = new OperatorBindingContext(AContext.Statement, AContext.OperatorName, AContext.ResolutionPath, AContext.CallSignature, AContext.IsExact);
					foreach (OperatorMatch LMatch in LContext.Matches)
					{
						// If the operator resolution is in any library at this level, add it to a binding context for this level
						if ((LMatch.Signature.Operator.Library == null) || LLevel.ContainsName(LMatch.Signature.Operator.Library.Name))
						{
							if (!LLevelContext.OperatorNameContext.Names.Contains(LMatch.Signature.Operator.OperatorName))
								LLevelContext.OperatorNameContext.Names.Add(LMatch.Signature.Operator.OperatorName);
								
							if (!LLevelContext.Matches.Contains(LMatch))
								LLevelContext.Matches.Add(LMatch);
						}
					}
					
					if (LLevelContext.Matches.IsExact)
					{
						LLevelContext.Operator = LLevelContext.Matches.Match.Signature.Operator;
						LLevelContext.OperatorNameContext.Object = this[IndexOfName(LLevelContext.Operator.OperatorName)];
						LLevelContext.OperatorNameContext.Names.Add(LLevelContext.OperatorNameContext.Object.Name);
						AContext.SetBindingDataFromContext(LLevelContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at this level
						foreach (string LName in LLevelContext.OperatorNameContext.Names)
							if (!AContext.OperatorNameContext.Names.Contains(LName))
								AContext.OperatorNameContext.Names.Add(LName);
								
						foreach (OperatorMatch LMatch in LLevelContext.Matches)
							if (!AContext.Matches.Contains(LMatch))
							{
								AContext.Matches.Add(LMatch);
								LDidResolve = true;
							}
					}
				}
				
				// If a partial match is found within the name resolution path, use it
				if (!AContext.IsExact && AContext.Matches.IsPartial)
				{
					LDidResolve = true;
				}
				else
				{
					// The name resolution path has been searched and no match was found, so attempt to resolve based on all signatures
					if (LContext.Matches.IsExact)
					{
						LContext.Operator = LContext.Matches.Match.Signature.Operator;
						LContext.OperatorNameContext.Object = this[IndexOfName(LContext.Operator.OperatorName)];
						LContext.OperatorNameContext.Names.Add(LContext.OperatorNameContext.Object.Name);
						AContext.SetBindingDataFromContext(LContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at all levels
						foreach (string LName in LContext.OperatorNameContext.Names)
							if (!AContext.OperatorNameContext.Names.Contains(LName))
								AContext.OperatorNameContext.Names.Add(LName);
								
						foreach (OperatorMatch LMatch in LContext.Matches)
							if (!AContext.Matches.Contains(LMatch))
							{
								AContext.Matches.Add(LMatch);
								LDidResolve = true;
							}
					}
				}
			
				// Ensure that if any resolutions were performed in this catalog, the binding data is set in the context
				if (LDidResolve)
				{
					if (AContext.Matches.IsExact || (!AContext.IsExact && AContext.Matches.IsPartial))
					{
						if ((AContext.Operator == null) || (AContext.Operator != AContext.Matches.Match.Signature.Operator))
						{
							AContext.Operator = AContext.Matches.Match.Signature.Operator;
							AContext.OperatorNameContext.Object = this[AContext.Operator.OperatorName];
						}
					}
					else
						AContext.Operator = null;
				}
			}
		}
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
		{
			if (!(AItem is OperatorMap))
				throw new SchemaException(SchemaException.Codes.OperatorMapContainer);
			base.Validate(AItem);
		}
		#endif
		
		public void AddOperator(Operator AOperator)
		{
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex < 0)
			{
				OperatorMap LOperatorMap = new OperatorMap(AOperator.OperatorName);
				LOperatorMap.Library = AOperator.Library;
				LIndex = Add(LOperatorMap);
			}
			else
			{
				if (String.Compare(this[LIndex].Name, AOperator.OperatorName) != 0)
					throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, AOperator.OperatorName, this[LIndex].Name);
			}
				
			this[LIndex].AddSignature(AOperator);
		}
		
		public void RemoveOperator(Operator AOperator)
		{
			AOperator.OperatorSignature = null;
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex >= 0)
			{
				this[LIndex].RemoveSignature(AOperator.Signature);
				if (this[LIndex].SignatureCount == 0)
					RemoveAt(LIndex);
			}
			else
				throw new SchemaException(SchemaException.Codes.OperatorMapNotFound, AOperator.Name);
		}
		
		public bool ContainsOperator(Operator AOperator)
		{
			int LIndex = IndexOfName(AOperator.OperatorName);
			if (LIndex >= 0)
				return this[LIndex].ContainsSignature(AOperator.Signature);
			else
				return false;
		}
		
		public string ShowMaps()
		{
			StringBuilder LString = new StringBuilder();
			foreach (OperatorMap LMap in this)
			{
				LString.Append(LMap.ShowMap());
				LString.Append("\n");
			}
			return LString.ToString();
		}
    }
}
