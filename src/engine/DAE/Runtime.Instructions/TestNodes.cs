/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.IO;
	using System.Text;
	using System.Reflection;

	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Schema = Alphora.Dataphor.DAE.Schema;

	[Serializable]
	public class TestException : System.Exception
	{
		// Constructors
		public TestException(string AMessage) : base(AMessage) {}
		public TestException(string AMessage, params object[] AParams) : base(string.Format(AMessage, AParams)) {}
		public TestException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base (AInfo, AContext) {}
	}
	
	// operator System.Diagnostics.TestCatalog();
	public class TestCatalogNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Objects LObjects = new Schema.Objects();
			Guid[] LIDs = new Guid[100];
			for (int LIndex = 0; LIndex < 100; LIndex++)
			{
				LIDs[LIndex] = Guid.NewGuid();
				LObjects.Add(new Schema.ScalarType(LIDs[LIndex], String.Format("Object_{0}", LIndex.ToString())));
			}
			
			// Verify lookup by name and id
			for (int LIndex = 0; LIndex < 100; LIndex++)
			{
				string LName = String.Format("Object_{0}", LIndex.ToString());
				Schema.Object LObject = LObjects[LName];
				if (LObject.Name != LName)
					throw new TestException("Name lookup failed for object {0}", LName);
				LObject = LObjects[LIDs[LIndex]];
				if (LObject.Name != LName)
					throw new TestException("ID lookup failed for object {0}", LName);
			}
			
			// Remove every other object between 25 and 75
			for (int LIndex = 25; LIndex < 75; LIndex++)
				if (LIndex % 2 == 0)
					LObjects.RemoveAt(LObjects.IndexOf(String.Format("Object_{0}", LIndex.ToString())));
					
			// Verify lookup by name and id
			for (int LIndex = 0; LIndex < 100; LIndex++)
			{
				if ((LIndex < 25) || (LIndex >= 75) || (LIndex % 2 != 0))
				{
					string LName = String.Format("Object_{0}", LIndex.ToString());
					Schema.Object LObject = LObjects[LName];
					if (LObject.Name != LName)
						throw new TestException("Name lookup failed for object {0}", LName);
					LObject = LObjects[LIDs[LIndex]];
					if (LObject.Name != LName)
						throw new TestException("ID lookup failed for object {0}", LName);
				}
			}

			return null;
		}
	}
	
	// operator System.Diagnostics.TestStreams();
	public class TestStreamsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Test the LocalStream
			Stream LStream = new MemoryStream();
			byte[] LData = new byte[100];
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				LData[LIndex] = 100;
			LStream.Write(LData, 0, LData.Length);
			LStream.Position = 0;
			
			LData = new byte[LData.Length];
			LocalStream LLocalStream = new LocalStream(LStream);
			LLocalStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
			{
				if (LData[LIndex] != 100)
					throw new TestException("LocalStream read failed");
				LData[LIndex] = 50;
			}
					
			LLocalStream.Position = 0;
			LLocalStream.Write(LData, 0, LData.Length);
			
			LData = new byte[LData.Length];
			LLocalStream.Position = 0;
			LLocalStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				if (LData[LIndex] != 50)
					throw new TestException("LocalStream write failed");
					
			LLocalStream.Flush();
			
			LData = new byte[LData.Length];
			LStream.Position = 0;
			LStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				if (LData[LIndex] != 50)
					throw new TestException("LocalStream flush failed");
					
			return null;
		}
	}
	
	// operator TestLocalStreamManager();
	public class TestLocalStreamManagerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			LocalStreamManager LStreamManager = new LocalStreamManager((IStreamManager)AProcess);
			Schema.RowType LRowType = new Schema.RowType();
			LRowType.Columns.Add(new Schema.Column("Name", Schema.IDataType.SystemString));
			using (Row LRow = new Row(LRowType, LStreamManager))
			{
				LRow["Name"] = Scalar.FromString("This is a good long string which should cause overflow");
				if (LRow["Name"].ToString() != "This is a good long string which should cause overflow")
					throw new TestException("LocalStreamManager failed");
			}
			
			using (Row LRow = new Row(LRowType, LStreamManager))
			{
				LRow["Name"] = Scalar.FromString("This is a good long string which should cause overflow");
				StreamID LOverflowStreamID = ((CellValueStream)LRow["Name"].Stream).OverflowStreamID;
				LRow["Name"] = Scalar.FromString("Short");
				if (LRow.HasOverflow(0))
					throw new TestException("Row value is not correctly tracking scalar overflow");
					
				LRow["Name"] = Scalar.FromString("This is a good long string which should cause overflow");
				if (((CellValueStream)LRow["Name"].Stream).OverflowStreamID == LOverflowStreamID)
					throw new TestException("Cell value stream does not request a new stream id for secondary overflow");
			}
			
			return null;
		}
	}
	
	// operator TestConveyors();
	public class TestConveyorsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
//			Schema.RowType LRowType = new Schema.RowType();
//			LRowType.Columns.Add(new Schema.Column("BooleanValue", Schema.IDataType.SystemBoolean));
//			LRowType.Columns.Add(new Schema.Column("ByteValue", Schema.IDataType.SystemByte));
//			LRowType.Columns.Add(new Schema.Column("SByteValue", Schema.IDataType.SystemSByte));
//			LRowType.Columns.Add(new Schema.Column("ShortValue", Schema.IDataType.SystemShort));
//			LRowType.Columns.Add(new Schema.Column("UShortValue", Schema.IDataType.SystemUShort));
//			LRowType.Columns.Add(new Schema.Column("IntegerValue", Schema.IDataType.SystemInteger));
//			LRowType.Columns.Add(new Schema.Column("UIntegerValue", Schema.IDataType.SystemUInteger));
//			LRowType.Columns.Add(new Schema.Column("LongValue", Schema.IDataType.SystemLong));
//			LRowType.Columns.Add(new Schema.Column("ULongValue", Schema.IDataType.SystemULong));
//			LRowType.Columns.Add(new Schema.Column("DecimalValue", Schema.IDataType.SystemDecimal));
//			LRowType.Columns.Add(new Schema.Column("StringValue", Schema.IDataType.SystemString));
//			LRowType.Columns.Add(new Schema.Column("GuidValue", Schema.IDataType.SystemGuid));
//			LRowType.Columns.Add(new Schema.Column("TimeSpanValue", Schema.IDataType.SystemTimeSpan));
//			LRowType.Columns.Add(new Schema.Column("DateTimeValue", Schema.IDataType.SystemDateTime));
//			LRowType.Columns.Add(new Schema.Column("MoneyValue", Schema.IDataType.SystemMoney));
//			LRowType.Columns.Add(new Schema.Column("ImageValue", Schema.IDataType.SystemImage));
//			
//			Row LRow = new Row(LRowType, new MemoryStream(), AProcess);
			
//			LConveyor = new BooleanConveyor(LStream);
//			((BooleanConveyor)LConveyor).Value = false;
//			if (((BooleanConveyor)LConveyor).Value != false)
//				throw new TestException("Boolean conveyor failed");
//				
//			((BooleanConveyor)LConveyor).Value = true;
//			if (((BooleanConveyor)LConveyor).Value != true)
//				throw new TestException("Boolean conveyor failed");
//				
//			LConveyor = new ByteConveyor(LStream);
//			((ByteConveyor)LConveyor).Value = Byte.MinValue;
//			if (((ByteConveyor)LConveyor).Value != Byte.MinValue)
//				throw new TestException("Byte conveyor failed");
//				
//			((ByteConveyor)LConveyor).Value = Byte.MaxValue;
//			if (((ByteConveyor)LConveyor).Value != Byte.MaxValue)
//				throw new TestException("Byte conveyor failed");
//				
//			LConveyor = new SByteConveyor(LStream);
//			((SByteConveyor)LConveyor).Value = SByte.MinValue;
//			if (((SByteConveyor)LConveyor).Value != SByte.MinValue)
//				throw new TestException("SByte conveyor failed");
//				
//			((SByteConveyor)LConveyor).Value = SByte.MaxValue;
//			if (((SByteConveyor)LConveyor).Value != SByte.MaxValue)
//				throw new TestException("SByte conveyor failed");
//			
//			LConveyor = new Int16Conveyor(LStream);
//			((Int16Conveyor)LConveyor).Value = Int16.MinValue;
//			if (((Int16Conveyor)LConveyor).Value != Int16.MinValue)
//				throw new TestException("Int16 conveyor failed");
//				
//			((Int16Conveyor)LConveyor).Value = Int16.MaxValue;
//			if (((Int16Conveyor)LConveyor).Value != Int16.MaxValue)
//				throw new TestException("Int16 conveyor failed");
//			
//			LConveyor = new UInt16Conveyor(LStream);
//			((UInt16Conveyor)LConveyor).Value = UInt16.MinValue;
//			if (((UInt16Conveyor)LConveyor).Value != UInt16.MinValue)
//				throw new TestException("UInt16 conveyor failed");
//				
//			((UInt16Conveyor)LConveyor).Value = UInt16.MaxValue;
//			if (((UInt16Conveyor)LConveyor).Value != UInt16.MaxValue)
//				throw new TestException("UInt16 conveyor failed");
//			
//			LConveyor = new Int32Conveyor(LStream);
//			((Int32Conveyor)LConveyor).Value = Int32.MinValue;
//			if (((Int32Conveyor)LConveyor).Value != Int32.MinValue)
//				throw new TestException("Int32 conveyor failed");
//				
//			((Int32Conveyor)LConveyor).Value = Int32.MaxValue;
//			if (((Int32Conveyor)LConveyor).Value != Int32.MaxValue)
//				throw new TestException("Int32 conveyor failed");
//			
//			LConveyor = new UInt32Conveyor(LStream);
//			((UInt32Conveyor)LConveyor).Value = UInt32.MinValue;
//			if (((UInt32Conveyor)LConveyor).Value != UInt32.MinValue)
//				throw new TestException("UInt32 conveyor failed");
//				
//			((UInt32Conveyor)LConveyor).Value = UInt32.MaxValue;
//			if (((UInt32Conveyor)LConveyor).Value != UInt32.MaxValue)
//				throw new TestException("UInt32 conveyor failed");
//			
//			LConveyor = new Int64Conveyor(LStream);
//			((Int64Conveyor)LConveyor).Value = Int64.MinValue;
//			if (((Int64Conveyor)LConveyor).Value != Int64.MinValue)
//				throw new TestException("Int64 conveyor failed");
//				
//			((Int64Conveyor)LConveyor).Value = Int64.MaxValue;
//			if (((Int64Conveyor)LConveyor).Value != Int64.MaxValue)
//				throw new TestException("Int64 conveyor failed");
//			
//			LConveyor = new UInt64Conveyor(LStream);
//			((UInt64Conveyor)LConveyor).Value = UInt64.MinValue;
//			if (((UInt64Conveyor)LConveyor).Value != UInt64.MinValue)
//				throw new TestException("UInt64 conveyor failed");
//				
//			((UInt64Conveyor)LConveyor).Value = UInt64.MaxValue;
//			if (((UInt64Conveyor)LConveyor).Value != UInt64.MaxValue)
//				throw new TestException("UInt64 conveyor failed");
//			
////			LConveyor = new DoubleConveyor(LStream);
////			((DoubleConveyor)LConveyor).Value = Double.MinValue;
////			if (((DoubleConveyor)LConveyor).Value != Double.MinValue)
////				throw new TestException("Double conveyor failed");
////				
////			((DoubleConveyor)LConveyor).Value = Double.MaxValue;
////			if (((DoubleConveyor)LConveyor).Value != Double.MaxValue)
////				throw new TestException("Double conveyor failed");
////			
//			LConveyor = new DecimalConveyor(LStream);
//			((DecimalConveyor)LConveyor).Value = Decimal.MinValue;
//			if (((DecimalConveyor)LConveyor).Value != Decimal.MinValue)
//				throw new TestException("Decimal conveyor failed");
//				
//			((DecimalConveyor)LConveyor).Value = Decimal.MaxValue;
//			if (((DecimalConveyor)LConveyor).Value != Decimal.MaxValue)
//				throw new TestException("Decimal conveyor failed");
//			
//			LConveyor = new StringConveyor(LStream);
//			((StringConveyor)LConveyor).Value = String.Empty;
//			if (((StringConveyor)LConveyor).Value != String.Empty)
//				throw new TestException("String conveyor failed");
//				
//			((StringConveyor)LConveyor).Value = "This is a good long string";
//			if (((StringConveyor)LConveyor).Value != "This is a good long string")
//				throw new TestException("String conveyor failed");
//			
//			LConveyor = new StreamIDConveyor(LStream);
//			((StreamIDConveyor)LConveyor).Value = StreamID.Null;
//			if (((StreamIDConveyor)LConveyor).Value != StreamID.Null)
//				throw new TestException("StreamID conveyor failed");
//				
//			((StreamIDConveyor)LConveyor).Value = new StreamID(65545);
//			if (((StreamIDConveyor)LConveyor).Value != new StreamID(65545))
//				throw new TestException("StreamID conveyor failed");
//			
//			LConveyor = new GuidConveyor(LStream);
//			((GuidConveyor)LConveyor).Value = Guid.Empty;
//			if (((GuidConveyor)LConveyor).Value != Guid.Empty)
//				throw new TestException("Guid conveyor failed");
//			
//			((GuidConveyor)LConveyor).Value = new Guid("{BA69917B-27ED-41fb-BA46-7DF95C146280}");
//			if (((GuidConveyor)LConveyor).Value != new Guid("{BA69917B-27ED-41fb-BA46-7DF95C146280}"))
//				throw new TestException("Guid conveyor failed");
//				
//			LConveyor = new ObjectConveyor(LStream);
//			Exception LException = new Exception("This is a serializable exception");
//			((ObjectConveyor)LConveyor).Value = LException;
//			if (((Exception)((ObjectConveyor)LConveyor).Value).Message != "This is a serializable exception")
//				throw new TestException("Object conveyor failed");
//				
			// TODO: Test all the IXXXConveyor interfaces
				
			return null;
		}
	}
	
	// operator TestStreamManager()
	public class TestStreamManagerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			IStreamManager LStreamManager = AProcess.Plan.StreamManager;

			// Allocate a new stream
			StreamID LStreamID = LStreamManager.Allocate();

			// Open the stream
			Stream LStream = LStreamManager.Open(LStreamID, LockMode.Exclusive);
			
			Scalar LValue = new Scalar(Schema.IDataType.SystemString, LStream);
			
			// Write some data to the stream
			((StringConveyor)LValue.Conveyor).SetValue(LValue, "This is a good long string");

			// Close the stream
			LStreamManager.Close(LStreamID);
			
			// Reopen the stream
			LStream = LStreamManager.Open(LStreamID, LockMode.Shared);
			
			LValue = new Scalar(Schema.IDataType.SystemString, LStream);
			
			// Read the data and verify that it is the same data
			if (((StringConveyor)LValue.Conveyor).GetValue(LValue) != "This is a good long string")
				throw new TestException("Stream manager write failed");
				
			// Open another stream on the same StreamID
			Stream LSecondStream = LStreamManager.Open(LStreamID, LockMode.Shared);
			
			Scalar LSecondValue = new Scalar(Schema.IDataType.SystemString, LSecondStream);
			
			LStream.Position = 0;
			if (((StringConveyor)LSecondValue.Conveyor).GetValue(LSecondValue) != "This is a good long string")
				throw new TestException("Stream manager read of second stream failed");
				
			if (LStream.Position != 0)
				throw new TestException("Stream manager streams not isolated");
				
			LStreamManager.Close(LStreamID);
			
			// Close the stream
			LStreamManager.Close(LStreamID);
			
			// Deallocate the stream
			LStreamManager.Deallocate(LStreamID);
			
			// Allocate Stream1
			StreamID LStreamID1 = LStreamManager.Allocate();
			Stream LStream1 = LStreamManager.Open(LStreamID1, LockMode.Exclusive);
			Scalar LValue1 = new Scalar(Schema.IDataType.SystemString, LStream1);
			
			// Write some data to Stream1
			((StringConveyor)LValue1.Conveyor).SetValue(LValue1, "This is the value for stream 1");

			// Reference Stream1 as Stream2
			StreamID LStreamID2 = LStreamManager.Reference(LStreamID1);
			Stream LStream2 = LStreamManager.Open(LStreamID2, LockMode.Exclusive);
			Scalar LValue2 = new Scalar(Schema.IDataType.SystemString, LStream2);

			// Reference Stream1 as Stream3
			StreamID LStreamID3 = LStreamManager.Reference(LStreamID1);
			Stream LStream3 = LStreamManager.Open(LStreamID3, LockMode.Exclusive);
			Scalar LValue3 = new Scalar(Schema.IDataType.SystemString, LStream3);

			// Change Stream1
			((StringConveyor)LValue1.Conveyor).SetValue(LValue1, "This is the new value for stream 1");
			
			// Change Stream3
			((StringConveyor)LValue3.Conveyor).SetValue(LValue3, "This is the new value for stream 3");
			
			// Verify the values of all streams are consistent
			if (((StringConveyor)LValue1.Conveyor).GetValue(LValue1) != "This is the new value for stream 1")
				throw new TestException("Stream manager reference tracking failed for stream 1");
				
			if (((StringConveyor)LValue2.Conveyor).GetValue(LValue2) != "This is the value for stream 1")
				throw new TestException("Stream manager reference tracking failed for stream 2");
				
			if (((StringConveyor)LValue3.Conveyor).GetValue(LValue3) != "This is the new value for stream 3")
				throw new TestException("Stream manager reference tracking failed for stream 3");
				
			// Reference Stream3 as Stream4
			StreamID LStreamID4 = LStreamManager.Reference(LStreamID3);
			Stream LStream4 = LStreamManager.Open(LStreamID4, LockMode.Exclusive);
			Scalar LValue4 = new Scalar(Schema.IDataType.SystemString, LStream4);
			
			// Reference Stream4 as Stream5
			StreamID LStreamID5 = LStreamManager.Reference(LStreamID4);
			Stream LStream5 = LStreamManager.Open(LStreamID5, LockMode.Exclusive);
			Scalar LValue5 = new Scalar(Schema.IDataType.SystemString, LStream5);
			
			// Deallocate Stream3
			LStream3.Close();
			LStreamManager.Close(LStreamID3);
			LStreamManager.Deallocate(LStreamID3);

			// Verify the values of Stream4 and Stream5
			if (((StringConveyor)LValue4.Conveyor).GetValue(LValue4) != "This is the new value for stream 3")
				throw new TestException("Stream manager reference tracking failed for stream 4");
				
			if (((StringConveyor)LValue5.Conveyor).GetValue(LValue5) != "This is the new value for stream 3")
				throw new TestException("Stream manager reference tracking failed for stream 5");
				
			// Deallocate Stream1
			LStream1.Close();
			LStreamManager.Close(LStreamID1);
			LStreamManager.Deallocate(LStreamID1);
			
			// Deallocate Stream2
			LStream2.Close();
			LStreamManager.Close(LStreamID2);
			LStreamManager.Deallocate(LStreamID2);
			
			// Deallocate Stream5
			LStream5.Close();
			LStreamManager.Close(LStreamID5);
			LStreamManager.Deallocate(LStreamID5);
			
			// Deallocate Stream4
			LStream4.Close();
			LStreamManager.Close(LStreamID4);
			LStreamManager.Deallocate(LStreamID4);
			
			return null;
		}
	}
	
	// operator TestSemaphore()
	public class TestSemaphoreNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// verify that the semaphore is functioning properly
			Semaphore LSemaphore = new Semaphore();
			LSemaphore.Acquire(0, LockMode.Shared);
			LSemaphore.Acquire(0, LockMode.Shared);
			LSemaphore.Release(0);
			LSemaphore.Release(0);
			LSemaphore.Acquire(0, LockMode.Exclusive);
			if (LSemaphore.AcquireImmediate(0, LockMode.Shared))
				throw new TestException("Semaphore exclusive lock failed");
			LSemaphore.Release(0);
			if (!LSemaphore.AcquireImmediate(0, LockMode.Exclusive))
				throw new TestException("Semaphore locking failed");
			return null;
		}
	}
	
	// operator TestRowValues()
	public class TestRowValuesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Test row values
			Schema.RowType LRowType = new Schema.RowType();
			LRowType.Columns.Add(new Schema.Column("Name", Schema.IDataType.SystemString));
			LRowType.Columns.Add(new Schema.Column("VisitCount", Schema.IDataType.SystemInteger));
			LRowType.Columns.Add(new Schema.Column("Salary", Schema.IDataType.SystemDecimal));
			using (Row LRow = new Row(LRowType, AProcess.Plan.StreamManager))
			{
				if (LRow.HasValue("Name"))
					throw new TestException("HasValue within a row failed");
				if (LRow.HasValue("VisitCount"))
					throw new TestException("HasValue within a row failed");
				if (LRow.HasValue("Salary"))
					throw new TestException("HasValue within a row failed");
					
				LRow["Name"] = Scalar.FromString("01234567890123456789");
				LRow["VisitCount"] = Scalar.FromInt32(10);
				LRow["Salary"] = Scalar.FromDecimal(10.0m);
				
				if (LRow["Name"].ToString() != "01234567890123456789")
					throw new TestException("String conveyor within a row failed");
				if (LRow["VisitCount"].ToInt32() != 10)
					throw new TestException("Int32 conveyor within a row failed");
				if (LRow["Salary"].ToDecimal() != 10.0m)
					throw new TestException("Decimal conveyor within a row failed");

				// Test overflow handling
				LRow["Name"] = Scalar.FromString("012345678901234567890");
				if (LRow["Name"].ToString() != "012345678901234567890")
					throw new TestException("Overflow handling within a row failed");
					
				LRow["Name"] = Scalar.FromString("0123456789");
				if (LRow["Name"].ToString() != "0123456789")
					throw new TestException("Overflow handling within a row failed");
					
				LRow.ClearValue("Name");
				if (LRow.HasValue("Name"))
					throw new TestException("ClearValue within a row failed");

			}
			return null;
		}
	}
	
	// operator TestOverflow()
	public class TestOverflowNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Build a row with an overflowing column
			Schema.RowType LRowType = new Schema.RowType();
			LRowType.Columns.Add(new Schema.Column("Description", Schema.IDataType.SystemString));
			using (Row LRow1 = new Row(LRowType, AProcess.StreamManager))
			{
				using (Row LRow2 = new Row(LRowType, AProcess.StreamManager))
				{
					LRow2[0] = Scalar.FromString("012345678901234567890123456789");
					if (LRow2[0].ToString() != "012345678901234567890123456789")
						throw new TestException("Row overflow failed");
					LRow2.CopyTo(LRow1);
				}

				if (LRow1[0].ToString() != "012345678901234567890123456789")
					throw new TestException("Row overflow reference copy failed");
			}
			return null;
		}
	}

	// operator TestExceptions()
	public class TestExceptionsNode : InstructionNode
	{
		internal class TestExceptionClass
		{
			public TestExceptionClass() {}

			private string FBaseName;
			public string BaseName
			{
				set { FBaseName = value; }
				get { return FBaseName; }
			}

			private Assembly FAssemblyType;
			public Assembly AssemblyType
			{
				set { FAssemblyType = value; }
				get { return FAssemblyType; }
			}

			public string GetMessage(int AErrorCode)
			{
				string LMessage = DataphorException.GetMessage(AErrorCode, FBaseName, FAssemblyType);
				if ((LMessage.Length > 18) && (LMessage.Substring(0, 18) == "DataphorException:"))
					throw new Exception(LMessage);

				return LMessage;
			}
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
#if DEBUG
			string LBuildMode = "debug";
#else
			string LBuildMode = "release";
#endif
			Assembly LBaseAsm = Assembly.LoadFrom(string.Format(@"..\..\..\base\bin\{0}\Alphora.Dataphor.dll", LBuildMode));
			Type LDataphorException = LBaseAsm.GetType("Alphora.Dataphor.DataphorException");
			CheckExceptions(string.Format(@"..\..\..\Base\bin\{0}\Alphora.Dataphor.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\BOP\bin\{0}\Alphora.Dataphor.BOP.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE\bin\{0}\Alphora.Dataphor.DAE.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Client\bin\{0}\Alphora.Dataphor.DAE.Client.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Client.Controls\bin\{0}\Alphora.Dataphor.DAE.Client.Controls.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Client.Provider\bin\{0}\Alphora.Dataphor.DAE.Client.Provider.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Dataphoria\bin\{0}\Dataphoria.exe", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Service\bin\{0}\DAEService.exe", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\DAE.Service.ConfigurationUtility\bin\{0}\DAE.Service.ConfigurationUtility.exe", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\Frontend.Client\bin\{0}\Alphora.Dataphor.Frontend.Client.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\Frontend.Client.Windows\bin\{0}\Alphora.Dataphor.Frontend.Client.Windows.dll", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\Frontend.Client.Windows.Executable\bin\{0}\WindowsClient.exe", LBuildMode), LDataphorException);
			CheckExceptions(string.Format(@"..\..\..\Frontend.Server\bin\{0}\Alphora.Dataphor.Frontend.Server.dll", LBuildMode), LDataphorException);

			return null;
		}

		public void CheckExceptions(string AAssemblyName, Type ADataphorException)
		{
			Assembly LAssembly = Assembly.LoadFrom(AAssemblyName);
			Type[] LTypes = LAssembly.GetTypes();
			foreach(Type LType in LTypes)
			{
				if((LType.IsSubclassOf(ADataphorException)) && (!LType.IsAbstract))
				{
					Type LCodes = null;
					// Get the Codes enum.  If there isn't one in the class, throw an exception.
					Type[] LNestedTypes = LType.GetNestedTypes();
					foreach(Type LNestedType in LNestedTypes)
					{
						if (LNestedType.Name == "Codes")
						{
							LCodes = LNestedType;
							break;
						}
					}

					if (LCodes == null)
						throw new Exception("No Codes enum found in " + LType.Name + ".");

					System.Array LValues = Enum.GetValues(LCodes);

					// Now get the BaseName
					FieldInfo LBaseName = LType.GetField("CErrorMessageBaseName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					if (LBaseName == null)
						throw new Exception("CErrorMessageBaseName is not declared or declared as public in " + LType.Name + ".");

					TestExceptionClass LTestExceptionClass = new TestExceptionClass();
					LTestExceptionClass.AssemblyType = LType.Assembly;
					LTestExceptionClass.BaseName = (string)LBaseName.GetValue(null);

					foreach(int LValue in LValues)
					{
						LTestExceptionClass.GetMessage(LValue);
					}
				}
			}
		}
	}

	// operator TestIndex();
	public class TestIndexNode : InstructionNode
	{
		private Row FIndexKey;
		private Row FCompareKey;
		private Row FData;
		private Row FTargetData;
		private Schema.Order FKey;
		private DataVar[] FDataVars;

		protected void IndexCompare(Index AIndex, ServerProcess AProcess, Stream AIndexKey, object AIndexContext, Stream ACompareKey, object ACompareContext, out int AResult)
		{
			// If AIndexContext is null, the index stream will have the structure of an index key,
			// Otherwise, the IndexKey stream could be a subset of the actual index key.
			// In that case, AIndexContext will be the RowType for the IndexKey stream
			// It is the caller's responsibility to ensure that the passed IndexKey RowType 
			// is a subset of the actual IndexKey with order intact.
			Row LIndexKey;
			if (AIndexContext == null)
			{
				LIndexKey = FIndexKey;
				LIndexKey.Stream = AIndexKey;
			}
			else
				LIndexKey = new Row((Schema.RowType)AIndexContext, AIndexKey, AProcess.Plan.StreamManager);
			try
			{
				// If ACompareContext is null, the compare stream will have the structure of an index key,
				// Otherwise the CompareKey could be a subset of the actual index key.
				// In that case, ACompareContext will be the RowType for the CompareKey stream
				// It is the caller's responsibility to ensure that the passed CompareKey RowType 
				// is a subset of the IndexKey with order intact.
				Row LCompareKey;
				if (ACompareContext == null)
				{
					LCompareKey = FCompareKey;
					LCompareKey.Stream = ACompareKey;
				}
				else
					LCompareKey = new Row((Schema.RowType)ACompareContext, ACompareKey, AProcess.Plan.StreamManager);
				try
				{
					AResult = 0;
					for (int LIndex = 0; LIndex < LIndexKey.DataType.Columns.Count; LIndex++)
					{
						if (LIndex >= LCompareKey.DataType.Columns.Count)
							break;

						if (LIndexKey.HasValue(LIndex) && LCompareKey.HasValue(LIndex))
						{
							FDataVars[0].Value = LIndexKey[LIndex];
							FDataVars[1].Value = LCompareKey[LIndex];
							DataVar LResultVar = FKey.Columns[LIndex].Sort.CompareNode.InternalExecute(AProcess, FDataVars);
							AResult = ((Scalar)LResultVar.Value).ToInt32();
						}
						else if (LIndexKey.HasValue(LIndex))
						{
							// Index Key Has A Value
							AResult = 1;
						}
						else if (LCompareKey.HasValue(LIndex))
						{
							// Compare Key Has A Value
							AResult = -1;
						}
						else
						{
							// Neither key has a value
							AResult = 0;
						}
						
						if (AResult != 0)
							break;
					}
				}
				finally
				{
					if (!ReferenceEquals(FCompareKey, LCompareKey))
						LCompareKey.Dispose();
				}
			}
			finally
			{
				if (!ReferenceEquals(FIndexKey, LIndexKey))
					LIndexKey.Dispose();
			}
		}
		
		private void IndexCopyKey(Index AIndex, Stream ASourceKey, Stream ATargetKey)
		{
			FIndexKey.Stream = ASourceKey;
			FCompareKey.Stream = ATargetKey;
			FIndexKey.CopyTo(FCompareKey);
		}
		
		private void IndexCopyData(Index AIndex, Stream ASourceData, Stream ATargetData)
		{
			FData.Stream = ASourceData;
			FTargetData.Stream = ATargetData;
			FData.CopyTo(FTargetData);
		}
		
		private void IndexDisposeKey(Index AIndex, Stream AKey)
		{
			FIndexKey.Stream = AKey;
			FIndexKey.ClearValues();
		}
		
		private void IndexDisposeData(Index AIndex, Stream AData)
		{
			FData.Stream = AData;
			FData.ClearValues();
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Gather variables for the diagnostic...
			int LFanout = 5;
			int LCapacity = 5;
			int LRowCount = 100;
			bool LTestOverflow = false;
			if (AArguments.Length == 4)
			{
				LFanout = ((Scalar)AArguments[0].Value).ToInt32();
				LCapacity = ((Scalar)AArguments[1].Value).ToInt32();
				LRowCount = ((Scalar)AArguments[2].Value).ToInt32();
				LTestOverflow = ((Scalar)AArguments[3].Value).ToBoolean();
			}

			Schema.RowType LKeyType = new Schema.RowType();
			Schema.RowType LDataType = new Schema.RowType();
			LKeyType.Columns.Add(new Schema.Column("ID", Schema.IDataType.SystemInteger));
			FKey = new Schema.Order();
			FKey.Columns.Add(new Schema.OrderColumn(LKeyType.Columns[0], true));
			FKey.Columns[0].Sort = FKey.Columns[0].Column.DataType.GetSort(AProcess.Plan);
			FDataVars = new DataVar[2];
			FDataVars[0] = new DataVar(FKey.Columns[0].Column.DataType);
			FDataVars[1] = new DataVar(FKey.Columns[0].Column.DataType);
			LDataType.Columns.Add(new Schema.Column("Name", Schema.IDataType.SystemString));
			if (!LTestOverflow)
				LDataType.Columns[0].StaticByteSize = 24;
			using (FIndexKey = new Row(LKeyType, AProcess.Plan.StreamManager))
			{
				using (FCompareKey = new Row(LKeyType, AProcess.Plan.StreamManager))
				{
					using (FData = new Row(LDataType, AProcess.Plan.StreamManager))
					{
						using (FTargetData = new Row(LDataType, AProcess.Plan.StreamManager))
						{
							Index LIndex = 
								new TableBufferIndex
								(
									AProcess,
									FKey,
									LKeyType,
									LDataType,
									true,
									LFanout, 
									LCapacity
								);
							
							// Insert LRowCount rows into the data
							Random LRandom = new Random();
							Row LKey = null;
							Row LData = null;
							int[] LIDArray = new int[LRowCount];
							for (int LCounter = 0; LCounter < LRowCount; LCounter++)
							{
								//LIDArray[LCounter] = LCounter;
								LIDArray[LCounter] = LRandom.Next();
								using (LKey = new Row(LKeyType, AProcess.Plan.StreamManager))
								{
									LKey.ValuesOwned = false;
									LKey[0] = Scalar.FromInt32(LIDArray[LCounter]);
									using (LData = new Row(LDataType, AProcess.Plan.StreamManager))
									{
										LData.ValuesOwned = false;
										LData[0] = Scalar.FromString(LIDArray[LCounter].ToString());
									
										LIndex.Insert(AProcess, LKey.Stream, LData.Stream);
									}
								}
							}
							
							// Find Each row using the Select method
							using (LKey = new Row(LKeyType, AProcess.Plan.StreamManager))
							{
								for (int LCounter = 0; LCounter < LRowCount; LCounter++)
								{
									LKey[0] = Scalar.FromInt32(LIDArray[LCounter]);
									SearchPath LSearchPath = new SearchPath();
									try
									{
										int LEntryNumber;
										bool LResult = LIndex.FindKey(AProcess, LKey.Stream, null, LSearchPath, out LEntryNumber);
										if (!LResult)
											throw new TestException("Index.FindKey failed");

										LData.Stream = LSearchPath.DataNode.Data(LEntryNumber);
										if (Convert.ToInt32(LData[0].ToString()) != LIDArray[LCounter])
											throw new TestException("Index.FindKey failed");
									}
									finally
									{
										LSearchPath.Dispose();
									}
								}
							}
							
							// Scan through the index using the leaf list and verify increasing order of rows
							long LLastKey = -1;
							long LCurrentKey;
							StreamID LNextStreamID = LIndex.HeadID;
							IndexNode LIndexNode;
							while (LNextStreamID != StreamID.Null)
							{
								using (LIndexNode = new IndexNode(AProcess, LIndex, LNextStreamID))
								{
									for (int LEntryIndex = 0; LEntryIndex < LIndexNode.EntryCount; LEntryIndex++)
									{
										LKey.Stream = LIndexNode.Key(LEntryIndex);
										LCurrentKey = LKey[0].ToInt32();
										if (LCurrentKey <= LLastKey)
											throw new TestException("Index ordering failed");
											
										LLastKey = LCurrentKey;
									}
									LNextStreamID = LIndexNode.NextNode;
								}
							}
							
							// Scan backwards through the index using the leaf list and verify decreasing order of rows
							LLastKey = (long)Int32.MaxValue + 1;
							LNextStreamID = LIndex.TailID;
							while (LNextStreamID != StreamID.Null)
							{
								using (LIndexNode = new IndexNode(AProcess, LIndex, LNextStreamID))
								{
									for (int LEntryIndex = LIndexNode.EntryCount - 1; LEntryIndex >= 0; LEntryIndex--)
									{
										LKey.Stream = LIndexNode.Key(LEntryIndex);
										LCurrentKey = LKey[0].ToInt32();
										if (LCurrentKey >= LLastKey)
											throw new TestException("Index ordering failed");
											
										LLastKey = LCurrentKey;
									}
									LNextStreamID = LIndexNode.PriorNode;
								}
							}
							
							// Delete all the rows in the index
							using (LKey = new Row(LKeyType, AProcess.Plan.StreamManager))
							{
								for (int LCounter = 0; LCounter < LRowCount; LCounter++)
								{
									LKey[0] = Scalar.FromInt32(LIDArray[LCounter]);
									LIndex.Delete(AProcess, LKey.Stream);
								}
							}

							// Drop the index
							LIndex.Drop(AProcess);
							return null;
						}
					}
				}
			}
		}
	}
	
	public class TestUtility
	{
		private static Random FRandom = new Random();
		
		public static string RandomName()
		{
			StringBuilder LName = new StringBuilder();
			LName.Append(Convert.ToChar(FRandom.Next(26) + 65));
			for (int LIndex = 0; LIndex < FRandom.Next(7, 15); LIndex++)
				LName.Append(Convert.ToChar(FRandom.Next(26) + 97));
			LName.Append(", ");
			LName.Append(Convert.ToChar(FRandom.Next(26) + 65));
			for (int LIndex = 0; LIndex < FRandom.Next(5, 10); LIndex++)
				LName.Append(Convert.ToChar(FRandom.Next(26) + 97));
			return LName.ToString();
		}
		
		public static string RandomPhone()
		{
			StringBuilder LPhone = new StringBuilder();
			LPhone.Append("(");
			for (int LIndex = 0; LIndex < 3; LIndex++)
				LPhone.Append(Convert.ToString(FRandom.Next(10)));
			LPhone.Append(")");
			for (int LIndex = 0; LIndex < 3; LIndex++)
				LPhone.Append(Convert.ToString(FRandom.Next(10)));
			LPhone.Append("-");
			for (int LIndex = 0; LIndex < 4; LIndex++)
				LPhone.Append(Convert.ToString(FRandom.Next(10)));
			return LPhone.ToString();
		}
	}
	
	public class TestTableBufferNode : InstructionNode
	{
		private Random FRandom;
		
		private string RandomName()
		{
			return TestUtility.RandomName();
		}
		
		private string RandomPhone()
		{
			return TestUtility.RandomPhone();
		}
		
		private void TestScan
		(
			ServerProcess AProcess,
			TableBuffer ATableBuffer, 
			TableBufferIndex AAccessPath, 
			ScanDirection ADirection, 
			Row AFirstKey, 
			Row ALastKey, 
			int AFirstKeyValue, 
			int ALastKeyValue, 
			int AColumnIndex
		)
		{
			// Scan forward on the clustered index with both a minimum and maximum key
			bool LForward = ADirection == ScanDirection.Forward;
			StringBuilder LScanDescription = new StringBuilder();
			LScanDescription.Append(LForward ? "Forward" : "Backward");
			if ((ALastKey == null) && (AFirstKey == null))
				LScanDescription.Append(" unranged ");
			else if (ALastKey == null)
				LScanDescription.Append(" lower ranged ");
			else if (AFirstKey == null)
				LScanDescription.Append(" upper ranged ");
			else
				LScanDescription.Append(" ranged ");
			LScanDescription.Append(AAccessPath.IsClustered ? "clustered" : "non clustered");
			LScanDescription.Append(" index scan failed: {0}");
			
			Scan LScan = new Scan(AProcess, ATableBuffer, AAccessPath, ADirection, AFirstKey, ALastKey);
			try
			{
				LScan.Open();
				int LCounter = AFirstKeyValue;
				while (true)
				{
					LScan.Next();
					if (LScan.EOF()) break;

					if (((LForward && (LCounter > ALastKeyValue)) || (!LForward && (LCounter < ALastKeyValue))) || (LScan.GetRow()[AColumnIndex].ToInt32() != LCounter))
						throw new TestException(LScanDescription.ToString(), "Next()");
						
					LCounter += LForward ? 1 : -1;
				}

				LScan.First();
				LScan.Next();
				if (LScan.GetRow()[AColumnIndex].ToInt32() != AFirstKeyValue)
					throw new TestException(LScanDescription.ToString(), "First()");
					
				LScan.Last();
				LScan.Prior();
				if (LScan.GetRow()[AColumnIndex].ToInt32() != ALastKeyValue)
					throw new TestException(LScanDescription.ToString(), "Last()");
					
				LScan.Last();
				LCounter = ALastKeyValue;
				while (true)
				{
					LScan.Prior();
					if (LScan.BOF()) break;
					
					if (((LForward && (LCounter < AFirstKeyValue)) || (!LForward && (LCounter > AFirstKeyValue))) || (LScan.GetRow()[AColumnIndex].ToInt32() != LCounter))
						throw new TestException(LScanDescription.ToString(), "Prior()");
						
					LCounter -= LForward ? 1 : -1;
				}
			}
			finally
			{
				LScan.Dispose();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			int LFanout = 5;
			int LCapacity = 5;
			int LRowCount = 100;
			bool LTestOverflow = false;
			if (AArguments.Length == 4)
			{
				LFanout = ((Scalar)AArguments[0].Value).ToInt32();
				LCapacity = ((Scalar)AArguments[1].Value).ToInt32();
				LRowCount = ((Scalar)AArguments[2].Value).ToInt32();
					LTestOverflow = ((Scalar)AArguments[3].Value).ToBoolean();
			}
			
			int LFirstKeyValue = (LRowCount / 4);
			int LLastKeyValue = 3 * LFirstKeyValue;

			// Test a Table Buffer
			Schema.TableType LTableType = new Schema.TableType();
			LTableType.Columns.Add(new Schema.Column("ID", Schema.IDataType.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("ID2", Schema.IDataType.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("Name", Schema.IDataType.SystemString));
			LTableType.Columns.Add(new Schema.Column("Phone", Schema.IDataType.SystemString));
			LTableType.Keys.Add(new Schema.Key(new Schema.Column[]{LTableType.Columns[0]}));

			if (!LTestOverflow)
			{
				LTableType.Columns[2].StaticByteSize = 44;
				LTableType.Columns[3].StaticByteSize = 44;
			}

			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[1], true));
			LTableType.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[2], true));
			LTableType.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[3], true));
			LTableType.Orders.Add(LOrder);

			Schema.BaseTableVar LTableVar = new Schema.BaseTableVar("Testing", LTableType);
			TableBuffer LTableBuffer = new TableBuffer(AProcess, LTableVar, LFanout, LCapacity);
			Schema.RowType LRowType = (Schema.RowType)LTableType.CreateRowType();
			FRandom = new Random();
			for (int LIndex = 0; LIndex < LRowCount; LIndex++)
			{
				using (Row LRow = new Row(LRowType, AProcess.Plan.StreamManager))
				{
					LRow[0] = Scalar.FromInt32(LIndex);
					LRow[1] = Scalar.FromInt32(LIndex);
					LRow[2] = Scalar.FromString(RandomName());
					LRow[3] = Scalar.FromString(RandomPhone());
					LTableBuffer.Insert(AProcess, LRow);
				}
			}
			
			using (Row LFirstKey = new Row(LTableBuffer.ClusteredIndex.KeyRowType, AProcess.Plan.StreamManager))
			{
				LFirstKey[0] = Scalar.FromInt32(LFirstKeyValue);

				using (Row LLastKey = new Row(LTableBuffer.ClusteredIndex.KeyRowType, AProcess.Plan.StreamManager))
				{
					LLastKey[0] = Scalar.FromInt32(LLastKeyValue);

					// Scan forward on the clustered index with no range
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Forward, null, null, 0, LRowCount - 1, 0);

					// Scan forward on the clustered index with a minimum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Forward, LFirstKey, null, LFirstKeyValue, LRowCount - 1, 0);

					// Scan forward on the clustered index with a maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Forward, null, LLastKey, 0, LLastKeyValue, 0);

					// Scan forward on the clustered index with both a minimum and maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Forward, LFirstKey, LLastKey, LFirstKeyValue, LLastKeyValue, 0);

					// Scan backward on the clustered index with no range
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Backward, null, null, LRowCount - 1, 0, 0);

					// Scan backward on the clustered index with a minimum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Backward, LLastKey, null, LLastKeyValue, 0, 0);
					
					// Scan backward on the clustered index with a maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Backward, null, LFirstKey, LRowCount - 1, LFirstKeyValue, 0);
					
					// Scan backward on the clustered index with both a minimum and maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.ClusteredIndex, ScanDirection.Backward, LLastKey, LFirstKey, LLastKeyValue, LFirstKeyValue, 0);

					// Scan forward on a non clustered index with no range
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Forward, null, null, 0, LRowCount - 1, 1);
					
					// Scan forward on a non clustered index with a minimum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Forward, LFirstKey, null, LFirstKeyValue, LRowCount - 1, 1);
					
					// Scan forward on a non clustered index with a maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Forward, null, LLastKey, 0, LLastKeyValue, 1);
					
					// Scan forward on a non clustered index with both a minimum and maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Forward, LFirstKey, LLastKey, LFirstKeyValue, LLastKeyValue, 1);
					
					// Scan backward on a non clustered index with no range
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Backward, null, null, LRowCount - 1, 0, 1);
					
					// Scan backward on a non clustered index with a minimum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Backward, LLastKey, null, LLastKeyValue, 0, 1);
					
					// Scan backward on a non clustered index with a maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Backward, null, LFirstKey, LRowCount - 1, LFirstKeyValue, 1);
					
					// Scan backward on a non clustered index with both a minimum and maximum key
					TestScan(AProcess, LTableBuffer, LTableBuffer.Indexes[1], ScanDirection.Backward, LLastKey, LFirstKey, LLastKeyValue, LFirstKeyValue, 1);

					using (Row LKeyRow = new Row(LTableBuffer.ClusteredIndex.KeyRowType, AProcess.Plan.StreamManager))
					{
						for (int LIndex = 0; LIndex < LRowCount; LIndex++)
						{
							LKeyRow[0] = Scalar.FromInt32(LIndex);
							LTableBuffer.Delete(AProcess, LKeyRow);
						}
					}
				}
			}
			return null;
		}
	}
	
	// operator TestBrowse()
	public class TestBrowseNode : InstructionNode
	{
		protected void PopulateTestData(ServerProcess AProcess)
		{
			// Test a Table Buffer
			Schema.TableType LTableType = new Schema.TableType();
			LTableType.Columns.Add(new Schema.Column("ID", Schema.IDataType.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("ID2", Schema.IDataType.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("Name", Schema.IDataType.SystemString));
			LTableType.Columns.Add(new Schema.Column("Phone", Schema.IDataType.SystemString));
			LTableType.Keys.Add(new Schema.Key(new Schema.Column[]{LTableType.Columns[0]}));
			LTableType.Columns[2].StaticByteSize = 44;
			LTableType.Columns[3].StaticByteSize = 44;

			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[1], true));
			LTableType.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[2], true));
			LTableType.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableType.Columns[3], true));
			LTableType.Orders.Add(LOrder);

			Schema.BaseTableVar LTableVar = new Schema.BaseTableVar("Testing", LTableType);
			LTableVar.Device = (Schema.Device)AProcess.Plan.Catalog.Objects["System.HeapDevice"];
			AProcess.Plan.Catalog.Add(LTableVar);
			AProcess.DeviceExecute(LTableVar.Device, new CreateTableNode(LTableVar));
			
			Random LRandom = new Random();
			
			//int LRowCount = 100;
			int LRowCount = 10;
			DataVar LResultVar = AProcess.DeviceExecute(LTableVar.Device, Compiler.EmitBaseTableVarNode(AProcess.Plan, LTableVar));
			try
			{
				Schema.RowType LRowType = (Schema.RowType)LTableType.CreateRowType();
				LRandom = new Random();
				for (int LIndex = 0; LIndex < LRowCount; LIndex++)
				{
					using (Row LRow = new Row(LRowType, AProcess.Plan.StreamManager))
					{
						//LRow[0] = Scalar.FromInt32(LRandom.Next());
						LRow[0] = Scalar.FromInt32(LIndex);
						LRow[1] = Scalar.FromInt32(LIndex);
						LRow[2] = Scalar.FromString(TestUtility.RandomName());
						LRow[3] = Scalar.FromString(TestUtility.RandomPhone());
						((MemoryScan)LResultVar.Value).TableBuffer.Insert(AProcess, LRow);
					}
				}
			}
			finally
			{
				((Table)LResultVar.Value).Dispose();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Create and populate a test data set
			PopulateTestData(AProcess);
			
			Random LRandom = new Random();
			
			Guid[] LBookmarks = new Guid[10];
			
			IServerExpressionPlan LPlan = ((IServerProcess)AProcess).PrepareExpression("select Testing browse by {ID} isolation chaos", null);
			try
			{
				IServerCursor LCursor = LPlan.Open(null);
				try
				{
					int LCounter = 0;
					while (LCursor.Next())
					{
						Row LRow = LCursor.Select();
						LBookmarks[LCounter] = LCursor.GetBookmark();
						if (LRow[0].ToInt32() != LCounter)
							throw new RuntimeException(RuntimeException.Codes.SetNotOrdered);
						
						LCounter++;
					}
					
					LCursor.GotoBookmark(LBookmarks[0]);
					if (LCursor.Prior())
						throw new RuntimeException(RuntimeException.Codes.SetShouldBeBOF);
					
//					while (LCursor.Prior())
//					{
//						Row LRow = LCursor.Select();
//						if (LRow[0].ToInt32() > LLastValue)
//							throw new RuntimeException("Set is not ordered");
//						LLastValue = LRow[0].ToInt32();
//					}
//					
//					for (int LIndex = 0; LIndex < LRandomValues.Length; LIndex++)
//					{
//						if (!LCursor.GotoBookmark(LRandomValues[LIndex]))
//							throw new RuntimeException("GotoBookmark failed");
//					}
//					
//					LCounter = 0;
//					Buid LBookmark = LRandomValues[LRandomValues.Length / 2];
//					if (!LCursor.GotoBookmark(LBookmark))
//						throw new RuntimeException("GotoBookmark failed");
//						
//					while (LCursor.Next())
//					{
//						LCounter++;
//						if (LCounter > LRandomValues.Length / 2)
//							break;
//					}
//					
//					if (LCounter < LRandomValues.Length / 2)
//						throw new RuntimeException("EOF reached before known rows");
//					
//					if (!LCursor.GotoBookmark(LBookmark))
//						throw new RuntimeException("GotoBookmark failed");
//						
//					LCounter = 0;
//					while (LCursor.Prior())
//					{
//						LCounter++;
//						if (LCounter > LRandomValues.Length / 2)
//							break;
//					}
//					
//					if (LCounter < LRandomValues.Length / 2)
//						throw new RuntimeException("BOF reached before known rows");
//						
//					Row LNewRow = new Row(((Schema.TableTypeBase)LCursor.Plan.DataType).CreateRowType(), LCursor.Plan.Session);
//					LNewRow[0] = Scalar.FromInt32(LRandom.Next());
//					LNewRow[1] = Scalar.FromInt32(100);
//					LNewRow[2] = Scalar.FromString(TestUtility.RandomName());
//					LNewRow[3] = Scalar.FromString(TestUtility.RandomPhone());
//					LCursor.Insert(LNewRow);
//					if (LCursor.Select()[0].ToInt32() != LNewRow[0].ToInt32())
//						throw new RuntimeException("Insert failed to refresh to proper row");
//						
//					LNewRow[1] = Scalar.FromInt32(101);
//					LCursor.Update(LNewRow);
//					if (LCursor.Select()[0].ToInt32() != LNewRow[0].ToInt32())
//						throw new RuntimeException("Update failed to refresh to proper row");
//					if (LCursor.Select()[1].ToInt32() != LNewRow[1].ToInt32())
//						throw new RuntimeException("Update failed");
//
//					Row LOldRow = LCursor.GetKey();						
//					LCursor.Delete();
//					if (LCursor.FindKey(LOldRow))
//						throw new RuntimeException("Delete failed");
				}
				finally
				{
					LPlan.Close(LCursor);
				}
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareExpression(LPlan);
			}
			
			return null;
		}
	}
	
//	public struct TestDataVar
//	{
//		string Name;
//		Schema.IDataType DataType;
//		DataValue Value;
//	}
//	
	public class TestDataVar : System.Object
	{
//		public TestDataVar(string AName, Schema.IDataType ADataType) : base()
//		{
//			Name = AName;
//			DataType = ADataType;
//		}
//		
//		public TestDataVar(string AName, Schema.IDataType ADataType, DataValue AValue) : base()
//		{
//			Name = AName;
//			DataType = ADataType;
//			Value = AValue;
//		}
//		
		public TestDataVar(Schema.IDataType ADataType) : base()
		{
			DataType = ADataType;
		}
		
		public TestDataVar(Schema.IDataType ADataType, DataValue AValue) : base()
		{
			DataType = ADataType;
			Value = AValue;
		}
		
//		public string Name;
//		protected string FName = string.Empty;
//		public string Name
//		{
//			get
//			{
//				return FName;
//			}
//			set
//			{
//				FName = value == null ? String.Empty : value;
//			}
//		}
		
		public Schema.IDataType DataType;
//		protected Schema.IDataType FDataType;
//		public Schema.IDataType DataType
//		{
//			get
//			{
//				return FDataType;
//			}
//		}

		public DataValue Value;		
//		protected DataValue FValue;
//		public virtual DataValue Value
//		{
//			get
//			{
//				return FValue;
//			}
//			set
//			{
//				FValue = value;
////				if (FValue != value)
////				{
////					if (FValue is IDisposableNotify)
////						((IDisposableNotify)FValue).OnDispose -= new EventHandler(ValueDispose);
////					if (FValue is IDisposable)
////						((IDisposable)FValue).Dispose();
////					FValue = value;
////					if (FValue is IDisposableNotify)
////						((IDisposableNotify)FValue).OnDispose += new EventHandler(ValueDispose);
////				}
//			}
//		}

//		private void ValueDispose(object ASender, EventArgs AEventArgs)
//		{
//			if (FValue is IDisposableNotify)
//				((IDisposableNotify)FValue).OnDispose -= new EventHandler(ValueDispose);
//			FValue = null;
//		}
//		
//		public DataValue DisownValue()
//		{
//			if (FValue is IDisposableNotify)
//				((IDisposableNotify)FValue).OnDispose -= new EventHandler(ValueDispose);
//
//			DataValue LValue = FValue;
//			FValue = null;
//			return LValue;
//		}
//		
//		// Clone
//		public object Clone()
//		{	
//			return new DataVar(FName, FDataType, FValue == null ? null : FValue.Copy());
//		}
//		
//        // ToString
//        public override string ToString()
//        {
//			return (Name == String.Empty) ? DataType.ToString() : Name;
//        }
	}
	
	public class TestReferenceTypeValue : DataValue
	{
		public TestReferenceTypeValue(Schema.IDataType ADataType) : base(ADataType){}
		public TestReferenceTypeValue(Schema.IDataType ADataType, object AValue) : base(ADataType)
		{
			Value = AValue;
		}
		
		public TestReferenceTypeValue(object AValue) : base(Schema.IDataType.SystemScalar)
		{
			Value = AValue;
		}
		
		public Object Value;
		
//		protected override void Dispose(bool ADisposing)
//		{
//			Value = null;
//			base.Dispose(ADisposing);
//		}
//		
//		private object FValue;
//		public object Value
//		{
//			get
//			{
//				return FValue;
//			}
//			set
//			{
//				if (FValue != value)
//				{
//					if (FValue is IDisposableNotify)
//						((IDisposableNotify)FValue).OnDispose -= new EventHandler(ValueDispose);
//					if (FValue is IDisposable)
//						((IDisposable)FValue).Dispose();
//					FValue = value;
//					if (FValue is IDisposableNotify)
//						((IDisposableNotify)FValue).OnDispose += new EventHandler(ValueDispose);
//				}
//			}
//		}
//		
//		private void ValueDispose(object ASender, EventArgs AArgs)
//		{
//			Value = null;
//		}
//
		public override DataValue Copy()
		{
			return new TestReferenceTypeValue(DataType, Value);
		}
	}
	
	public class TestPlanNode : InstructionNode
	{
		private class TestObject{}
		private class TestReference
		{
			public TestReference(DataVar[] AArguments)
			{
				Arguments = AArguments;
			}
			
			public DataVar[] Arguments;
		}
		
		private int TestAsArgument(TestObject AObject, DataVar[] AArguments)
		{
			return AArguments.Length;
		}
		
		private int TestAsReference(TestReference AReference)
		{
			return AReference.Arguments.Length;
		}
		
		private void TestNativeMultiply()
		{
			int AValue = 5 * 5;
			if (AValue != 25)
				throw new Exception("Testing");
		}
		
		private void TestPureStack(ServerProcess AProcess)
		{
			AProcess.Context.Push(new DataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(5)));
			AProcess.Context.Push(new DataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(5)));
			PureStackMultiply(AProcess);
			AProcess.Context.Pop();
		}
		
		private void PureStackMultiply(ServerProcess AProcess)
		{
//			using (DataVar LRightValue = AProcess.Stack.Pop())
//			{
//				using (DataVar LLeftValue = AProcess.Stack.Pop())
//				{
//					AProcess.Stack.Push(new DataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(((Scalar)LLeftValue.Value).ToInt32() * ((Scalar)LRightValue.Value).ToInt32())));
//				}
//			}
		}
		
		private void TestCallingWithScalars()
		{
			MultiplyWithScalars(Scalar.FromInt32(5), Scalar.FromInt32(5));
		}
		
		private Scalar MultiplyWithScalars(Scalar ALeftValue, Scalar ARightValue)
		{
			return Scalar.FromInt32(ALeftValue.ToInt32() * ARightValue.ToInt32());
		}
		
		private void TestCallingWithScalarDataVars()
		{
			MultiplyWithScalarDataVars(new TestDataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(5)), new TestDataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(5)));
		}
		
		private TestDataVar MultiplyWithScalarDataVars(TestDataVar ALeftValue, TestDataVar ARightValue)
		{
			return new TestDataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(((Scalar)ALeftValue.Value).ToInt32() * ((Scalar)ARightValue.Value).ToInt32()));
		}
		
		private void TestCallingWithObjectDataVars()
		{
			MultiplyWithObjectDataVars(new TestDataVar(Schema.IDataType.SystemInteger, new TestReferenceTypeValue(Schema.IDataType.SystemInteger, 5)), new TestDataVar(Schema.IDataType.SystemInteger, new TestReferenceTypeValue(Schema.IDataType.SystemInteger, 5)));
		}
		
		private TestDataVar MultiplyWithObjectDataVars(TestDataVar ALeftValue, TestDataVar ARightValue)
		{
			return new TestDataVar(Schema.IDataType.SystemInteger, new TestReferenceTypeValue(Schema.IDataType.SystemInteger, (int)((TestReferenceTypeValue)ALeftValue.Value).Value * (int)((TestReferenceTypeValue)ARightValue.Value).Value));
		}
		
		private void TestCallingWithReferenceTypeValues()
		{
			MultiplyWithReferenceTypeValues(new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5), new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5)).Dispose();
		}
		
		private ReferenceTypeValue MultiplyWithReferenceTypeValues(ReferenceTypeValue ALeftValue, ReferenceTypeValue ARightValue)
		{
			ReferenceTypeValue LResult = new ReferenceTypeValue(Schema.IDataType.SystemInteger, (int)ALeftValue.Value * (int)ARightValue.Value);
			ALeftValue.Dispose();
			ARightValue.Dispose();
			return LResult;
		}
		
		private void TestCallingWithObjects()
		{
			MultiplyWithObjects(5, 5);
		}
		
		private object MultiplyWithObjects(object ALeftValue, object ARightValue)
		{
			return (int)ALeftValue * (int)ARightValue;
		}
		
		private void TestCallingWithIntegers2()
		{
			TestCallingWithIntegers();
		}
		
		private void TestCallingWithIntegers3()
		{
			TestCallingWithIntegers2();
		}
		
		private void TestCallingWithIntegers()
		{
			MultiplyWithIntegers(5, 5);
		}
		
		private int MultiplyWithIntegers(int ALeftValue, int ARightValue)
		{
			return ALeftValue * ARightValue;
		}
		
		// creating the variable
		// evaluating the multiply
		// evaluating literals
		// assigning the variable
		// evaluating the condition
		// dropping the variable
		
		private PlanNode FBlockNode;
		private void PrepareD4Multiply(Plan APlan)
		{
			BlockNode LNode = new BlockNode();
			VariableNode LVarNode = new VariableNode();
			LVarNode.VariableName = "AValue";
			LVarNode.VariableType = Schema.IDataType.SystemInteger;
			//LVarNode.Nodes.Add(Compiler.EmitBinaryNode(AProcess, new ValueNode(new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5)), Instructions.Multiplication, new ValueNode(new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5))));
			LVarNode.Nodes.Add(Compiler.EmitBinaryNode(APlan, new ValueNode(Scalar.FromInt32(5)), Instructions.Multiplication, new ValueNode(Scalar.FromInt32(5))));
			LNode.Nodes.Add(LVarNode);
//			IfNode LIfNode = new IfNode();
//			LIfNode.Nodes.Add(Compiler.EmitBinaryNode(AProcess, new StackReferenceNode(Schema.IDataType.SystemInteger, 0), Instructions.NotEqual, new ValueNode(Scalar.FromInt32(25))));
//			RaiseNode LRaiseNode = new RaiseNode();
//			SystemErrorNode LErrorNode = new SystemErrorNode();
//			LErrorNode.Nodes.Add(new ValueNode(Scalar.FromString("Testing")));
//			LErrorNode.DetermineDataType(AProcess);
//			LRaiseNode.Nodes.Add(LErrorNode);
//			LIfNode.Nodes.Add(LRaiseNode);
//			LNode.Nodes.Add(LIfNode);
			LNode.Nodes.Add(new DropVariableNode());
			FBlockNode = LNode;
		}
		
		private void TestD4Multiply(ServerProcess AProcess)
		{
			FBlockNode.Execute(AProcess);
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			PrepareD4Multiply(APlan);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			int ACount = ((Scalar)AArguments[0].Value).ToInt32();

			// Get Timing for passing the arguments as parameters
			DateTime LStartTime = DateTime.Now;
			
			//TestObject LObject = new TestObject();
			for (int LIndex = 0; LIndex < ACount; LIndex++)
				//TestNativeMultiply();
				TestCallingWithObjectDataVars();
				//TestPureStack(AProcess);
				//if (TestAsArgument(LObject, AArguments) != AArguments.Count)
				//	throw new RuntimeException("Testing");

			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- Native time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));

			// Get Timing for passing the arguments as a reference on the plan
			LStartTime = DateTime.Now;					
			
			//TestReference LReference = new TestReference(AArguments);
			for (int LIndex = 0; LIndex < ACount; LIndex++)
				//TestCallingWithIntegers();
				//TestCallingWithObjects();
				TestCallingWithScalarDataVars();
				//TestPureStack(AProcess);
				//TestD4Multiply(AProcess);
				//if (TestAsReference(LReference) != AArguments.Count)
				//	throw new RuntimeException("Testing");

			TimeSpan LTotalTime = DateTime.Now - LStartTime;
//			#if TIMING
//			TimeSpan LOtherTime = LTotalTime - AProcess.CreateTime - AProcess.DropTime - AProcess.EvaluateTime - AProcess.AssignTime - AProcess.PassingTime;
//			decimal LCreatePercent = ((decimal)AProcess.CreateTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			decimal LDropPercent = ((decimal)AProcess.DropTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			decimal LEvaluatePercent = ((decimal)AProcess.EvaluateTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			decimal LAssignPercent = ((decimal)AProcess.AssignTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			decimal LPassingPercent = ((decimal)AProcess.PassingTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			decimal LOtherPercent = ((decimal)LOtherTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
//			#endif
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 total time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (LTotalTime).ToString()));
//			#if TIMING
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 create time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.CreateTime.ToString(), LCreatePercent.ToString()));
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 drop time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.DropTime.ToString(), LDropPercent.ToString()));
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Evaluate time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.EvaluateTime.ToString(), LEvaluatePercent.ToString()));
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Assign time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.AssignTime.ToString(), LAssignPercent.ToString()));
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Passing time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.PassingTime.ToString(), LPassingPercent.ToString()));
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Other time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), (LOtherTime).ToString(), LOtherPercent.ToString()));
//			#endif
					
			return null;
		}
	}
	
	public class TestNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			int LCount = ((Scalar)AArguments[0].Value).ToInt32();
			
			List LList = new List();
			for (int LIndex = 0; LIndex < 1000; LIndex++)
				LList.Add(LIndex);

			DateTime LStartTime = DateTime.Now;
			
			for (int LIndex = 0; LIndex < LCount; LIndex++)
				foreach (object LObject in LList)
					if (!(LObject is int))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNameRequired);
			
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- Enumerator time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));

			LStartTime = DateTime.Now;
			
			for (int LIndex = 0; LIndex < LCount; LIndex++)
				for (int LObjectIndex = 0; LObjectIndex < LList.Count; LObjectIndex++) 
					if (!(LList[LObjectIndex] is int))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNameRequired);
			
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- For loop time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));

			return null;
		}
	}
}
