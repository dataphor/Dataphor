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
		public OperatorSignature(Operator AOperator)
		{
			FOperator = AOperator;
			FSignatures = new OperatorSignatures(this);
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
		
		private OperatorSignatures FSignatures;
		public OperatorSignatures Signatures { get { return FSignatures; } }
		
		[Reference]
		private Operator FOperator;
		public Operator Operator { get { return FOperator; } }
		
		public Signature Signature { get { return FOperator.Signature; } }

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
		
		public OperatorSignature ResolveVirtual(Signature ASignature)
		{
			#if USEVIRTUAL
			foreach (OperatorSignature LSignature in FSignatures)
				if (LSignature.Operator.IsOverride && LSignature.Signature.Is(ASignature))
					return LSignature.ResolveVirtual(ASignature);
			#endif
			return this;
		}
		
		public override bool Equals(object AValue)
		{
			return (AValue is OperatorSignature) && Signature.Equals(((OperatorSignature)AValue).Signature);
		}
		
		public override int GetHashCode()
		{
			return Signature.GetHashCode();
		}
		
		public string ShowSignature(int ADepth)
		{
			StringBuilder LString = new StringBuilder();
			LString.Append(new String('\t', ADepth));
			LString.Append(Signature.ToString());
			LString.Append("\n");
			LString.Append(FSignatures.ShowSignatures(ADepth + 1));
			return LString.ToString();
		}
    }
    
	public class OperatorSignatures : System.Object
    {
		public OperatorSignatures(OperatorSignature ASignature) : base()
		{
			FSignature = ASignature;
		}
		
		private OperatorSignature FSignature;
		private Dictionary<Signature, OperatorSignature> FSignatures = new Dictionary<Signature, OperatorSignature>(); // keys : Signature, values : OperatorSignature
		
		public int Count { get { return FSignatures.Count; } }

		public Dictionary<Signature, OperatorSignature> Signatures { get { return FSignatures; } }

		public OperatorSignature this[Signature ASignature]
		{
			get
			{
				OperatorSignature LSignature;
				if (!FSignatures.TryGetValue(ASignature, out LSignature))
					throw new SchemaException(SchemaException.Codes.SignatureNotFound, ASignature.ToString());
				return LSignature;
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
		public void Add(OperatorSignature ASignature)
		{
			#if USETYPEINHERITANCE
			bool LAdded = false;
			int LIndex;
			for (LIndex = 0; LIndex < Count; LIndex++)
			{		
				if (ASignature.Signature.Equals(this[LIndex].Signature))
					continue;
				else if (ASignature.Signature.Is(this[LIndex].Signature))
				{
					if (!this[LIndex].Signatures.Contains(ASignature))
						this[LIndex].Signatures.Add(ASignature);
					LAdded = true;
				}
				else if (this[LIndex].Signature.Is(ASignature.Signature))
				{
					if (!Contains(ASignature))
					{
						for (int LInnerIndex = Count - 1; LInnerIndex >= 0; LInnerIndex--)
							if (this[LInnerIndex].Signature.Is(ASignature.Signature))
								if (!ASignature.Signatures.Contains(this[LInnerIndex].Signature))
									ASignature.Signatures.Add(InternalRemoveAt(LInnerIndex));
								else
									InternalRemoveAt(LInnerIndex);
						InternalAdd(ASignature);
					}
					LAdded = true;
				}
			}

			if (!LAdded)
				InternalAdd(ASignature);
			
			Adding(AValue, LIndex);
			return LIndex;
			#endif
			FSignatures.Add(ASignature.Signature, ASignature);
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
		
		public void Remove(Signature ASignature)
		{
			FSignatures.Remove(ASignature);
			#if USETYPEINHERITANCE
			int LIndex = IndexOf(ASignature);
			if (LIndex >= 0)
				RemoveAt(LIndex);
			else
			{
				for (LIndex = 0; LIndex < Count; LIndex++)
					if (ASignature.Is(this[LIndex].Signature))
						this[LIndex].Signatures.Remove(ASignature);
			}
			#endif
		}
		
		public void Remove(OperatorSignature ASignature)
		{
			Remove(ASignature.Signature);
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
		
		public void Resolve(Plan APlan, OperatorBindingContext AContext)
		{
			OperatorSignature LResultSignature = null;
			if (FSignatures.TryGetValue(AContext.CallSignature, out LResultSignature))
			{
				if (!AContext.Matches.Contains(LResultSignature))
					AContext.Matches.Add(new OperatorMatch(LResultSignature, true));
			}
			else
			{
				foreach (KeyValuePair<Signature, OperatorSignature> LEntry in FSignatures)
				{
					var LSignature = LEntry.Value;
					if (AContext.CallSignature.Is(LSignature.Signature))
					{
						int LMatchCount = AContext.Matches.Count;
						LSignature.Signatures.Resolve(APlan, AContext);
						if (AContext.Matches.IsExact)
							break;
						else if (LMatchCount == AContext.Matches.Count)
						{
							if (!AContext.Matches.Contains(LSignature))
							{
								OperatorMatch LMatch = new OperatorMatch(LSignature, false);
								for (int LIndex = 0; LIndex < LSignature.Signature.Count; LIndex++)
									LMatch.CanConvert[LIndex] = true;
								AContext.Matches.Add(LMatch);
							}
						}
					}
					else
					{
						if (AContext.CallSignature.Count == LSignature.Signature.Count)
						{
							if (!AContext.Matches.Contains(LSignature))
							{
								OperatorMatch LMatch = new OperatorMatch(LSignature);
								bool LAddMatch = true;
								LMatch.IsMatch = true;
								for (int LElementIndex = 0; LElementIndex < AContext.CallSignature.Count; LElementIndex++)
								{
									LMatch.CanConvert[LElementIndex] = AContext.CallSignature[LElementIndex].DataType.Is(LSignature.Signature[LElementIndex].DataType);
									if (!LMatch.CanConvert[LElementIndex] && (AContext.CallSignature[LElementIndex].Modifier != Modifier.Var) && (LSignature.Signature[LElementIndex].Modifier != Modifier.Var))
									{
										LMatch.ConversionContexts[LElementIndex] = Compiler.FindConversionPath(APlan, AContext.CallSignature[LElementIndex].DataType, LSignature.Signature[LElementIndex].DataType);
										LMatch.CanConvert[LElementIndex] = LMatch.ConversionContexts[LElementIndex].CanConvert;
										
										// As soon as the match being constructed is more narrowing or longer than the best match found so far, it can be safely discarded as a candidate.
										if ((LMatch.NarrowingScore < AContext.Matches.BestNarrowingScore) || ((LMatch.NarrowingScore == AContext.Matches.BestNarrowingScore) && (LMatch.PathLength > AContext.Matches.ShortestPathLength)))
										{
											LAddMatch = false;
											break;
										}
									}

									if (!LMatch.CanConvert[LElementIndex])
										LMatch.IsMatch = false;
								}
								if (LAddMatch)
									AContext.Matches.Add(LMatch);
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
		
		public bool Contains(Signature ASignature)
		{
			#if USETYPEINHERITANCE
			if (FSignatures.Contains(ASignature))
				return true;
			foreach (DictionaryEntry LEntry in FSignatures)
				if (((OperatorSignature)LEntry.Value).Contains(ASignature))
					return true;
			return false;
			#else
			return FSignatures.ContainsKey(ASignature);
			#endif
		}
		
		public bool Contains(OperatorSignature ASignature)
		{
			return Contains(ASignature.Signature);
		}

		public string ShowSignatures(int ADepth)
		{
			StringBuilder LString = new StringBuilder();
			foreach (KeyValuePair<Signature, OperatorSignature> LEntry in FSignatures)
				LString.Append(LEntry.Value.ShowSignature(ADepth));
			return LString.ToString();
		}
    }
}
