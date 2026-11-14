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
		public DotNetCommand(DotNetConnection connection, IDbCommand command) : base(connection)
		{
			_command = command;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				UnprepareCommand();
				
				if (_command != null)
				{
					_command.Dispose();
					_command = null;
				}
			}
			finally
			{
				base.Dispose(disposing);	
			}
		}
		
		public new DotNetConnection Connection { get { return (DotNetConnection)base.Connection; } }

		protected IDbCommand _command;
		
		protected void PrepareCommand(bool execute, SQLIsolationLevel isolationLevel)
		{
			_command.CommandText = PrepareStatement(Statement);
			if (CommandTimeout >= 0)
				_command.CommandTimeout = CommandTimeout;
			switch (CommandType)
			{
				case SQLCommandType.Statement : _command.CommandType = System.Data.CommandType.Text; break;
				case SQLCommandType.Table : _command.CommandType = System.Data.CommandType.TableDirect; break;
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
				SQLParameter parameter;
				for (int index = 0; index < _parameterIndexes.Length; index++)
				{
					parameter = Parameters[_parameterIndexes[index]];
					if ((parameter.Direction == SQLDirection.In) || (parameter.Direction == SQLDirection.InOut))
						if (parameter.Value == null)
							ParameterByIndex(index).Value = DBNull.Value;
						else
						{
							// TODO: Better story for the length of string parameters.  This usage prevents the use of prepared commands in general
							IDbDataParameter dataParameter = (IDbDataParameter)ParameterByIndex(index);
							if ((parameter.Value is string) && (dataParameter.Size < ((string)parameter.Value).Length))
								dataParameter.Size = ((string)parameter.Value).Length;
							dataParameter.Value = parameter.Value;
						}
				}
			}
		}

		protected virtual void GetParameters()
		{
			if (UseParameters)
			{
				SQLParameter parameter;
				for (int index = 0; index < _parameterIndexes.Length; index++)
				{
					parameter = Parameters[_parameterIndexes[index]];
					if ((parameter.Direction == SQLDirection.InOut) || (parameter.Direction == SQLDirection.Out) || (parameter.Direction == SQLDirection.Result))
					{
						if (ParameterByIndex(index).Value == DBNull.Value)
							parameter.Value = null;
						else
							parameter.Value = ParameterByIndex(index).Value;
					}
				}
			}
		}

		/// <summary> Access parameters by index through this routine to allow descendants to change the semantics. </summary>
		protected virtual IDataParameter ParameterByIndex(int index)
		{
			return (IDataParameter)_command.Parameters[index];
		}

		protected virtual void ClearParameters()
		{
			_command.Parameters.Clear();
		}

		#endregion

		protected override void InternalExecute()
		{
			PrepareCommand(true, SQLIsolationLevel.Serializable);
			try
			{
				SetParameters();
				_command.ExecuteNonQuery();
				GetParameters();
			}
			finally
			{
				UnprepareCommand();
			}
		}
		
		protected virtual CommandBehavior SQLCommandBehaviorToCommandBehavior(SQLCommandBehavior commandBehavior)
		{
			System.Data.CommandBehavior behavior = System.Data.CommandBehavior.Default | System.Data.CommandBehavior.SingleResult;
			
			if ((commandBehavior & SQLCommandBehavior.KeyInfo) != 0)
				behavior |= System.Data.CommandBehavior.KeyInfo;
				
			if ((commandBehavior & SQLCommandBehavior.SchemaOnly) != 0)
				behavior |= System.Data.CommandBehavior.SchemaOnly;
				
			return behavior;
		}
		
		protected override SQLCursor InternalOpen(SQLCursorType cursorType, SQLIsolationLevel isolationLevel)
		{
			PrepareCommand(false, isolationLevel);
			SetParameters();
			IDataReader cursor = _command.ExecuteReader(SQLCommandBehaviorToCommandBehavior(CommandBehavior));
			GetParameters();
			return new DotNetCursor(this, cursor);
		}
		
		protected override void InternalClose()
		{
			if (_command != null)
				UnprepareCommand();
		}
		
		public void Cancel()
		{
			if ((_command != null) && (Connection != null) && Connection.IsConnectionValid())
				_command.Cancel();
		}
	}
}
