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
	
	/// <summary>Contains information about potential signature resolution matches.</summary>
    public class OperatorMatch : System.Object
    {
		/// <summary>Constructs a potential match.</summary>
		public OperatorMatch(OperatorSignature ASignature) : base()
		{
			Signature = ASignature;
			FConversionContexts = new ConversionContext[ASignature.Signature.Count];
			FCanConvert = new BitArray(ASignature.Signature.Count);
			for (int LIndex = 0; LIndex < FCanConvert.Length; LIndex++)
				FCanConvert[LIndex] = true;
		}

		/// <summary>Constructs an exact or partial match, depending on the value of AIsExact.</summary>		
		public OperatorMatch(OperatorSignature ASignature, bool AIsExact) : base()
		{
			Signature = ASignature;
			FConversionContexts = new ConversionContext[ASignature.Signature.Count];
			FCanConvert = new BitArray(ASignature.Signature.Count);
			IsExact = AIsExact;
			IsMatch = true;
		}
		
		[Reference]
		public OperatorSignature Signature;

		/// <summary>Indicates whether this signature is an exact match with the call signature. (No casting or conversion required)</summary>
		public bool IsExact;
		
		/// <summary>Indicates whether this signature is a match with the call signature. (Casting or conversion may be required)</summary>
		public bool IsMatch;
		
		/// <summary>Indicates that this signature is a match with the call signature but that casting or conversion is required.</summary>
		public bool IsPartial { get { return IsMatch && !IsExact; } }

		private BitArray FCanConvert;
		/// <summary>For each parameter in the signature, indicates whether a potential conversion was found between the calling signature argument type and this signatures parameter type.</summary>
		public BitArray CanConvert { get { return FCanConvert; } }

		private ConversionContext[] FConversionContexts;
		/// <summary>Contains a potential conversion context for each parameter in the signature.  If the reference is null, if CanConvert is true, then no conversion is required, otherwise, the modifiers were not compatible.</summary>
		public ConversionContext[] ConversionContexts { get { return FConversionContexts; } }
		
		/// <summary>Indicates the total narrowing score for this match.  The narrowing score is the sum of the narrowing scores for all conversions in the match. </summary>
		public int NarrowingScore
		{
			get
			{
				int LNarrowingScore = 0;
				for (int LIndex = 0; LIndex < FConversionContexts.Length; LIndex++)
				{
					if (FConversionContexts[LIndex] != null)
					{
						if (FConversionContexts[LIndex].CanConvert)
							LNarrowingScore += FConversionContexts[LIndex].NarrowingScore;
						else
						{
							LNarrowingScore = Int32.MinValue;
							break;
						}
					}
				}
				return LNarrowingScore;
			}
		}				  
		
		/// <summary>Indicates the total path length for the conversions in this match.</summary>
		public int PathLength
		{
			get
			{
				int LPathLength = 0;
				for (int LIndex = 0; LIndex < FConversionContexts.Length; LIndex++)
				{
					if (FConversionContexts[LIndex] != null)
					{
						if (FConversionContexts[LIndex].CanConvert)
							LPathLength += FConversionContexts[LIndex].PathLength;
						else
						{
							LPathLength = Int32.MaxValue;
							break;
						}
					}
				}
				return LPathLength;
			}
		}
    }

	#if USETYPEDLIST    
    public class OperatorMatchList : TypedList
    {
		public OperatorMatchList() : base(typeof(OperatorMatch)) {}
		
		public new OperatorMatch this[int AIndex]
		{
			get { return (OperatorMatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			OperatorMatch LMatch = (OperatorMatch)AValue;
			if (IndexOf(LMatch) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateOperatorMatch, LMatch.Signature.Operator.Name);
		}
	#else
	public class OperatorMatchList : NonNullList<OperatorMatch>
	{
		protected override void Validate(OperatorMatch AValue)
		{
			base.Validate(AValue);
			if (IndexOf(AValue) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateOperatorMatch, AValue.Signature.Operator.Name);
		}
	#endif
	
		public int IndexOf(Operator AOperator)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if 
				(
					(String.Compare(this[LIndex].Signature.Operator.Name, AOperator.Name) == 0) ||
					(
						this[LIndex].Signature.Operator.IsATObject &&
						(String.Compare(this[LIndex].Signature.Operator.SourceOperatorName, AOperator.OperatorName) == 0) &&
						this[LIndex].Signature.Operator.Signature.Equals(AOperator.Signature)
					) ||
					(
						AOperator.IsATObject &&
						(String.Compare(this[LIndex].Signature.Operator.OperatorName, AOperator.SourceOperatorName) == 0) &&
						this[LIndex].Signature.Operator.Signature.Equals(AOperator.Signature)
					)
				)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Operator AOperator)
		{
			return IndexOf(AOperator) >= 0;
		}
		
		public int IndexOf(OperatorSignature ASignature)
		{
			return IndexOf(ASignature.Operator);
		}
		
		public bool Contains(OperatorSignature ASignature)
		{
			return Contains(ASignature.Operator);
		}
		
		#if USETYPEDLIST
		public int IndexOf(OperatorMatch AMatch)
		{
			return IndexOf(AMatch.Signature.Operator);
		}
		
		public bool Contains(OperatorMatch AMatch)
		{
			return Contains(AMatch.Signature.Operator);
		}
		#else
		public override int IndexOf(OperatorMatch AMatch)
		{
			return IndexOf(AMatch.Signature.Operator);
		}
		#endif
    }
    
    public class OperatorMatches : OperatorMatchList
    {
		public OperatorMatches() : base() {}
		
		/// <summary>Indicates whether or not a successful signature match was found.</summary>
		public bool IsMatch { get { return Match != null; } }
		
		/// <summary>Indicates whether the resolved signature is an exact match for the calling signature. (No casting or conversion required) </summary>
		public bool IsExact { get { return Match == null ? false : Match.IsExact; } }

		/// <summary>Indicates whether the resolved signature is a partial match for the calling signature. (Casting or conversion required) </summary>
		public bool IsPartial { get { return Match == null ? false : Match.IsPartial; } }
		
		/// <summary>Indicates whether more than one signature matches the calling signature.</summary>
		public bool IsAmbiguous 
		{ 
			get 
			{
				if (IsExact)
					return false;
					
				int LBestMatchCount = 0;
				foreach (OperatorMatch LMatch in FBestMatches)
					if (LMatch.PathLength == FShortestPathLength)
						LBestMatchCount++;
				return LBestMatchCount > 1;
			} 
		}
		
		private OperatorMatch FMatch;
		/// <summary>Returns the resolved signature for the calling signature.  Null if no match was found.</summary>
		public OperatorMatch Match
		{
			get
			{
				if (!FIsMatchComputed)
				{
					FindMatch();
					FIsMatchComputed = true;
				}
				return FMatch;
			}
		}
		
		private bool FIsMatchComputed;
		
		private void FindMatch()
		{
			FMatch = null;
			int LExactCount = 0;
			foreach (OperatorMatch LMatch in FBestMatches)
			{
				if (LMatch.IsExact)
				{
					LExactCount++;
					if (LExactCount == 1)
						FMatch = LMatch;
					else
					{
						FMatch = null;
						break;
					}
				}
				else if (LMatch.IsPartial)
				{
					if (FMatch == null)
					{
						if (LMatch.PathLength == FShortestPathLength)
							FMatch = LMatch;
					}
					else
					{
						if (FMatch.PathLength == LMatch.PathLength)
						{
							FMatch = null;
							break;
						}
					}
				}
			}
		}
		
		private int FBestNarrowingScore = Int32.MinValue;
		/// <summary>Returns the best narrowing score for the possible matches for the calling signature.</summary>
		public int BestNarrowingScore { get { return FBestNarrowingScore; } }
		
		private int FShortestPathLength = Int32.MaxValue;
		/// <summary>Returns the shortest path length among the possible matches with the best narrowing score.</summary>
		public int ShortestPathLength { get { return FShortestPathLength; } }
		
		private OperatorMatchList FBestMatches = new OperatorMatchList();
		/// <summary>Returns the set of possible matches with the best narrowing score.</summary>
		public OperatorMatchList BestMatches { get { return FBestMatches; } }
		
		/// <summary>Returns the closest match for the given signature.</summary>
		public OperatorMatch ClosestMatch
		{
			get
			{
				// The most converting path with the least narrowing score and shortest path length
				int LMatchCount = 0;
				int LConversionCount = 0;
				int LBestConversionCount = 0;
				int LBestNarrowingScore = Int32.MinValue;
				int LShortestPathLength = Int32.MaxValue;
				OperatorMatch LClosestMatch = null;
				foreach (OperatorMatch LMatch in this)
				{
					LConversionCount = 0;
					foreach (bool LCanConvert in LMatch.CanConvert)
						if (LCanConvert)
							LConversionCount++;

					if ((LClosestMatch == null) || (LConversionCount > LBestConversionCount))
					{
						LBestConversionCount = LConversionCount;
						LBestNarrowingScore = LMatch.NarrowingScore;
						LShortestPathLength = LMatch.PathLength;
						LClosestMatch = LMatch;
						LMatchCount = 1;
					}
					else if (LConversionCount == LBestConversionCount)
					{
						if (LMatch.NarrowingScore > LBestNarrowingScore)
						{
							LBestNarrowingScore = LMatch.NarrowingScore;
							LShortestPathLength = LMatch.PathLength;
							LClosestMatch = LMatch;
							LMatchCount = 1;
						}
						else if (LMatch.NarrowingScore == LBestNarrowingScore)
						{
							if (LMatch.PathLength < LShortestPathLength)
							{
								LShortestPathLength = LMatch.PathLength;
								LClosestMatch = LMatch;
								LMatchCount = 1;
							}
							else
								LMatchCount++;
						}
						else
							LMatchCount++;
					}
				}
				
				return LMatchCount == 1 ? LClosestMatch : null;
			}
		}
		
		private void ComputeBestNarrowingScore()
		{
			FBestNarrowingScore = Int32.MinValue;
			foreach (OperatorMatch LMatch in this)
				if (LMatch.IsMatch && (LMatch.NarrowingScore > FBestNarrowingScore))
					FBestNarrowingScore = LMatch.NarrowingScore;
		}
		
		private void ComputeBestMatches()
		{
			FBestMatches.Clear();
			FShortestPathLength = Int32.MaxValue;
			foreach (OperatorMatch LMatch in this)
				if (LMatch.IsMatch && (LMatch.NarrowingScore == FBestNarrowingScore))
				{
					FBestMatches.Add(LMatch);
					if (LMatch.PathLength < FShortestPathLength)
						FShortestPathLength = LMatch.PathLength;
				}
		}
		
		#if USETYPEDLIST
		protected override void Adding(object AValue, int AIndex)
		{
			OperatorMatch LMatch = (OperatorMatch)AValue;
		#else
		protected override void Adding(OperatorMatch LMatch, int AIndex)
		{
		#endif
			if (LMatch.IsMatch)
			{
				if (LMatch.NarrowingScore > FBestNarrowingScore)
				{
					FBestNarrowingScore = LMatch.NarrowingScore;
					ComputeBestMatches();
				}
				else if (LMatch.NarrowingScore == FBestNarrowingScore)
				{
					FBestMatches.Add(LMatch);
					if (LMatch.PathLength < FShortestPathLength)
						FShortestPathLength = LMatch.PathLength;
				}
			}
			
			FIsMatchComputed = false;

			//base.Adding(AValue, AIndex);
		}

		private bool FIsClearing;
				
		public override void Clear()
		{
			FIsClearing = true;
			try
			{
				base.Clear();
			}
			finally
			{
				FIsClearing = false;
			}

			ComputeBestNarrowingScore();
			ComputeBestMatches();
			FIsMatchComputed = false;
		}
		
		#if USETYPEDLIST
		protected override void Removing(object AValue, int AIndex)
		#else
		protected override void Removing(OperatorMatch AValue, int AIndex)
		#endif
		{
			if (!FIsClearing)
			{
				ComputeBestNarrowingScore();
				ComputeBestMatches();
				FIsMatchComputed = false;
			}
			//base.Removing(AValue, AIndex);
		}
    }
}
