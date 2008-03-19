/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using DAEClient = Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.COM
{
	[ComVisible(false)]
	[Guid("CD5D75E0-2D86-4BE2-988D-86584BCBCE59")]
	public struct SessionSetting
	{
		public string Key;
		public string Value;
	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("9D010EF1-B3B1-451d-BB4A-72CEF933C3FD")]
	public interface ISessionSettings
	{
		[DispId(1000)]
		void Add(string AKey, string AValue);
		[DispId(1100)]
		void Remove(string AKey);
		[DispId(1200)]
		void Clear();
		
		[DispId(2000)]
		string GetValue(string AKey);
		[DispId(2100)]
		int Count { get; }
		[DispId(2200)]
		SessionSetting[] Settings { get; }
	}

	// CoCreated
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("49BD8980-E046-4453-953D-DAA8BC07ED27")]
	public class SessionSettings : MarshalByRefObject, ISessionSettings
	{
		private Hashtable FSettings = new Hashtable();

		public void Add(string AKey, string AValue)
		{
			FSettings.Add(AKey, AValue);
		}

		public void Remove(string AKey)
		{
			FSettings.Remove(AKey);
		}

		public void Clear()
		{
			FSettings.Clear();
		}

		public string GetValue(string AKey)
		{
			return (string)FSettings[AKey];
		}

		public int Count 
		{ 
			get { return FSettings.Count; }
		}
		
		public SessionSetting[] Settings
		{
			get
			{
				SessionSetting[] LSettings = new SessionSetting[FSettings.Count];
				int i = 0;
				foreach (DictionaryEntry LEntry in FSettings)
				{
					LSettings[i].Key = (string)LEntry.Key;
					LSettings[i].Value = (string)LEntry.Value;
					i++;
				}
				return LSettings;
			}
		}
	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("D5934B08-5E2F-4712-A110-F00840682EAC")]
	public interface IConnector
	{
		[DispId(1000)]
		ISession ConnectSession(string AServerHost, int AServerPort, ISessionSettings ASessionSettings);
		[DispId(1100)]
		void DisconnectSession(ISession ASession);
	}

	// CoCreated
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("B9D367CF-BEA9-4894-AAF7-46FD4D8CC961")]
	public class Connector : IConnector
	{
		private SessionInfo SessionInfoFromSettings(ISessionSettings ASettings)
		{
			SessionSetting[] LSettings = ASettings.Settings;
			SessionInfo LSessionInfo = new SessionInfo();
			Type LSessionInfoType = LSessionInfo.GetType();
			PropertyInfo LPropInfo;
			for (int i = 0; i < LSettings.Length; i++)
			{
				LPropInfo = LSessionInfoType.GetProperty(LSettings[i].Key);
				if (LPropInfo == null)
					throw new COMClientException(COMClientException.Codes.InvalidSessionSetting, LSettings[i].Key);
				LPropInfo.SetValue(LSessionInfo, ReflectionUtility.StringToValue(LSettings[i].Value, LPropInfo.PropertyType), new object[] {});
			}
			return LSessionInfo;
		}

		public ISession ConnectSession(string AServerHost, int AServerPort, ISessionSettings ASessionSettings)
		{
			DAEClient.DataSession LSession = new DAEClient.DataSession();
			try
			{
				LSession.ServerUri = String.Format("tcp://{0}:{1}/Dataphor", AServerHost, AServerPort.ToString());
				LSession.SessionInfo = SessionInfoFromSettings(ASessionSettings);
				LSession.Open();
				return new Session(this, LSession);
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}

		public void DisconnectSession(ISession ASession)
		{
			((Session)ASession).DataSession.Close();
		}
	}


	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("9093FC41-5C57-4914-9A32-B0714DCB5051")]
	public interface IColumnDefinition
	{
		[DispId(1000)]
		string Name { get; }

		[DispId(2000)]
		string TypeName { get; }
	}

	[Guid("DC7DC97B-0435-42F4-8803-20D06CD2D996")]
	public class ColumnDefinition : IColumnDefinition
	{
		public ColumnDefinition(string AName, string ATypeName)
		{
			FName = AName;
			FTypeName = ATypeName;
		}

		private string FName;
		public string Name 
		{ 
			get { return FName; }
		}

		private string FTypeName;
		public string TypeName
		{
			get { return FTypeName; }
		}
	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("AADEED10-C701-4287-A0DC-8D41C9F0DCF6")]
	public interface IColumnDefinitions
	{
		[DispId(1000)]
		int Count { get; }

		[DispId(2000)]
		IColumnDefinition this[int AIndex] { get; }
	}

	[Guid("20D3F5FF-33C9-4E4C-8089-A49A0731A431")]
	public class ColumnDefinitions : IColumnDefinitions
	{
		public ColumnDefinitions(ColumnDefinition[] ADefinitions)
		{
			FDefinitions = ADefinitions;
		}

		private ColumnDefinition[] FDefinitions;

		public int Count
		{
			get { return FDefinitions.Length; }
		}

		public IColumnDefinition this[int AIndex]
		{
			get { return FDefinitions[AIndex]; }
		}
	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("F245E64A-AC44-4cb7-AFF1-300B53EEAD9E")]
	public interface ISession
	{
		[DispId(999)]
		IConnector Connector { get; }

		[DispId(1000)]
		IDataView CreateDataView();
		
		[DispId(2000)]
		IStatement CreateStatement();
		
		[DispId(3000)]
		void ExecuteScript(string AScript);

		[DispId(4000)]
		IColumnDefinitions GetColumnDefinitions(string AExpression);
	}

	[ClassInterface(ClassInterfaceType.None)]
	[Guid("ABA4606F-DA3D-47D7-BA08-BCDBD826DCEF")]
	public class Session : MarshalByRefObject, ISession
	{
		public Session(IConnector AConnector, DAEClient.DataSession ADataSession)
		{
			FConnector = AConnector;
			FDataSession = ADataSession;
		}

		private IConnector FConnector;
		public IConnector Connector
		{
			get { return FConnector; }
		}

		private DAEClient.DataSession FDataSession;
		[ComVisible(false)]
		public DAEClient.DataSession DataSession
		{
			get { return FDataSession; }
		}

		public IDataView CreateDataView()
		{
			DAEClient.DataView LView = new DAEClient.DataView();
			try
			{
				LView.Session = FDataSession;
				return new DataView(this, LView);
			}
			catch
			{
				LView.Dispose();
				throw;
			}
		}

		public IStatement CreateStatement()
		{
			return new Statement(this);
		}

		public void ExecuteScript(string AScript)
		{
			IServerProcess LProcess = FDataSession.ServerSession.StartProcess();
			try
			{
				IServerScript LScript = LProcess.PrepareScript(AScript);
				try
				{
					LScript.Execute(null);
				}
				finally
				{
					LProcess.UnprepareScript(LScript);
				}
			}
			finally
			{
				FDataSession.ServerSession.StopProcess(LProcess);
			}
		}

		public static DAE.Schema.ScalarType FindSystemType(DAE.Schema.ScalarType AType)
		{
			if (AType.IsSystem)
				return AType;
			if (AType.LikeType != null)
				return FindSystemType(AType.LikeType);
			throw new COMClientException(COMClientException.Codes.NativeTypeRequired, AType.Name);
		}

		public IColumnDefinitions GetColumnDefinitions(string AExpression)
		{
			IServerProcess LProcess = FDataSession.ServerSession.StartProcess();
			try
			{
				IServerExpressionPlan LPlan = LProcess.PrepareExpression(AExpression, null);
				try
				{
					DAE.Schema.TableType LTableType = (DAE.Schema.TableType)LPlan.DataType;
					ColumnDefinition[] LResult = new ColumnDefinition[LTableType.Columns.Count];
					for (int i = 0; i < LTableType.Columns.Count; i++)
					{
						LResult[i] = 
							new ColumnDefinition
							(
								LTableType.Columns[i].Name,
								FindSystemType((DAE.Schema.ScalarType)LTableType.Columns[i].DataType).Name
							);
					}
					return new ColumnDefinitions(LResult);
				}
				finally
				{
					LProcess.UnprepareExpression(LPlan);
				}
			}
			finally
			{
				FDataSession.ServerSession.StopProcess(LProcess);
			}
		}

	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("55B2A03F-057F-46BD-ACEC-F57A3D35744A")]
	public interface IDataView
	{
		[DispId(999)]
		ISession Session { get; }
		
		[DispId(1000)]
		string Expression { get; set; }
		
		[DispId(2000)]
		string OrderColumnNames { get; set; }
		
		[DispId(3000)]
		IDataView MasterDataView { get; set; }
		[DispId(3100)]
		string MasterKeyNames { get; set; }
		[DispId(3200)]
		string DetailKeyNames { get; set; }

		[DispId(4000)]
		Guid GetBookmark();
		[DispId(4100)]
		bool GotoBookmark(Guid ABookmark);
		[DispId(4200)]
		int CompareBookmarks(Guid ABookmark1, Guid ABookmark2);
		[DispId(4300)]
		void FreeBookmark(Guid ABookmark);
		[DispId(4400)]
		bool IsBookmarkValid(Guid ABookmark);

		[DispId(5000)]
		void Open();
		[DispId(5100)]
		void Close();
		[DispId(5200)]
		bool IsActive { get; }

		[DispId(6000)]
		bool BOF { get; }
		[DispId(6100)]
		bool EOF { get; }
		[DispId(6200)]
		bool MoveNext();
		[DispId(6300)]
		bool MovePrior();
		[DispId(6400)]
		void MoveFirst();
		[DispId(6500)]
		void MoveLast();

		[DispId(7000)]
		bool FindKey(object[] AKey);
		[DispId(7100)]
		void FindNearest(object[] APartialKey);

		[DispId(8000)]
		void Refresh(object[] ARecord);

		[DispId(9000)]
		object GetField(int AFieldIndex);
		[DispId(9100)]
		byte[] GetFieldBinary(int AFieldIndex);
		[DispId(9200)]
		void SetField(int AFieldIndex, object AValue);

		[DispId(10000)]
		void Insert();
		[DispId(10100)]
		void Edit();
		[DispId(10200)]
		void Delete();
		[DispId(10300)]
		void Post();
		[DispId(10400)]
		void Cancel();

		[DispId(11000)]
		IColumnDefinitions ColumnDefinitions { get; }
	}

	[ClassInterface(ClassInterfaceType.None)]
	[Guid("DBD74120-802C-46AC-8891-D9E5889AC2B8")]
	public class DataView : MarshalByRefObject, IDataView
	{
		public DataView(Session ASession, DAEClient.DataView AView)
		{
			FSession = ASession;
			FView = AView;
		}

		private DAEClient.DataView FView;
		[ComVisible(false)]
		public DAEClient.DataView View
		{
			get { return FView; }
		}

		private Session FSession;
		public ISession Session
		{
			get { return FSession; }
		}

		public string Expression
		{
			get { return FView.Expression; }
			set { FView.Expression = value; }
		}

		public string OrderColumnNames
		{
			get { return FView.OrderString; }
			set { FView.OrderString = value; }
		}

		private IDataView FMasterDataView;
		public IDataView MasterDataView
		{
			get { return FMasterDataView; }
			set
			{
				if (!Object.ReferenceEquals(value, FMasterDataView))
				{
					if (FView.MasterSource != null)
						FView.MasterSource.Dispose();
					FMasterDataView = value;
					if (value != null)
					{
						FView.MasterSource = new DAEClient.DataSource();
						FView.MasterSource.View = ((DataView)value).View;
					}
				}
			}
		}

		public string MasterKeyNames
		{
			get { return FView.MasterKeyNames; }
			set { FView.MasterKeyNames = value; }
		}

		public string DetailKeyNames
		{
			get { return FView.DetailKeyNames; }
			set { FView.DetailKeyNames = value; }
		}

		public Guid GetBookmark()
		{
			return FView.GetBookmark();
		}

		public bool GotoBookmark(Guid ABookmark)
		{
			try
			{
				FView.GotoBookmark(ABookmark);
			}
			catch
			{
				return false;
			}
			return true;
		}

		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			return FView.CompareBookmarks(ABookmark1, ABookmark2);
		}

		public void FreeBookmark(Guid ABookmark)
		{
			FView.FreeBookmark(ABookmark);
		}

		public bool IsBookmarkValid(Guid ABookmark)
		{
			return FView.IsBookmarkValid(ABookmark);
		}

		public void Open()
		{
			FView.Open();
		}

		public void Close()
		{
			FView.Close();
		}

		public bool IsActive 
		{ 
			get { return FView.Active; }
		}

		public bool BOF 
		{ 
			get { return FView.BOF; }
		}

		public bool EOF 
		{ 
			get { return FView.EOF; }
		}

		public bool MoveNext()
		{
			FView.Next();
			return !FView.EOF;
		}

		public bool MovePrior()
		{
			FView.Prior();
			return !FView.BOF;
		}

		public void MoveFirst()
		{
			FView.First();
		}

		public void MoveLast()
		{
			FView.Last();
		}

		private void CheckType(object AValue, Type AType)
		{
			if (AValue.GetType() != AType)
				throw new COMClientException(COMClientException.Codes.IncorrectType, AValue.GetType(), AType);
		}

		private DAE.Runtime.Data.Scalar ObjectToScalar(object AValue, DAE.Schema.ScalarType ADataType)
		{
			if (AValue != null)
				switch (AValue.GetType().Name)
				{
					case "String" : CheckType(AValue, typeof(string)); break;
					case "Decimal" : CheckType(AValue, typeof(decimal)); break;
					case "Int64" : CheckType(AValue, typeof(long)); break;
					case "Int32" : CheckType(AValue, typeof(int)); break;
					case "Int16" : CheckType(AValue, typeof(short)); break;
					case "Byte" : CheckType(AValue, typeof(byte)); break;
					case "Boolean" : CheckType(AValue, typeof(bool)); break;
					case "Guid" : CheckType(AValue, typeof(Guid)); break;
					case "TimeSpan" : CheckType(AValue, typeof(TimeSpan)); break;
					case "DateTime" : CheckType(AValue, typeof(DateTime)); break;
					case "Byte[]" : return new DAE.Runtime.Data.Scalar(FView.Process, ADataType, new MemoryStream((Byte[])AValue));
					default : throw new COMClientException(COMClientException.Codes.UnrecognizedType, AValue.GetType().Name);
				}
			return new DAE.Runtime.Data.Scalar(FView.Process, ADataType, AValue);
		}

		private DAE.Runtime.Data.Row RecordToRow(object[] ARecord)
		{
			DAE.Schema.RowType LRowType = new DAE.Schema.RowType();	
			DAE.Schema.OrderColumn LColumn;
			for (int i = 0; i < ARecord.Length; i++)
			{
				LColumn = FView.Order.Columns[i];
				LRowType.Columns.Add
				(
					new DAE.Schema.Column
					(
						LColumn.Column.Name, 
						(DAE.Schema.ScalarType)LColumn.Column.DataType
					)
				);
			}

			DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(FView.Process, LRowType);
			try
			{
				DAE.Runtime.Data.Scalar LScalar;
				for (int i = 0; i < LRow.DataType.Columns.Count; i++)
				{
					LScalar = ObjectToScalar(ARecord[i], (DAE.Schema.ScalarType)LRow.DataType.Columns[i].DataType);
					try
					{
						LRow[i] = LScalar;
					}
					finally
					{
						LScalar.Dispose();
					}
				}
				return LRow;
			}
			catch
			{
				LRow.Dispose();
				throw;
			}
		}

		public bool FindKey(object[] AKey)
		{
			return FView.FindKey(RecordToRow(AKey));
		}

		public void FindNearest(object[] APartialKey)
		{
			FView.FindNearest(RecordToRow(APartialKey));
		}

		public void Refresh(object[] ARecord)
		{
			if (ARecord != null)
				FView.Refresh(RecordToRow(ARecord));
			else
				FView.Refresh();
		}

		public object GetField(int AFieldIndex)
		{
			DAE.Runtime.Data.Scalar LValue = FView.Fields[AFieldIndex].Value;
			switch (LValue.DataType.Name)
			{
				case "System.String" :
				case "System.IString" :
				case "System.Name" : return LValue.ToString();
				case "System.Money" :
				case "System.Decimal" : return LValue.ToDecimal();
				case "System.Long" : return LValue.ToInt64();
				case "System.Integer" : return LValue.ToInt32();
				case "System.Short" : return LValue.ToInt16();
				case "System.Byte" : return LValue.ToByte();
				case "System.Boolean" : return LValue.ToBoolean();
				case "System.Guid" : return LValue.ToGuid();
				case "System.TimeSpan" : return LValue.ToTimeSpan();
				case "System.DateTime" : return LValue.ToDateTime();
				case "System.Binary" :
				default : return LValue.AsByteArray;
			}
		}

		public byte[] GetFieldBinary(int AFieldIndex)
		{
			return FView.Fields[AFieldIndex].Value.AsByteArray;
		}

		public void SetField(int AFieldIndex, object AValue)
		{
			DataField LField = FView.Fields[AFieldIndex];
			if (AValue == null)
				LField.ClearValue();
			else
			{
				using (DAE.Runtime.Data.Scalar LScalar = ObjectToScalar(AValue, (DAE.Schema.ScalarType)LField.DataType))
				{
					LField.Value = LScalar;
				};
			}
		}
		
		public void Insert()
		{
			FView.Insert();
		}

		public void Edit()
		{
			FView.Edit();
		}

		public void Delete()
		{
			FView.Delete();
		}

		public void Post()
		{
			FView.Post();
		}

		public void Cancel()
		{
			FView.Cancel();
		}

		private ColumnDefinitions FColumnDefinitions;
		public IColumnDefinitions ColumnDefinitions
		{ 
			get
			{
				if (FColumnDefinitions == null)
				{
					ColumnDefinition[] LDefinitions = new ColumnDefinition[FView.Fields.Count];
					for (int i = 0; i < FView.Fields.Count; i++)
					{
						LDefinitions[i] = 
							new ColumnDefinition
							(
								FView.Fields[i].ColumnName,
								Alphora.Dataphor.DAE.Client.COM.Session.FindSystemType((DAE.Schema.ScalarType)FView.Fields[i].DataType).Name
							);
					}
					FColumnDefinitions = new ColumnDefinitions(LDefinitions);
				}
				return FColumnDefinitions;
			}
		}
	}

//	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
//	[Guid("D876A177-498F-472F-A08E-BF9EDD3762BC")]
//	public interface IParam
//	{
//		string Name { get; set; }
//		string Type { get; set; }	// a .NET type name perhaps?
//		object Value { get; set; }
//	}

	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	[Guid("FCB104DD-726E-4B6A-A2DD-57EDC2B639B1")]
	public interface IStatement
	{
		[DispId(999)]
		ISession Session { get; }

		[DispId(1000)]
		string StatementText { get; set; }
		
		[DispId(2000)]
		void Execute();
		// TODO: params
	}

	[ClassInterface(ClassInterfaceType.None)]
	[Guid("4F13EDE0-F5A7-4801-9DDC-CE0AF197A9DE")]
	public class Statement : MarshalByRefObject, IStatement
	{
		public Statement(Session ASession)
		{
			FSession = ASession;
		}

		private Session FSession;
		public ISession Session
		{
			get { return FSession; }
		}

		private string FStatementText = String.Empty;
		public string StatementText
		{
			get { return FStatementText; }
			set { FStatementText = value; }
		}

		// TODO: Params

		public void Execute()
		{
			// TODO: implement Statement.Execute()
		}

	}

}
