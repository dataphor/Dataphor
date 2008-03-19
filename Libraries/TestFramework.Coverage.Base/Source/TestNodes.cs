/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Diagnostics
{
	using System;
	using System.IO;
	using System.Text;
	using System.Resources;
	using System.Reflection;
	using System.Collections.Specialized;
	using System.Windows.Forms;

	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language;
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
	
	// operator System.Diagnostics.TestCatalog();
	public class TestCatalogNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Objects LObjects = new Schema.Objects();
			for (int LIndex = 0; LIndex < 100; LIndex++)
			{
				LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), String.Format("Object_{0}", LIndex.ToString())));
			}
			
			// Verify lookup by name and id
			for (int LIndex = 0; LIndex < 100; LIndex++)
			{
				string LName = String.Format("Object_{0}", LIndex.ToString());
				Schema.Object LObject = LObjects[LName];
				if (LObject.Name != LName)
					throw new TestException("Name lookup failed for object {0}", LName);
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
				}
			}
			
			// Verify that duplicates are not allowed
			bool LErrorHit = false;
			try
			{
				LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "Object_1"));
			}
			catch
			{
				LErrorHit = true;
			}
			
			if (!LErrorHit)
				throw new TestException("Duplicate object name allowed.");
			
			// Verify that ambiguous names are not allowed
			LErrorHit = false;
			try
			{
				LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "A.Object_1")); // cannot create a hiding identifier
			}
			catch
			{
				LErrorHit = true;
			}

			if (!LErrorHit)
				throw new TestException("Hiding object name allowed.");
			
			LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "A.A"));

			LErrorHit = false;
			try
			{
				LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "A")); // cannot create a hidden identifier
			}
			catch
			{
				LErrorHit = true;
			}

			if (!LErrorHit)
				throw new TestException("Hidden object name allowed.");
			
			// Verify that ambiguous references are not allowed
			LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "B.A"));
			StringCollection LNames = new StringCollection();
			int LObjectIndex = LObjects.IndexOf("A", LNames);
			if (LObjectIndex >= 0)
				throw new TestException("Ambiguous resolution allowed.");
				
			LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "X.Y.Z"));
			LObjects.Add(new Schema.ScalarType(Schema.Object.GetNextObjectID(), "X.Z"));
			LNames.Clear();
			LObjectIndex = LObjects.IndexOf("Z", LNames);
			if (LObjectIndex >= 0)
				throw new TestException("Multi-level ambiguous resolution allowed.");

			return null;
		}
	}
	
	// operator System.Diagnostics.TestStreams();
	public class TestStreamsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Test the DeferredWriteStream
			Stream LStream = new MemoryStream();
			byte[] LData = new byte[100];
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				LData[LIndex] = 100;
			LStream.Write(LData, 0, LData.Length);
			LStream.Position = 0;
			
			LData = new byte[LData.Length];
			DeferredWriteStream LDeferredWriteStream = new DeferredWriteStream(LStream);
			LDeferredWriteStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
			{
				if (LData[LIndex] != 100)
					throw new TestException("DeferredWriteStream read failed");
				LData[LIndex] = 50;
			}
					
			LDeferredWriteStream.Position = 0;
			LDeferredWriteStream.Write(LData, 0, LData.Length);
			
			LData = new byte[LData.Length];
			LDeferredWriteStream.Position = 0;
			LDeferredWriteStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				if (LData[LIndex] != 50)
					throw new TestException("DeferredWriteStream write failed");
					
			LDeferredWriteStream.Flush();
			
			LData = new byte[LData.Length];
			LStream.Position = 0;
			LStream.Read(LData, 0, LData.Length);
			for (int LIndex = 0; LIndex < LData.Length; LIndex++)
				if (LData[LIndex] != 50)
					throw new TestException("DeferredWriteStream flush failed");
					
			return null;
		}
	}
	
	// operator TestLocalStreamManager();
//	public class TestLocalStreamManagerNode : InstructionNode
//	{
//		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
//		{
//			LocalStreamManager LStreamManager = new LocalStreamManager((IStreamManager)AProcess);
//			Schema.RowType LRowType = new Schema.RowType();
//			LRowType.Columns.Add(new Schema.Column("Name", AProcess.DataTypes.SystemString));
//			using (Row LRow = new Row(AProcess, LRowType, LStreamManager))
//			{
//				LRow["Name"] = Scalar.FromString(AProcess, "This is a good long string which should cause overflow");
//				if (LRow["Name"].ToString() != "This is a good long string which should cause overflow")
//					throw new TestException("LocalStreamManager failed");
//			}
//			
//			using (Row LRow = new Row(AProcess, LRowType, LStreamManager))
//			{
//				LRow["Name"] = Scalar.FromString(AProcess, "This is a good long string which should cause overflow");
//				StreamID LOverflowStreamID = ((CellValueStream)LRow["Name"].Stream).OverflowStreamID;
//				LRow["Name"] = Scalar.FromString(AProcess, "Short");
//					
//				LRow["Name"] = Scalar.FromString(AProcess, "This is a good long string which should cause overflow");
//				if (((CellValueStream)LRow["Name"].Stream).OverflowStreamID == LOverflowStreamID)
//					throw new TestException("Cell value stream does not request a new stream id for secondary overflow");
//			}
//			
//			return null;
//		}
//	}
//	
/*
	// operator TestConveyors();
	public class TestConveyorsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Scalar LScalar;
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemBoolean);
			try
			{
				((BooleanConveyor)LScalar.Conveyor).SetValue(LScalar, false);
				if (LScalar.ToBoolean() != false)
					throw new TestException("Boolean conveyor failed");
					
				((BooleanConveyor)LScalar.Conveyor).SetValue(LScalar, true);
				if (LScalar.ToBoolean() != true)
					throw new TestException("Boolean conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemByte);
			try
			{
				((ByteConveyor)LScalar.Conveyor).SetValue(LScalar, Byte.MinValue);
				if (LScalar.ToByte() != Byte.MinValue)
					throw new TestException("Byte conveyor failed");
					
				((ByteConveyor)LScalar.Conveyor).SetValue(LScalar, Byte.MaxValue);
				if (LScalar.ToByte() != Byte.MaxValue)
					throw new TestException("Byte conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemShort);
			try
			{
				((Int16Conveyor)LScalar.Conveyor).SetValue(LScalar, Int16.MinValue);
				if (LScalar.ToInt16() != Int16.MinValue)
					throw new TestException("Int16 conveyor failed");
					
				((Int16Conveyor)LScalar.Conveyor).SetValue(LScalar, Int16.MaxValue);
				if (LScalar.ToInt16() != Int16.MaxValue)
					throw new TestException("Int16 conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemInteger);
			try
			{
				((Int32Conveyor)LScalar.Conveyor).SetValue(LScalar, Int32.MinValue);
				if (LScalar.ToInt32() != Int32.MinValue)
					throw new TestException("Int32 conveyor failed");
					
				((Int32Conveyor)LScalar.Conveyor).SetValue(LScalar, Int32.MaxValue);
				if (LScalar.ToInt32() != Int32.MaxValue)
					throw new TestException("Int32 conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemLong);
			try
			{
				((Int64Conveyor)LScalar.Conveyor).SetValue(LScalar, Int64.MinValue);
				if (LScalar.ToInt64() != Int64.MinValue)
					throw new TestException("Int64 conveyor failed");
					
				((Int64Conveyor)LScalar.Conveyor).SetValue(LScalar, Int64.MaxValue);
				if (LScalar.ToInt64() != Int64.MaxValue)
					throw new TestException("Int64 conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemDecimal);
			try
			{
				((DecimalConveyor)LScalar.Conveyor).SetValue(LScalar, Decimal.MinValue);
				if (LScalar.ToDecimal() != Decimal.MinValue)
					throw new TestException("Decimal conveyor failed");
					
				((DecimalConveyor)LScalar.Conveyor).SetValue(LScalar, Decimal.MaxValue);
				if (LScalar.ToDecimal() != Decimal.MaxValue)
					throw new TestException("Decimal conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemString);
			try
			{
				((StringConveyor)LScalar.Conveyor).SetValue(LScalar, String.Empty);
				if (LScalar.ToString() != String.Empty)
					throw new TestException("String conveyor failed");
					
				((StringConveyor)LScalar.Conveyor).SetValue(LScalar, "This is a good long string");
				if (LScalar.ToString() != "This is a good long string")
					throw new TestException("String conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
//			LConveyor = new StreamIDConveyor(LStream);
//			((StreamIDConveyor)LConveyor).Value = StreamID.Null;
//			if (((StreamIDConveyor)LConveyor).Value != StreamID.Null)
//				throw new TestException("StreamID conveyor failed");
//				
//			((StreamIDConveyor)LConveyor).Value = new StreamID(65545);
//			if (((StreamIDConveyor)LConveyor).Value != new StreamID(65545))
//				throw new TestException("StreamID conveyor failed");
//			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemGuid);
			try
			{
				((GuidConveyor)LScalar.Conveyor).SetValue(LScalar, Guid.Empty);
				if (LScalar.ToGuid() != Guid.Empty)
					throw new TestException("Guid conveyor failed");
					
				((GuidConveyor)LScalar.Conveyor).SetValue(LScalar, new Guid("{BA69917B-27ED-41fb-BA46-7DF95C146280}"));
				if (LScalar.ToGuid() != new Guid("{BA69917B-27ED-41fb-BA46-7DF95C146280}"))
					throw new TestException("Guid conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemTimeSpan);
			try
			{
				((TimeSpanConveyor)LScalar.Conveyor).SetValue(LScalar, TimeSpan.MinValue);
				if (LScalar.ToTimeSpan() != TimeSpan.MinValue)
					throw new TestException("TimeSpan conveyor failed");
					
				((TimeSpanConveyor)LScalar.Conveyor).SetValue(LScalar, TimeSpan.MaxValue);
				if (LScalar.ToTimeSpan() != TimeSpan.MaxValue)
					throw new TestException("TimeSpan conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemDateTime);
			try
			{
				((DateTimeConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MinValue);
				if (LScalar.ToDateTime() != DateTime.MinValue)
					throw new TestException("DateTime conveyor failed");
					
				((DateTimeConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MaxValue);
				if (LScalar.ToDateTime() != DateTime.MaxValue)
					throw new TestException("DateTime conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemDate);
			try
			{
				((DateConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MinValue);
				if (LScalar.ToDate() != DateTime.MinValue)
					throw new TestException("Date conveyor failed");
					
				((DateConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MaxValue);
				if (LScalar.ToDate() != DateTime.MaxValue)
					throw new TestException("Date conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
			LScalar = AProcess.ScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemTime);
			try
			{
				((TimeConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MinValue);
				if (LScalar.ToTime() != DateTime.MinValue)
					throw new TestException("Time conveyor failed");
					
				((TimeConveyor)LScalar.Conveyor).SetValue(LScalar, DateTime.MaxValue);
				if (LScalar.ToTime() != DateTime.MaxValue)
					throw new TestException("Time conveyor failed");
			}
			finally
			{
				AProcess.ScalarManager.ReleaseScalar(LScalar);
			}
			
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
	
	public class TestRowManagerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			RowManager LRowManager = new RowManager(-1);
			for (int LIndex = 0; LIndex < (LRowManager.MaxRows * 2); LIndex++)
			{
				Row LRow = LRowManager.RequestRow(AProcess, new Schema.RowType());
				try
				{
				}
				finally
				{
					LRowManager.ReleaseRow(LRow);
				}
			}
			return null;
		}
	}
*/

	#if USESCALARMANAGER	
	public class TestScalarManagerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			ScalarManager LScalarManager = new ScalarManager(-1);
			for (int LIndex = 0; LIndex < (LScalarManager.MaxScalars * 2); LIndex++)
			{
				Scalar LScalar = LScalarManager.RequestScalar(AProcess, AProcess.DataTypes.SystemString, "");
				try
				{
				}
				finally
				{
					LScalarManager.ReleaseScalar(LScalar);
				}
			}
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
			
			Scalar LValue = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream);
			
			// Write some data to the stream
			((StringConveyor)LValue.Conveyor).SetValue(LValue, "This is a good long string");

			// Close the stream
			LStreamManager.Close(LStreamID);
			
			// Reopen the stream
			LStream = LStreamManager.Open(LStreamID, LockMode.Shared);
			
			LValue = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream);
			
			// Read the data and verify that it is the same data
			if (((StringConveyor)LValue.Conveyor).GetValue(LValue) != "This is a good long string")
				throw new TestException("Stream manager write failed");
				
			// Open another stream on the same StreamID
			Stream LSecondStream = LStreamManager.Open(LStreamID, LockMode.Shared);
			
			Scalar LSecondValue = new Scalar(AProcess, AProcess.DataTypes.SystemString, LSecondStream);
			
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
			Scalar LValue1 = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream1);
			
			// Write some data to Stream1
			((StringConveyor)LValue1.Conveyor).SetValue(LValue1, "This is the value for stream 1");

			// Reference Stream1 as Stream2
			StreamID LStreamID2 = LStreamManager.Reference(LStreamID1);
			Stream LStream2 = LStreamManager.Open(LStreamID2, LockMode.Exclusive);
			Scalar LValue2 = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream2);

			// Reference Stream1 as Stream3
			StreamID LStreamID3 = LStreamManager.Reference(LStreamID1);
			Stream LStream3 = LStreamManager.Open(LStreamID3, LockMode.Exclusive);
			Scalar LValue3 = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream3);

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
			Scalar LValue4 = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream4);
			
			// Reference Stream4 as Stream5
			StreamID LStreamID5 = LStreamManager.Reference(LStreamID4);
			Stream LStream5 = LStreamManager.Open(LStreamID5, LockMode.Exclusive);
			Scalar LValue5 = new Scalar(AProcess, AProcess.DataTypes.SystemString, LStream5);
			
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
	#endif
	
	/*
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
			if (LSemaphore.AcquireImmediate(1, LockMode.Shared))
				throw new TestException("Semaphore exclusive lock failed");
			LSemaphore.Release(0);
			if (!LSemaphore.AcquireImmediate(1, LockMode.Exclusive))
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
			LRowType.Columns.Add(new Schema.Column("Name", AProcess.DataTypes.SystemString));
			LRowType.Columns.Add(new Schema.Column("VisitCount", AProcess.DataTypes.SystemInteger));
			LRowType.Columns.Add(new Schema.Column("Salary", AProcess.DataTypes.SystemDecimal));
			Row LRow = AProcess.RowManager.RequestRow(AProcess, LRowType);
			try
			{
				if (LRow.HasValue("Name"))
					throw new TestException("HasValue within a row failed");
				if (LRow.HasValue("VisitCount"))
					throw new TestException("HasValue within a row failed");
				if (LRow.HasValue("Salary"))
					throw new TestException("HasValue within a row failed");
					
				LRow["Name"] = Scalar.FromString(AProcess, "01234567890123456789");
				LRow["VisitCount"] = Scalar.FromInt32(AProcess, 10);
				LRow["Salary"] = Scalar.FromDecimal(AProcess, 10.0m);
				
				if (LRow["Name"].ToString() != "01234567890123456789")
					throw new TestException("String conveyor within a row failed");
				if (LRow["VisitCount"].ToInt32() != 10)
					throw new TestException("Int32 conveyor within a row failed");
				if (LRow["Salary"].ToDecimal() != 10.0m)
					throw new TestException("Decimal conveyor within a row failed");

				// Test overflow handling
				LRow["Name"] = Scalar.FromString(AProcess, "012345678901234567890");
				if (LRow["Name"].ToString() != "012345678901234567890")
					throw new TestException("Overflow handling within a row failed");
					
				LRow["Name"] = Scalar.FromString(AProcess, "0123456789");
				if (LRow["Name"].ToString() != "0123456789")
					throw new TestException("Overflow handling within a row failed");
					
				LRow.ClearValue("Name");
				if (LRow.HasValue("Name"))
					throw new TestException("ClearValue within a row failed");
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LRow);
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
			LRowType.Columns.Add(new Schema.Column("Description", AProcess.DataTypes.SystemString));
			Row LRow1 = AProcess.RowManager.RequestRow(AProcess, LRowType);
			try
			{
				Row LRow2 = AProcess.RowManager.RequestRow(AProcess, LRowType);
				try
				{
					LRow2[0] = Scalar.FromString(AProcess, "012345678901234567890123456789");
					if (LRow2[0].ToString() != "012345678901234567890123456789")
						throw new TestException("Row overflow failed");
					LRow2.CopyTo(LRow1);
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(LRow2);
				}

				if (LRow1[0].ToString() != "012345678901234567890123456789")
					throw new TestException("Row overflow reference copy failed");
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(LRow1);
			}
			return null;
		}
	}

	// operator TestExceptions()
	public class TestExceptionsNode : InstructionNode
	{
		// operator TestExceptions(const AAssemblyFileName : String, const AExceptionClassName : String);
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			StringCollection LUnknownCodes = new StringCollection();
			string LAssemblyFileName = ((Scalar)AArguments[0].Value).ToString();
			if (!Path.IsPathRooted(LAssemblyFileName))
				LAssemblyFileName = Path.Combine(Application.StartupPath, LAssemblyFileName);
			string LExceptionClassName = ((Scalar)AArguments[1].Value).ToString();
			Assembly LAssembly = Assembly.LoadFrom(LAssemblyFileName);
			Type LExceptionClass = LAssembly.GetType(LExceptionClassName);
			Type LCodesEnum = LExceptionClass.GetNestedType("Codes");
			System.Array LValues = Enum.GetValues(LCodesEnum);
			ResourceManager LResourceManager = LExceptionClass.GetField("FResourceManager", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ResourceManager;
			foreach (int LValue in LValues)
				if (LResourceManager.GetString(LValue.ToString()) == null)
					LUnknownCodes.Add(LValue.ToString());
			if (LUnknownCodes.Count > 0)
				throw new TestException(String.Format(@"The following codes were not found as resource strings in the ""{0}"" exception class: {1}.", LExceptionClassName, ExceptionUtility.StringsToCommaList(LUnknownCodes)));
			return null;
		}
	}
	*/
	
/*
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
				LIndexKey = AProcess.RowManager.RequestRow(AProcess, (Schema.RowType)AIndexContext, AIndexKey);
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
					LCompareKey = AProcess.RowManager.RequestRow(AProcess, (Schema.RowType)ACompareContext, ACompareKey);
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
						AProcess.RowManager.ReleaseRow(LCompareKey);
				}
			}
			finally
			{
				if (!ReferenceEquals(FIndexKey, LIndexKey))
					AProcess.RowManager.ReleaseRow(LIndexKey);
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
			LKeyType.Columns.Add(new Schema.Column("ID", AProcess.DataTypes.SystemInteger));
			FKey = new Schema.Order();
			FKey.Columns.Add(new Schema.OrderColumn(new Schema.TableVarColumn(LKeyType.Columns[0]), true));
			FKey.Columns[0].Sort = ((Schema.ScalarType)FKey.Columns[0].Column.DataType).GetSort(AProcess.Plan);
			FDataVars = new DataVar[2];
			FDataVars[0] = new DataVar(FKey.Columns[0].Column.DataType);
			FDataVars[1] = new DataVar(FKey.Columns[0].Column.DataType);
			LDataType.Columns.Add(new Schema.Column("Name", AProcess.DataTypes.SystemString));
			FIndexKey = AProcess.RowManager.RequestRow(AProcess, LKeyType);
			try
			{
				FCompareKey = AProcess.RowManager.RequestRow(AProcess, LKeyType);
				try
				{
					FData = AProcess.RowManager.RequestRow(AProcess, LDataType);
					try
					{
						FTargetData = AProcess.RowManager.RequestRow(AProcess, LDataType);
						try
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
								LKey = AProcess.RowManager.RequestRow(AProcess, LKeyType);
								try
								{
									LKey.ValuesOwned = false;
									LKey[0] = Scalar.FromInt32(AProcess, LIDArray[LCounter]);
									LData = AProcess.RowManager.RequestRow(AProcess, LDataType);
									try
									{
										LData.ValuesOwned = false;
										LData[0] = Scalar.FromString(AProcess, LIDArray[LCounter].ToString());
									
										LIndex.Insert(AProcess, LKey.Stream, LData.Stream);
									}
									finally
									{
										AProcess.RowManager.ReleaseRow(LData);
									}
								}
								finally
								{
									AProcess.RowManager.ReleaseRow(LKey);
								}
							}
							
							// Find Each row using the Select method
							LKey = AProcess.RowManager.RequestRow(AProcess, LKeyType);
							try
							{
								for (int LCounter = 0; LCounter < LRowCount; LCounter++)
								{
									LKey[0] = Scalar.FromInt32(AProcess, LIDArray[LCounter]);
									SearchPath LSearchPath = new SearchPath();
									try
									{
										int LEntryNumber;
										bool LResult = LIndex.FindKey(AProcess, LKey.Stream, null, LSearchPath, out LEntryNumber);
										if (!LResult)
											throw new TestException("Index.FindKey failed");

										LData = AProcess.RowManager.RequestRow(AProcess, LDataType, LSearchPath.DataNode.Data(LEntryNumber));
										try
										{
											LData.ValuesOwned = false;
											if (Convert.ToInt32(LData[0].ToString()) != LIDArray[LCounter])
												throw new TestException("Index.FindKey failed");
										}
										finally
										{
											AProcess.RowManager.ReleaseRow(LData);
										}
									}
									finally
									{
										LSearchPath.Dispose();
									}
								}
							
								// Scan through the index using the leaf list and verify increasing order of rows
								long LLastKey = -1;
								long LCurrentKey;
								StreamID LNextStreamID = LIndex.HeadID;
								IndexNode LIndexNode;
								LKey.ValuesOwned = false;
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
							}
							finally
							{
								AProcess.RowManager.ReleaseRow(LKey);
							}

							// Delete all the rows in the index
							LKey = AProcess.RowManager.RequestRow(AProcess, LKeyType);
							try
							{
								for (int LCounter = 0; LCounter < LRowCount; LCounter++)
								{
									LKey[0] = Scalar.FromInt32(AProcess, LIDArray[LCounter]);
									LIndex.Delete(AProcess, LKey.Stream);
								}
							}
							finally
							{
								AProcess.RowManager.ReleaseRow(LKey);
							}

							// Drop the index
							LIndex.Drop(AProcess);
							return null;
						}
						finally
						{
							AProcess.RowManager.ReleaseRow(FTargetData);
						}
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(FData);
					}
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(FCompareKey);
				}
			}
			finally
			{
				AProcess.RowManager.ReleaseRow(FIndexKey);
			}
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
			LTableType.Columns.Add(new Schema.Column("ID", AProcess.DataTypes.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("ID2", AProcess.DataTypes.SystemInteger));
			LTableType.Columns.Add(new Schema.Column("Name", AProcess.DataTypes.SystemString));
			LTableType.Columns.Add(new Schema.Column("Phone", AProcess.DataTypes.SystemString));
			Schema.TableVar LTableVar = new Schema.BaseTableVar("Testing", LTableType);
			foreach (Schema.Column LColumn in LTableType.Columns)
				LTableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
			LTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{LTableVar.Columns[0]}));

			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableVar.Columns[1], true));
			LTableVar.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableVar.Columns[2], true));
			LTableVar.Orders.Add(LOrder);

			LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(LTableVar.Columns[3], true));
			LTableVar.Orders.Add(LOrder);

			TableBuffer LTableBuffer = new TableBuffer(AProcess, LTableVar, LFanout, LCapacity);
			try
			{
				Schema.RowType LRowType = (Schema.RowType)LTableType.CreateRowType();
				FRandom = new Random();
				for (int LIndex = 0; LIndex < LRowCount; LIndex++)
				{
					Row LRow = AProcess.RowManager.RequestRow(AProcess, LRowType);
					try
					{
						LRow[0] = Scalar.FromInt32(AProcess, LIndex);
						LRow[1] = Scalar.FromInt32(AProcess, LIndex);
						LRow[2] = Scalar.FromString(AProcess, RandomName());
						LRow[3] = Scalar.FromString(AProcess, RandomPhone());
						LTableBuffer.Insert(AProcess, LRow);
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(LRow);
					}
				}
				
				Row LFirstKey = AProcess.RowManager.RequestRow(AProcess, LTableBuffer.ClusteredIndex.KeyRowType);
				try
				{
					LFirstKey[0] = Scalar.FromInt32(AProcess, LFirstKeyValue);

					Row LLastKey = AProcess.RowManager.RequestRow(AProcess, LTableBuffer.ClusteredIndex.KeyRowType);
					try
					{
						LLastKey[0] = Scalar.FromInt32(AProcess, LLastKeyValue);

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

						Row LKeyRow = AProcess.RowManager.RequestRow(AProcess, LTableBuffer.ClusteredIndex.KeyRowType);
						try
						{
							for (int LIndex = 0; LIndex < LRowCount; LIndex++)
							{
								LKeyRow[0] = Scalar.FromInt32(AProcess, LIndex);
								LTableBuffer.Delete(AProcess, LKeyRow);
							}
						}
						finally
						{
							AProcess.RowManager.ReleaseRow(LKeyRow);
						}
					}
					finally
					{
						AProcess.RowManager.ReleaseRow(LLastKey);
					}
				}
				finally
				{
					AProcess.RowManager.ReleaseRow(LFirstKey);
				}
			}
			finally
			{
				LTableBuffer.Dispose();
			}
			return null;
		}
	}
*/	
	// operator TestBrowse()
	public class TestBrowseNode : InstructionNode
	{
		public const string CPrepareTestData = 
			@"
create table Testing in Temp
{
	ID : System.Integer,
	ID2 : System.Integer,
	Name : System.String,
	Phone : System.String,
	key { ID },
	order { ID2 },
	order { Name },
	order { Phone }
};

for LIndex : Integer := 1 to 100 do
	insert table { row { LIndex ID, LIndex ID2, RandomName() Name, RandomPhone() Phone } } 
		into Testing;
			";
			
		public const string CUnprepareTestData =
			@"
drop table Testing;
			";

		protected void PrepareTestData(ServerProcess AProcess)
		{
			IServerScript LScript = ((IServerProcess)AProcess).PrepareScript(CPrepareTestData);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareScript(LScript);
			}
		}
		
		protected void UnprepareTestData(ServerProcess AProcess)
		{
			IServerScript LScript = ((IServerProcess)AProcess).PrepareScript(CUnprepareTestData);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareScript(LScript);
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Create and populate a test data set
			PrepareTestData(AProcess);
			try
			{
				Random LRandom = new Random();
				Guid[] LBookmarks = new Guid[100];
				
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
							try
							{
								LBookmarks[LCounter] = LCursor.GetBookmark();
								if (LRow[0].AsInt32 != LCounter)
									throw new RuntimeException(RuntimeException.Codes.SetNotOrdered);
							
								LCounter++;
							}
							finally
							{
								LRow.Dispose();
							}
						}
						
						LCursor.GotoBookmark(LBookmarks[0], true);
						if (LCursor.Prior())
							throw new RuntimeException(RuntimeException.Codes.SetShouldBeBOF);

						LCursor.GotoBookmark(LBookmarks[0], false);
						if (LCursor.Prior())
							throw new RuntimeException(RuntimeException.Codes.SetShouldBeBOF);
						
//						while (LCursor.Prior())
//						{
//							Row LRow = LCursor.Select();
//							try
//							{
//								if (LRow[0].ToInt32() > LLastValue)
//									throw new RuntimeException("Set is not ordered");
//								LLastValue = LRow[0].ToInt32();
//							}
//							finally
//							{
//								AProcess.RowManager.ReleaseRow(LRow);
//							}
//						}
//						
//						for (int LIndex = 0; LIndex < LRandomValues.Length; LIndex++)
//						{
//							if (!LCursor.GotoBookmark(LRandomValues[LIndex]))
//								throw new RuntimeException("GotoBookmark failed");
//						}
//						
//						LCounter = 0;
//						Guid LBookmark = LRandomValues[LRandomValues.Length / 2];
//						if (!LCursor.GotoBookmark(LBookmark))
//							throw new RuntimeException("GotoBookmark failed");
//							
//						while (LCursor.Next())
//						{
//							LCounter++;
//							if (LCounter > LRandomValues.Length / 2)
//								break;
//						}
//						
//						if (LCounter < LRandomValues.Length / 2)
//							throw new RuntimeException("EOF reached before known rows");
//						
//						if (!LCursor.GotoBookmark(LBookmark))
//							throw new RuntimeException("GotoBookmark failed");
//							
//						LCounter = 0;
//						while (LCursor.Prior())
//						{
//							LCounter++;
//							if (LCounter > LRandomValues.Length / 2)
//								break;
//						}
//						
//						if (LCounter < LRandomValues.Length / 2)
//							throw new RuntimeException("BOF reached before known rows");
//							
						Row LNewRow = new Row(AProcess, ((Schema.TableType)LCursor.Plan.DataType).CreateRowType());
						try
						{
							LNewRow[0].AsInt32 = LRandom.Next();
							LNewRow[1].AsInt32 = 100;
							LNewRow[2].AsString = TestUtility.RandomName();
							LNewRow[3].AsString = TestUtility.RandomPhone();
							LCursor.Insert(LNewRow);
							Row LSelectRow = LCursor.Select();
							try
							{
								if (LSelectRow[0].AsInt32 != LNewRow[0].AsInt32)
									throw new TestException("Insert failed to refresh to proper row");
									
								LNewRow[1].AsInt32 = 101;
								LCursor.Update(LNewRow);
								LCursor.Select(LSelectRow);
								if (LSelectRow[0].AsInt32 != LNewRow[0].AsInt32)
									throw new TestException("Update failed to refresh to proper row");
								if (LSelectRow[1].AsInt32 != LNewRow[1].AsInt32)
									throw new TestException("Update failed");
			
								Row LOldRow = LCursor.GetKey();						
								try
								{
									LCursor.Delete();
									if (LCursor.FindKey(LOldRow))
										throw new TestException("Delete failed");
								}
								finally
								{
									LOldRow.Dispose();
								}
							}
							finally
							{
								LSelectRow.Dispose();
							}
						}
						finally
						{
							LNewRow.Dispose();
						}
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
			}
			finally
			{
				UnprepareTestData(AProcess);
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
	
//	public class TestReferenceTypeValue : DataValue
//	{
//		public TestReferenceTypeValue(IServerProcess AProcess, Schema.IDataType ADataType) : base(AProcess, ADataType){}
//		public TestReferenceTypeValue(IServerProcess AProcess, Schema.IDataType ADataType, object AValue) : base(ADataType)
//		{
//			Value = AValue;
//		}
//		
//		public TestReferenceTypeValue(object AValue) : base(Schema.IDataType.SystemScalar)
//		{
//			Value = AValue;
//		}
//		
//		public Object Value;
//		
////		protected override void Dispose(bool ADisposing)
////		{
////			Value = null;
////			base.Dispose(ADisposing);
////		}
////		
////		private object FValue;
////		public object Value
////		{
////			get
////			{
////				return FValue;
////			}
////			set
////			{
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
////			}
////		}
////		
////		private void ValueDispose(object ASender, EventArgs AArgs)
////		{
////			Value = null;
////		}
////
//		public override DataValue Copy()
//		{
//			return new TestReferenceTypeValue(DataType, Value);
//		}
//	}
//	
//	public class TestPlanNode : InstructionNode
//	{
//		private class TestObject{}
//		private class TestReference
//		{
//			public TestReference(DataVar[] AArguments)
//			{
//				Arguments = AArguments;
//			}
//			
//			public DataVar[] Arguments;
//		}
//		
//		private int TestAsArgument(TestObject AObject, DataVar[] AArguments)
//		{
//			return AArguments.Length;
//		}
//		
//		private int TestAsReference(TestReference AReference)
//		{
//			return AReference.Arguments.Length;
//		}
//		
//		private void TestNativeMultiply()
//		{
//			int AValue = 5 * 5;
//			if (AValue != 25)
//				throw new Exception("Testing");
//		}
//		
//		private void TestPureStack(ServerProcess AProcess)
//		{
//			AProcess.Context.Push(new DataVar(AProcess.DataTypes.SystemInteger, Scalar.FromInt32(AProcess, 5)));
//			AProcess.Context.Push(new DataVar(AProcess.DataTypes.SystemInteger, Scalar.FromInt32(AProcess, 5)));
//			PureStackMultiply(AProcess);
//			AProcess.Context.Pop();
//		}
//		
//		private void PureStackMultiply(ServerProcess AProcess)
//		{
////			using (DataVar LRightValue = AProcess.Stack.Pop())
////			{
////				using (DataVar LLeftValue = AProcess.Stack.Pop())
////				{
////					AProcess.Stack.Push(new DataVar(Schema.IDataType.SystemInteger, Scalar.FromInt32(((Scalar)LLeftValue.Value).ToInt32() * ((Scalar)LRightValue.Value).ToInt32())));
////				}
////			}
//		}
//		
//		private void TestCallingWithScalars(ServerProcess AProcess)
//		{
//			MultiplyWithScalars(Scalar.FromInt32(AProcess, 5), Scalar.FromInt32(AProcess, 5));
//		}
//		
//		private Scalar MultiplyWithScalars(Scalar ALeftValue, Scalar ARightValue)
//		{
//			return Scalar.FromInt32(AProcess, ALeftValue.ToInt32() * ARightValue.ToInt32());
//		}
//		
//		private void TestCallingWithScalarDataVars(ServerProcess AProcess)
//		{
//			MultiplyWithScalarDataVars(new TestDataVar(AProcess.DataTypes.SystemInteger, Scalar.FromInt32(AProcess, 5)), new TestDataVar(AProcess.DataTypes.SystemInteger, Scalar.FromInt32(AProcess, 5)));
//		}
//		
//		private TestDataVar MultiplyWithScalarDataVars(ServerProcess AProcess, TestDataVar ALeftValue, TestDataVar ARightValue)
//		{
//			return new TestDataVar(AProcess.DataTypes.SystemInteger, Scalar.FromInt32(AProcess, ((Scalar)ALeftValue.Value).ToInt32() * ((Scalar)ARightValue.Value).ToInt32()));
//		}
//		
//		private void TestCallingWithObjectDataVars(ServerProcess AProcess)
//		{
//			MultiplyWithObjectDataVars(new TestDataVar(AProcess.DataTypes.SystemInteger, new TestReferenceTypeValue(AProcess.DataTypes.SystemInteger, 5)), new TestDataVar(AProcess.DataTypes.SystemInteger, new TestReferenceTypeValue(AProcess.DataTypes.SystemInteger, 5)));
//		}
//		
//		private TestDataVar MultiplyWithObjectDataVars(ServerProcess AProcess, TestDataVar ALeftValue, TestDataVar ARightValue)
//		{
//			return new TestDataVar(AProcess.DataTypes.SystemInteger, new TestReferenceTypeValue(AProcess.DataTypes.SystemInteger, (int)((TestReferenceTypeValue)ALeftValue.Value).Value * (int)((TestReferenceTypeValue)ARightValue.Value).Value));
//		}
//		
//		private void TestCallingWithReferenceTypeValues(ServerProcess AProcess)
//		{
//			MultiplyWithReferenceTypeValues(new ReferenceTypeValue(AProcess.DataTypes.SystemInteger, 5), new ReferenceTypeValue(AProcess.DataTypes.SystemInteger, 5)).Dispose();
//		}
//		
//		private ReferenceTypeValue MultiplyWithReferenceTypeValues(ServerProcess AProcess, ReferenceTypeValue ALeftValue, ReferenceTypeValue ARightValue)
//		{
//			ReferenceTypeValue LResult = new ReferenceTypeValue(AProcess.DataTypes.SystemInteger, (int)ALeftValue.Value * (int)ARightValue.Value);
//			ALeftValue.Dispose();
//			ARightValue.Dispose();
//			return LResult;
//		}
//		
//		private void TestCallingWithObjects()
//		{
//			MultiplyWithObjects(5, 5);
//		}
//		
//		private object MultiplyWithObjects(object ALeftValue, object ARightValue)
//		{
//			return (int)ALeftValue * (int)ARightValue;
//		}
//		
//		private void TestCallingWithIntegers2()
//		{
//			TestCallingWithIntegers();
//		}
//		
//		private void TestCallingWithIntegers3()
//		{
//			TestCallingWithIntegers2();
//		}
//		
//		private void TestCallingWithIntegers()
//		{
//			MultiplyWithIntegers(5, 5);
//		}
//		
//		private int MultiplyWithIntegers(int ALeftValue, int ARightValue)
//		{
//			return ALeftValue * ARightValue;
//		}
//		
//		// creating the variable
//		// evaluating the multiply
//		// evaluating literals
//		// assigning the variable
//		// evaluating the condition
//		// dropping the variable
//		
//		private PlanNode FBlockNode;
//		private void PrepareD4Multiply(Plan APlan)
//		{
//			BlockNode LNode = new BlockNode();
//			VariableNode LVarNode = new VariableNode();
//			LVarNode.VariableName = "AValue";
//			LVarNode.VariableType = APlan.Catalog.DataTypes.SystemInteger;
//			//LVarNode.Nodes.Add(Compiler.EmitBinaryNode(AProcess, new ValueNode(new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5)), Instructions.Multiplication, new ValueNode(new ReferenceTypeValue(Schema.IDataType.SystemInteger, 5))));
//			LVarNode.Nodes.Add(Compiler.EmitBinaryNode(APlan, new ValueNode(Scalar.FromInt32(APlan.ServerProcess, 5)), Instructions.Multiplication, new ValueNode(Scalar.FromInt32(APlan.ServerProcess, 5))));
//			LNode.Nodes.Add(LVarNode);
////			IfNode LIfNode = new IfNode();
////			LIfNode.Nodes.Add(Compiler.EmitBinaryNode(AProcess, new StackReferenceNode(Schema.IDataType.SystemInteger, 0), Instructions.NotEqual, new ValueNode(Scalar.FromInt32(25))));
////			RaiseNode LRaiseNode = new RaiseNode();
////			SystemErrorNode LErrorNode = new SystemErrorNode();
////			LErrorNode.Nodes.Add(new ValueNode(Scalar.FromString("Testing")));
////			LErrorNode.DetermineDataType(AProcess);
////			LRaiseNode.Nodes.Add(LErrorNode);
////			LIfNode.Nodes.Add(LRaiseNode);
////			LNode.Nodes.Add(LIfNode);
//			LNode.Nodes.Add(new DropVariableNode());
//			FBlockNode = LNode;
//		}
//		
//		private void TestD4Multiply(ServerProcess AProcess)
//		{
//			FBlockNode.Execute(AProcess);
//		}
//		
//		public override void DetermineDataType(Plan APlan)
//		{
//			PrepareD4Multiply(APlan);
//		}
//		
//		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
//		{
//			int ACount = ((Scalar)AArguments[0].Value).ToInt32();
//
//			// Get Timing for passing the arguments as parameters
//			DateTime LStartTime = DateTime.Now;
//			
//			//TestObject LObject = new TestObject();
//			for (int LIndex = 0; LIndex < ACount; LIndex++)
//				//TestNativeMultiply();
//				TestCallingWithObjectDataVars();
//				//TestPureStack(AProcess);
//				//if (TestAsArgument(LObject, AArguments) != AArguments.Count)
//				//	throw new RuntimeException("Testing");
//
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- Native time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
//
//			// Get Timing for passing the arguments as a reference on the plan
//			LStartTime = DateTime.Now;					
//			
//			//TestReference LReference = new TestReference(AArguments);
//			for (int LIndex = 0; LIndex < ACount; LIndex++)
//				//TestCallingWithIntegers();
//				//TestCallingWithObjects();
//				TestCallingWithScalarDataVars();
//				//TestPureStack(AProcess);
//				//TestD4Multiply(AProcess);
//				//if (TestAsReference(LReference) != AArguments.Count)
//				//	throw new RuntimeException("Testing");
//
//			TimeSpan LTotalTime = DateTime.Now - LStartTime;
////			#if TIMING
////			TimeSpan LOtherTime = LTotalTime - AProcess.CreateTime - AProcess.DropTime - AProcess.EvaluateTime - AProcess.AssignTime - AProcess.PassingTime;
////			decimal LCreatePercent = ((decimal)AProcess.CreateTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			decimal LDropPercent = ((decimal)AProcess.DropTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			decimal LEvaluatePercent = ((decimal)AProcess.EvaluateTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			decimal LAssignPercent = ((decimal)AProcess.AssignTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			decimal LPassingPercent = ((decimal)AProcess.PassingTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			decimal LOtherPercent = ((decimal)LOtherTime.Ticks / (decimal)LTotalTime.Ticks) * 100.0m;
////			#endif
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 total time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (LTotalTime).ToString()));
////			#if TIMING
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 create time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.CreateTime.ToString(), LCreatePercent.ToString()));
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 drop time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.DropTime.ToString(), LDropPercent.ToString()));
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Evaluate time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.EvaluateTime.ToString(), LEvaluatePercent.ToString()));
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Assign time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.AssignTime.ToString(), LAssignPercent.ToString()));
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Passing time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), AProcess.PassingTime.ToString(), LPassingPercent.ToString()));
////			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- D4 Other time: {1} ({2}%)", DateTime.Now.ToString("hh:mm:ss.ffff"), (LOtherTime).ToString(), LOtherPercent.ToString()));
////			#endif
//					
//			return null;
//		}
//	}
//	
//	public class TestNode : InstructionNode
//	{
//		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
//		{
//			int LCount = ((Scalar)AArguments[0].Value).ToInt32();
//			
//			List LList = new List();
//			for (int LIndex = 0; LIndex < 1000; LIndex++)
//				LList.Add(LIndex);
//
//			DateTime LStartTime = DateTime.Now;
//			
//			for (int LIndex = 0; LIndex < LCount; LIndex++)
//				foreach (object LObject in LList)
//					if (!(LObject is int))
//						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNameRequired);
//			
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- Enumerator time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
//
//			LStartTime = DateTime.Now;
//			
//			for (int LIndex = 0; LIndex < LCount; LIndex++)
//				for (int LObjectIndex = 0; LObjectIndex < LList.Count; LObjectIndex++) 
//					if (!(LList[LObjectIndex] is int))
//						throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNameRequired);
//			
//			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- For loop time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
//
//			return null;
//		}
//	}

	// operator TestNext(const AExpression : String, const AExpectedExpression : String) : Boolean
	public abstract class TestCursorNode : InstructionNode
	{
		protected abstract void InternalTest(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode);
		
		protected bool RowsSame(ServerProcess AProcess, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			DataVar LRowVar = new DataVar("ALeftRow", ARow.DataType, ARow);
			DataVar LExpectedRowVar = new DataVar("ARightRow", AExpectedRow.DataType, AExpectedRow);

			AProcess.Context.Push(LRowVar);
			try
			{
				AProcess.Context.Push(LExpectedRowVar);
				try
				{
					DataVar LResult = ARowEqualNode.Execute(AProcess);
					if (LResult.Value == null)
					{
						bool LSame = true;
						for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
						{
							LSame = ARow.HasValue(LIndex) == AExpectedRow.HasValue(LIndex);
							if (!LSame)
								break;
						}
						return LSame;
					}
					else
						return LResult.Value.AsBoolean;
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string AExpression = AArguments[0].Value.AsString;
			string AExpectedExpression = AArguments[1].Value.AsString;
			CursorNode LNode = (CursorNode)Compiler.BindNode(AProcess.Plan, Compiler.OptimizeNode(AProcess.Plan, Compiler.CompileCursor(AProcess.Plan, new Parser().ParseCursorDefinition(AExpression))));
			Table LTable = (Table)LNode.SourceNode.Execute(AProcess).Value;
			try
			{
				CursorNode LExpectedNode = (CursorNode)Compiler.BindNode(AProcess.Plan, Compiler.OptimizeNode(AProcess.Plan, Compiler.CompileCursor(AProcess.Plan, new Parser().ParseCursorDefinition(AExpectedExpression))));
				Table LExpectedTable = (Table)LExpectedNode.SourceNode.Execute(AProcess).Value;
				try
				{
					Row LRow = new Row(AProcess, LTable.DataType.CreateRowType());
					try
					{
						Row LExpectedRow = new Row(AProcess, LExpectedTable.DataType.CreateRowType());
						try
						{
							PlanNode LEqualNode = null;
							AProcess.Plan.Symbols.Push(new DataVar("ALeftRow", LRow.DataType, LRow));
							try
							{
								AProcess.Plan.Symbols.Push(new DataVar("ARightRow", LExpectedRow.DataType, LExpectedRow));
								try
								{
									AProcess.Plan.EnterRowContext();
									try
									{
										LEqualNode = Compiler.CompileExpression(AProcess.Plan, Compiler.BuildRowEqualExpression(AProcess.Plan, "ALeftRow", "ARightRow", LRow.DataType.Columns));
									}
									finally
									{
										AProcess.Plan.ExitRowContext();
									}
								}
								finally
								{
									AProcess.Plan.Symbols.Pop();
								}
							}
							finally
							{
								AProcess.Plan.Symbols.Pop();
							}
							
							InternalTest(AProcess, LTable, LExpectedTable, LRow, LExpectedRow, LEqualNode);
							return null;
						}
						finally
						{
							LExpectedRow.Dispose();
						}
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					LExpectedTable.Dispose();
				}
			}
			finally
			{
				LTable.Dispose();
			}
		}

		// Opens the cursor, walks through it, resets it, calls last, resets it and walks through it again.
		protected void TestNavigable(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			if (!ATable.BOF())
				throw new TestException("BOF() failed.");

			while (ATable.Next())
			{
				if (!AExpectedTable.Next())
					throw new TestException("Next() failed.");
					
				ATable.Select(ARow);
				AExpectedTable.Select(AExpectedRow);
				if (!RowsSame(AProcess, ARow, AExpectedRow, ARowEqualNode))
					throw new TestException("Next() returned an unexpected row.");
			}
			
			AExpectedTable.Next();
			
			if (!ATable.EOF())
				throw new TestException("EOF() failed.");
				
			if (ATable.EOF() != AExpectedTable.EOF())
				throw new TestException("Unexpected EOF().");
				
			ATable.Reset();
			AExpectedTable.Reset();
			
			if (!ATable.BOF())
				throw new TestException("Reset() failed.");
				
			ATable.Last();
			if (!ATable.EOF())
				throw new TestException("Last() failed.");
				
			ATable.Reset();

			while (ATable.Next())
			{
				if (!AExpectedTable.Next())
					throw new TestException("Next() after Reset() failed.");
					
				ATable.Select(ARow);
				AExpectedTable.Select(AExpectedRow);
				if (!RowsSame(AProcess, ARow, AExpectedRow, ARowEqualNode))
					throw new TestException("Next() after Reset() returned an unexpected row.");
			}

			AExpectedTable.Next();

			if (ATable.EOF() != AExpectedTable.EOF())
				throw new TestException("Unexpected EOF() after Reset().");
		}

		// Calls last and walks backwards through it, walks forward through it, then calls first and walks forward through it again.
		// Expects ATable and AExpectedTable to be on the EOF crack
		protected void TestBackwardsNavigable(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			DataVar LRowVar = new DataVar(ARow.DataType, ARow);
			DataVar LExpectedRowVar = new DataVar(AExpectedRow.DataType, AExpectedRow);
			
			if (!ATable.EOF())
				throw new TestException("EOF() failed.");
				
			while (ATable.Prior())
			{
				if (!AExpectedTable.Prior())
					throw new TestException("Prior() failed.");
					
				ATable.Select(ARow);
				AExpectedTable.Select(AExpectedRow);
				if (!RowsSame(AProcess, ARow, AExpectedRow, ARowEqualNode))
					throw new TestException("Prior() returned an unexpected row.");
			}
			
			AExpectedTable.Prior();
			
			if (!ATable.BOF())
				throw new TestException("BOF() failed.");
				
			if (ATable.BOF() != AExpectedTable.BOF())
				throw new TestException("Unexpected BOF().");
				
			while (ATable.Next())
			{
				if (!AExpectedTable.Next())
					throw new TestException("Next() after Prior() failed.");
					
				ATable.Select(ARow);
				AExpectedTable.Select(AExpectedRow);
				if (!RowsSame(AProcess, ARow, AExpectedRow, ARowEqualNode))
					throw new TestException("Next() after Prior() returned an unexpected row.");
			}
			
			ATable.First();
			if (!ATable.BOF())
				throw new TestException("First() failed.");
		}
		
		protected void TestSearchable(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			bool LFirst = true;
			Row LFirstKey = null;
			AExpectedTable.Reset();
			while (AExpectedTable.Next())
			{
				AExpectedTable.Select(ARow);
				if (!ATable.FindKey(ARow))
					throw new TestException("FindKey() failed.");
					
				if (LFirst)
				{
					if (ATable.Prior())
						throw new TestException("Prior() failed after FindKey() to first row.");
					ATable.Next();
					LFirstKey = ATable.GetKey();
					LFirst = false;
				}
			}
			try
			{
				if (ATable.Next())
					throw new TestException("Next() failed after FindKey() to last row.");
					
				if ((LFirstKey != null) && !ATable.FindKey(LFirstKey))
					throw new TestException("FindKey() failed to row returned from GetKey()");
			}
			finally
			{
				if (LFirstKey != null)
					LFirstKey.Dispose();
			}
				
			LFirst = true;
			AExpectedTable.Reset();
			while (AExpectedTable.Next())
			{
				AExpectedTable.Select(ARow);
				ATable.FindNearest(ARow);
				if (ATable.BOF() || ATable.EOF())
					throw new TestException("FindNearest() failed.");
					
				if (LFirst)
				{
					if (ATable.Prior())
						throw new TestException("Prior() failed after FindNearest() to first row.");
					ATable.Next();
					LFirst = false;
				}
			}
			
			if (ATable.Next())
				throw new TestException("Next() failed after FindNearest() to last row.");
		}
	}
	
	public class TestNavigableNode : TestCursorNode
	{
		protected override void InternalTest(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			TestNavigable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
		}
	}

	public class TestBackwardsNavigableNode : TestCursorNode
	{
		protected override void InternalTest(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			TestNavigable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
			TestBackwardsNavigable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
		}
	}

	public class TestSearchableNode : TestCursorNode
	{
		protected override void InternalTest(ServerProcess AProcess, Table ATable, Table AExpectedTable, Row ARow, Row AExpectedRow, PlanNode ARowEqualNode)
		{
			TestNavigable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
			TestBackwardsNavigable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
			TestSearchable(AProcess, ATable, AExpectedTable, ARow, AExpectedRow, ARowEqualNode);
		}
	}
	
	// operator TestParserEmitter(AScript : String);
	public class TestParserEmitterNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LSource = AArguments[0].Value.AsString;
			Parser LParser = new Parser();
			D4TextEmitter LEmitter = new D4TextEmitter();
			Statement LStatement = LParser.ParseScript(LSource, null);
			string LCanonicalSource = LEmitter.Emit(LStatement);
			LStatement = LParser.ParseScript(LCanonicalSource, null);
			string LTarget = LEmitter.Emit(LStatement);
			if (String.Compare(LCanonicalSource, LTarget) != 0)
				throw new TestException(String.Format("Parser/Emitter failed for input: \r\n{0}", LSource));
			return null;
		}
	}
}

