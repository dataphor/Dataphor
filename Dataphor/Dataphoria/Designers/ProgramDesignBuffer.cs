using System;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	// TODO: Introduce the notion of ReadOnly into the buffers to prevent save attempts

	public class ProgramDesignBuffer : DesignBuffer
	{
		public ProgramDesignBuffer(IDataphoria ADataphoria, DebugLocator ALocator) 
			: base(ADataphoria, ALocator)
		{
		}
		
		public override string GetDescription()
		{
			return Locator.Locator;
		}

		public override bool Equals(object AObject)
		{
			ProgramDesignBuffer LBuffer = AObject as ProgramDesignBuffer;
			if ((LBuffer != null) && (LBuffer.Locator.Locator == Locator.Locator))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return Locator.Locator.GetHashCode();
		}

		public override void SaveData(string AData)
		{
			throw new NotImplementedException();
		}

		public override void SaveBinaryData(System.IO.Stream AData)
		{
			throw new NotImplementedException();
		}

		public override string LoadData()
		{
			return ((Scalar)Dataphoria.EvaluateQuery(String.Format(".System.Debug.GetSource('{0}')", Locator.Locator.Replace("'", "''")))).AsString;
		}

		public override void LoadData(System.IO.Stream AData)
		{
			Error.Fail("LoadData(Stream) is not supported for ProgramDesignBuffer");
		}

		public override bool LocatorNameMatches(string AName)
		{
			return AName != null && String.Equals(AName, Locator.Locator);
		}
		
		public static bool IsProgramLocator(string AName)
		{
			return DebugLocator.IsProgramLocator(AName) || DebugLocator.IsOperatorLocator(AName);
		}
	}
}
