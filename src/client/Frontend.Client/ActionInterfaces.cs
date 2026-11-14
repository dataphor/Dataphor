/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.Frontend.Client
{
	/**************  Nodes  **************/
	
	/// <summary> Base abstract node from which all actions descend. </summary>
	public interface IAction : INode, IEnableable
	{
		/// <summary> Called when the action text property changes. </summary>
		event EventHandler OnTextChanged;

		/// <summary> Called when the enabled property is changed </summary>
		event EventHandler OnEnabledChanged;

		/// <summary> Called when the hint property changes. </summary>
		event EventHandler OnHintChanged;

		/// <summary> Called when the image ref property is changed. </summary>
		event EventHandler OnImageChanged;

		/// <summary> Called when the visible property is changed. </summary>
		event EventHandler OnVisibleChanged;

		/// <summary> Gets the actual text to use. </summary>
		string GetText();

		/// <summary> Gets a "friendly" description of the action. </summary>
		string GetDescription();

		// Published

		/// <summary> Called to execute the action. </summary> <doc/>
		void Execute();
		
		/// <summary> Called to execute the action. </summary> <doc/>
		void Execute(INode ASender, EventParams AParams);

		/// <summary> A text string that will be used by this action.
		/// </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> If this is set, nodes which attach to this action may use 
		/// it's Text value. e.g. A trigger that does not have its own Text property 
		/// set will display the text of the Action's Text property.
		/// </remarks>
		string Text { get; set; }

		/// <summary> A text string that will be shown in tooltips, etc.
		/// </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> If this is set, nodes which attach to this action may use 
		/// it's Hint value. e.g. A trigger that does not have its own Hint property 
		/// set will display the hint of the Action's Hint property.
		/// </remarks>
		string Hint { get; set; }

		/// <summary> An image used by this action's controls as an icon.
		/// </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <example>Image("Frontend","Warning")</example>
		/// <remarks> If an Image is specified, the image will be shown with any 
		/// attached control. The image height and width are manipulated using 
		/// properties of the control.</remarks>
		string Image { get; set; }
		
		/// <summary> An action that will be executed before this action is executed.
		/// </summary> <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The action identified in the BeforeExecute property will be 
		/// executed before this action. NOTE: Do not set this property to execute 
		/// itself since this would cause and endless loop.
		/// </remarks>
		IAction BeforeExecute { get; set; }
		
		/// <summary> An action that will be executed after this action is executed.
		/// </summary> <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The action identified in the AfterExecute property will be 
		/// executed after this action. NOTE: Do not set this property to execute 
		/// itself since this would cause and endless loop.
		/// </remarks>
		IAction AfterExecute { get; set; }

		/// <summary> Determines whether the controls that are associated with 
		/// this action are visible. </summary> <doc/>
		/// <value> Boolean: True|False</value>
		/// <remarks> If this property is set to false, the controls associated 
		/// with this action will not be visible even if the control's Visible 
		/// property is set to true.  If this property is set to true, the 
		/// control's Visible property determines if the control is visible or 
		/// not.</remarks>
		bool Visible { get; set; }
	}

	/// <summary> Executes an action if the condition evaluates to true. </summary> <doc/>
	/// <remarks> An Actions is allowed as a child. The action that is a child 
	/// of the the ConditionalAction will be executed if the Condition evaluates to true.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface IConditionalAction : IAction
	{
		// Published
		/// <summary> Condition to be evaluated on upon executing. </summary> <doc/>
		/// <doc/>
		/// <value> <para>string: The d4 expression representing the condition.</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The child action will only be executed if the condition
		/// evaluates to true.  An error will be thrown if the condition contains
		/// more than a single expression or if the expression does not result
		/// in a boolean value.</remarks>
		/// 
		string Condition { get; set; }
	}

		/// <summary> Executes a list of actions sequentially, 
	/// like a begin...end block. </summary> <doc/>
	/// <remarks> Actions are allowed as children. Each action that is a child 
	/// of the the BlockAction will be executed. The order of execution will be 
	/// from the first in the list to the last in the list, sequentially. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface IBlockAction : IAction
	{
	}
	
	/// <summary> Executes the specified action node when executed. </summary> <doc/>
	/// <remarks>
	///	This action is useful within a block action to re-use an existing action.
	///	</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface ICallAction : IAction
	{
		// Published

		/// <summary> Action to be called upon executing. </summary> <doc/>
		/// <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The action identified will be executed.  An error will occur 
		/// if this action reference is set to this action, though note that there 
		/// are other ways to cause endless loops using actions.</remarks>
		IAction Action { get; set; }
	}

	// Published

	/// <summary> Invokes the context sensitive help system. </summary> <doc/>
	/// <remarks> If <see cref="HelpKeyword"/> is specified it will be used to navigate 
	///	within the help (based on the setting of <see cref="HelpKeywordBehavior"/>), 
	///	otherwise <see cref="HelpString"/> is used displayed as "popup" help.  
	///	If HelpString is blank, than the help is shown without initial navigation. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample02 [dfd] document.</example>
	public interface IHelpAction : IAction
	{
		// Published

		/// <summary> The keyword to use to navigate within the help. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The use of the keyword depends on the value of 
		/// the <see cref="HelpKeywordBehavior"/> property.  </remarks>
		string HelpKeyword { get; set; }

		/// <summary> Specifies the mode in which to initially show or navigate help. </summary> <doc/>
		/// <value> <para>HelpKeywordBehavior: AssociateIndex|Find|Index|KeywordIndex|TableOfContents|Topic</para>
		/// <para>Default: KeywordIndex</para></value>
		/// <remarks>
		/// <para>AssociateIndex - Specifies that the index for a specified topic is performed.</para> 
		/// <para>Find - Specifies that the search page is displayed.</para>
		/// <para>Index - Specifies that the index is displayed.</para>
		/// <para>KeywordIndex - Specifies a keyword to search for and the action to take.</para>
		/// <para>TableOfContents - Specifies that the table of contents is displayed.</para>
		/// <para>Topic - Specifies that the topic referenced is displayed.</para>
		/// </remarks>
		HelpKeywordBehavior HelpKeywordBehavior { get; set; }

		/// <summary> The help text to display (if <see cref="HelpKeyword"/> is not 
		/// specified). </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string HelpString { get; set; }
	}

	// Published

	/// <summary> Displays a notification/tip to the user. </summary> <doc/>
	public interface INotifyAction : IAction
	{
		// Published

		/// <summary> The title to display. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value> 		
		string TipTitle { get; set; }

		/// <summary> The text to display. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value> 		
		string TipText { get; set; }

		/// <summary> The icon to display. </summary> <doc/>
		/// <value> <para>NotifyIcon</para>
		/// <para>Default: NotifyIcon.Info</para></value>
		NotifyIcon TipIcon { get; set; }
	}
	
	/// <summary> An action that sets a property of a specified node when triggered.
	/// </summary> <doc/>
	/// <remarks> The property, specified by MemberName, of the node, specified by 
	/// Node, will be changed.  If Source is specified the new value will be taken 
	/// from the ColumnName of Source.  If Source is not specified the new value 
	/// will be taken from the Value property.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface ISetPropertyAction : IAction
	{
		// Published

		/// <summary> The target node that has the target member to set. </summary> <doc/>
		/// <value> <para>INode: The name of one of the nodes on the current form.</para>
		/// <para>Default: nothing</para></value>
		/// <remarks> The node where the property will be modified.
		/// </remarks>
		INode Node { get; set; }

		/// <summary> The target member to set. </summary> <doc/>
		/// <value> <para>String: The name of the property.</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> This property in the node specified in node will be changed.
		/// </remarks>
		string MemberName { get; set; }

		/// <summary> The value to set the target member to. </summary> <doc/>
		/// <value> <para>String: </para>
		/// <para>Default: empty string</para></value>
		/// <remarks> This value is used to set the property of the target node.
		/// If Source is specified the Value property is ignored.
		/// </remarks>
		string Value { get; set; }
	}

	/// <summary> Opens a web page in a seperate browser window. </summary><doc/>
	/// <remarks> Will open a web browser and display the contents of a web 
	/// URL or a local or network folder.
	/// The contents will be displayed in the web browser.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample05 [dfd] document.</example>
	public interface IShowLinkAction : IAction
	{
		// Published

		/// <summary> An address to a web page or local or network folder 
		/// as would be entered into the Address bar of the Windows Explorer
		/// or Internet Explorer. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <example> http://www.alphora.com </example>
		string URL { get; set; }
	}

	/// <summary> Performs data source manipulation. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface ISourceAction : IDataAction
	{
		// Published

		/// <summary> The action type that will be performed against the data source. </summary> <doc/>
		/// <value> <para>SourceActions: First|Prior|Next|Last|Refresh|Insert|Append|Edit|Delete|Post|Cancel|RequestSave|Validate|Close|Open</para>
		/// <para>Default: "<see cref="SourceActions"/>.First"</para></value>
		SourceActions Action { get; set; }
	}

	/// <summary> Finds a row in the data source. </summary> <doc/>
	/// <remarks> FindAction is used to position the dataset where
	/// the value of the specified ordered column is equal to (or near to)
	/// a specific value.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface IFindAction : IDataAction
	{
		// Published

		/// <summary> Determines the method used to find the row. </summary> <doc/>
		/// <value> <para>FindActionMode: Nearest|Exact|ExactOnly</para>
		/// <para>Default: "<see cref="FindActionMode"/>.Nearest"</para></value>
		/// <remarks> <para> Nearest - Navigate the dataset return a match if possible,
		/// otherwise return a row close to a match.</para>
		/// <para> Exact - If an exact match is not found, don't change the cursor
		/// position and don't raise an error.</para>
		/// <para> ExactOnly - If an exact match is not found raise an error.</para>
		/// </remarks>
		FindActionMode Mode { get; set; }

		/// <summary> The column name to search by. </summary> <doc/>
		/// <value> <para>String: The column name in the Source</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The Value will be searched for in this column. The column
		/// must be ordered.
		/// </remarks>
		string ColumnName { get; set; }

		/// <summary> The value to search for. </summary> <doc/>
		/// <value> <para>String: </para>
		/// <para>Default: an empty string</para></value>
		string Value { get; set; }
	}

	/// <summary> Executes a D4 script in the context of the Dataphor server. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface IDataScriptAction : IAction
	{
		// Published

		/// <summary> The D4 script to run. This script will be parameterized 
		/// by any parameters specified using DataArgument child nodes. </summary> <doc/>
		/// <value> <para>String: A D4 script.</para>
		/// <para>Default: empty string</para></value>
		string Script { get; set; }

		/// <summary> A Source node to enlist with for application transactions. </summary> <doc/> 
		/// <value> <para>ISource: A source on the same form.</para>
		/// <para>Default: (None)</para></value>
		/// <remarks> If the EnlistWith source is in an application transaction, 
		/// this script will run within that transaction. This ensures that all
		/// changes are rolled back or committed together.
		/// </remarks>
		ISource EnlistWith { get; set; }
	}

	public interface IBaseArgument : INode
	{
	}

	/// <summary> A UserStateArgument provides parameter(s) to a parent component. </summary> <doc/>
	/// <remarks> The DataArgument can be child of a Source or a DataScriptAction.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface IUserStateArgument : IBaseArgument
	{
		// Published

		/// <summary> The name of the UserState item of the current interface to get/set for this param. </summary> <doc/>
		string KeyName { get; set; }
		
		/// <summary> The default value to get and/or set if the key name isn't set or doesn't 
		/// reference an existing item. This value is expected to be a D4 literal (e.g. 'string', nil, etc.). </summary> <doc/>
		string DefaultValue { get; set; }
		
		/// <summary> The name of the parameter produced by this argument. </summary> <doc/>
		string ParamName { get; set; }
		
		/// <summary> Indicates the "direction" of the parameters. </summary> <doc/>
		/// <value> <para><see cref="DAE.Language.Modifier"/></para>
		/// <para>Default: Modifier.Const</para></value>
		DAE.Language.Modifier Modifier { get; set; }
	}
	
	/// <summary> A DataArgument provides parameter(s) to a parent component. </summary> <doc/>
	/// <remarks> The DataArgument can be child of a Source or a DataScriptAction.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface IDataArgument : IBaseArgument
	{
		// Published

		/// <summary> The data source from which to retrieve data arguments. </summary> <doc/>
		/// <value> <para>ISource: a source in the same node tree.</para>
		/// <para>Default: (None)</para></value>
		ISource Source { get; set; }

		/// <summary> Comma separated list of column names or parameter=name pairs. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> 
		///		If no column names are specified, all columns from the data 
		///		source will be used.  To rename a column, specify the name of
		///		the data source's column followed by an equal then the name for
		///		the column as a parameter.
		///	</remarks>
		///	<example> "AID=Customer_ID, Name, PhoneNumber" </example>
		string Columns { get; set; }

		/// <summary> Indicates the "direction" of the parameters. </summary> <doc/>
		/// <value> <para><see cref="DAE.Language.Modifier"/></para>
		/// <para>Default: Modifier.Const</para></value>
		DAE.Language.Modifier Modifier { get; set; }
	}

	/// <summary> Closes the current form, accepting or rejecting as necessary. </summary> <doc/>
	public interface IFormAction : IAction
	{
		// Published

		/// <summary> The behavior of the FormAction. </summary> <doc/>
		/// <value> <para>CloseBehavior: RejectOrClose|AcceptOrClose </para>
		/// <para>Default: "RejectOrClose"</para></value>
		CloseBehavior Behavior { get; set; }
	}

	/// <summary> Sets the next page to be loaded when this one closes. </summary> <doc/>
	/// <remarks> 
	///		This action allows the chaining of interfaces (forms). 
	///		When the main interface document for the application closes, 
	///		the next interface (form) in the chain is loaded.
	///		ClearNextRequestAction is used to clear setting.
	///	</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample06 [dfd] document.</example>
	public interface ISetNextRequestAction : IAction, IDocumentReference
	{
	}

	/// <summary> Clears the next request for the current user interface. </summary> <doc/>
	/// <remarks> Clears the interface (form) that is set to be used after the
	/// current interface is closed, as set using SetNextRequestedAction. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample06 [dfd] document.</example>
	public interface IClearNextRequestAction : IAction
	{
	}

	/// <summary> Loads a module and executes the specified action. </summary> <doc/>
	/// <remarks> Modules contain reusable code.</remarks>
	public interface IExecuteModuleAction : IAction
	{
		/// <summary> A D4 expression evaluating to a module to be loaded containing the action to be executed. </summary> <doc/>
		string ModuleExpression { get; set; }

		/// <summary> The name of the action to be executed. An action with this name must be present in the module being loaded. </summary> </dilref>
		string ActionName { get; set; }
	}

	/// <summary> Shows a form non-modally. </summary> <doc/>
	/// <remarks> Shows a form while the current form is maintained in the background.  
	/// When the new form is closed the calling form will be active again.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample06 [dfd] document.</example>
	public interface IShowFormAction : IAction, IDocumentReference, ISourceLink, IBlockable
	{
		// Published

		/// <summary> If true and a sourcelink is set, the source is refreshed 
		/// after execution. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool SourceLinkRefresh { get; set; }

		/// <summary> FormMode used when opening the interface. </summary> <doc/>
		/// <value> <para>FormMode: None|Insert|Edit|Delete|Query</para>
		/// <para>Default: None</para></value>
		FormMode Mode { get; set; }

		/// <summary> Determines whether or not to set the OpenState of the Source on the form being shown. </summary> <doc/>
		/// <remarks> If UseOpenState is true, the OpenState of the main Source	on the form being shown will be set before
		/// the form is activated to the DataSetState value corresponding to the Mode being used to show the form. </remarks>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool UseOpenState { get; set; }

		/// <summary> Determines whether or not to set the IsWriteOnly property of the Source on the form being shown. </summary> <doc/>
		/// <remarks> If ManageWriteOnly is true, the IsWriteOnly property of the main Source on the form being shown will be set before
		/// the form is activated. If the form is being shown in Insert mode, IsWriteOnly will be set to true.</remarks>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool ManageWriteOnly { get; set; }

		/// <summary> Determines whether or not to set the RefreshAfterPost property of the Source on the form being shown. </summary> <doc/>
		/// <remarks> If ManageRefreshAfterPost is true, the RefreshAfterPost property of the main Source on the form being shown will be set
		/// before the form is activated. If the form is being shown in Insert or Edit mode, RefreshAfterPost will be set to false.</remarks>		
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool ManageRefreshAfterPost { get; set; }
		
		/// <summary> Indicates whether a confirm delete form will be displayed 
		/// when Mode is delete. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool ConfirmDelete { get; set; }

		/// <summary> When true, if the current form is being queried 
		/// (typically as a lookup) and the shown form is in insert mode 
		/// and is accepted, the current form will automatically be accepted. 
		/// </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		/// <remarks> When a user is looking up a value, then decides to add one, 
		/// they typically would rather assume that the newly entered one is also 
		/// the one they would like to choose for the lookup.
		/// </remarks>
		bool AutoAcceptAfterInsertOnQuery { get; set; }

		/// <summary> Filter (string expression) to apply to the main source 
		/// of the target form. </summary> <doc/>
		/// <value> <para>String:</para>
		/// <para>Default: empty string</para></value>
		string Filter { get; set; }
		
		/// <summary> The TopMost setting of the shown form. </summary>
		bool TopMost { get; set; }

		/// <summary> An action that will be executed after the form is closed. </summary> <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default: (None)</para></value>
		IAction OnFormClose { get; set; }

		/// <summary> An action that will be executed after the form is accepted. </summary> <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default: (None)</para></value>
		IAction OnFormAccepted { get; set; }

		/// <summary> An action that will be executed after the form is rejected. </summary> <doc/>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default (None)</para></value>
		IAction OnFormRejected { get; set; }

		/// <summary> An action that will be executed after the form is created, but before it is activated. </summary> <doc/>
		/// <remarks> This is an ideal event to use to set data source filters and otherwise affect the 
		/// properties of the form before it is activated. </remarks>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default (None)</para></value>
		IAction BeforeFormActivated { get; set; }
		
		/// <summary> An action that will be executed after the form is activated, but before it is shown. </summary> <doc/>
		/// <remarks> This event can be used to perform processing that needs to be done on the form before it is actually
		/// displayed to the user, but that requires access to the data on the form. </remarks>
		/// <value> <para>IAction: The name of an action.</para>
		/// <para>Default (None)</para></value>
		IAction AfterFormActivated { get; set; }
	}

	/// <summary> Edits the filter for the specified data source. </summary> <doc/>
	/// <remarks> The filter restricts the rows using a D4 expression. 
	/// When the action is executed a text editing form will be displayed allowing
	/// the current filter to be modified.
	/// The filter property of the Source node will be modified.</remarks>
	/// <example> An example of the EditFilterAction and a custom filter 
	/// can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface IEditFilterAction : IDataAction
	{
	}

	/// <summary> Executes a script within the context of the client. </summary> <doc/>
	/// <remarks>
	///		<para>
	///		The following pre-processor directives are interpreted by the script 
	///		action:
	///		<list type="table">
	///			<listheader>
	///				<term>Directive</term>
	///				<description>Description</description>
	///			</listheader>
	///			<item>
	///				<term>#unit</term>
	///				<description>Begins a block of code to be included directly 
	///				in the compilation unit. This block can be used to include
	///				namespaces that will be accessed.</description>
	///			</item>
	///			<item>
	///				<term>#endunit</term>
	///				<description>Ends a compilation unit code block.</description>
	///			</item>
	///			<item>
	///				<term>#class</term>
	///				<description>Begins a block of code to be included directly 
	///				in the class. This block can be used to define other methods
	///				and properties that can be accessed from the main execution block.</description>
	///			</item>
	///			<item>
	///				<term>#endclass</term>
	///				<description>Ends class code block.</description>
	///			</item>
	///		</list>
	///		The pre-processor directives may only appear once each.  All other code 
	///		is executed within a method block.  Within the class scope, the 
	///		ScriptAction node is accessible through the "Action" property.  From 
	///		the Action property, it is possible to access the rest of the document 
	///		through calls to members such as <see cref="Node.HostNode"/> and 
	///		<see cref="Node.FindNode"/>.  All access to components should be done 
	///		through the component interfaces to ensure client portability.
	///		</para>
	///		<para>
	///		All the components and controls within the current document will be 
	///		available as properties	of the class. In addition to these, the 
	///		following implicit references are available:
	///		<list type="table">
	///			<listheader>
	///				<term>Property</term>
	///				<description>Description</description>
	///			</listheader>
	///			<item>
	///				<term>Action</term>
	///				<description>
	///				Provides access to the currently executing ScriptAction.
	///				</description>
	///			</item>
	///			<item>
	///				<term>Host</term>
	///				<description>
	///				Provides access to the <see cref="HostNode"/> of the current document.
	///				</description>
	///			</item>
	///			<item>
	///				<term>Interface</term>
	///				<description>
	///				Provides access to the interface containing the ScriptAction. This
	///				may be an <see cref="IFormInterface"/> if the ScriptAction is
	///				at the top-level of a form document, or it may be a <see cref="IFrameInterface"/>
	///				if the component is contained within a frame within some other interface.
	///				</description>
	///			</item>
	///			<item>
	///				<term>Form</term>
	///				<description>
	///				Provides access to the <see cref="IFormInterface"/> containing the component.
	///				This will always result in the top-level interface, even if the component
	///				is actually contained within a frame of the current document.
	///				</description>
	///			</item>
	///		</list>
	///		</para>
	/// </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface IScriptAction : IAction
	{
		// Published

		/// <summary> Script lanugage. </summary> <doc/>
		/// <value><para>ScriptLanguages: CSharp|VisualBasic</para>
		/// <para>Default: CSharp</para></value>
		ScriptLanguages Language { get; set; }

		/// <summary> The script to execute. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string Script { get; set; }

		/// <summary>
		/// A document expression that returns a script used to display a form
		/// </summary>
		/// <remarks> The expression is expected to result in a script of the selected language. </remarks>
		/// <example>
		/// Load('Application.Example','CSharpScript')
		/// </example>
		string ScriptDocument { get; set; }

		/// <summary>Set of assembly references to use when compiling the dynamic script.  One fully qualified assembly name per line. </summary>
		string References { get; set; }
		
		
		/// <summary> When true, the script will be compiled with debug information. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: False</para></value>
		bool CompileWithDebug { get; set; }
	}
	
	/// <summary> Sets the sources associated with the child data arguments from the action params. </summary>
	public interface ISetArgumentsFromParams : IAction
	{
	}
	
	/// <summary> When attached to an event that originates from a form (such as FormAccepted), sets the sources associated with the child data arguments from the form's main source fields. </summary>
	public interface ISetArgumentsFromFormMainSource : IAction
	{
	}
	
	/// <summary> Copies the parameters provided by DataArgument nodes to the clipboard. </summary>
	/// <remarks> Cut can be implemented by following the copy or paste action with a deltion. </remarks>
	public interface ICopyAction : IAction
	{
		// Published

		/// <summary> The identifier to use when storing or retrieving information from the clipboard. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: DataphorData</para></value>
		string ClipboardFormatName { get; set; }
	}
	
	/// <summary> Pastes parameters previously placed on the clipboard by applying the parameters to any var or out parameters in DataArgument nodes. </summary>
	/// <remarks> Cut can be implemented by following the copy or paste action with a deltion. </remarks>
	public interface IPasteAction : IAction
	{
		// Published

		/// <summary> The identifier to use when storing or retrieving information from the clipboard. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: DataphorData</para></value>
		string ClipboardFormatName { get; set; }
	}

	/**************  Interfaces  **************/

	/// <summary> An abstract node which data actions derive from. </summary>
	public interface IDataAction : IAction
	{
		// Published

		/// <summary> The source that the data action will work on. </summary> <doc/>
		/// <value> ISource: the name of a source available in the same node tree.</value>
		ISource Source { get; set; }
	}

	/// <summary> An abstract interface for nodes that point to a document. </summary>
	public interface IDocumentReference
	{
		// Published

		/// <summary> A document expression that returns a document used to display a form. </summary> <doc/>
		/// <remarks> The form can be derived or an existing dfd or dfdx document. </remarks>
		/// <example>
		/// Derive('System.DataTypes','Browse','','',true) or Form('Sample.Rental','AddSale')
		/// </example>
		string Document { get; set; }
	}

	/// <summary> An action that performs a lookup without using a 
	/// lookup a control. </summary><doc/>
	/// <remarks>Used to select a row from a query and set values in one or more
	/// columns of the source from columns from the selected row.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample04 [dfd] document.</example>
	public interface ILookupAction : IAction, ILookup 
	{
		// Published

		/// <summary> Determines whether the lookup action will auto post the source. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		bool AutoPost { get; set; }

		/// <summary> An action to execute after the lookup form has been rejected. </summary> <doc/>
		IAction OnLookupRejected { get; set; }

		/// <summary> An action to execute after the lookup form has been accepted. </summary> <doc/>
		IAction OnLookupAccepted { get; set; }

		/// <summary> An action to execute after the lookup form has been closed. </summary> <doc/>
		IAction OnLookupClose { get; set; }

		/// <summary> An action to execute after the lookup form is created, but before it is activated. </summary>
		IAction BeforeLookupActivated { get; set; }
	}

	/**************  Enumerations & Delegates **************/

	/// <summary> The types of actions that a SourceAction can perform. </summary>
	public enum SourceActions 
	{
		/// <summary> Move to the top of the data set. </summary>
		First,

		/// <summary> Move to the previous of the data set. </summary>
		Prior,

		/// <summary> Move to the next row in the data set. </summary>
		Next,

		/// <summary> Move to the bottom of the data set. </summary>
		Last,

		/// <summary> Refresh the data set. </summary>
		Refresh,

		/// <summary> Insert a new row into the data set before the current row. </summary>
		Insert,

		/// <summary> Insert a new row into the data set after the last row. </summary>
		Append,

		/// <summary> Edits the current row of the data set. </summary>
		Edit,

		/// <summary> Deletes the current row of the data set. </summary>
		Delete,

		/// <summary> Posts the currently edited row to the database. </summary>
		Post,

		/// <summary> Posts and added/edited details of the current data set. </summary>
		PostDetails,

		/// <summary> Cancels any changes made to the currently edited row. </summary>
		Cancel,

		/// <summary> Updates the source with any modifications that may be pending from the controls. </summary>
		/// <remarks> This is automatically done when the source is posted, but invoking this allows for the the update to occur without (or before) posting. </remarks>
		RequestSave,
		
		/// <summary> Validates the values of the buffered insertion row without posting. </summary>
		Validate,

		/// <summary> Closes the data Source. </summary>
		Close,

		/// <summary> Opens the data Source. </summary>
		Open,

        /// <summary> Posts and added/edited details of the current data set if it has been modified. </summary>
        PostIfModified
	}

	/// <summary> Search methods that can be used by <see cref="FindAction"/>. </summary>
	public enum FindActionMode 
	{
		/// <summary> Locate at the nearest row. </summary>
		Nearest,

		/// <summary> Find the exact row or don't move at all. </summary>
		Exact,

		/// <summary> Find the exact row or throw exception and don't move. </summary>
		ExactOnly
	};

	/// <summary> Indicates the data manipulation intention when a form is shown modally. </summary>
	public enum FormMode
	{
		/// <summary> Do nothing with the form's main data source. </summary>
		None,

		/// <summary> Insert a new row into the form's main data source upon showing. </summary>
		Insert,

		/// <summary> Place the form's main data source into an edit state upon showing. </summary>
		Edit,

		/// <summary> Delete the current row of the form's main data source after accepting. </summary>
		Delete,

		/// <summary> A value is being "looked up" from the source. </summary>
		Query
	}

	/// <summary> Form closure behaviors. </summary>
	public enum CloseBehavior
	{
		/// <summary> Rejects the form if modal.  Closes otherwise. </summary>
		RejectOrClose, 

		/// <summary> Accepts the form if modal.  Closes otherwise. </summary>
		AcceptOrClose
	}

	/// <summary> Languages supported by the client side scripting engine. </summary>
	public enum ScriptLanguages
	{
		CSharp,
		VisualBasic
	}
}
