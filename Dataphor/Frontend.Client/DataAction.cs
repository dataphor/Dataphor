/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.ComponentModel;
using System.Drawing.Design;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Language.D4;
using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client
{
	public abstract class DataAction : Action, ISourceReference, IDataAction
	{
		/// <remarks> Dereference source on dispose. </remarks>
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}
		
		private ISource FSource;
		/// <remarks> Hooks and unhooks SourceDataChanged and SourceDisposed to the Sources event hooks. </remarks>
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("The source that the data action will work on.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					if (FSource != null)
					{
						FSource.StateChanged -= new DataLinkHandler(SourceDataChanged);
						FSource.DataChanged -= new DataLinkHandler(SourceDataChanged);
						FSource.Disposed -= new EventHandler(SourceDisposed);
					}
					FSource = value;
					if (FSource != null)
					{
						FSource.StateChanged += new DataLinkHandler(SourceDataChanged);
						FSource.DataChanged += new DataLinkHandler(SourceDataChanged);
						FSource.Disposed += new EventHandler(SourceDisposed);
					}
					EnabledChanged();
				}
			}
		}
		
		/// <remarks> Checks to see if the enabled state has changed. </remarks>
		protected virtual void SourceDataChanged(DataLink ALink, DataSet ADataSet)
		{
			EnabledChanged();
		}
		
		/// <remarks> Enabled if base.Enabled and the source exists, is active and valid. </remarks>
		public override bool GetEnabled()
		{
			return base.GetEnabled() && (FSource != null) && (FSource.DataView != null) && FSource.DataView.Active; //IsValid();
		}
			
		/// <remarks> Dereference source on dispose. </remarks>
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}
	}
	
	public class SourceAction : DataAction, ISourceAction
    {
		private SourceActions FAction = SourceActions.First;
		[Description("The action type that will be performed against the data source.")]
		public SourceActions Action
		{
			get { return FAction; }
			set 
			{ 
				if (FAction != value)
				{
					FAction = value;
					EnabledChanged();
				}
			}
		}
		
		/// <summary> Updates enabled when first or last row is selected. </summary>
		public override bool GetEnabled()
		{
			if (!base.GetEnabled())
				return false;
			switch (FAction)
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
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			if (Source != null)
				Source.HandleEvent(new ViewActionEvent(FAction));
		}
    }

	public class FindAction : DataAction, IFindAction
	{
		private FindActionMode FMode;
		[DefaultValue(FindActionMode.Nearest)]
		[Description("Determines the method used to find the row.")]
		public FindActionMode Mode
		{
			get { return FMode; }
			set { FMode = value; }
		}

		private string FColumnName = String.Empty;
		[TypeConverter(typeof(ColumnNameConverter))]
		[DefaultValue("")]
		[Description("The column name to search by.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value; }
		}

		private string FValue = String.Empty;
		[DefaultValue("")]
		[Description("The value to search for.")]
		public string Value
		{
			get { return FValue; }
			set { FValue = value; }
		}

		/// <remarks> Performs the search. </remarks>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			if ((Source != null) && (Source.DataView != null))
			{
				DAE.Schema.Order LSaveOrder = null;
				if ((Source.Order == null) || (Source.Order.Columns.Count != 1) || (Source.Order.Columns[0].Column.Name != FColumnName))
				{
					LSaveOrder = Source.Order;
					Source.OrderString = String.Format("order {{ {0} }}", FColumnName);
				}
				try
				{
					DAE.Schema.RowType LRowType = new DAE.Schema.RowType();
					LRowType.Columns.Add(new DAE.Schema.Column(FColumnName, (Source.DataView.TableType.Columns[FColumnName]).DataType));
					using (DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(Source.DataView.Process, LRowType))
					{
						((DAE.Runtime.Data.Scalar)LRow.GetValue(FColumnName)).AsString = FValue;
						if (Mode == FindActionMode.Nearest)
							Source.DataView.FindNearest(LRow);
						else
						{
							bool LFound = Source.DataView.FindKey(LRow);
							if (!LFound && (Mode == FindActionMode.ExactOnly))
								throw new ClientException(ClientException.Codes.ValueNotFound, FValue);
						}
					}
				}
				finally
				{
					if (LSaveOrder != null)
						Source.Order = LSaveOrder;
				}
			}
		}
	}

	public class DataScriptAction : Action, IDataScriptAction
	{
		protected override void Dispose(bool ADisposing)
		{
			EnlistWith = null;
			base.Dispose(ADisposing);
		}

		// Script

		private string FScript = String.Empty;
		[DefaultValue("")]
		[Description("The D4 script to run.  This script will be parameterized by any parameters specified using DataArgument child nodes.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Script
		{
			get { return FScript; }
			set { FScript = (value == null ? String.Empty : value); }
		}

		// EnlistWith

		private ISource FEnlistWith;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("A Source node to enlist with for application transactions.  If the EnlistWith source is in an application transaction, this script will run within that transaction.")]
		public ISource EnlistWith
		{
			get { return FEnlistWith; }
			set 
			{ 
				if (FEnlistWith != value)
				{
					if (FEnlistWith != null)
						FEnlistWith.Disposed -= new EventHandler(EnlistWithDisposed);
					FEnlistWith = value; 
					if (FEnlistWith != null)
						FEnlistWith.Disposed += new EventHandler(EnlistWithDisposed);
				}
			}
		}

		private void EnlistWithDisposed(object ASender, EventArgs AArgs)
		{
			EnlistWith = null;
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IDataArgument).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		// Action

		/// <summary> Runs script on the local server. </summary>
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			DAE.Runtime.DataParams LParams = DataArgument.CollectArguments(this);

			if (FScript != String.Empty)
			{
			
				Guid LEnlistWithATID = Guid.Empty;
				
				if ((FEnlistWith != null) && (FEnlistWith.DataView != null) && FEnlistWith.DataView.Active && (FEnlistWith.DataView.ApplicationTransactionServer != null))
					LEnlistWithATID = FEnlistWith.DataView.ApplicationTransactionServer.ApplicationTransactionID;
				
				DAE.IServerProcess LProcess = HostNode.Session.DataSession.ServerSession.StartProcess(new DAE.ProcessInfo(HostNode.Session.DataSession.ServerSession.SessionInfo));
				try
				{
					if (LEnlistWithATID != Guid.Empty)
						LProcess.JoinApplicationTransaction(LEnlistWithATID, false);
						
					ErrorList LErrors = new ErrorList();

					DAE.IServerScript LScript = LProcess.PrepareScript(FScript);
					try
					{
						foreach (DAE.IServerBatch LBatch in LScript.Batches)
							if (LBatch.IsExpression())
							{
								DAE.IServerExpressionPlan LPlan = LBatch.PrepareExpression(LParams);
								try
								{
									LErrors.AddRange(LPlan.Messages);

									if (LPlan.DataType is DAE.Schema.TableType)
										LPlan.Close(LPlan.Open(LParams));
									else
										LPlan.Evaluate(LParams).Dispose();
								}
								finally
								{
									LBatch.UnprepareExpression(LPlan);
								}
							}
							else
							{
								DAE.IServerStatementPlan LPlan = LBatch.PrepareStatement(LParams);
								try
								{
									LErrors.AddRange(LPlan.Messages);

									LPlan.Execute(LParams);
								}
								finally
								{
									LBatch.UnprepareStatement(LPlan);
								}
							}
					}
					finally
					{
						LProcess.UnprepareScript(LScript);
					}
					
					HostNode.Session.ReportErrors(HostNode, LErrors);
					
				}
				finally
				{
					HostNode.Session.DataSession.ServerSession.StopProcess(LProcess);
				}

				DataArgument.ApplyArguments(this, LParams);
			}
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.DataArgument')")]
	[DesignerCategory("Non Visual")]
	public class DataArgument : Node, IDataArgument
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}

		// Source

		private ISource FSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("The data source to use as arguments to the script.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
				{
					if (FSource != null)
						FSource.Disposed -= new EventHandler(SourceDisposed);
					FSource = value;
					if (FSource != null)
						FSource.Disposed += new EventHandler(SourceDisposed);
				}
			}
		}

		private void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		// Modifier

		private DAE.Language.Modifier FModifier = DAE.Language.Modifier.Const;
		[DefaultValue(DAE.Language.Modifier.Const)]
		[Description(@"The ""direction"" of the parameters (In, Out, Var, Const)")]
		public DAE.Language.Modifier Modifier
		{
			get { return FModifier; }
			set { FModifier = value; }
		}

		// Columns

		private string FColumns = String.Empty;
		[Description("Comma separated list of column names or parameter=name pairs.")]
		public string Columns
		{
			get { return FColumns; }
			set { FColumns = (value == null ? String.Empty : value); }
		}

		public static DAE.Runtime.DataParams CollectArguments(INode ANode)
		{
			DAE.Runtime.DataParams LParams = null;
			if (ANode != null)
			{
				foreach (Node LNode in ANode.Children)
				{
					DataArgument LArgument = LNode as DataArgument;
					if ((LArgument != null) && (LArgument.Source != null) && (LArgument.Source.DataView != null) && LArgument.Source.DataView.Active)
					{
						string[] LParamNames;
						DAE.Schema.Column[] LColumns;

						GetParams(LArgument, out LParamNames, out LColumns);

						for (int i = 0; i < LColumns.Length; i++)
						{
							// Create the collection when we find the first item to add to it
							if (LParams == null)
								LParams = new DAE.Runtime.DataParams();

							DAE.Schema.Column LColumn = LColumns[i];
							DataField LField = LArgument.Source.DataView.Fields[LColumn.Name];

							LParams.Add
							(
								new DAE.Runtime.DataParam
								(
									LParamNames[i], 
									LColumn.DataType, 
									LArgument.Modifier, 
									LArgument.Source.IsEmpty ? null : (LField.HasValue() ? LField.AsNative : null)
								)
							);
						}
					}
				}
			}
			return LParams;
		}

		public static void ApplyArguments(INode ANode, DAE.Runtime.DataParams AParams)
		{
			if (ANode != null)
			{
				foreach (Node LNode in ANode.Children)
				{
					DataArgument LArgument = LNode as DataArgument;
					if ((LArgument != null) && (LArgument.Source != null) && (LArgument.Source.DataView != null) && LArgument.Source.DataView.Active && !LArgument.Source.DataView.IsReadOnly)
					{
						string[] LParamNames;
						DAE.Schema.Column[] LColumns;

						GetParams(LArgument, out LParamNames, out LColumns);

						for (int i = 0; i < LColumns.Length; i++)
						{
							DAE.Runtime.DataParam LParam = AParams[LParamNames[i]];
							if (((LParam.Modifier == DAE.Language.Modifier.Out) || (LParam.Modifier == DAE.Language.Modifier.Var)) && !LArgument.Source.IsEmpty)
							{
								DAE.Schema.Column LColumn = LColumns[i];
								DataField LField = LArgument.Source.DataView.Fields[LColumn.Name];
								LField.AsNative = LParam.Value;
							}
						}
					}
				}
			}
		}

		public static void GetParams(IDataArgument LArgument, out string[] LParamNames, out DAE.Schema.Column[] LColumns)
		{
			if (LArgument.Columns != String.Empty)
			{
				string LParamName;
				string LColumnName;
				LParamNames = LArgument.Columns.Split(new char[] { ';', ',', '\n', '\r' });
				LColumns = new DAE.Schema.Column[LParamNames.Length];
				for (int i = 0; i < LParamNames.Length; i++)
				{
					LParamName = LParamNames[i];
					int LPos = LParamName.IndexOf('=');
					if (LPos >= 0)
					{
						LColumnName = LParamName.Substring(LPos + 1).Trim();
						LParamName = LParamName.Substring(0, LPos).Trim();
					}
					else
					{
						LParamName = LParamName.Trim();
						LColumnName = LParamName;
					}
					LColumns[i] = LArgument.Source.DataView.TableType.Columns[LColumnName];
					LParamNames[i] = LParamName;
				}
			}
			else
			{
				LColumns = new DAE.Schema.Column[LArgument.Source.DataView.TableType.Columns.Count];
				LArgument.Source.DataView.TableType.Columns.CopyTo(LColumns, 0);
				LParamNames = new string[LColumns.Length];
				for (int i = 0; i < LColumns.Length; i++)
					LParamNames[i] = LColumns[i].Name;
			}
		}
	}
}