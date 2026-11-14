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
		public OperatorMap(string name) : base(name)
		{
			_signatures = new OperatorSignatures(null);
		}
		
		protected OperatorSignatures _signatures;
		
		public OperatorSignatures Signatures { get { return _signatures; } }
		
		public int SignatureCount
		{
			get { return _signatures.Count; }
		}
		
		public void AddSignature(Operator operatorValue)
		{
			_signatures.Add(new OperatorSignature(operatorValue));
		}
		
		public void RemoveSignature(Signature signature)
		{
			_signatures.Remove(signature);
		}
		
		public bool ContainsSignature(Signature signature)
		{
			return _signatures.Contains(signature);
		}
		
		public void ResolveSignature(Plan plan, OperatorBindingContext context)
		{
			_signatures.Resolve(plan, context);
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
			StringBuilder stringValue = new StringBuilder(Name);
			stringValue.Append(":\n");
			stringValue.Append(_signatures.ShowSignatures(1));
			return stringValue.ToString();
		}
    }
    
	public class OperatorMaps : Schema.Objects<OperatorMap>
    {		
		#if SINGLENAMESPACE
		public new OperatorMap this[string AName, string ANameSpace]
		{
			get { return (OperatorMap)base[AName, ANameSpace]; }
			set { base[AName, ANameSpace] = value; }
		}
		#endif
		
		public void ResolveCall(Plan plan, OperatorBindingContext context)
		{
			lock (this)
			{
				bool didResolve = false;
				IntegerList indexes = InternalIndexesOf(context.OperatorName);
				OperatorBindingContext localContext = new OperatorBindingContext(context.Statement, context.OperatorName, context.ResolutionPath, context.CallSignature, context.IsExact);
				for (int index = 0; index < indexes.Count; index++)
				{
					localContext.OperatorNameContext.Names.Add(this[indexes[index]].Name);
					this[indexes[index]].ResolveSignature(plan, localContext);
				}
				
				foreach (Schema.LoadedLibraries level in plan.NameResolutionPath)
				{
					OperatorBindingContext levelContext = new OperatorBindingContext(context.Statement, context.OperatorName, context.ResolutionPath, context.CallSignature, context.IsExact);
					foreach (OperatorMatch match in localContext.Matches)
					{
						// If the operator resolution is in any library at this level, add it to a binding context for this level
						if ((match.Signature.Operator.Library == null) || level.ContainsName(match.Signature.Operator.Library.Name))
						{
							if (!levelContext.OperatorNameContext.Names.Contains(match.Signature.Operator.OperatorName))
								levelContext.OperatorNameContext.Names.Add(match.Signature.Operator.OperatorName);
								
							if (!levelContext.Matches.Contains(match))
								levelContext.Matches.Add(match);
						}
					}
					
					if (levelContext.Matches.IsExact)
					{
						levelContext.Operator = levelContext.Matches.Match.Signature.Operator;
						levelContext.OperatorNameContext.Object = this[IndexOfName(levelContext.Operator.OperatorName)];
						levelContext.OperatorNameContext.Names.Add(levelContext.OperatorNameContext.Object.Name);
						context.SetBindingDataFromContext(levelContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at this level
						foreach (string name in levelContext.OperatorNameContext.Names)
							if (!context.OperatorNameContext.Names.Contains(name))
								context.OperatorNameContext.Names.Add(name);
								
						foreach (OperatorMatch match in levelContext.Matches)
							if (!context.Matches.Contains(match))
							{
								context.Matches.Add(match);
								didResolve = true;
							}
					}
				}
				
				// If a partial match is found within the name resolution path, use it
				if (!context.IsExact && context.Matches.IsPartial)
				{
					didResolve = true;
				}
				else
				{
					// The name resolution path has been searched and no match was found, so attempt to resolve based on all signatures
					if (localContext.Matches.IsExact)
					{
						localContext.Operator = localContext.Matches.Match.Signature.Operator;
						localContext.OperatorNameContext.Object = this[IndexOfName(localContext.Operator.OperatorName)];
						localContext.OperatorNameContext.Names.Add(localContext.OperatorNameContext.Object.Name);
						context.SetBindingDataFromContext(localContext);
						return;
					}
					else
					{
						// If there is no match, or a partial match, collect the signatures and map names resolved at all levels
						foreach (string name in localContext.OperatorNameContext.Names)
							if (!context.OperatorNameContext.Names.Contains(name))
								context.OperatorNameContext.Names.Add(name);
								
						foreach (OperatorMatch match in localContext.Matches)
							if (!context.Matches.Contains(match))
							{
								context.Matches.Add(match);
								didResolve = true;
							}
					}
				}
			
				// Ensure that if any resolutions were performed in this catalog, the binding data is set in the context
				if (didResolve)
				{
					if (context.Matches.IsExact || (!context.IsExact && context.Matches.IsPartial))
					{
						if ((context.Operator == null) || (context.Operator != context.Matches.Match.Signature.Operator))
						{
							context.Operator = context.Matches.Match.Signature.Operator;
							context.OperatorNameContext.Object = this[context.Operator.OperatorName];
						}
					}
					else
						context.Operator = null;
				}
			}
		}
		
		public void AddOperator(Operator operatorValue)
		{
			int index = IndexOfName(operatorValue.OperatorName);
			if (index < 0)
			{
				OperatorMap operatorMap = new OperatorMap(operatorValue.OperatorName);
				operatorMap.Library = operatorValue.Library;
				index = Add(operatorMap);
			}
			else
			{
				if (String.Compare(this[index].Name, operatorValue.OperatorName) != 0)
					throw new SchemaException(SchemaException.Codes.AmbiguousObjectName, operatorValue.OperatorName, this[index].Name);
			}
				
			this[index].AddSignature(operatorValue);
		}
		
		public void RemoveOperator(Operator operatorValue)
		{
			operatorValue.OperatorSignature = null;
			int index = IndexOfName(operatorValue.OperatorName);
			if (index >= 0)
			{
				this[index].RemoveSignature(operatorValue.Signature);
				if (this[index].SignatureCount == 0)
					RemoveAt(index);
			}
			else
				throw new SchemaException(SchemaException.Codes.OperatorMapNotFound, operatorValue.Name);
		}
		
		public bool ContainsOperator(Operator operatorValue)
		{
			int index = IndexOfName(operatorValue.OperatorName);
			if (index >= 0)
				return this[index].ContainsSignature(operatorValue.Signature);
			else
				return false;
		}
		
		public string ShowMaps()
		{
			StringBuilder stringValue = new StringBuilder();
			foreach (OperatorMap map in this)
			{
				stringValue.Append(map.ShowMap());
				stringValue.Append("\n");
			}
			return stringValue.ToString();
		}
    }
}
