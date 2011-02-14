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
		public OperatorMatch(OperatorSignature signature) : base()
		{
			Signature = signature;
			_conversionContexts = new ConversionContext[signature.Signature.Count];
			_canConvert = new BitArray(signature.Signature.Count);
			for (int index = 0; index < _canConvert.Length; index++)
				_canConvert[index] = true;
		}

		/// <summary>Constructs an exact or partial match, depending on the value of AIsExact.</summary>		
		public OperatorMatch(OperatorSignature signature, bool isExact) : base()
		{
			Signature = signature;
			_conversionContexts = new ConversionContext[signature.Signature.Count];
			_canConvert = new BitArray(signature.Signature.Count);
			IsExact = isExact;
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

		private BitArray _canConvert;
		/// <summary>For each parameter in the signature, indicates whether a potential conversion was found between the calling signature argument type and this signatures parameter type.</summary>
		public BitArray CanConvert { get { return _canConvert; } }

		private ConversionContext[] _conversionContexts;
		/// <summary>Contains a potential conversion context for each parameter in the signature.  If the reference is null, if CanConvert is true, then no conversion is required, otherwise, the modifiers were not compatible.</summary>
		public ConversionContext[] ConversionContexts { get { return _conversionContexts; } }
		
		/// <summary>Indicates the total narrowing score for this match.  The narrowing score is the sum of the narrowing scores for all conversions in the match. </summary>
		public int NarrowingScore
		{
			get
			{
				int narrowingScore = 0;
				for (int index = 0; index < _conversionContexts.Length; index++)
				{
					if (_conversionContexts[index] != null)
					{
						if (_conversionContexts[index].CanConvert)
							narrowingScore += _conversionContexts[index].NarrowingScore;
						else
						{
							narrowingScore = Int32.MinValue;
							break;
						}
					}
				}
				return narrowingScore;
			}
		}				  
		
		/// <summary>Indicates the total path length for the conversions in this match.</summary>
		public int PathLength
		{
			get
			{
				int pathLength = 0;
				for (int index = 0; index < _conversionContexts.Length; index++)
				{
					if (_conversionContexts[index] != null)
					{
						if (_conversionContexts[index].CanConvert)
							pathLength += _conversionContexts[index].PathLength;
						else
						{
							pathLength = Int32.MaxValue;
							break;
						}
					}
				}
				return pathLength;
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
		protected override void Validate(OperatorMatch tempValue)
		{
			base.Validate(tempValue);
			if (IndexOf(tempValue) >= 0)
				throw new SchemaException(SchemaException.Codes.DuplicateOperatorMatch, tempValue.Signature.Operator.Name);
		}
	#endif
	
		public int IndexOf(Operator operatorValue)
		{
			for (int index = 0; index < Count; index++)
				if 
				(
					(String.Compare(this[index].Signature.Operator.Name, operatorValue.Name) == 0) ||
					(
						this[index].Signature.Operator.IsATObject &&
						(String.Compare(this[index].Signature.Operator.SourceOperatorName, operatorValue.OperatorName) == 0) &&
						this[index].Signature.Operator.Signature.Equals(operatorValue.Signature)
					) ||
					(
						operatorValue.IsATObject &&
						(String.Compare(this[index].Signature.Operator.OperatorName, operatorValue.SourceOperatorName) == 0) &&
						this[index].Signature.Operator.Signature.Equals(operatorValue.Signature)
					)
				)
					return index;
			return -1;
		}
		
		public bool Contains(Operator operatorValue)
		{
			return IndexOf(operatorValue) >= 0;
		}
		
		public int IndexOf(OperatorSignature signature)
		{
			return IndexOf(signature.Operator);
		}
		
		public bool Contains(OperatorSignature signature)
		{
			return Contains(signature.Operator);
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
		public override int IndexOf(OperatorMatch match)
		{
			return IndexOf(match.Signature.Operator);
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
					
				int bestMatchCount = 0;
				foreach (OperatorMatch match in _bestMatches)
					if (match.PathLength == _shortestPathLength)
						bestMatchCount++;
				return bestMatchCount > 1;
			} 
		}
		
		private OperatorMatch _match;
		/// <summary>Returns the resolved signature for the calling signature.  Null if no match was found.</summary>
		public OperatorMatch Match
		{
			get
			{
				if (!_isMatchComputed)
				{
					FindMatch();
					_isMatchComputed = true;
				}
				return _match;
			}
		}
		
		private bool _isMatchComputed;
		
		private void FindMatch()
		{
			_match = null;
			int exactCount = 0;
			foreach (OperatorMatch match in _bestMatches)
			{
				if (match.IsExact)
				{
					exactCount++;
					if (exactCount == 1)
						_match = match;
					else
					{
						_match = null;
						break;
					}
				}
				else if (match.IsPartial)
				{
					if (_match == null)
					{
						if (match.PathLength == _shortestPathLength)
							_match = match;
					}
					else
					{
						if (_match.PathLength == match.PathLength)
						{
							_match = null;
							break;
						}
					}
				}
			}
		}
		
		private int _bestNarrowingScore = Int32.MinValue;
		/// <summary>Returns the best narrowing score for the possible matches for the calling signature.</summary>
		public int BestNarrowingScore { get { return _bestNarrowingScore; } }
		
		private int _shortestPathLength = Int32.MaxValue;
		/// <summary>Returns the shortest path length among the possible matches with the best narrowing score.</summary>
		public int ShortestPathLength { get { return _shortestPathLength; } }
		
		private OperatorMatchList _bestMatches = new OperatorMatchList();
		/// <summary>Returns the set of possible matches with the best narrowing score.</summary>
		public OperatorMatchList BestMatches { get { return _bestMatches; } }
		
		/// <summary>Returns the closest match for the given signature.</summary>
		public OperatorMatch ClosestMatch
		{
			get
			{
				// The most converting path with the least narrowing score and shortest path length
				int matchCount = 0;
				int conversionCount = 0;
				int bestConversionCount = 0;
				int bestNarrowingScore = Int32.MinValue;
				int shortestPathLength = Int32.MaxValue;
				OperatorMatch closestMatch = null;
				foreach (OperatorMatch match in this)
				{
					conversionCount = 0;
					foreach (bool canConvert in match.CanConvert)
						if (canConvert)
							conversionCount++;

					if ((closestMatch == null) || (conversionCount > bestConversionCount))
					{
						bestConversionCount = conversionCount;
						bestNarrowingScore = match.NarrowingScore;
						shortestPathLength = match.PathLength;
						closestMatch = match;
						matchCount = 1;
					}
					else if (conversionCount == bestConversionCount)
					{
						if (match.NarrowingScore > bestNarrowingScore)
						{
							bestNarrowingScore = match.NarrowingScore;
							shortestPathLength = match.PathLength;
							closestMatch = match;
							matchCount = 1;
						}
						else if (match.NarrowingScore == bestNarrowingScore)
						{
							if (match.PathLength < shortestPathLength)
							{
								shortestPathLength = match.PathLength;
								closestMatch = match;
								matchCount = 1;
							}
							else
								matchCount++;
						}
						else
							matchCount++;
					}
				}
				
				return matchCount == 1 ? closestMatch : null;
			}
		}
		
		private void ComputeBestNarrowingScore()
		{
			_bestNarrowingScore = Int32.MinValue;
			foreach (OperatorMatch match in this)
				if (match.IsMatch && (match.NarrowingScore > _bestNarrowingScore))
					_bestNarrowingScore = match.NarrowingScore;
		}
		
		private void ComputeBestMatches()
		{
			_bestMatches.Clear();
			_shortestPathLength = Int32.MaxValue;
			foreach (OperatorMatch match in this)
				if (match.IsMatch && (match.NarrowingScore == _bestNarrowingScore))
				{
					_bestMatches.Add(match);
					if (match.PathLength < _shortestPathLength)
						_shortestPathLength = match.PathLength;
				}
		}
		
		#if USETYPEDLIST
		protected override void Adding(object AValue, int AIndex)
		{
			OperatorMatch LMatch = (OperatorMatch)AValue;
		#else
		protected override void Adding(OperatorMatch LMatch, int index)
		{
		#endif
			if (LMatch.IsMatch)
			{
				if (LMatch.NarrowingScore > _bestNarrowingScore)
				{
					_bestNarrowingScore = LMatch.NarrowingScore;
					ComputeBestMatches();
				}
				else if (LMatch.NarrowingScore == _bestNarrowingScore)
				{
					_bestMatches.Add(LMatch);
					if (LMatch.PathLength < _shortestPathLength)
						_shortestPathLength = LMatch.PathLength;
				}
			}
			
			_isMatchComputed = false;

			//base.Adding(AValue, AIndex);
		}

		private bool _isClearing;
				
		public override void Clear()
		{
			_isClearing = true;
			try
			{
				base.Clear();
			}
			finally
			{
				_isClearing = false;
			}

			ComputeBestNarrowingScore();
			ComputeBestMatches();
			_isMatchComputed = false;
		}
		
		#if USETYPEDLIST
		protected override void Removing(object AValue, int AIndex)
		#else
		protected override void Removing(OperatorMatch tempValue, int index)
		#endif
		{
			if (!_isClearing)
			{
				ComputeBestNarrowingScore();
				ComputeBestMatches();
				_isMatchComputed = false;
			}
			//base.Removing(AValue, AIndex);
		}
    }
}
