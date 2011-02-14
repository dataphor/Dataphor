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
	using Alphora.Dataphor.DAE.Language;
	
	public class OperatorSignature : System.Object
    {
		public OperatorSignature(Operator operatorValue)
		{
			_operator = operatorValue;
			_signatures = new OperatorSignatures(this);
			#if USEVIRTUAL
			#if USETYPEDLIST
			FParentSignatures = new TypedList(typeof(OperatorSignature), false);
			#else
			FParentSignatures = new BaseList<OperatorSignature>();
			#endif
			#endif
		}

		#if USEVIRTUAL
		#if USETYPEDLIST
		private TypedList FParentSignatures;
		public TypedList ParentSignatures { get { return FParentSignatures; } }
		#else
		private BaseList<OperatorSignature> FParentSiagntures;
		public BaseList<OperatorSignature> ParentSignatures { get { return FParentSignatures; } }
		#endif
		#endif
		
		private OperatorSignatures _signatures;
		public OperatorSignatures Signatures { get { return _signatures; } }
		
		[Reference]
		private Operator _operator;
		public Operator Operator { get { return _operator; } }
		
		public Signature Signature { get { return _operator.Signature; } }

		/*
			Virtual Resolution Algorithm ->
				this algorithm works from the following assumptions:
					1. This operator is virtual (abstract, virtual or override is true)
					2. This operator "is" the signature being resolved
					
				foreach signature in this list
					if the signature is an override and it "is" the signature being resolved
						return the ResolveSignature of the signature
				return this signature
		*/
		
		public OperatorSignature ResolveVirtual(Signature signature)
		{
			#if USEVIRTUAL
			foreach (OperatorSignature localSignature in FSignatures)
				if (localSignature.Operator.IsOverride && localSignature.Signature.Is(ASignature))
					return localSignature.ResolveVirtual(ASignature);
			#endif
			return this;
		}
		
		public override bool Equals(object tempValue)
		{
			return (tempValue is OperatorSignature) && Signature.Equals(((OperatorSignature)tempValue).Signature);
		}
		
		public override int GetHashCode()
		{
			return Signature.GetHashCode();
		}
		
		public string ShowSignature(int depth)
		{
			StringBuilder stringValue = new StringBuilder();
			stringValue.Append(new String('\t', depth));
			stringValue.Append(Signature.ToString());
			stringValue.Append("\n");
			stringValue.Append(_signatures.ShowSignatures(depth + 1));
			return stringValue.ToString();
		}
    }
    
	public class OperatorSignatures : System.Object
    {
		public OperatorSignatures(OperatorSignature signature) : base()
		{
			_signature = signature;
		}
		
		private OperatorSignature _signature;
		private Dictionary<Signature, OperatorSignature> _signatures = new Dictionary<Signature, OperatorSignature>(); // keys : Signature, values : OperatorSignature
		
		public int Count { get { return _signatures.Count; } }

		public Dictionary<Signature, OperatorSignature> Signatures { get { return _signatures; } }

		public OperatorSignature this[Signature signature]
		{
			get
			{
				OperatorSignature localSignature;
				if (!_signatures.TryGetValue(signature, out localSignature))
					throw new SchemaException(SchemaException.Codes.SignatureNotFound, signature.ToString());
				return localSignature;
			}
		}
		
		/*
			Insertion Algorithm ->
				for each signature
					if the signature being added is the signature
						add the signature to this signature
					else if the signature is the signature being added
						for each signature in this list
							if the signature is the signature being added
								remove it from this list
								add it to the signature being added
						add all child signatures of signatures in this list to the signature, if applicable
						add the signature to this list
				if the signature has not yet been added
					add all child signatures of signatures in this list to the signature, if applicable
					add the signature to this list
		*/
		
		//public override int Add(object AValue)
		public void Add(OperatorSignature signature)
		{
			#if USETYPEINHERITANCE
			bool added = false;
			int index;
			for (index = 0; index < Count; index++)
			{		
				if (ASignature.Signature.Equals(this[index].Signature))
					continue;
				else if (ASignature.Signature.Is(this[index].Signature))
				{
					if (!this[index].Signatures.Contains(ASignature))
						this[index].Signatures.Add(ASignature);
					added = true;
				}
				else if (this[index].Signature.Is(ASignature.Signature))
				{
					if (!Contains(ASignature))
					{
						for (int innerIndex = Count - 1; innerIndex >= 0; innerIndex--)
							if (this[innerIndex].Signature.Is(ASignature.Signature))
								if (!ASignature.Signatures.Contains(this[innerIndex].Signature))
									ASignature.Signatures.Add(InternalRemoveAt(innerIndex));
								else
									InternalRemoveAt(innerIndex);
						InternalAdd(ASignature);
					}
					added = true;
				}
			}

			if (!added)
				InternalAdd(ASignature);
			
			Adding(AValue, index);
			return index;
			#endif
			_signatures.Add(signature.Signature, signature);
		}
		
		/*
			Removal Algorithm ->
				if the signature is in this list
					Remove the signature
					for each signature in the signature being removed
						if the signature is not in this list
							add the signature to this list
				else
					for each signature in this list
						if the signature being removed is the signature
							remove the signature being removed from the signatures for this signature
					if the signature was not removed
						throw a SignatureNotFound
		*/
		
		public void Remove(Signature signature)
		{
			_signatures.Remove(signature);
			#if USETYPEINHERITANCE
			int index = IndexOf(ASignature);
			if (index >= 0)
				RemoveAt(index);
			else
			{
				for (index = 0; index < Count; index++)
					if (ASignature.Is(this[index].Signature))
						this[index].Signatures.Remove(ASignature);
			}
			#endif
		}
		
		public void Remove(OperatorSignature signature)
		{
			Remove(signature.Signature);
		}
		
		/*
			Resolution Algorithm ->
				if the signature is in this list
					return the operator signature
				else
					for each signature in this list
						if the given signature is the signature
							return the Resolve on the signature
					return null
		*/
		
		public void Resolve(Plan plan, OperatorBindingContext context)
		{
			OperatorSignature resultSignature = null;
			if (_signatures.TryGetValue(context.CallSignature, out resultSignature))
			{
				if (!context.Matches.Contains(resultSignature))
					context.Matches.Add(new OperatorMatch(resultSignature, true));
			}
			else
			{
				foreach (KeyValuePair<Signature, OperatorSignature> entry in _signatures)
				{
					var signature = entry.Value;
					if (context.CallSignature.Is(signature.Signature))
					{
						int matchCount = context.Matches.Count;
						signature.Signatures.Resolve(plan, context);
						if (context.Matches.IsExact)
							break;
						else if (matchCount == context.Matches.Count)
						{
							if (!context.Matches.Contains(signature))
							{
								OperatorMatch match = new OperatorMatch(signature, false);
								for (int index = 0; index < signature.Signature.Count; index++)
									match.CanConvert[index] = true;
								context.Matches.Add(match);
							}
						}
					}
					else
					{
						if (context.CallSignature.Count == signature.Signature.Count)
						{
							if (!context.Matches.Contains(signature))
							{
								OperatorMatch match = new OperatorMatch(signature);
								bool addMatch = true;
								match.IsMatch = true;
								for (int elementIndex = 0; elementIndex < context.CallSignature.Count; elementIndex++)
								{
									match.CanConvert[elementIndex] = context.CallSignature[elementIndex].DataType.Is(signature.Signature[elementIndex].DataType);
									if (!match.CanConvert[elementIndex] && (context.CallSignature[elementIndex].Modifier != Modifier.Var) && (signature.Signature[elementIndex].Modifier != Modifier.Var))
									{
										match.ConversionContexts[elementIndex] = Compiler.FindConversionPath(plan, context.CallSignature[elementIndex].DataType, signature.Signature[elementIndex].DataType);
										match.CanConvert[elementIndex] = match.ConversionContexts[elementIndex].CanConvert;
										
										// As soon as the match being constructed is more narrowing or longer than the best match found so far, it can be safely discarded as a candidate.
										if ((match.NarrowingScore < context.Matches.BestNarrowingScore) || ((match.NarrowingScore == context.Matches.BestNarrowingScore) && (match.PathLength > context.Matches.ShortestPathLength)))
										{
											addMatch = false;
											break;
										}
									}

									if (!match.CanConvert[elementIndex])
										match.IsMatch = false;
								}
								if (addMatch)
									context.Matches.Add(match);
							}
						}
					}
				}
			}
		}

		#if USEVIRTUAL		
		public OperatorSignature ResolveInherited(Signature ASignature)
		{
			OperatorSignature LSignature = Resolve(ASignature, false);
			if (LSignature == null)
				return null;
			else
			{
				if (LSignature.ParentSignatures.Count == 1)
					return (OperatorSignature)LSignature.ParentSignatures[0];
				else
					throw new SchemaException(SchemaException.Codes.AmbiguousInheritedCall, LSignature.Operator.Name);
			} 
		}
		#endif
		
		public bool Contains(Signature signature)
		{
			#if USETYPEINHERITANCE
			if (FSignatures.Contains(ASignature))
				return true;
			foreach (DictionaryEntry entry in FSignatures)
				if (((OperatorSignature)entry.Value).Contains(ASignature))
					return true;
			return false;
			#else
			return _signatures.ContainsKey(signature);
			#endif
		}
		
		public bool Contains(OperatorSignature signature)
		{
			return Contains(signature.Signature);
		}

		public string ShowSignatures(int depth)
		{
			StringBuilder stringValue = new StringBuilder();
			foreach (KeyValuePair<Signature, OperatorSignature> entry in _signatures)
				stringValue.Append(entry.Value.ShowSignature(depth));
			return stringValue.ToString();
		}
    }
}
