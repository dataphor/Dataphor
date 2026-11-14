/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.Frontend.Client
{
	/**************  Nodes  **************/

	/// <summary> Data source component. </summary> <doc/>
	/// <remarks>
	///		Provides access to data specified by a D4 expression.  If the expression
	///		is blank, the source is disabled upon activation.
	///	</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface ISource : INode
	{
		/// <summary> Called when the source's active state changes. </summary>
		event DataLinkHandler ActiveChanged;

		/// <summary> Called when the source's state has changed. </summary>
		event DataLinkHandler StateChanged;

		/// <summary> Called when data changes except for data edits to the active row. </summary>
		event DataLinkHandler DataChanged;

		/// <summary> Called when column-level edits are made to the active row. </summary>
		event DataLinkFieldHandler RowChanging;

		/// <summary> Called after column-level edits are made to the active row. </summary>
		event DataLinkFieldHandler RowChanged;

		/// <summary> Called when determining the default values for a newly inserted row. </summary>
		event DataLinkHandler Default;

		/// <summary> Gets the dataphor <see cref="DataSource"/>. 
		/// </summary> <doc/>
		DataSource DataSource { get; }

		/// <summary> Gets the dataphor <see cref="DataView"/>. 
		/// </summary> <doc/>
		DataView DataView { get; }

		/// <summary> Called when DataView is updated. </summary>
		event EventHandler OnUpdateView;
		
		/// <summary> 
		/// Returns the <see cref="IServerProcess"/> reference that 
		/// this source is using. Only available while the source is active.
		/// </summary> <doc/>
		IServerProcess Process { get; }
		
		/// <summary> Provides access to the <see cref="Schema.TableVar"/> 
		/// reference describing the result set for this source.
		/// </summary> <doc/>
		Schema.TableVar TableVar { get; }
		
		/// <summary> Provides access to the <see cref="Schema.Order"/> 
		/// reference describing the order of the result set for this source.
		/// </summary> <doc/>
		Schema.Order Order { get; set; }
		
		/// <summary> Provides access to the order of the result set for this 
		/// source as a D4 order definition.
		/// </summary> <doc/>
		string OrderString { get; set; }
		
		/// <summary> Provides access to the <see cref="DataField"/> reference 
		/// for the specified column name, which can be used to get or set the 
		/// column value in various representations. 
		/// </summary> <doc/>
		DataField this[string AColumnName] { get; }
		
		/// <summary> Provides access to the <see cref="DataField"/> reference 
		/// for the specified column by ordinal position in the source, which 
		/// can be used to get or set the column value in various representations. 
		/// </summary> <doc/>
		DataField this[int AColumnIndex] { get; }
		
		/// <summary> A <see cref="DataSetState"/> value indicating the 
		/// current state of the source.
		/// </summary> <doc/>
		DataSetState State { get; }
		
		/// <summary> Indicates whether the source is positioned at the 
		/// beginning of the result set.
		/// </summary> <doc/>
		bool BOF { get; }
		
		/// <summary> Indicates whether the source is positioned at the end of 
		/// the result set.
		/// </summary> <doc/>
		bool EOF { get; }
		
		/// <summary> True if the result set is empty, false otherwise.
		/// </summary> <doc/>
		bool IsEmpty { get; }
		
		/// <summary> Positions the source on the first row in the result set.
		/// </summary> <doc/>
		void First();
		
		/// <summary> Positions the source on the prior row in the result set.
		/// </summary> <doc/>
		void Prior();
		
		/// <summary> Positions the source on the next row in the result set.
		/// </summary> <doc/>
		void Next();
		
		/// <summary> Positions the source on the last row in the result set.
		/// </summary> <doc/>
		void Last();
		
		/// <summary> Returns a <see cref="Row"/> that uniquely identifies 
		/// the current row in the result set.
		/// </summary> <doc/>
		DAE.Runtime.Data.IRow GetKey();
		
		/// <summary> Positions the source on the row matching the row given 
		/// in ARow. Returns true if a row is found, false otherwise. 
		/// If a row is not found, the source position is not changed.
		/// </summary> <doc/>
		bool FindKey(DAE.Runtime.Data.IRow AKey);
		
		/// <summary> Positions the source on the row most closely matching 
		/// the row given in ARow.</summary> <doc/>
		void FindNearest(DAE.Runtime.Data.IRow AKey);
		
		/// <summary> Refreshes the result set, repositioning the source 
		/// on the current row if possible.
		/// </summary> <doc/>
		void Refresh();
		
		/// <summary> Indicates whether or not the contents of the current 
		/// row have been modified.
		/// </summary> <doc/>
		bool IsModified { get; }
		
		/// <summary> Puts the source into insert state and defaults the 
		/// new column values. 
		/// </summary> <doc/>
		void Insert();
		
		/// <summary> Puts the source into edit state, allowing column 
		/// values in the current row to be changed. If the result set 
		/// is empty, this is equivalent to calling Insert.
		/// </summary> <doc/>
		void Edit();
		
		/// <summary> Saves any outstanding changes to the current row. 
		/// If this is a new row, it will be inserted, otherwise, an update will be used to save the data.</summary> <doc/>
		void Post();
		
		/// <summary> Cancels any outstanding changes to the current row. 
		/// </summary> <doc/>
		void Cancel();
		
		/// <summary> Deletes the current row in the result set.
		/// </summary> <doc/>
		void Delete();
		
		// Published

		/// <summary> The D4 expression to be used to select the data set. 
		/// </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		string Expression { get; set; }

		/// <summary> Represents the state of the data source. 
		/// </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		/// <remarks>
		///		When false, the source is not active and cannot be used. 
		///		If the expression is invalid, this source will become 
		///		disabled automatically.
		/// </remarks>
		bool Enabled { get; set; }

		/// <summary> The D4 filter expression to apply to the data source. 
		/// </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string Filter { get; set; }
		
		/// <summary>
		/// An optional set of parameters that can be passed directly to the DataView.
		/// </summary>
		DataParams Params { get; set; }

		/// <summary> A D4 script that will be executed source is activated 
		/// (before opening the expression cursor). </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string BeginScript { get; set; }

		/// <summary> A D4 script that will be executed when the source is 
		/// deactivated (after the expression cursor is closed). </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string EndScript { get; set; }

		/// <summary> When this is set, the source uses the data from the 
		/// surrogate source. </summary> <doc/>
		/// <value> <para>ISource: A source from the same node tree.</para>
		/// <para> Default: (None)</para></value>
		ISource Surrogate { get; set; }

		/// <summary> The key column(s) in the master source to use for the 
		/// master-detail relationship. </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks>Comma serparated list of column names</remarks>
		string MasterKeyNames { get; set; }

		/// <summary> The key column(s) in this source to use as the detail key for a master-detail relationship. </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks>Comma serparated list of column names</remarks>
		string DetailKeyNames { get; set; }

		/// <summary> When set, this source will be filtered and will re-query 
		/// as the master source navigates. </summary> <doc/>
		/// <value> <para>ISource: A source from the same node tree.</para>
		/// <para> Default: (None)</para></value>
		ISource Master { get; set; }
		
		/// <summary> Indicates whether the source should use a browse or 
		/// order by clause when requesting data. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		bool UseBrowse { get; set; }
		
		/// <summary> Indicates whether the source should use application 
		/// transactions. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		bool UseApplicationTransactions { get; set; }
		
		/// <summary> Indicates whether data in the source can be modified. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: False</para></value>
		bool IsReadOnly { get; set; }

		/// <summary> Indicates whether the source is to be used only for inserting data. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: False</para></value>
		bool IsWriteOnly { get; set; }

		/// <summary> Indicates whether or not the source will be refreshed after a call to post. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		bool RefreshAfterPost { get; set; }

		/// <summary> Indicates whether the source should enlist in the 
		/// application transaction of its master source. </summary> <doc/>
		/// <value><para>EnlistMode: Default|True|False</para>
		/// <para>Default: Default</para></value>
		EnlistMode ShouldEnlist { get; set; }
		
		/// <summary> Indicates the open state for the source. </summary> <doc/>
		/// <value><para>DataSetState: Browse|Edit|Insert|Inactive</para>
		/// <para>Default: Browse</para></value>
		DataSetState OpenState { get; set; }

		/// <summary> An action that will be executed when a different row in 
		/// the source is selected. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnChange { get; set; }

		/// <summary> An action that will be executed when the active property 
		/// of the source changes. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnActiveChange { get; set; }
		
		/// <summary> An action that will be executed when the state of the 
		/// source changes. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnStateChange { get; set; }
		
		/// <summary> An action that will be executed when any data value in 
		/// the active row of the source is changing. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnRowChanging { get; set; }

		/// <summary> An action that will be executed when any data value in 
		/// the active row of the source changes. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnRowChange { get; set; }

		/// <summary> An action that will be executed to allow for setting 
		/// of default values for a new row. Values set during this action 
		/// will not set the modified of the source. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnDefault { get; set; }

		/// <summary> An action that will be executed before the source 
		/// posts the active row. </summary> <doc/>
		/// <remarks> An exception here will prevent the source from 
		/// posting.</remarks>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnValidate { get; set; }

		/// <summary> An action that will be executed before the data set 
		/// opens. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeOpen { get; set; }

		/// <summary> An action that will be executed after the data set 
		/// opens. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterOpen { get; set; }

		/// <summary> An action that will be executed before the data set 
		/// closes. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeClose { get; set; }

		/// <summary> An action that will be executed after the data set 
		/// closes. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterClose { get; set; }

		/// <summary> An action that will be executed before the data set 
		/// enters insert state. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeInsert { get; set; }

		/// <summary> An action that will be executed after the data set 
		/// enters insert state. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterInsert { get; set; }

		/// <summary> An action that will be executed before the data set 
		/// enters edit state. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeEdit { get; set; }

		/// <summary> An action that will be executed after the data set 
		/// enters edit state. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterEdit { get; set; }

		/// <summary> An action that will be executed before a row in 
		/// the data set is deleted. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeDelete { get; set; }

		/// <summary> An action that will be executed after a row in 
		/// the data set is deleted. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterDelete { get; set; }

		/// <summary> An action that will be executed before the data 
		/// set is posted. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforePost { get; set; }

		/// <summary> An action that will be executed after the data 
		/// set is posted. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterPost { get; set; }

		/// <summary> An action that will be executed before the data 
		/// set is canceled. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction BeforeCancel { get; set; }

		/// <summary> An action that will be executed after the data 
		/// set is canceled. </summary> <doc/>
		/// <value><para>IAction: An action from the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction AfterCancel { get; set; }

		/// <summary>Determines whether the source will automatically 
		/// produce the where clause necessary to limit the result set 
		/// by the master source.</summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		/// <remarks> If false, the expression must contain 
		/// the necessary restrictions.  Current master data view values 
		/// are available as AMaster&lt;detail column name (with 
		/// qualifiers replaced with underscores)&gt; parameters within 
		/// the expression.</remarks>
		/// <example><para>With table called Phone and a master that has a column 
		/// named ID, the Expression could be written:</para>
		/// <para> Phone where Employee_ID = AMasterID</para></example>
		bool WriteWhereClause { get; set; }

		/// <summary>A custom D4 insert statement to be used to override the 
		/// default insert behavior of the source. Columns of the row to 
		/// be inserted can be accessed by name within the statement.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string InsertStatement { get; set; }

		/// <summary>A custom D4 update statement to be used to override 
		/// the default update behavior of the source. Columns of the row 
		/// to be updated can be accessed by name within the statement.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string UpdateStatement { get; set; }

		/// <summary>A custom delete statement to be used to override the 
		/// default delete behavior of the source. Columns of the row to 
		/// be deleted can be accessed by name within the statement.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string DeleteStatement { get; set; }

		/// <summary>Determines the behavior of the cursor with respect to 
		/// updates made after the cursor is opened.</summary> <doc/>
		/// <value><para>DAE.CursorType: Static|Dynamic</para>
		/// <para>Default: Dynamic</para></value>
		/// <remarks> <para>If the cursor type is dynamic, updates made through 
		/// the cursor will be visible.  If the cursor type is static, 
		/// updates will not be visible.</para>
		/// <para>For more information see
		/// <ulink url="D4LGDataManipulation-SelectStatement-CursorType.html" type="olinkb">
		/// Cursor Type</ulink>.</para>
		/// </remarks>
		DAE.CursorType CursorType { get; set; }

		/// <summary>The isolation level for transactions performed by this 
		/// source.</summary> <doc/>
		/// <value><para>DAE.IsolationLevel: Browse|CursorStability|Isolated</para>
		/// <para>Default: Browse</para></value>
		/// <remarks> <para>Browse -- Prevents lost updates but allows dirty reads. 
		/// Data that is written is locked but data that is read may be 
		/// uncommitted data from other transactions. </para>
		/// <para>CursorStability -- Prevents lost updates and doesn't allow 
		/// dirty reads. Data that is written is locked and data that is read is 
		/// only committed data from other transactions.</para>
		/// <para>Isolated -- Prevents lost updates and ensures repeatable reads, 
		/// which implies no dirty reads. This is the highest degree of isolation 
		/// and provides complete isolation from other transactions.</para>
		/// <para>For more information see
		/// <ulink url="Alphora.Dataphor.DAE.IsolationLevel.html" type="xref">
		/// IsolationLevel Enumeration</ulink>.</para>
		/// </remarks>
		DAE.IsolationLevel IsolationLevel { get; set; }

		/// <summary>The requested relative isolation of the cursor. 
		/// This will be used in conjunction with the isolation level of 
		/// the transaction to determine the actual isolation of the cursor.
		/// </summary> <doc/>
		/// <value><para>DAE.CursorIsolation: None|Chaos|Browse|CursorStability|Isolated</para>
		/// <para>Default: Browse</para></value>
		/// <remarks>
		/// <para>None -- Indicates that the cursor runs at the isolation level 
		/// of the current transaction.</para>
		/// <para>Chaos -- Indicates that the cursor does not lock data that it 
		/// writes, and reads uncommitted data from other transactions.</para>
		/// <para>Browse -- Indicates that the cursor protects it's writes, 
		/// but may read uncommitted data from other transactions. </para>
		/// <para>CursorStability -- Indicates that the cursor protects it's writes, 
		/// and reads only committed data from other transactions. </para>
		/// <para>Isolated -- Indicates that the cursor runs isolated from all 
		/// other transactions. </para>
		/// <para>For more information see
		/// <ulink url="D4LGDataManipulation-SelectStatement-CursorIsolation.html" type="olinkb">
		/// Cursor Isolation</ulink>.</para>
		/// </remarks>
		DAE.CursorIsolation RequestedIsolation { get; set; }
	}

	/// <summary> Base class for data value defaulting. </summary>
	public interface IDataDefault
	{
		// Published

		/// <summary> The default will only be performed if Enabled is true. </summary> <doc/>
		bool Enabled { get; set; }
		
		/// <summary> The comma or semicolon separated list of columns in the Target source that are to be defaulted. </summary> <doc/>
		string TargetColumns { get; set; }
	}

	/// <summary> Defaulting from literal values. </summary> <doc/>
	public interface IDataValueDefault : IDataDefault
	{
		// Published

		/// <summary> Source values in D4 list literal format (e.g. 'String value', nil, 5 ). </summary> <doc/>
		string SourceValues { get; set; }
	}

	/// <summary> Defaulting from another data source's values. </summary> <doc/>
	interface IDataSourceDefault : IDataDefault
	{
		// Published

		/// <summary> The source that this node uses to obtain the default value. </summary> <doc/>
		/// <value> <para>ISource: A source in the same node tree.</para>
		/// <para>Default: (None)</para> </value>
		/// <example> Main </example>
		ISource Source { get; set; }

		/// <summary> Comma or semicolon delimited list of source column names from which to default. </summary> <doc/>
		string SourceColumns { get; set; }
	}

	/**************  Interfaces  **************/

	/// <summary> Children of the <see cref="ISource"/> node implement this 
	/// interface to identify themselves as legitimate. </summary>
	public interface ISourceChild {}

	/// <summary> For nodes which reference a data source. </summary>
	public interface ISourceReference
	{
		// Published

		/// <summary> The source that this control is attached to. </summary> <doc/>
		/// <value> <para>ISource: A source in the same node tree.</para>
		/// <para>Default: (None)</para> </value>
		/// <example> Main </example>
		ISource Source { get; set; }
	}
	
	/// <summary> Children of the <see cref="ISourceReference"/> node implement 
	/// this interface to identify themselves as legitimate. </summary>
	public interface ISourceReferenceChild {}

	/// <summary> Abstract interface for nodes which may be restricted 
	/// from editing. </summary>
	public interface IReadOnly
	{
		// Published

		/// <summary> Determines if control can modify the data in the 
		/// source column. </summary> <doc/>
		/// <value> Boolean: True|False </value>
		/// <remarks> When true the control will not modify the data in 
		/// the data source. </remarks>
		bool ReadOnly { get; set; }
	}

	/**************  Enumerations, Delegates & Structs/Classes **************/

	/// <summary> A node event that is used to handle view actions. </summary>
	public class ViewActionEvent : NodeEvent
	{
		/// <summary> Constructs a new ViewActionEvent object. </summary>
		/// <param name="action"> The type of action this event will perform. </param>
		public ViewActionEvent(SourceActions action) : base()
		{
			Action = action;
		}

		/// <summary> The type of action this event will perform. </summary>
		public SourceActions Action;
	}
}
