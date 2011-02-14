/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
    public struct SignatureElement
    {
		public SignatureElement(IDataType ADataType)
		{
			FDataType = ADataType;
			FModifier = Modifier.In;
		}
		
		public SignatureElement(IDataType ADataType, Modifier AModifier)
		{
			FDataType = ADataType;
			FModifier = AModifier;
		}
		
		[Reference]
		private IDataType FDataType;
		public IDataType DataType { get { return FDataType; } }
		
		private Modifier FModifier;
		public Modifier Modifier { get { return FModifier; } }
		
		public override bool Equals(object AObject)
		{
			if (AObject is SignatureElement)
			{
				SignatureElement LObject = (SignatureElement)AObject;
				return
					(
						((FModifier == Modifier.Var) && (LObject.Modifier == Modifier.Var)) ||
						((FModifier != Modifier.Var) && (LObject.Modifier != Modifier.Var))
					) &&
					FDataType.Equals(LObject.DataType);
			}
			return false;
		}
		
		public override int GetHashCode()
		{
			if (FModifier == Modifier.Var)
				return FDataType.GetHashCode() ^ FModifier.GetHashCode();
			else
				return FDataType.GetHashCode();
		}
		
		public bool Is(SignatureElement AElement)
		{
			return
				#if VARBINDINGEXACT
				((FModifier == Modifier.Var) && (AElement.Modifier == Modifier.Var) && FDataType.Equals(AElement.IDataType)) ||
				((FModifier != Modifier.Var) && (AElement.Modifier != Modifier.Var) && FDataType.Is(AElement.IDataType));
				#else
				(
					((FModifier == Modifier.Var) && (AElement.Modifier == Modifier.Var)) ||
					((FModifier != Modifier.Var) && (AElement.Modifier != Modifier.Var))
				) &&
				FDataType.Is(AElement.DataType);
				#endif
		}
		
		public override string ToString()
		{
			switch (FModifier)
			{
				case Modifier.Var: return String.Format("{0} {1}", Keywords.Var, FDataType.Name); 
				//case Modifier.Const: return String.Format("{0} {1}", Keywords.Const, FDataType.Name); 
				default: return FDataType.Name; 
			}
		}
    }

	public class Signature
    {
		public Signature(SignatureElement[] signature)
		{
			_signature = new SignatureElement[signature.Length];
			for (int index = 0; index < Count; index++)
				_signature[index] = signature[index];
		}

		public Signature(Operands operands)
		{
			_signature = new SignatureElement[operands.Count];
			for (int index = 0; index < Count; index++)
				_signature[index] = new SignatureElement(operands[index].DataType, operands[index].Modifier);
		}

		public Signature(Signature signature)
		{
			_signature = new SignatureElement[signature.Count];
			for (int index = 0; index < Count; index++)
				_signature[index] = signature[index];
		}

		private SignatureElement[] _signature;

		public int Count
		{
			get { return _signature.Length; }
		}

		public SignatureElement this[int index]
		{
			get { return _signature[index]; }
		}

		public override bool Equals(object tempValue)
		{
			if (tempValue is Signature)
			{
				Signature signature = (Signature)tempValue;
				if (signature.Count == Count)
				{
					for (int index = 0; index < Count; index++)
						if (!(this[index].Equals(signature[index])))
							return false;
					return true;
				}
			}
			return false;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			for (int index = 0; index < Count; index++)
				hashCode ^= this[index].GetHashCode();
			return hashCode;
		}

		public bool Is(Signature signature)
		{
			if (signature.Count == Count)
			{
				for (int index = 0; index < Count; index++)
					if (!(this[index].Is(signature[index])))
						return false;
				return true;
			}
			return false;
		}
		
		public bool HasNonScalarElements()
		{
			for (int index = 0; index < Count; index++)
				if (!((this[index].DataType is Schema.IScalarType) || (this[index].DataType is Schema.IGenericType)))
					return true;
			return false; 
		}

		public override string ToString()
		{
			StringBuilder stringValue = new StringBuilder();
			stringValue.Append(Keywords.BeginGroup);
			for (int index = 0; index < Count; index++)
			{
				if (index > 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(this[index].ToString());
			}
			stringValue.Append(Keywords.EndGroup);
			return stringValue.ToString();
		}
    }
}

