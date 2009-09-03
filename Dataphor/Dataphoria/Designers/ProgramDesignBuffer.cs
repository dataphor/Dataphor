using System;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	// TODO: Introduce the notion of ReadOnly into the buffers to prevent save attempts

	public class ProgramDesignBuffer : DesignBuffer
	{
		public ProgramDesignBuffer(IDataphoria ADataphoria, string ALocator) : base(ADataphoria)
		{
			FLocator = ALocator;
		}
		
		private string FLocator;
		public string Locator 
		{ 
			get { return FLocator; } 
		}
		
		public override string GetDescription()
		{
			return FLocator;
		}

		public override bool Equals(object AObject)
		{
			ProgramDesignBuffer LBuffer = AObject as ProgramDesignBuffer;
			if ((LBuffer != null) && (LBuffer.Locator == Locator))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return Locator.GetHashCode();
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
			return ((Scalar)Dataphoria.EvaluateQuery(String.Format(".Debug.GetSource('{0}')", Locator.Replace("'", "''")))).AsString;
		}

		public override void LoadData(System.IO.Stream AData)
		{
			Error.Fail("LoadData(Stream) is not supported for ProgramDesignBuffer");
		}

		public override string GetLocatorName()
		{
			return Locator;
		}

		public override bool LocatorNameMatches(string AName)
		{
			return AName != null && AName == Locator;
		}
		
		public static bool IsProgramLocator(string AName)
		{
			return DebugLocator.IsProgramLocator(AName) || DebugLocator.IsOperatorLocator(AName);
		}
	}
}
