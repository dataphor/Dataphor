using System;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	// TODO: Introduce the notion of ReadOnly into the buffers to prevent save attempts

	public class ProgramDesignBuffer : DesignBuffer
	{
		public ProgramDesignBuffer(IDataphoria dataphoria, DebugLocator locator) 
			: base(dataphoria, locator)
		{
		}
		
		public override string GetDescription()
		{
			return Locator.Locator;
		}

		public override bool Equals(object objectValue)
		{
			ProgramDesignBuffer buffer = objectValue as ProgramDesignBuffer;
			if ((buffer != null) && (buffer.Locator.Locator == Locator.Locator))
				return true;
			else
				return base.Equals(objectValue);
		}

		public override int GetHashCode()
		{
			return Locator.Locator.GetHashCode();
		}

		public override void SaveData(string data)
		{
			throw new NotImplementedException();
		}

		public override void SaveBinaryData(System.IO.Stream data)
		{
			throw new NotImplementedException();
		}

		public override string LoadData()
		{
			return ((IScalar)Dataphoria.EvaluateQuery(String.Format(".System.Debug.GetSource('{0}')", Locator.Locator.Replace("'", "''")))).AsString;
		}

		public override void LoadData(System.IO.Stream data)
		{
			Error.Fail("LoadData(Stream) is not supported for ProgramDesignBuffer");
		}

		public override bool LocatorNameMatches(string name)
		{
			return name != null && String.Equals(name, Locator.Locator);
		}
		
		public static bool IsProgramLocator(string name)
		{
			return DebugLocator.IsProgramLocator(name) || DebugLocator.IsOperatorLocator(name);
		}
	}
}
