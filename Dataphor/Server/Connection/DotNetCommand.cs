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
	public abstract class DotNetCommand : SQLCommand
	{
		public DotNetCommand(DotNetConnection AConnection, IDbCommand ACommand) : base(AConnection)
		{
			FCommand = ACommand;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				UnprepareCommand();
				
				if (FCommand != null)
				{
					FCommand.Dispose();
					FCommand = null;
				}
			}
			finally
			{
				base.Dispose(ADisposing);	
			}
		}
		
		public new DotNetConnection Connection { get { return (DotNetConnection)base.Connection; } }

		protected IDbCommand FCommand;
		
		protected void PrepareCommand(bool AExecute, SQLIsolationLevel AIsolationLevel)
		{
			FCommand.CommandText = PrepareStatement(Statement);
			if (CommandTimeout >= 0)
				FCommand.CommandTimeout = CommandTimeout;
			switch (CommandType)
			{
				case SQLCommandType.Statement : FCommand.CommandType = System.Data.CommandType.Text; break;
				case SQLCommandType.Table : FCommand.CommandType = System.Data.CommandType.TableDirect; break;
			}
			if (UseParameters)
				PrepareParameters();
			//FCommand.Prepare();//commented out because the MS Oracle Provider gets an iteration exception when prepared
			// Todo: put back in when MS Oracle can prepare
		}

		protected void UnprepareCommand()
		{
			ClearParameters();
		}
		
		protected override void InternalPrepare()
		{
		}
		
		protected override void InternalUnprepare()
		{
		}

		#region Parameters

		protected abstract void PrepareParameters();

		protected virtual void SetParameters()
		{
			if (UseParameters)
			{
				SQLParameter LParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LParameter.Direction == SQLDirection.In) || (LParameter.Direction == SQLDirection.InOut))
						if (LParameter.Value == null)
							ParameterByIndex(LIndex).Value = DBNull.Value;
						else
						{
							// TODO: Better story for the length of string parameters.  This usage prevents the use of prepared commands in general
							IDbDataParameter LDataParameter = (IDbDataParameter)ParameterByIndex(LIndex);
							if ((LParameter.Value is string) && (LDataParameter.Size < ((string)LParameter.Value).Length))
								LDataParameter.Size = ((string)LParameter.Value).Length;
							LDataParameter.Value = LParameter.Value;
						}
				}
			}
		}

		protected virtual void GetParameters()
		{
			if (UseParameters)
			{
				SQLParameter LParameter;
				for (int LIndex = 0; LIndex < FParameterIndexes.Length; LIndex++)
				{
					LParameter = Parameters[FParameterIndexes[LIndex]];
					if ((LParameter.Direction == SQLDirection.InOut) || (LParameter.Direction == SQLDirection.Out) || (LParameter.Direction == SQLDirection.Result))
					{
						if (ParameterByIndex(LIndex).Value == DBNull.Value)
							LParameter.Value = null;
						else
							LParameter.Value = ParameterByIndex(LIndex).Value;
					}
				}
			}
		}

		/// <summary> Access parameters by index through this routine to allow descendants to change the semantics. </summary>
		protected virtual IDataParameter ParameterByIndex(int AIndex)
		{
			return (IDataParameter)FCommand.Parameters[AIndex];
		}

		protected virtual void ClearParameters()
		{
			FCommand.Parameters.Clear();
		}

		#endregion

		protected override void InternalExecute()
		{
			PrepareCommand(true, SQLIsolationLevel.Serializable);
			try
			{
				SetParameters();
				FCommand.ExecuteNonQuery();
				GetParameters();
			}
			finally
			{
				UnprepareCommand();
			}
		}
		
		protected virtual CommandBehavior SQLCommandBehaviorToCommandBehavior(SQLCommandBehavior ACommandBehavior)
		{
			System.Data.CommandBehavior LBehavior = System.Data.CommandBehavior.Default | System.Data.CommandBehavior.SingleResult;
			
			if ((ACommandBehavior & SQLCommandBehavior.KeyInfo) != 0)
				LBehavior |= System.Data.CommandBehavior.KeyInfo;
				
			if ((ACommandBehavior & SQLCommandBehavior.SchemaOnly) != 0)
				LBehavior |= System.Data.CommandBehavior.SchemaOnly;
				
			return LBehavior;
		}
		
		protected override SQLCursor InternalOpen(SQLCursorType ACursorType, SQLIsolationLevel AIsolationLevel)
		{
			PrepareCommand(false, AIsolationLevel);
			SetParameters();
			IDataReader LCursor = FCommand.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));
			GetParameters();
			return new DotNetCursor(this, LCursor);
		}
		
		protected internal override void InternalClose()
		{
			if (FCommand != null)
				UnprepareCommand();
		}
		
		public void Cancel()
		{
			if ((FCommand != null) && (Connection != null) && Connection.IsConnectionValid())
				FCommand.Cancel();
		}
	}
}
