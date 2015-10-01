/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Language.D4;
using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Client;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Frontend.Client
{
	public abstract class DataAction : Action, ISourceReference, IDataAction
	{
		/// <remarks> Dereference source on dispose. </remarks>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}
		
		private ISource _source;
		/// <remarks> Hooks and unhooks SourceDataChanged and SourceDisposed to the Sources event hooks. </remarks>
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The source that the data action will work on.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null)
					{
						_source.StateChanged -= new DataLinkHandler(SourceDataChanged);
						_source.DataChanged -= new DataLinkHandler(SourceDataChanged);
						_source.Disposed -= new EventHandler(SourceDisposed);
					}
					_source = value;
					if (_source != null)
					{
						_source.StateChanged += new DataLinkHandler(SourceDataChanged);
						_source.DataChanged += new DataLinkHandler(SourceDataChanged);
						_source.Disposed += new EventHandler(SourceDisposed);
					}
					EnabledChanged();
				}
			}
		}
		
		/// <remarks> Checks to see if the enabled state has changed. </remarks>
		protected virtual void SourceDataChanged(DataLink link, DataSet dataSet)
		{
			EnabledChanged();
		}
		
		/// <remarks> Enabled if base.Enabled and the source exists, is active and valid. </remarks>
		public override bool GetEnabled()
		{
			return base.GetEnabled() && (_source != null) && (_source.DataView != null) && _source.DataView.Active; //IsValid();
		}
			
		/// <remarks> Dereference source on dispose. </remarks>
		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}
	}
	
	public class SourceAction : DataAction, ISourceAction
    {
		private SourceActions _action = SourceActions.First;
		[Description("The action type that will be performed against the data source.")]
		public SourceActions Action
		{
			get { return _action; }
			set 
			{ 
				if (_action != value)
				{
					_action = value;
					EnabledChanged();
				}
			}
		}
		
		/// <summary> Updates enabled when first or last row is selected. </summary>
		public override bool GetEnabled()
		{
			if (!base.GetEnabled())
				return false;
			switch (_action)
			{
				case SourceActions.First:
				case SourceActions.Prior:
					return !Source.DataView.BOF;
				case SourceActions.Next:
				case SourceActions.Last:
					return !Source.DataView.EOF;
				case SourceActions.Append:
				case SourceActions.Edit:
				case SourceActions.Insert:
				case SourceActions.Refresh:
					return Source.DataView.State == DataSetState.Browse;
				case SourceActions.Cancel:
				case SourceActions.Post:
				case SourceActions.RequestSave:
				case SourceActions.Validate:
					return (Source.DataView.State == DataSetState.Edit) || (Source.DataView.State == DataSetState.Insert);
				case SourceActions.Delete:
					return (Source.DataView.State == DataSetState.Browse) || !Source.DataView.IsEmpty();
				case SourceActions.Close:
					return Source.DataView.State != DataSetState.Inactive;
				case SourceActions.Open:
					return Source.DataView.State == DataSetState.Inactive;
				case SourceActions.PostDetails:
					return !Source.DataView.IsEmpty();
                case SourceActions.PostIfModified:
                    return (Source.IsModified && ((Source.DataView.State == DataSetState.Edit) || (Source.DataView.State == DataSetState.Insert)));
				default:
					return false;	// to keep the compiler happy
			}
		}
		
		/// <remarks> Creates and sends a ViewActionEvent. </remarks>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if (Source != null)
				Source.HandleEvent(new ViewActionEvent(_action));
		}
    }

	public class FindAction : DataAction, IFindAction
	{
		private FindActionMode _mode;
		[DefaultValue(FindActionMode.Nearest)]
		[Description("Determines the method used to find the row.")]
		public FindActionMode Mode
		{
			get { return _mode; }
			set { _mode = value; }
		}

		private string _columnName = String.Empty;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.ColumnNameConverter,Alphora.Dataphor.Frontend.Client")]
		[DefaultValue("")]
		[Description("The column name to search by.")]
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value; }
		}

		private string _value = String.Empty;
		[DefaultValue("")]
		[Description("The value to search for.")]
		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		/// <remarks> Performs the search. </remarks>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if ((Source != null) && (Source.DataView != null))
			{
				DAE.Schema.Order saveOrder = null;
				if ((Source.Order == null) || (Source.Order.Columns.Count != 1) || (Source.Order.Columns[0].Column.Name != _columnName))
				{
					saveOrder = Source.Order;
					Source.OrderString = String.Format("order {{ {0} }}", _columnName);
				}
				try
				{
					DAE.Schema.RowType rowType = new DAE.Schema.RowType();
					rowType.Columns.Add(new DAE.Schema.Column(_columnName, (Source.DataView.TableType.Columns[_columnName]).DataType));
					using (DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(Source.DataView.Process.ValueManager, rowType))
					{
						((DAE.Runtime.Data.Scalar)row.GetValue(_columnName)).AsString = _value;
						if (Mode == FindActionMode.Nearest)
							Source.DataView.FindNearest(row);
						else
						{
							bool found = Source.DataView.FindKey(row);
							if (!found && (Mode == FindActionMode.ExactOnly))
								throw new ClientException(ClientException.Codes.ValueNotFound, _value);
						}
					}
				}
				finally
				{
					if (saveOrder != null)
						Source.Order = saveOrder;
				}
			}
		}
	}

	public class DataScriptAction : Action, IDataScriptAction
	{
		protected override void Dispose(bool disposing)
		{
			EnlistWith = null;
			base.Dispose(disposing);
		}

		// Script

		private string _script = String.Empty;
		[DefaultValue("")]
		[Description("The D4 script to run.  This script will be parameterized by any parameters specified using DataArgument child nodes.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Script
		{
			get { return _script; }
			set { _script = (value == null ? String.Empty : value); }
		}

		// EnlistWith

		private ISource _enlistWith;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("A Source node to enlist with for application transactions.  If the EnlistWith source is in an application transaction, this script will run within that transaction.")]
		public ISource EnlistWith
		{
			get { return _enlistWith; }
			set 
			{ 
				if (_enlistWith != value)
				{
					if (_enlistWith != null)
						_enlistWith.Disposed -= new EventHandler(EnlistWithDisposed);
					_enlistWith = value; 
					if (_enlistWith != null)
						_enlistWith.Disposed += new EventHandler(EnlistWithDisposed);
				}
			}
		}

		private void EnlistWithDisposed(object sender, EventArgs args)
		{
			EnlistWith = null;
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			return typeof(IBaseArgument).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}

		// Action

		/// <summary> Runs script on the local server. </summary>
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			DAE.Runtime.DataParams localParamsValue = BaseArgument.CollectArguments(this);

			if (_script != String.Empty)
			{
			
				Guid enlistWithATID = Guid.Empty;
				
				if ((_enlistWith != null) && (_enlistWith.DataView != null) && _enlistWith.DataView.Active && (_enlistWith.DataView.ApplicationTransactionServer != null))
					enlistWithATID = _enlistWith.DataView.ApplicationTransactionServer.ApplicationTransactionID;
				
				DAE.IServerProcess process = HostNode.Session.DataSession.ServerSession.StartProcess(new DAE.ProcessInfo(HostNode.Session.DataSession.ServerSession.SessionInfo));
				try
				{
					if (enlistWithATID != Guid.Empty)
						process.JoinApplicationTransaction(enlistWithATID, false);
						
					ErrorList errors = new ErrorList();

					DAE.IServerScript script = process.PrepareScript(_script);
					try
					{
						foreach (DAE.IServerBatch batch in script.Batches)
							if (batch.IsExpression())
							{
								DAE.IServerExpressionPlan plan = batch.PrepareExpression(localParamsValue);
								try
								{
									errors.AddRange(plan.Messages);

									if (plan.DataType is DAE.Schema.TableType)
										plan.Close(plan.Open(localParamsValue));
									else
										plan.Evaluate(localParamsValue).Dispose();
								}
								finally
								{
									batch.UnprepareExpression(plan);
								}
							}
							else
							{
								DAE.IServerStatementPlan plan = batch.PrepareStatement(localParamsValue);
								try
								{
									errors.AddRange(plan.Messages);

									plan.Execute(localParamsValue);
								}
								finally
								{
									batch.UnprepareStatement(plan);
								}
							}
					}
					finally
					{
						process.UnprepareScript(script);
					}
					
					HostNode.Session.ReportErrors(HostNode, errors);
					
				}
				finally
				{
					HostNode.Session.DataSession.ServerSession.StopProcess(process);
				}

				BaseArgument.ApplyArguments(this, localParamsValue);
			}
		}
	}

	[Description("Executes an action if the D4 condition evaluates to true.")]
	public class DataConditionalAction : BaseConditionalAction
	{
		[Description("The boolean expression to evaluate.  This script will be parameterized by any parameters specified using DataArgument child nodes.")]
		[Editor("Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor,Alphora.Dataphor.DAE.Client.Controls", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public override string Condition
		{
			get { return base.Condition; }
			set { base.Condition = value; }
		}

		/// <remarks> Only one IAction is allowed as a child action. 
		/// IBaseArgument Actions are allowed.</remarks>
		public override bool IsValidChild(Type childType)
		{
			if (typeof(IAction).IsAssignableFrom(childType))
				foreach (Node localNode in Children)
					if (localNode is IAction)
						return false;
			return (typeof(IAction).IsAssignableFrom(childType)) || typeof(IBaseArgument).IsAssignableFrom(childType);
		}	  
		
		protected override bool EvaluateCondition()
		{
			bool result = false;
			if (Condition != String.Empty)
			{
				DAE.Runtime.DataParams localParamsValue = BaseArgument.CollectArguments(this);
				DAE.IServerProcess process = HostNode.Session.DataSession.ServerSession.StartProcess(new DAE.ProcessInfo(HostNode.Session.DataSession.ServerSession.SessionInfo));
				try
				{
					ErrorList errors = new ErrorList();
					DAE.IServerScript script = process.PrepareScript(String.Format("select {0}", Condition));
					try
					{
						DAE.IServerBatch batch = script.Batches[0];
						DAE.IServerExpressionPlan plan = batch.PrepareExpression(localParamsValue);
						try
						{
							errors.AddRange(plan.Messages);
							using (IDataValue dataValue = plan.Evaluate(localParamsValue))
								result = dataValue == null ? false : (bool)dataValue.AsNative;
						}
						finally
						{
							batch.UnprepareExpression(plan);
						}
					}
					finally
					{
						process.UnprepareScript(script);
					}

					HostNode.Session.ReportErrors(HostNode, errors);
				}
				finally
				{
					HostNode.Session.DataSession.ServerSession.StopProcess(process);
				}

				BaseArgument.ApplyArguments(this, localParamsValue);
			}
			return result;
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.DataArgument')")]
	[DesignerCategory("Non Visual")]
	public abstract class BaseArgument : Node
	{
		public static DAE.Runtime.DataParams CollectArguments(INode node)
		{
			DAE.Runtime.DataParams paramsValue = null;
			if (node != null)
			{
				foreach (Node localNode in node.Children)
				{
					BaseArgument argument = localNode as BaseArgument;
					if (argument != null)
						argument.CollectArguments(ref paramsValue);
				}
			}
			return paramsValue;
		}
		
		protected abstract void CollectArguments(ref DAE.Runtime.DataParams paramsValue);

		public static void ApplyArguments(INode node, DAE.Runtime.DataParams paramsValue)
		{
			if (node != null)
			{
				foreach (Node localNode in node.Children)
				{
					BaseArgument argument = localNode as BaseArgument;
					if (argument != null)
						argument.ApplyArguments(paramsValue);
				}
			}
		}

		protected abstract void ApplyArguments(DAE.Runtime.DataParams paramsValue);

		public static void CollectDataSetParamGroup(INode node, List<DataSetParamGroup> groups)
		{
			foreach (INode localNode in node.Children)
			{
				BaseArgument argument = localNode as BaseArgument;
				if (argument != null)
					argument.CollectDataSetParamGroup(groups);
			}
		}

		protected abstract void CollectDataSetParamGroup(List<DataSetParamGroup> groups);
	}
	
	public class UserStateArgument : BaseArgument, IUserStateArgument
	{
		public const string DefaultDefaultValue = "nil";
		
		// KeyName

		private string _keyName = String.Empty;
		[Description("The name of the UserState item of the current interface to get/set for this param.")]
		public string KeyName
		{
			get { return _keyName; }
			set { _keyName = (value == null ? String.Empty : value); }
		}
		
		// DefaultValue

		private string _defaultValue = DefaultDefaultValue;
		[Description("The default value to get and/or set if the key name isn't set or doesn't reference an existing item. This value is expected to be a D4 literal (e.g. 'string', nil, etc.).")]
		[DefaultValue(DefaultDefaultValue)]
		public string DefaultValue
		{
			get { return _defaultValue; }
			set { _defaultValue = (value == null ? String.Empty : value); }
		}

		// ParamName

		private string _paramName = String.Empty;
		[Description("The name of the parameter produced by this argument.")]
		public string ParamName
		{
			get { return _paramName; }
			set { _paramName = (value == null ? String.Empty : value); }
		}

		// Modifier

		private DAE.Language.Modifier _modifier = DAE.Language.Modifier.Const;
		[DefaultValue(DAE.Language.Modifier.Const)]
		[Description(@"The ""direction"" of the parameters (In, Out, Var, Const)")]
		public DAE.Language.Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}

		protected override void CollectArguments(ref DAE.Runtime.DataParams paramsValue)
		{
			if (!(String.IsNullOrEmpty(_keyName) && String.IsNullOrEmpty(_defaultValue)) && !String.IsNullOrEmpty(_paramName))
			{
				DAE.Schema.IDataType type;
				object value;
				GetTypeAndValue(out type, out value);

				if (type != null)
				{
					if (paramsValue == null)
						paramsValue = new DAE.Runtime.DataParams();
					paramsValue.Add
					(
						new DAE.Runtime.DataParam
						(
							_paramName,
							type,
							Modifier,
							value
						)
					);
				}
			}
		}

		private void GetTypeAndValue(out DAE.Schema.IDataType LType, out object LValue)
		{
			LType = null;
			LValue = null;
			var interfaceValue = FindParent(typeof(IInterface)) as IInterface;
			if (interfaceValue == null || String.IsNullOrEmpty(_keyName) || !interfaceValue.UserState.ContainsKey(_keyName))
			{
				// Get from the default value by parsing
				var expression = new Alphora.Dataphor.DAE.Language.D4.Parser().ParseExpression(_defaultValue);
				if (!(expression is ValueExpression))
					throw new ClientException(ClientException.Codes.ValueExpressionExpected);
				var process = HostNode.Session.DataSession.UtilityProcess;
				var source = (ValueExpression)expression;
				switch (source.Token)
				{
					case TokenType.Boolean:
						LType = process.DataTypes.SystemBoolean;
						LValue = (bool)source.Value;
						break;
					case TokenType.Decimal:
						LType = process.DataTypes.SystemDecimal;
						LValue = (decimal)source.Value;
						break;
					case TokenType.Integer:
						LType = process.DataTypes.SystemInteger;
						LValue = (int)source.Value;
						break;
					case TokenType.Money:
						LType = process.DataTypes.SystemMoney;
						LValue = (decimal)source.Value;
						break;
					case TokenType.Nil:
						LType = process.DataTypes.SystemNilGeneric;
						LValue = null;
						break;
					case TokenType.String:
						LType = process.DataTypes.SystemString;
						LValue = (string)source.Value;
						break;
				}
			}
			else
			{
				// Get from user state
				LValue = interfaceValue.UserState[_keyName];
				LType = LValue == null ? null : DataSession.ScalarTypeFromNativeType(HostNode.Session.DataSession.UtilityProcess, LValue.GetType());
			}
		}

		protected override void ApplyArguments(DAE.Runtime.DataParams paramsValue)
		{
			if
			(
				(Modifier == DAE.Language.Modifier.Out || Modifier == DAE.Language.Modifier.Var)
					&& !(String.IsNullOrEmpty(_keyName) && String.IsNullOrEmpty(_defaultValue)) 
					&& !String.IsNullOrEmpty(_paramName)
			)
			{
				var interfaceValue = FindParent(typeof(IInterface)) as IInterface;
				if (interfaceValue == null || String.IsNullOrEmpty(_keyName) || !interfaceValue.UserState.ContainsKey(_keyName))
				{
					var param = paramsValue[_paramName];
					TokenType tokenType = TokenType.EOF;
					if (param.Value == null)
						tokenType = TokenType.Nil;
					else
					{
						switch (param.DataType.Name)
						{
							case "System.Boolean": tokenType = TokenType.Boolean; break;
							case "System.Decimal": tokenType = TokenType.Decimal; break;
							case "System.Integer": tokenType = TokenType.Integer; break;
							case "System.Money": tokenType = TokenType.Money; break;
							case "System.String": tokenType = TokenType.String; break;
						}
					}
					if (tokenType != TokenType.EOF)
					{
						var expression = new ValueExpression(param.Value, tokenType);
						var emitter = new Alphora.Dataphor.DAE.Language.D4.D4TextEmitter();
						DefaultValue = emitter.Emit(expression);
					}
				}
				else
					interfaceValue.UserState[_keyName] = paramsValue[_paramName].Value;
			}
		}

		protected override void CollectDataSetParamGroup(List<DataSetParamGroup> groups)
		{
			DAE.Schema.IDataType type;
			object value;
			GetTypeAndValue(out type, out value);

			if (type != null)
			{
				var group = new DataSetParamGroup();
				group.Params.Add
				(
					new DataSetParam
					{
						Name = _paramName,
						DataType = type,
						Modifier = Modifier,
						Value = value
					}
				);
				groups.Add(group);
			}
		}
	}

	public class DataArgument : BaseArgument, IDataArgument
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}

		// Source

		private ISource _source;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("The data source to use as arguments to the script.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
				{
					if (_source != null)
						_source.Disposed -= new EventHandler(SourceDisposed);
					_source = value;
					if (_source != null)
						_source.Disposed += new EventHandler(SourceDisposed);
				}
			}
		}

		private void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		// Modifier

		private DAE.Language.Modifier _modifier = DAE.Language.Modifier.Const;
		[DefaultValue(DAE.Language.Modifier.Const)]
		[Description(@"The ""direction"" of the parameters (In, Out, Var, Const)")]
		public DAE.Language.Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}

		// Columns

		private string _columns = String.Empty;
		[Description("Comma or semicolon separated list of column names or parameter=name pairs.")]
		public string Columns
		{
			get { return _columns; }
			set { _columns = (value == null ? String.Empty : value); }
		}
		
		protected override void CollectArguments(ref DAE.Runtime.DataParams paramsValue)
		{
			if ((Source != null) && (Source.DataView != null) && Source.DataView.Active)
			{
				string[] paramNames;
				DAE.Schema.Column[] columns;

				GetParams(out paramNames, out columns);

				for (int i = 0; i < columns.Length; i++)
				{
					// Create the collection when we find the first item to add to it
					if (paramsValue == null)
						paramsValue = new DAE.Runtime.DataParams();

					DAE.Schema.Column column = columns[i];
					DataField field = Source.DataView.Fields[column.Name];

					paramsValue.Add
					(
						new DAE.Runtime.DataParam
						(
							paramNames[i],
							column.DataType,
							Modifier,
							Source.IsEmpty ? null : (field.HasValue() ? field.AsNative : null)
						)
					);
				}
			}
		}

		protected override void ApplyArguments(DAE.Runtime.DataParams paramsValue)
		{
			if
			(
				(Modifier == DAE.Language.Modifier.Out || Modifier == DAE.Language.Modifier.Var)
					&& (Source != null)
					&& (Source.DataView != null)
					&& Source.DataView.Active
					&& !Source.DataView.IsReadOnly
			)
			{
				string[] paramNames;
				DAE.Schema.Column[] columns;

				GetParams(out paramNames, out columns);

				for (int i = 0; i < columns.Length; i++)
				{
					DAE.Runtime.DataParam param = paramsValue[paramNames[i]];
					if (((param.Modifier == DAE.Language.Modifier.Out) || (param.Modifier == DAE.Language.Modifier.Var)) && !Source.IsEmpty)
					{
						DAE.Schema.Column column = columns[i];
						DataField field = Source.DataView.Fields[column.Name];
						field.AsNative = param.Value;
					}
				}
			}
		}

		protected override void CollectDataSetParamGroup(List<DataSetParamGroup> groups)
		{
			if ((Source != null) && (Source.DataView != null))
			{
				var paramGroup = new DataSetParamGroup();
				paramGroup.Source = Source.DataSource;

				string[] paramNames;
				DAE.Schema.Column[] columns;

				GetParams(out paramNames, out columns);

				for (int i = 0; i < columns.Length; i++)
				{
					DAE.Schema.Column column = columns[i];
					DataSetParam param = new DataSetParam();
					param.Name = paramNames[i];
					param.Modifier = Modifier;
					param.DataType = column.DataType;
					param.ColumnName = column.Name;
					paramGroup.Params.Add(param);
				}

				groups.Add(paramGroup);
			}
		}

		public void GetParams(out string[] LParamNames, out DAE.Schema.Column[] LColumns)
		{
			if (Columns != String.Empty)
			{
				string paramName;
				string columnName;
				LParamNames = Columns.Split(new char[] { ';', ',', '\n', '\r' });
				LColumns = new DAE.Schema.Column[LParamNames.Length];
				for (int i = 0; i < LParamNames.Length; i++)
				{
					paramName = LParamNames[i];
					int pos = paramName.IndexOf('=');
					if (pos >= 0)
					{
						columnName = paramName.Substring(pos + 1).Trim();
						paramName = paramName.Substring(0, pos).Trim();
					}
					else
					{
						paramName = paramName.Trim();
						columnName = paramName;
					}
					LColumns[i] = Source.DataView.TableType.Columns[columnName];
					LParamNames[i] = paramName;
				}
			}
			else
			{
				LColumns = new DAE.Schema.Column[Source.DataView.TableType.Columns.Count];
				Source.DataView.TableType.Columns.CopyTo(LColumns, 0);
				LParamNames = new string[LColumns.Length];
				for (int i = 0; i < LColumns.Length; i++)
					LParamNames[i] = LColumns[i].Name;
			}
		}
	}

	public class SetArgumentsFromParamsAction : Action, ISetArgumentsFromParams
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			var localParamsValue = new Alphora.Dataphor.DAE.Runtime.DataParams();
			var process = HostNode.Session.DataSession.UtilityProcess;
			foreach (KeyValuePair<string, object> entry in paramsValue)
			{
				localParamsValue.Add
				(
					new Alphora.Dataphor.DAE.Runtime.DataParam
					(
						entry.Key, 
						// If null, arbitrarily use string
						entry.Value == null 
							? process.DataTypes.SystemString 
							: Alphora.Dataphor.DAE.Client.DataSession.ScalarTypeFromNativeType(process, entry.Value.GetType()),
						DAE.Language.Modifier.Out,
						entry.Value
					)
				);
			} 
			BaseArgument.ApplyArguments(this, localParamsValue);
		}

		public override bool IsValidChild(Type childType)
		{
			return typeof(IBaseArgument).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}
	}

	public class SetArgumentsFromFormMainSourceAction : Action, ISetArgumentsFromFormMainSource
	{
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			var form = paramsValue["AForm"] as IFormInterface;
			if (form != null && form.MainSource != null && form.MainSource.Active && form.MainSource.DataView != null && form.MainSource.DataView.Active)
			{
				var localParamsValue = new Alphora.Dataphor.DAE.Runtime.DataParams();
				foreach (DataField field in form.MainSource.DataView.Fields)
					localParamsValue.Add(new DAE.Runtime.DataParam(field.Name, field.DataType, DAE.Language.Modifier.Out, field.AsNative));
				BaseArgument.ApplyArguments(this, localParamsValue);
			}
		}

		public override bool IsValidChild(Type childType)
		{
			return typeof(IBaseArgument).IsAssignableFrom(childType) || base.IsValidChild(childType);
		}
	}
}