/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Net;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Frontend.Client
{
	/**************  Nodes  **************/

	/// <summary> Root or nested menu item. </summary> <doc/>
	/// <remarks> 
	///		May contain menus as children.  If the menu item is a parent menu 
	///		(has child menus), then the menu action will be to display a 
	///		sub-menu and click events on the parent menu will not execute
	///		an associated action.  
	///		If the Text of the menu is "-" and the
	///		menu is not a parent menu then the menu will act as a menu item group 
	///		separator (and there will be no way to execute the associated action).
	///	</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample02 [dfd] document.</example>
	public interface IMenu : IActionNode
	{
	}
	
	/// <summary> Exposes an action on the form interface (toolbar). </summary> <doc/>
	/// <remarks> Places a graphic, text or both on the toolbar.  
	/// An associated action will be executed when the graphic/text is clicked.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample02 [dfd] document.</example>
	public interface IExposed : IActionNode
	{
	}

	/// <summary> Search column information node. </summary> <doc/>
	/// <remarks> Provides extended information about a column for the 
	/// search element. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface ISearchColumn : INode, IReadOnly, ISourceReferenceChild
	{
		// Published

		/// <summary> The title of the search column. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para> </value>
		/// <remarks> 
		/// <para>The Title property is used as the title of the search column and is
		/// displayed on the form.</para>
		/// <para>If the Title is set to an empty string the name of the column from 
		/// the ColumnName property will be used.</para>
		/// </remarks>
		string Title { get; set; }

		/// <summary> Text which describes the search column to the end-user. </summary> <doc/>
		/// <value> <para>String </para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The Hint text will be displayed in the bar at the bottom of
		/// the form when the search column has focus.</remarks>
		string Hint { get; set; }

		/// <summary> The name of the column within the data source. </summary> <doc/>
		string ColumnName { get; set; }

		/// <summary> Specifies the natural display width of the search column. 
		/// </summary> <doc/>
		/// <value> <para>Integer (number of characters)</para>
		/// <para>Default: 15</para></value>
		/// <remarks> The width will vary based on the current status of the form.
		/// This property specified the desired normal display width. Width does 
		///	not control how many characters are allowed, only how the control 
		///	is displayed. The width is approximate and based on the average character 
		///	width of the current font.
		///	</remarks>
		int Width { get; set; }

		/// <summary> The alignment of content within the search column control. </summary> <doc/>
		/// <value> <para>HorizontalAlignment: Left|Center|Right</para>
		/// <para>Default: Left</para></value>
		HorizontalAlignment TextAlignment { get; set; }
	}

	/// <summary> Column specification node for use under the grid. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface IGridColumn : INode, ISourceReferenceChild
	{
		// Published

		/// <summary> Text which describes the grid column to the end-user. </summary> <doc/>
		/// <value> <para>String </para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The Hint text will be displayed in the bar at the bottom of
		/// the form when the search column has focus.</remarks>
		string Hint { get; set; }

		/// <summary> The title of the grid column. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para> </value>
		/// <remarks> 
		/// <para>The Title property is used as the title of the grid column and is
		/// displayed on the form.</para>
		/// <para>If the Title is set to an empty string the name of the column from 
		/// the ColumnName property will be used.</para>
		/// </remarks>
		string Title { get; set; }

		/// <summary> Specifies the natural display width of the grid column. 
		/// </summary> <doc/>
		/// <value> <para>Integer (number of characters)</para>
		/// <para>Default: 10</para></value>
		/// <remarks> The width will vary based on the current status of the form.
		/// This property specified the desired normal display width. Width does 
		///	not control how many characters are allowed, only how the control 
		///	is displayed. The width is approximate and based on the average character 
		///	width of the current font.
		///	</remarks>
		int Width { get; set; }

		/// <summary> Determines whether the grid column will be visible.
		/// </summary> <doc/>
		/// <value> <para>Boolean: True|False </para>
		/// <para>Default: true</para></value>
		bool Visible { get; set; }

	}

	/// <summary> A grid column containing a trigger (button) for an action. </summary> <doc/>
	/// <remarks> The TriggerColumn column is used as a child of a Grid.
	/// When the trigger (button) is clicked the row containing the button will
	/// be selected and the associated action will be executed.
	/// The same action is executed regardless of which row is selected.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface ITriggerColumn : IGridColumn
	{
		// Published

		/// <summary> A text string that describes the actions of this trigger. </summary> <doc/>
		/// <value><para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> If this is not set, the text property of the action will be used. </remarks>
		string Text { get; set; }

		/// <summary> The action to be triggered. </summary> <doc/>
		/// <value><para>IAction: An action in the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction Action { get; set; }
	}
		
	/// <summary> A grid column that allows a row to be dragged and then dropped
	/// within the grid. </summary> <doc/>
	/// <remarks> The Sequence column is used as a child of a Grid.
	/// A D4 script, in the Script property, is executed when the drop
	/// occures.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface ISequenceColumn : IGridColumn
	{
		// Published

		/// <summary> The image that is shown in the column. </summary> <doc/>
		string Image { get; set; }

		/// <summary> The D4 script that is executed when a row is 
		/// clicked on dragged and dropped. </summary> <doc/>
		/// <value><para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks>
		///	<para> Available arguments inside the script are AFromRow.(ColumnNames), 
		///	AToRow.(ColumnNames), and AAbove.</para>
		///	<para> AFromRow is the row that was clicked on and dragged.
		///	The values in AFromRow are available 
		///	as AFromRow.XXX where XXX is the name of a column in the source.</para>
		///	<para> AAbove (a boolean) indicates if AFromRow was dropped 
		///	above or below AToRow.
		///	AAbove will be true if AFromRow was dropped above AToRow.
		///	AAbove will be false if AFromRow was dropped below AToRow.</para>
		///	<para> AToRow is the row above or below the location where AFromRow 
		///	was dropped (see the explination of AAbove).
		///	The AToRow values are available as AToRow.XXX
		///	where XXX is the name of a column in the source.</para>
		/// </remarks>
		string Script { get; set; }

		/// <summary>Enlist with the application transaction when running Script, 
		/// if there is one active.</summary>
		/// <remarks>If the associated Source is involved in an application transaction, 
		/// setting this property to true will cause the script to be ran in that 
		/// application transaction.
		///		<para>Default: true</para> </remarks>
		bool ShouldEnlist { get; set; }
	}

	public interface IDataGridColumn : IGridColumn
	{
		// Published

		/// <summary> The name of the column within the data source. </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string ColumnName { get; set; }
	}

	/// <summary> A grid column containing data bound text. </summary> <doc/>
	/// <remarks> The TextColumn is used as a child of a Grid to display most 
	/// types of data from the source.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface ITextColumn : IDataGridColumn
	{
		// Published

		/// <summary> The alignment of content within the TextColumn control. </summary> <doc/>
		/// <value> <para>HorizontalAlignment: Left|Center|Right</para>
		/// <para>Default: Left</para></value>
		HorizontalAlignment TextAlignment { get; set; }

		/// <summary> Maximum height a row can be in rows of text.</summary>
		/// <value> <para>Integer</para>
		/// <para> Default: -1</para></value>
		/// <remarks> A value of -1 tell causes the height to be calculated. </remarks>
		int MaxRows { get; set; }

		/// <summary> The vertical alignment of content within the TextColumn control. </summary> <doc/>
		/// <value> <para>VerticalAlignment: Top|Middle|Bottom</para>
		/// <para>Default: Top</para></value>
		VerticalAlignment VerticalAlignment { get; set; }

		/// <summary> Indicates if text is automatically word-wrapped for multi-line rows. </summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para> Default: False</para></value>
		bool WordWrap { get; set; }

		/// <summary> Indicates if text is to be painted vertical or horizontal. </summary> <doc/>
		/// <value><para>Boolean</para>
		/// <para>Default: False</para></value>
		bool VerticalText { get; set; }
	}

	/// <summary> A grid column containing data bound images. </summary> <doc/>
	/// <remarks> The ImageColumn is used as a child of a Grid to display 
	/// the data from a row in the source that contains Graphic data.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface IImageColumn : IDataGridColumn
	{
		// Published

		/// <summary> The alignment of content within the ImageColumn control. </summary> <doc/>
		/// <value> <para>HorizontalAlignment: Left|Center|Right</para>
		/// <para>Default: Left</para></value>
		HorizontalAlignment HorizontalAlignment { get; set; }

		/// <summary> The maximum height of a row in pixels.</summary>
		/// <value> <para>Integer</para>
		/// <para> Default: -1</para></value>
		/// <remarks> A value of -1 tell causes the height to be calculated. </remarks>
		int MaxRowHeight { get; set; }
	}

	/// <summary> A grid column containing a data bound checkbox. </summary> <doc/>
	/// <remarks> The CheckBoxColumn is used as a child of a Grid to display 
	/// data from a column with a Boolean value.</remarks>
	/// <remarks> The CheckBoxColumn is unique in that the value of the column
	/// can be changed using the check box on the grid.  To do this the 
	/// ReadOnly property must be set to false.  
	/// By default, the ReadOnly property of CheckBoxColumns is true. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface ICheckBoxColumn : IDataGridColumn, IReadOnly
	{
	}

	public interface ISourceLink
	{
		// Published

		/// <summary> Determines the data relationship between this document 
		/// and the embedded one. </summary> <doc/>
		/// <value> SourceLinkType: None|Surrogate|Detail</value>
		/// <remarks> The default is None. </remarks>
		SourceLinkType SourceLinkType { get; set; }

		/// <summary> Contains the specific settings based on 
		/// the <see cref="P:Alphora.Dataphor.Frontend.Client.ISourceLink.SourceLinkType"></see>. </summary> <doc/>
		/// <remarks> 
		/// <para> If SourceLinkType is Surrogate, 
		/// SourceLink will have one child property: Source.</para> 
		/// <para> If SorceLinkType is Detail,
		/// SourceLink will have four child properties:
		/// Attach Master, DetailKeyName, MasterKeyName, and Source.</para>
		/// </remarks>
		SourceLink SourceLink { get; }
	}

	/// <summary> A Timer executes an action at regular intervals. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface ITimer : INode
	{
		/// <summary>Indicates whether the timer will run continuously, 
		/// or only once.</summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		/// <remarks>
		/// Setting this property to true will cause the timer to reset 
		/// after each call to OnElapsed, meaning the event will be raised 
		/// again after Interval milliseconds.  If this property is set to
		/// false, the event will be raised only once, and the timer will stop.
		/// </remarks>
		bool AutoReset { get; set; }
		
		/// <summary>Indicates whether the timer is running.</summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: False</para></value>
		/// <remarks>
		///	Setting this property to true will start the timer, setting 
		///	it to false will stop it.  If the timer is already enabled, 
		///	setting enabled to true will reset the interval.
		/// </remarks>
		bool Enabled { get; set; }
		
		/// <summary>The interval, in milliseconds between each OnElapsed 
		/// call.</summary> <doc/>
		/// <value><para>Double</para>
		/// <para>Default: 100</para></value>
		/// <remarks>Setting the interval when the timer is enabled has 
		/// the effect of resetting the interval count.</remarks>
		int Interval { get; set; }
		
		/// <summary>The action to bo executed when the timer elapses.</summary> <doc/>
		/// <value><para>IAction: An action in the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnElapsed { get; set; }

		/// <summary>Starts the timer by setting enabled to true.</summary> <doc/>		
		/// <remarks>Note that if the timer is already started, calling 
		/// Start() will reset the interval.</remarks>
		void Start();
		
		/// <summary>Stops the timer by setting enabled to false.</summary> <doc/>
		void Stop();
	}
	
	/**************  Interfaces  **************/

	/// <summary> Hosts all other nodes. </summary>
	/// <remarks>
	///		Responsable for loading and activating other nodes.  Also associates
	///		the node tree with a <see cref="Session"/>.
	///	</remarks>
	public interface IHost : INode
	{
		/// <summary> The session that this host belongs to. </summary>
		Session Session { get; }
		
		/// <summary> A web request pipe which will be used to make the requests to the web. </summary>
		Pipe Pipe { get; }
		
		/// <summary> The form to be loaded when this one is closed. </summary>
		/// <seealso cref="SetNextRequestAction"/>
		Request NextRequest { get; set; }

		/// <summary> Assignes a unique name to the given node. </summary>
		void GetUniqueName(INode ANode);

		/// <summary> Activates all the nodes and marks itself active. </summary>
		void Open();
		
		/// <summary> Just like open, except that the AfterActivate steps are not performed. </summary>
		/// <remarks> This call should always be followed by AfterOpen(). </remarks>
		void Open(bool ADeferAfterActivate);

		/// <summary> AfterOpen is to be called only if the host was opened with ADeferAfterActivate = true. </summary>
		void AfterOpen();

		/// <summary> Deactivates all the nodes and marks itself inactive. </summary>
		void Close();
		
		/// <summary> The Document that this host was loaded from. </summary>
		string Document { get; set; }

		/// <summary> Event triggered when the value of the document property changes. </summary>
		event EventHandler OnDocumentChanged;
		
		/// <summary> Creates a child node for this host. </summary>
		/// <remarks> Any previous children are removed. </remarks>
		/// <param name="ADocument"> The Document to load the node from. </param>
		/// <param name="AInstance"> An object to deserialize into.  Can be null. </param>
		/// <returns> Returns the new (or passed) node instance. </returns>
		INode Load(string ADocument, object AInstance);
		
		/// <summary> Loads the NextRequest url when called. </summary>
		/// <param name="AInstance"> The object to be deserialized into.  May be null. </param>
		/// <returns> The root node deserialized page. </returns>
		/// <seealso cref="SetNextRequestAction"/>
		INode LoadNext(object AInstance);
	}

	/// <summary> Collection used for <see cref="INode.Children"/>. </summary>
	public interface IChildCollection : IList
	{
		new INode this[int AIndex] { get; }

		void Disown(INode AItem);

		INode DisownAt(int AIndex);
	}

	/// <summary> Abstract base node. </summary>
	/// <remarks>
	///		Manages a parent/child relationship between nodes.  Also maintains active
	///		state flag.  All nodes within a given tree are collectively active or not.
	/// </remarks>
	public interface INode : IDisposable, System.ComponentModel.IComponent
	{
		/// <summary> A link to the owning node or null if root node. </summary>
		INode Owner { get; set; }

		/// <summary> The node which control's this node's behavior. </summary>
		/// <remarks>
		///		This is usually the owner, but may be another node.  For example,
		///		interfaces within <see cref="IFrame"/>s have the frame as a parent.
		///	</remarks>
		INode Parent { get; }
		
		/// <summary> A list of children nodes. </summary>
		IChildCollection Children { get; }
		
		/// <summary> Determines whether the specified node could be added as a child 
		/// node. </summary>
		/// <remarks> This does not necessarily mean that the child node accepts this node as its owner, just that this node accepts the potential child. </remarks>
		bool IsValidChild(INode AChild);

		/// <summary> Determines whether a given type is a valid child for the node. </summary>
		/// <remarks> This does not necessarily mean that the child node accepts this node as its owner, just that this node accepts the potential child. </remarks>
		bool IsValidChild(Type AChildType);
		
		/// <summary> Determines if the current node could be added as one of the specified owner's children. </summary>
		/// <remarks> This does not necessarily mean that the owner accepts this node as a child, just that this node accepts the potential owner. </remarks>
		bool IsValidOwner(INode AOwner);

		/// <summary> Determines if the current node could be added as one of the specified owner type's children. </summary>
		/// <remarks> This does not necessarily mean that the owner accepts this node as a child, just that this node accepts the potential owner. </remarks>
		bool IsValidOwner(Type AOwnerType);

		/// <summary> Finds the deepest host node in the node hierarchy. </summary>
		IHost HostNode { get; }
		
		/// <summary> Finds the first parent which implements the specified interface. </summary>
		/// <returns> The located node, or null if non found. </returns>
		INode FindParent(Type AType);
		
		/// <summary> Called when name is changed. </summary>
		event NameChangeHandler OnValidateName;
		
		/// <summary> Locates a node by name excluding the specified node. </summary>
		/// <remarks> Looks at the current node, then recurses to the children. </remarks>
		/// <returns> Located node.  Returns null if a node is not found. </returns>
		INode GetNode(string AName, INode AExcluding);
		
		/// <summary> Locates a node by name.</summary>
		/// <remarks> Looks at the current node, then recurses to the children. </remarks>
		/// <returns> Located node.  Returns null if a node is not found. </returns>
		INode GetNode(string AName);
		
		/// <summary> Locates a node by name. (See <see cref="GetNode"/>) </summary>
		/// <returns> Located node.  Throws an exception if a node is not found. </returns>
		INode FindNode(string AName);
		
		/// <summary> Node is transitioning to/from active state. </summary>
		bool Transitional { get; }

		/// <summary> Indicates whether the node (tree) is currently active. </summary>
		/// <remarks>
		///		Except for <see cref="IHost"/>, nodes are active when they have a parent, and the parent is active.
		///		Individual nodes cannot have disparate active states, activity is tree-wide. Nodes are 
		///		automatically activated when inserted into an already active node tree. Active is false while 
		///		transitional.
		/// </remarks>
		bool Active { get; }

		/// <summary> Propigates an event object. </summary>
		void BroadcastEvent(NodeEvent AEvent);
		
		/// <summary> Handles/screens a node event. </summary>
		void HandleEvent(NodeEvent AEvent);

		// Published
		
		/// <summary> The name used to allow the node to be uniquely identified. </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: auto generated unique name</para></value>
		/// <remarks> A name is automatically generated at creation time. The name can
		/// be changed but must be unique. The name can be qualified. If the name is
		/// qualified, when a variable is declared for the component within a script
		/// action, all qualifiers (periods) will be replaced with underscores to 
		/// construct the name of the variable.</remarks>
		string Name { get; set; }
		
		/// <summary> A user-defined scratch-pad for use by the application. </summary> <doc/>
		/// <value> Object</value>
		/// <remarks> The UserData property can be used to store any type of data.
		/// The data is available only until the form on which the componant 
		/// exists is closed i.e. rejected, or accepted. </remarks>
		object UserData { get; set; }
	}

	/// <summary>Generic container for non-visual frontend modules.</summary> <doc/>
	public interface IModule { }
	
	/// <summary>
	/// Node event handler delegate.
	/// </summary>
	public delegate void NodeEventHandler(INode ANode, EventParams AParams);
	
	/// <summary>
	/// An interface implemented to allow synchronous chaining.
	/// </summary>
	public interface IBlockable : INode
	{
		/// <summary>
		/// Invoked when the component implementing the IBlockable interface is done executing.
		/// </summary>
		event NodeEventHandler OnCompleted;
	}

	public interface IEnableable
	{
		/// <summary> True if the node is actually enabled. </summary>
		/// <value> Boolean: True|False </value>
		/// <remarks> The actual enabled state of the node is based on the enabled 
		/// flag AND possibly other conditions. </remarks>
		bool GetEnabled();

		// Published

		/// <summary> When this is true and other requisits are satisfied, 
		/// the node is in a proper state to be executed. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		/// <remarks> Even if Enabled is true, the node may not be truly enabled, 
		/// just that it is allowed to be enabled. </remarks>
		bool Enabled { get; set; }
	}

	/// <summary> An abstract node which is associated with an action. </summary>
	public interface IActionNode : INode, IEnableable, IVisual
	{
		/// <summary> Gets the actual text to use. </summary>
		string GetText();

		// Published

		/// <summary> A text string that will be used by this node. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> If this is not set the text property of the action will be used. </remarks>
		string Text { get; set; }

		/// <summary> Associated Action node. </summary> <doc/>
		IAction Action { get; set; }
	}
	
	/// <summary> Used by MemberNameConverter to determine the referenced node. </summary>
	public interface INodeReference
	{ 
		INode Node { get; } 
	}

	/**************  Enumerations, Delegates, Structs/Classes **************/

	/// <summary> A request object used by IHost.NextRequest. </summary>
	public class Request
	{
		/// <summary> Creates a new request for a Document. </summary>
		public Request(string document) : base()
		{
			_document = document;
		}

		// Document

		private string _document;

		/// <summary> The reference to the page.  </summary>
		public string Document
		{
			get { return _document; }
			set { _document = value; }
		}
	}

	/// <summary> Base for all node events. </summary>
	public abstract class NodeEvent
	{
		/// <summary> Used to tell parent classes that this event has been handled. </summary>
		public bool IsHandled;

		/// <summary> Handle this event. </summary>
		/// <param name="node"> Node that will be used to handle the event. </param>
		/// <remarks> Base implementation does nothing. </remarks>
		public virtual void Handle(INode node) {}
	}

	/// <summary> A delegate definition for to handle name changes. (<see cref="Node.OnValidateName"/>). </summary>
	public delegate void NameChangeHandler(object ASender, string AOldName, string ANewName);

}