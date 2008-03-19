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
    public class SignatureElement : System.Object
    {
		public SignatureElement(IDataType ADataType) : base()
		{
			FDataType = ADataType;
		}
		
		public SignatureElement(IDataType ADataType, Modifier AModifier) : base()
		{
			FDataType = ADataType;
			FModifier = AModifier;
		}
		
		private IDataType FDataType;
		public IDataType DataType { get { return FDataType; } }
		
		private Modifier FModifier;
		public Modifier Modifier { get { return FModifier; } }
		
		public override bool Equals(object AObject)
		{
			SignatureElement LObject = AObject as SignatureElement;
			return
				(LObject != null) &&
				(
					((FModifier == Modifier.Var) && (LObject.Modifier == Modifier.Var)) ||
					((FModifier != Modifier.Var) && (LObject.Modifier != Modifier.Var))
				) &&
				FDataType.Equals(LObject.DataType);
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

	public class Signature : System.Object
    {
		public Signature(SignatureElement[] ASignature)
		{
			FSignature = new SignatureElement[ASignature.Length];
			for (int LIndex = 0; LIndex < Count; LIndex++)
				FSignature[LIndex] = ASignature[LIndex];
		}

		public Signature(Operands AOperands)
		{
			FSignature = new SignatureElement[AOperands.Count];
			for (int LIndex = 0; LIndex < Count; LIndex++)
				FSignature[LIndex] = new SignatureElement(AOperands[LIndex].DataType, AOperands[LIndex].Modifier);
		}

		public Signature(Signature ASignature)
		{
			FSignature = new SignatureElement[ASignature.Count];
			for (int LIndex = 0; LIndex < Count; LIndex++)
				FSignature[LIndex] = ASignature[LIndex];
		}

		private SignatureElement[] FSignature;

		public int Count
		{
			get { return FSignature.Length; }
		}

		public SignatureElement this[int AIndex]
		{
			get { return FSignature[AIndex]; }
		}

		public override bool Equals(object AValue)
		{
			Signature LSignature = AValue as Signature;
			if ((LSignature != null) && (LSignature.Count == Count))
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (!(this[LIndex].Equals(LSignature[LIndex])))
						return false;
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int LHashCode = 0;
			for (int LIndex = 0; LIndex < Count; LIndex++)
				LHashCode ^= this[LIndex].GetHashCode();
			return LHashCode;
		}

		public bool Is(Signature ASignature)
		{
			if (ASignature.Count == Count)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (!(this[LIndex].Is(ASignature[LIndex])))
						return false;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			StringBuilder LString = new StringBuilder();
			LString.Append(Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < Count; LIndex++)
			{
				if (LIndex > 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(this[LIndex].ToString());
			}
			LString.Append(Keywords.EndGroup);
			return LString.ToString();
		}
    }
}

