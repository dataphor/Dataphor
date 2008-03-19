/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.IO;
	using System.Text;
	using System.Reflection;
	using System.Collections;
	using System.ComponentModel;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	
	// VersionNumber scalar type host representation
	[TypeConverter(typeof(VersionNumberConverter))]
	public struct VersionNumber
	{
		public VersionNumber(int AMajor, int AMinor, int ARevision, int ABuild)
		{
			FMajor = -1;
			FMinor = -1;
			FRevision = -1;
			FBuild = -1;
			Major = AMajor;
			Minor = AMinor;
			Revision = ARevision;
			Build = ABuild;
		}
		
		private int FMajor;
		public int Major
		{
			get { return FMajor; }
			set 
			{ 
				if (value < -1)
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumberComponent);
				FMajor = value; 
				if (FMajor == -1)
				{
					FMinor = -1;
					FRevision = -1;
					FBuild = -1;
				}
			}
		}
		
		private int FMinor;
		public int Minor
		{
			get { return FMinor; }
			set
			{
				if (value < -1)
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumberComponent);
				if ((value > -1) && (FMajor == -1))
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumber);
				FMinor = value;
				if (FMinor == -1)
				{
					FRevision = -1;
					FBuild = -1;
				}
			}
		}
		
		private int FRevision;
		public int Revision
		{
			get { return FRevision; }
			set
			{
				if (value < -1)
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumberComponent);
				if ((value > -1) && (FMinor == -1))
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumber);
				FRevision = value;
				if (FRevision == -1)
				{
					FBuild = -1;
				}
			}
		}
		
		private int FBuild;
		public int Build
		{
			get { return FBuild; }
			set
			{
				if (value < -1)
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumberComponent);
				if ((value > -1) && (FRevision == -1))
					throw new RuntimeException(RuntimeException.Codes.InvalidVersionNumber);
				FBuild = value;
			}
		}

		public override bool Equals(object AObject)
		{
			return AObject is VersionNumber && (Compare(this, (VersionNumber)AObject) == 0);
		}
		
		public override int GetHashCode()
		{
			return FMajor ^ FMinor ^ FBuild ^ FRevision;
		}
		
		public override string ToString()
		{
			if (Build == -1)
				if (Revision == -1)
					if (Minor == -1)
						if (Major == -1)
							return "*";
						else
							return String.Format("{0}.*", Major);
					else
						return String.Format("{0}.{1}.*", Major, Minor);
				else
					return String.Format("{0}.{1}.{2}.*", Major, Minor, Revision);
			else
				return String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}
		
		public static VersionNumber Parse(string AValue)
		{
			VersionNumber LVersion = new VersionNumber(-1, -1, -1, -1);
			string[] LNumbers = AValue.Split('.');
			for (int LIndex = 0; LIndex < LNumbers.Length; LIndex++)
			{
				switch (LIndex)
				{
					case 0 : 
						if (LNumbers[LIndex] != "*")
							LVersion.Major = Int32.Parse(LNumbers[LIndex]);
					break;
					
					case 1 :
						if (LNumbers[LIndex] != "*")
							LVersion.Minor = Int32.Parse(LNumbers[LIndex]);
					break;
					
					case 2 : 
						if (LNumbers[LIndex] != "*")
							LVersion.Revision = Int32.Parse(LNumbers[LIndex]);
					break;
					
					case 3 :
						if (LNumbers[LIndex] != "*")
							LVersion.Build = Int32.Parse(LNumbers[LIndex]);
					break;
					
					default : throw new ConveyorException(ConveyorException.Codes.InvalidStringArgument);
				}
			}
			return LVersion;
		}
		
		public static int Compare(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			if (ALeftValue.Major == ARightValue.Major)
				if (ALeftValue.Minor == ARightValue.Minor)
					if (ALeftValue.Revision == ARightValue.Revision)
						if (ALeftValue.Build == ARightValue.Build)
							return 0;
						else if (ALeftValue.Build < ARightValue.Build)
							return -1;
						else
							return 1;
					else if (ALeftValue.Revision < ARightValue.Revision)
						return -1;
					else
						return 1;
				else if (ALeftValue.Minor < ARightValue.Minor)
					return -1;
				else
					return 1;
			else if (ALeftValue.Major < ARightValue.Major)
				return -1;
			else
				return 1;
		}

		public static bool operator ==(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) == 0;
		}

		public static bool operator !=(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) != 0;
		}

		public static bool operator <(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) < 0;
		}

		public static bool operator <=(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) <= 0;
		}

		public static bool operator >(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) > 0;
		}

		public static bool operator >=(VersionNumber ALeftValue, VersionNumber ARightValue)
		{
			return Compare(ALeftValue, ARightValue) >= 0;
		}

		// * is compatible with any version number
		// W.* is compatible with any version number with major W
		// W.X.* is compatible with any version number with major number W and minor number X
		// W.X.Y.* is compatible with any version number with major number W, minor number X and revision number Y
		// W.X.Y.Z is only compatible with version number W.X.Y.Z
		public static bool Compatible(VersionNumber ASource, VersionNumber ATarget)
		{
			if (ASource.Major != -1)
				if (ASource.Minor != -1)
					if (ASource.Revision != -1)
						if (ASource.Build != -1)
							return Compare(ASource, ATarget) == 0;
						else
							return (ASource.Major == ATarget.Major) && (ASource.Minor == ATarget.Minor) && (ASource.Revision == ATarget.Revision);
					else
						return (ASource.Major == ATarget.Major) && (ASource.Minor == ATarget.Minor);
				else
					return (ASource.Major == ATarget.Major);
			else
				return true;
		}
	}
	
	public class VersionNumberConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue)
		{
			if (AValue is string)
				return VersionNumber.Parse((string)AValue);
			else
				return null;
		}
	}
	
    public class VersionNumberConveyor : Conveyor
    {
		public VersionNumberConveyor() : base() {}
		
		public unsafe override int GetSize(object AValue)
		{
			return sizeof(VersionNumber);
		}

		public unsafe override object Read(byte[] ABuffer, int AOffset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				return *((VersionNumber*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int AOffset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
			{
				*((VersionNumber*)LBufferPtr) = (VersionNumber)AValue;
			}
		}
    }
    
	// operator VersionNumber(AMajor : Integer) : VersionNumber;
	// operator VersionNumber(AMajor : Integer, AMinor : Integer) : VersionNumber;
	// operator VersionNumber(AMajor : Integer, AMinor : Integer, ARevision : Integer) : VersionNumber;	
	// operator VersionNumber(AMajor : Integer, AMinor : Integer, ARevision : Integer, ABuild : Integer) : VersionNumber;
	public class VersionNumberSelectorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			for (int LIndex = 0; LIndex < AArguments.Length; LIndex++)
				if ((AArguments[LIndex].Value == null) || AArguments[LIndex].Value.IsNil)
					return new DataVar(FDataType);
			#endif

			return 
				new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						new VersionNumber
						(
							AArguments[0].Value.AsInt32, 
							AArguments.Length > 1 ? AArguments[1].Value.AsInt32 : -1,
							AArguments.Length > 2 ? AArguments[2].Value.AsInt32 : -1,
							AArguments.Length > 3 ? AArguments[3].Value.AsInt32 : -1
						)
					)
				);
		}
	}
	
	// operator VersionNumber.ReadMajor(AValue : VersionNumber) : Integer;
	public class VersionNumberMajorReadAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).Major));
		}
	}
	
	// operator VersionNumber.WriteMajor(AValue : VersionNumber, AMajor : Integer) : VersionNumber;
	public class VersionNumberMajorWriteAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new VersionNumber(AArguments[1].Value.AsInt32, LValue.Minor, LValue.Revision, LValue.Build)));
		}
	}

	// operator VersionNumber.ReadMinor(AValue : VersionNumber) : Integer;
	public class VersionNumberMinorReadAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).Minor));
		}
	}
	
	// operator VersionNumber.WriteMinor(AValue : VersionNumber, AMinor : Integer) : VersionNumber;
	public class VersionNumberMinorWriteAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new VersionNumber(LValue.Major, AArguments[1].Value.AsInt32, LValue.Revision, LValue.Build)));
		}
	}

	// operator VersionNumber.ReadRevision(AValue : VersionNumber) : Integer;
	public class VersionNumberRevisionReadAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).Revision));
		}
	}
	
	// operator VersionNumber.WriteRevision(AValue : VersionNumber, ARevision : Integer) : VersionNumber;
	public class VersionNumberRevisionWriteAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new VersionNumber(LValue.Major, LValue.Minor, AArguments[1].Value.AsInt32, LValue.Build)));
		}
	}

	// operator VersionNumber.ReadBuild(AValue : VersionNumber) : Integer;
	public class VersionNumberBuildReadAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).Build));
		}
	}
	
	// operator VersionNumber.WriteBuild(AValue : VersionNumber, ABuild : Integer) : VersionNumber;
	public class VersionNumberBuildWriteAccessorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new VersionNumber(LValue.Major, LValue.Minor, LValue.Revision, AArguments[1].Value.AsInt32)));
		}
	}
	
    // VersionNumberAsStringSelectorNode
    public class VersionNumberAsStringSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, VersionNumber.Parse(AArguments[0].Value.AsString)));
		}
    }
    
    // VersionNumberAsStringReadAccessorNode
    public class VersionNumberAsStringReadAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).ToString()));
		}
    }
    
    // VersionNumberAsStringWriteAccessorNode
    public class VersionNumberAsStringWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, VersionNumber.Parse(AArguments[1].Value.AsString)));
		}
    }   

	// operator iCompare(ALeftValue : VersionNumber, ARightValue : VersionNumber) : Integer
	public class VersionNumberCompareNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, VersionNumber.Compare((VersionNumber)AArguments[0].Value.AsNative, (VersionNumber)AArguments[1].Value.AsNative)));
		}
	}

	// operator Max(ALeftValue : VersionNumber, ARightValue : VersionNumber) : VersionNumber
	public class VersionNumberMaxNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsNative));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsNative));
			else
			{
				VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
				VersionNumber RValue = (VersionNumber)AArguments[1].Value.AsNative;
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LValue > RValue ? LValue : RValue));
			}
		}
	}

	// operator Min(ALeftValue : VersionNumber, ARightValue : VersionNumber) : VersionNumber
	public class VersionNumberMinNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
					return new DataVar(FDataType, null);
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[1].Value.AsNative));
			else if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsNative));
			else
			{
				VersionNumber LValue = (VersionNumber)AArguments[0].Value.AsNative;
				VersionNumber RValue = (VersionNumber)AArguments[1].Value.AsNative;
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LValue < RValue ? LValue : RValue));
			}
		}
	}

	// create operator iEqual(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative == (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// create operator iNotEqual(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberNotEqualNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative != (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// create operator iLess(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative < (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// create operator iInclusiveLess(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberInclusiveLessNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative <= (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// create operator iGreater(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative > (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// create operator iInclusiveGreater(const ALeftValue : VersionNumber, const ARightValue : VersionNumber) : Boolean
	public class VersionNumberInclusiveGreaterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (VersionNumber)AArguments[0].Value.AsNative >= (VersionNumber)AArguments[1].Value.AsNative));
		}
	}

	// operator Compatible(const ASource : VersionNumber, const ATarget : VersionNumber) : Boolean
	public class VersionNumberCompatibleNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, VersionNumber.Compatible((VersionNumber)AArguments[0].Value.AsNative, (VersionNumber)AArguments[1].Value.AsNative)));
		}
	}
	
	// operator ToString(const AVersionNumber : VersionNumber) : String;
	// operator ToIString(const AVersionNumber : VersionNumber) : IString;
	public class VersionNumberToStringNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((VersionNumber)AArguments[0].Value.AsNative).ToString()));
		}
	}
	
	// operator ToVersionNumber(const AString : String) : VersionNumber;
	// operator ToVersionNumber(const AString : IString) : VersionNumber;
	public class StringToVersionNumberNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType);
			#endif
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.IScalarType)FDataType, VersionNumber.Parse(AArguments[0].Value.AsString)));
		}
	}
}

