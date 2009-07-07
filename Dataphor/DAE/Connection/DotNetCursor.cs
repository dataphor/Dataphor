/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;

namespace Alphora.Dataphor.DAE.Connection
{
	public class DotNetCursor : SQLCursor
	{
		public DotNetCursor(DotNetCommand ACommand, IDataReader ACursor) : base(ACommand)
		{
			FCursor = ACursor;
			FRecord = (IDataRecord)ACursor;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FCursor != null)
			{
                try
                {
                    FCursor.Dispose();
                }
                finally
                {
					FCursor = null;
					FRecord = null;
                }
			}
			base.Dispose(ADisposing);
		}
		
		protected IDataReader FCursor;
		protected IDataRecord FRecord;
		
		protected bool IsNull(object AValue)
		{
			return (AValue == null) || (AValue == DBNull.Value);
		}
		
		protected override SQLTableSchema InternalGetSchema()
		{
			DataTable LDataTable = FCursor.GetSchemaTable();
			SQLTableSchema LSchema = new SQLTableSchema();
			
			if (LDataTable.Rows.Count > 0)
			{
				SQLIndex LIndex = new SQLIndex("");
				LIndex.IsUnique = true;
				
				bool LContainsIsHidden = LDataTable.Columns.Contains("IsHidden");
				int LColumnIndex = 0;
				foreach (System.Data.DataRow LRow in LDataTable.Rows)
				{
					string LColumnName = (string)LRow["ColumnName"];

					object LValue = LRow["IsUnique"];
					bool LIsUnique = IsNull(LValue) ? false : (bool)LValue;

					LValue = LRow["IsKey"];
					bool LIsKey = IsNull(LValue) ? false : (bool)LValue;
					
					if (LIsUnique || LIsKey)
						LIndex.Columns.Add(new SQLIndexColumn(LColumnName));
					
					Type LDataType = FCursor.GetFieldType(LColumnIndex);
					
					LValue = LRow["ColumnSize"];
					int LLength = IsNull(LValue) ? 0 : (int)LValue;
					
					LValue = LRow["NumericPrecision"];
					int LPrecision = IsNull(LValue) ? 0 : Convert.ToInt32(LValue);
					
					LValue = LRow["NumericScale"];
					int LScale = IsNull(LValue) ? 0 : Convert.ToInt32(LValue);
					
					LValue = LRow["IsLong"];
					bool LIsLong = IsNull(LValue) ? false : (bool)LValue;

					bool LIsHidden = false;
					if (LContainsIsHidden)
					{
						LValue = LRow["IsHidden"];
						LIsHidden = IsNull(LValue) ? false : (bool)LValue;
					}

                    if (!LIsHidden)
    					LSchema.Columns.Add(new SQLColumn(LColumnName, new SQLDomain(LDataType, LLength, LPrecision, LScale, LIsLong)));
					
					LColumnIndex++;
				}

				if (LIndex.Columns.Count > 0)
					LSchema.Indexes.Add(LIndex);
			}
			else
			{
				for (int LIndex = 0; LIndex < FCursor.FieldCount; LIndex++)
					LSchema.Columns.Add(new SQLColumn(FCursor.GetName(LIndex), new SQLDomain(FCursor.GetFieldType(LIndex), 0, 0, 0, false)));
			}
			
			return LSchema;
		}
		
		protected override int InternalGetColumnCount()
		{
			return ((System.Data.Common.DbDataReader)FCursor).VisibleFieldCount;	// Visible field count hides internal fields like "rowstat" returned by server side cursors
		}
		
		protected override bool InternalNext()
		{
			return FCursor.Read();
		}
		
		protected override object InternalGetColumnValue(int AIndex)
		{
			return FRecord.GetValue(AIndex);
		}
		
		protected override bool InternalIsNull(int AIndex)
		{
			return FRecord.IsDBNull(AIndex);
		}
		
		protected override bool InternalIsDeferred(int AIndex)
		{
			switch (FRecord.GetDataTypeName(AIndex).ToLower())
			{
				case "image": 
					return true;
				default: return false;
			}
		}
		
		protected override Stream InternalOpenDeferredStream(int AIndex)
		{
			switch (FRecord.GetDataTypeName(AIndex).ToLower())
			{
				case "image": 
					long LLength = FRecord.GetBytes(AIndex, 0, null, 0, 0);
					if (LLength > Int32.MaxValue)
						throw new ConnectionException(ConnectionException.Codes.DeferredOverflow);
						
					byte[] LData = new byte[(int)LLength];
					FRecord.GetBytes(AIndex, 0, LData, 0, LData.Length);
					return new MemoryStream(LData, 0, LData.Length, true, true);	// Must use this overload or GetBuffer() will not be accessible

				default: throw new ConnectionException(ConnectionException.Codes.NonDeferredDataType, FRecord.GetDataTypeName(AIndex));
			}
		}
	}
}
