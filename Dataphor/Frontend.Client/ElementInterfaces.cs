/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;

namespace Alphora.Dataphor.Frontend.Client
{
	/**************  Nodes  **************/

	/// <summary> Used on elements that have to be aligned horizontally within their 
	/// allocated space when they are givin more space than they can use. </summary>
	public interface IHorizontalAlignedElement
	{
		/// <summary>
		/// The horizontal alignment that is to be used when there is more space
		/// than needed by the control.
		/// </summary> <doc/>
		/// <value> HorizontalAlignment: Left|Center|Right</value>
		HorizontalAlignment HorizontalAlignment { get; set; }
	}

	/// <summary> Used on elements that have to be aligned vertically within their 
	/// allocated space when they are givin more space than they can use. </summary>
	public interface IVerticalAlignedElement
	{
		// Published

		/// <summary>
		/// VerticalAlignment specified the vertical placement of the control given extra space.
		/// </summary> <doc/>
		/// <value> <para>VerticalAligmnent: Top|Middle|Bottom</para>
		/// <para>Default: Top</para></value>
		/// <remarks>
		/// When this control is given more space than it can use, the 
		/// VerticalAlignment property specifies where the control wil be placed
		/// within the available space.
		/// </remarks>
		VerticalAlignment VerticalAlignment { get; set; }
	}

	/// <summary> Arranges child elements horizontally in a row. </summary> <doc/>
	/// <remarks> May contain multiple elements as children. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface IRow : IElement, IHorizontalAlignedElement
	{
	}

	/// <summary> Arranges child elements vertically in a column. </summary> <doc/>
	/// <remarks> May contain multiple elements as children. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface IColumn : IElement, IVerticalAlignedElement
	{
	}

	/// <summary> Element that provides a titled grouping of sub-elements. </summary> <doc/>
	/// <remarks> May contain only one element as a child. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface IGroup : IElement
	{
		// Published

		/// <summary> A text string that will be used to describe the group. </summary> <doc/>
		/// <value><para>String</para>
		/// <para> Default: empty string</para></value>
		string Title { get; set; }
	}

	/// <summary> A date/time control is a data aware component that is 
	/// bound to a data source. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface IDateTimeBox : IAlignedElement
	{
		/// <summary> Determines whether or the not the check box will 
		/// automatically update the underlying value when checked. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: false</para></value>
		bool AutoUpdate { get; set; }
		
		/// <summary> Sets the number of milliseconds to wait before updating 
		/// the underlying value when checked. </summary> <doc/>
		/// <value> <para>Integer: </para>
		/// <para>Default: 200</para></value>
		int AutoUpdateInterval { get; set; }
	}

	/// <summary> Allows the triggering of an action (button). </summary> <doc/>
	/// <remarks> The trigger is used to display a button on the form that can
	/// be pressed to execute an action.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface ITrigger : IElement, IEnableable, IVerticalAlignedElement
	{
		/// <summary> Gets the actual text to use. </summary>
		string GetText();

		// Published

		/// <summary> A text string that describes the actions of this trigger. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> If this is not set, the text property of the action 
		/// will be used.</remarks>
		string Text { get; set; }

		/// <summary> The action to be triggered. </summary> <doc/>
		/// <value> IAction</value>
		/// <remarks> An action available in the same node tree.</remarks>
		IAction Action { get; set; }

		/// <summary> The pre-configured width of the image. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 0</para></value>
		/// <remarks> The width in pixels. 0 (zero) indicated the natural width of
		/// the image is to be used.</remarks>
		int ImageWidth { get; set; }

		/// <summary> The pre-configured height of the image. </summary> <doc/>
		/// <value><para>Integer</para>
		/// <para>Default: 0</para></value>
		/// <remarks> The height in pixels. 0 (zero) indicated the natural height of
		/// the image is to be used.</remarks>
		int ImageHeight { get; set; }
	}

	/// <summary> User interface root node. </summary> <doc/>
	public interface IInterface : IElement
	{
		/// <summary> Use to ensure the existence of a MainSource. </summary>
		void CheckMainSource();

		/// <summary> Gets the actual text to use. </summary>
		string GetText();

		/// <summary> Performs the default action for the interface. </summary>
		void PerformDefaultAction();

		/// <summary> Requests that any child data changes be posted. </summary>
		void PostChanges();

		/// <summary> Requests that any child data changes be posted. </summary>
		void PostChangesIfModified();

		/// <summary> Requests that any child data changes be canceled. </summary>
		void CancelChanges();

		// Published

		/// <summary> A text string that describes the user interface. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string Text { get; set; }

		/// <summary> The primary data source for this interface. </summary> <doc/>
		/// <value> <para>ISource</para>
		/// <para>Default: (None)</para></value>
		/// <remarks> A source available in the same node tree.</remarks>
		ISource MainSource { get; set; }

		/// <summary> An action to invoke as the default for the form. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		/// <remarks> 
		///	<para>This action will only be invoked by a form if it is not in an input mode. </para>
		///	<para>If this property is not set, then the form will be Accepted or Closed. </para>
		///	</remarks>
		IAction OnDefault { get; set; }

		/// <summary> An action to invoke when the interface is shown. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnShown { get; set; }

		/// <summary> Action taken when data is posted. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnPost { get; set; }

		/// <summary> Action taken when data is canceled. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnCancel { get; set; }

		/// <summary> Action taken when the user interface is activated. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnActivate { get; set; }

		/// <summary> Action taken after the user interface is activated. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnAfterActivate { get; set; }

		/// <summary> Action taken before when the user interface is deactivated. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnBeforeDeactivate { get; set; }

		/// <summary> Dictionary of non-persisted miscellenious data. </summary> <doc/>
		IndexedDictionary<string, object> UserState { get; }
	}

	/// <summary> Data aware image element. </summary> <doc/>
	public interface IImage : ITitledElement
	{
		/// <summary> Unused common property. </summary>
		int Width { get; set; }

		// Published

		/// <summary> The configured width of the image. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: -1</para></value>
		/// <remarks> If -1, the actual size of the image is used.</remarks>
		int ImageWidth { get; set; }

		/// <summary> The configured height of the image. </summary> <doc/>
		/// <value><para>Integer</para>
		/// <para>Default: -1</para></value>
		/// <remarks> If -1, the actual size of the image is used.</remarks>
		int ImageHeight { get; set; }

		/// <summary> Specifies how to fit the image into the specified space. </summary> <doc/>
		/// <value> <para>StretchStyles: StretchRatio|StretchFill|NoStretch</para>
		/// <para>Default: StretchRatio</para></value>
		/// <remarks>
		/// <para>This value is ignored if ImageWidth and ImageHeight are -1.</para>
		/// <para>StretchRatio - Stretches the image to fille the specified size while maintaining the image's aspect ratio.</para>
		///	<para>StretchFill - Stretches the image to fill the specified size.</para>
		///	<para>NoStretch - Maintain original image size.</para>
		/// </remarks>
		StretchStyles StretchStyle { get; set; }

		/// <summary> Specifies whether to center the image. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para> Default: True </para></value>
		bool Center { get; set; }

		/// <summary> An object capable of loading an Image. </summary> <doc/>	 	
		IImageSource ImageSource { get; } 		
	}

	public interface IImageSource 
	{
		System.IO.Stream Stream { get; }
		void LoadImage();
		bool Loading { get; }
	}

	/// <summary> Displays a read-only representation of a data field. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface IText : IAlignedElement
	{
		// Published

		/// <summary> The height (in rows) of the text control (for multi-line text). </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 1</para></value>
		int Height { get; set; }
	}

	/// <summary> Base interface for TextBox-based controls. </summary>
	public interface ITextBoxBase : IAlignedElement
	{
		// Published

		/// <summary> Determines if the source column can be set to nil
		/// by the control. 
		/// </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: true</para></value>
		/// <remarks> When true, setting the contents of the control 
		/// to an empty string ('""'), will clear (set to nil) the source column.
		/// </remarks>
		/// 
		bool NilIfBlank { get; set; }
		/// <summary> Determines whether or the not the check box will 
		/// automatically update the underlying value when checked. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: false</para></value>
		bool AutoUpdate { get; set; }
		
		/// <summary> Sets the number of milliseconds to wait before updating 
		/// the underlying value when checked. </summary> <doc/>
		/// <value> <para>Integer: </para>
		/// <para>Default: 200</para></value>
		int AutoUpdateInterval { get; set; }
	}

	/// <summary> A TextBox is a data-aware control component that allows 
	/// the display of data from a column. Optionally the data can be editing. 
	/// </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample01 [dfd] document.</example>
	public interface ITextBox : ITextBoxBase
	{
		// Published

		/// <summary> The number of character rows to use when editing. </summary> <doc/>
		/// <value> <para>An Integer value of at least 1.</para>
		/// <para>Default: 1</para></value>
		/// <remarks> This is the approximate natural height of the textbox. 
		/// The height will automatically adjust as needed from a minimum height of 1
		/// to a maximum needed to fill the form. 
		/// </remarks>
		int Height { get; set; }

		/// <summary> Determines whether the actual characters or masking 
		/// characters will be displayed. </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: false</para></value>
		/// <remarks> If true, masking characters are used instead of the actual 
		/// characters. 
		/// </remarks>
		bool IsPassword { get; set; }

		/// <summary> Used to determine whether to scroll horizontally or wrap text. </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: false</para></value>
		/// <remarks> This property is only effective when the Height property 
		/// is greater than one.
		/// </remarks>
		bool WordWrap { get; set; }

		/// <summary> Used to determine if the textbox will accept tab characters.
		/// </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: false</para></value>
		/// <remarks> This property is only effective when the Height property is 
		/// greater than one. When set to true the textbox will accept tab 
		/// characters (rather than allowing them to be handled by the form). 
		/// </remarks>
		bool AcceptsTabs { get; set; }

		/// <summary> Used to determine if the textbox will accept carrage return 
		/// characters. </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: true</para></value>
		/// <remarks> When set to true and the Height of the textbox is > 1, the 
		/// textbox will accept carrage return characters. 
		/// </remarks>
		bool AcceptsReturn { get; set; }
		
		/// <summary> Indicates the maximum number of characters that can be 
		/// entered into the textbox. </summary> <doc/>
		/// <value> <para>An Integer value of at least -1.</para>
		/// <para>Default: -1</para></value>
		/// <remarks> A value of -1 indicates that the entry is 
		/// limited by the underlying control's default.  A value of 0 indicates that 
		/// the entry is limited by the underlying control's maximum value.  Any other 
		/// positive value, up to the underlying control maximum value, will set the 
		/// maximum number of characters to that value. 
		/// Common maximum values are 32766 and 2147483646.
		/// </remarks>
		int MaxLength { get; set; }
	}

	/// <summary> A NumericTextBox is a data aware control that allows 
	/// the display of numeric data from a column. Optionally the data can be editing. 
	/// </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample09 [dfd] document.</example>
	public interface INumericTextBox : ITextBoxBase
	{
		// Published
		
		/// <summary> The increment of the control when spinning slow.</summary> <doc/>
		/// <value> <para>Integer: must be greater than 0.</para>
		/// <para>Default: 1</para></value>
		/// <remarks>This property does not restrict possible values. 
		/// </remarks>
		int SmallIncrement { get; set; }
		
		/// <summary> The increment of the control when spinning fast. </summary> <doc/>
		/// <value> <para>Integer: must be greater than 0.</para>
		/// <para>Default: 10</para></value>
		/// <remarks>This property does not restrict possible values.
		/// </remarks>
		int LargeIncrement { get; set; }
	}

	/// <summary> A TextEditor is a data aware programmers editor control. </summary> <doc/>
	public interface ITextEditor : ITitledElement
	{
		// Published

		/// <summary> Determines if the source column can be set to nil
		/// by the control. 
		/// </summary> <doc/>
		/// <value> <para>Boolean: true|false</para>
		/// <para>Default: true</para></value>
		/// <remarks> When true, setting the contents of the control 
		/// to an empty string ('""'), will clear (set to nil) the source column.
		/// </remarks>
		bool NilIfBlank { get; set; }

		/// <summary> The number of character rows to use when editing. </summary> <doc/>
		/// <value> <para>An Integer value of at least 1.</para>
		/// <para>Default: 1</para></value>
		/// <remarks> This is the approximate natural height of the textbox. 
		/// The height will automatically adjust as needed from a minimum height of 1
		/// to a maximum needed to fill the form. 
		/// </remarks>
		int Height { get; set; }

		/// <summary> The type of document being edited (D4, SQL, CS, Default, etc.). </summary>
		string DocumentType { get; set; }
	}
		
	/// <summary> The Search component is a grouping control used to implement
	/// incremental searching of the data source. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample07 [dfd] document.</example>
	public interface ISearch : IDataElement
	{
		// Published

		/// <summary> Specifies how titles are aligned relative to the search controls. </summary> <doc/>
		/// <value> <para>Top, Left, or None.</para>
		/// <para>Default: Left</para></value>
		TitleAlignment TitleAlignment { get; set; }
	}

	/// <remarks>
	/// The row in the dataset that is the current row is indicated on the grid.
	/// The current row can be changed by clicking on another row.  
	/// A grid can display any number of columns but need not display 
	/// every column available from the source.  Columns are displayed using 
	/// the child elements TextColumn, ImageColumn, and/or CheckBoxColumn.
	/// Two other column elements can be used with the GridColumn:
	/// The SequenceColumn provides a drag and drop capability on the grid.
	/// The TriggerColumn provides a way to select a row and execute an action.
	/// </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample03 [dfd] document.</example>
	public interface IGrid : IDataElement
	{
		// Published

		/// <summary> The desired number of rows to display in the grid (by default).
		/// </summary> <doc/>
		/// <value><para>Integer: must be greater than 0</para>
		/// <para>Default: 10</para></value>
		/// <remarks>
		/// The number of rows will automatically adjust as needed from a minimum 
		/// of 1 to a maximum needed to fill the form. When the form is first
		/// derived, if possible the number of rows displayed will be the RowCount.
		/// </remarks>
		int RowCount { get; set; }

		/// <summary> The natural number of columns to show without scrolling (if possible). </summary> <doc/>
		/// <value><para>Integer: must be -1 or 1 or more. </para><para>Default: -1</para></value>
		/// <remarks> A value of -1 indicates that the total number of visible columns should be used.  If 
		/// this value is set to less than 1, it will be treated as -1.</remarks>
		int ColumnCount { get; set; }
		
		/// <summary> Action that takes place when the grid is double clicked. </summary> <doc/>
		/// <value> <para>IAction: An action available in the same node tree.</para>
		/// <para>Default: (None)</para></value>
		/// <remarks> If no action is selected, the action selected in the OnDefault
		/// property of the parent interface (found in the Root Form Node)
		/// will be used. </remarks>
		IAction OnDoubleClick { get; set; }

		/// <summary> Use Natural Width as the Max Width. </summary> <doc/>
		/// <value> <para>Boolean</para>
		/// <para>Default: (false)</para></value>  		
		bool UseNaturalMaxWidth { get; set; }
													
	}

	public interface ILookupElement : IElement, ILookup, IReadOnly
	{ 
		/// <summary> Resets the auto-lookup feature, allowing auto-lookup 
		/// to take place when focus is navigated to the control again. </summary>
		/// <remarks> 
		///		Ordinarily, auto-lookup only occurs once for the lookup control instance (to avoid annoying the user with unnecessary lookup 
		///		behavior during tabstop navigation).  ResetAutoLookup() sets/resets the control so that the next time the control is focused, 
		///		auto-lookup will occur (if the AutoLookup property is true). 
		///	</remarks>
		void ResetAutoLookup();

		// Published

		/// <summary> When true, the control automatically invokes its lookup when focus reaches it. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para> Default: False</para> </value>
		/// <remarks> AutoLookup only occurs once for the control, unless ResetAutoLookup() 
		/// is invoked.  In addition, an auto-lookup is only performed if the dataset is 
		/// empty or at least one column involved in the lookup does not have a value.  Note that 
		/// defaults might prevent the auto-lookup by setting a value for all of the columns involved.</remarks>
		bool AutoLookup { get; set; }

		/// <summary> When true, the control will still perform an auto-lookup even when all of the columns are not nil. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para> Default: False</para> </value>
		bool AutoLookupWhenNotNil { get; set; }
	}

	/// <summary> A data-aware container that allows the lookup of typically one value.</summary> <doc/>
	/// <remarks> <para>The hot-key for the QuickLookup is Alt-DownArrow or
	/// Insert. Either one will display the document defined in the Document 
	/// property when the QuickLookup has focus.</para>
	/// <para> The QuickLookup can contain any controls, but will typically contain a 
	/// single, editable control that is associated with the same column.</para>
	/// </remarks>
	public interface IQuickLookup : ILookupElement, IVerticalAlignedElement
	{
		// Published

		/// <summary> The column in the data source to look up. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		string ColumnName { get; set; }

		/// <summary> A column name which will to retrieved from the lookup data source. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks><para>When a selection is made from the document, the value in this column 
		/// will be copied to the column identified in the ColumnName property.</para>
		/// </remarks>
		string LookupColumnName { get; set; }
	}
	
	/// <summary> A data-aware group container that allows the collective 
	/// lookup of one or more data values. </summary> <doc/>
	/// <remarks>
	/// The FullLookup is used to look up one or more column values while showing
	/// any number on controls.  The controls contained in the full lookup do not
	/// have to be associated with the columns being looked-up, but they typically
	/// will be either associated with the lookup columns, or controls representing 
	/// the lookup values.  Typically, the child controls contained in this control 
	/// should be made read-only.  A full lookup is usually used when there is more
	/// than one column being looked up, or the developer would rather show a
	/// descriptor column rather than the actual value being looked-up. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample04 [dfd] document.</example>
	public interface IFullLookup : ILookupElement
	{
		string GetTitle();

		// Published

		/// <summary> The description of the lookup. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para> </value>
		/// <remarks> </remarks>
		string Title { get; set; }

		/// <summary> The column or columns in the data source to look up. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> When more than one column is specified, they must be seperated
		/// by a comma or semicolon. </remarks>
		string ColumnNames { get; set; }

		/// <summary> A set of comma or semicolon seperated column names which are 
		/// to be retrieved from the lookup data source. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks><para>When a selection is made from the document, the value in these columns 
		/// will be copied to the columns identified by the ColumnNames property.</para>
		/// <para>When more than one column is specified, they must be seperated
		/// by a comma or semicolon. </para>
		/// </remarks>
		string LookupColumnNames { get; set; }
	}

	/// <summary> A check box component that displays/edits a boolean data field. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample09 [dfd] document.</example>
	public interface ICheckBox : IColumnElement
	{
		// Published

		// Width only here because it is a common property
		/// <summary> Width has no affect for the CheckBox. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 0</para></value>
		int Width { get; set; }

		/// <summary> Determines whether or the not the check box will 
		/// automatically update the underlying value when checked. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool AutoUpdate { get; set; }
		
		/// <summary> Sets the number of milliseconds to wait before updating 
		/// the underlying value when checked. </summary> <doc/>
		/// <value> <para>Integer: </para>
		/// <para>Default: 200</para></value>
		int AutoUpdateInterval { get; set; }

		/// <summary> Determines the CheckState transition sequence for CheckBoxes with three-states. </summary> <doc/>
		/// <example>Frontend.CheckBox.TrueFirst='true'
		/// provides the checkstate sequence nil->true(checked)->false(unchecked). Frontend.CheckBox.TrueFirst='false' provides
		/// the checkstate sequence nil->false(uncheck)->true(checked).
		/// </example>
		/// <value> <para>Boolean: </para>
		/// <para>Default: true</para></value>
		bool TrueFirst { get; set; }
	}

	/// <summary> Choice is a control which presents a pre-determined set of 
	/// values as a choice (radio group). </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample09 [dfd] document.</example>
	public interface IChoice : IColumnElement
	{
		// Published

		/// <summary> Comma or semicolon separated list of available name-value 
		/// pairs to be listed as choices. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> Spaces that are put into this text will be maintained.  
		/// Pairs are of the form "name=value" (without the quotes). </remarks>
		/// <example> Male=M,Female=F</example>
		string Items { get; set; }

		/// <summary>Determines whether or not the choice control will 
		/// immediately update the underlying value when changed. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: false</para></value>
		/// <remarks> If false, the underlying value will be 
		/// updated when focus leaves the control. </remarks>
		bool AutoUpdate { get; set; }
		
		/// <summary> Sets the number of milliseconds to wait before updating 
		/// the underlying value when checked. </summary> <doc/>
		/// <value> <para>Integer: </para>
		/// <para>Default: 200</para></value>
		int AutoUpdateInterval { get; set; }

		/// <summary> The number of columns to display radio buttons in. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 1</para></value>
		/// <remarks> There must be at least one column. </remarks>
		int Columns { get; set; }
	}

	/// <summary> Displays data from the data source in a tree. </summary> <doc/>
	/// <remarks> <para>The Expression in the Source, and the RootExpression, 
	/// ParentExpression, and ChildExpression are all closely interconnected.</para>
	/// <para>The Expression of the Source must not eliminate any rows needed by the 
	/// expressions of the tree.  The Expression of the Source must not allow rows 
	/// that will not be used by the expressions of the tree.
	/// i.e. The Expression of the Source must return every row that is a part of the 
	/// tree to be displayed and must not return any rows that are not a part of the 
	/// tree to be displayed.</para>
	/// </remarks>
	public interface ITree : IDataElement
	{
		// Published
		
		/// <summary>The expression defining the root nodes of the tree.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks>
		///	The columns in this result must include the order columns for 
		///	the data source of the tree, and the ColumnName.  The master 
		///	key and other parameters of the associated DataView are 
		///	available as variables such as AMasterXXX (where XXX 
		///	is the name of the detail column with '.'s changed to '_'s).
		///	</remarks>
		string RootExpression { get; set; }
		
		/// <summary>The expression used to determine the child node(s) for 
		/// any given node of the tree.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks>
		///	The values for the current key are available as variables 
		///	named ACurrentXXX, where XXX is the name of the key column, 
		///	within this expression. The columns in this result must 
		///	include the order columns for the data source of the tree, 
		///	and the ColumnName.
		/// </remarks>
		string ChildExpression { get; set; }
		
		/// <summary>The expression used to determine the parent node for 
		/// any given node of the tree.</summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks>
		///	The values for the current key are available as variables 
		///	named ACurrentXXX, where XXX is the name of the key column, 
		///	within this expression. The columns in this result must 
		///	include the order columns for the data source of the tree, 
		///	and the ColumnName. If this result returns more than one 
		///	row, only the first row will be used.
		/// </remarks>
		string ParentExpression { get; set; }

		/// <summary> Approximate width (in characters) of the control. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 25</para></value>
		int Width { get; set; }

		/// <summary> Height (in rows) of the control. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 20</para></value>
		int Height { get; set; }
	}
 
	/// <summary> Non data aware control for loaded an image from the server. </summary> <doc/>
	public interface IStaticImage : IElement, IVerticalAlignedElement, IHorizontalAlignedElement
	{
		// Published

		/// <summary> The width in pixels of the image. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: -1</para></value>
		/// <remarks> If this value is -1, the actual width of the loaded image is used. </remarks>
		int ImageWidth { get; set; }

		/// <summary> The height in pixels of the image. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: -1</para></value>
		/// <remarks> If this value is -1, the actual height of the loaded image is used. </remarks>
		int ImageHeight { get; set; }

		/// <summary> Specifies how to fit the image into the specified space. </summary> <doc/>
		/// <value> <para>StretchStyles: StretchRatio|StretchFill|NoStretch</para>
		/// <para>Default: StretchRatio</para></value>
		/// <remarks>
		/// <para>This value is ignored if ImageWidth and ImageHeight are -1.</para>
		/// <para>StretchRatio - Stretches the image to fille the specified size while maintaining the image's aspect ratio.</para>
		///	<para>StretchFill - Stretches the image to fill the specified size.</para>
		///	<para>NoStretch - Maintain original image size.</para>
		/// </remarks>
		StretchStyles StretchStyle { get; set; }

		/// <summary> The specified image expression to load. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <example>Image("Frontend","Warning")</example>
		string Image { get; set; }
	}
 
	/// <summary> Non data aware control for the display of text. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample08 [dfd] document.</example>
	public interface IStaticText : IElement
	{
		// Published

		/// <summary> A text string to display. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The text is wrapped at <see cref="Width"/> characters. 
		/// </remarks>
		string Text { get; set; }

		/// <summary> The maximum width in characters of each line of text. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 40</para></value>
		int Width { get; set; }
	}

	/// <summary> A control for showing the contents of a particular URL or folder. </summary> <doc/>
	/// <remarks> Will display the contents of a web URL or a local or network folder.
	/// The contents will be displayed in the control.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample05 [dfd] document.</example>
	public interface IHtmlBox : IElement
	{
		// Published

		/// <summary> The natural width in pixels of the html box. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 100</para></value>
		int PixelWidth { get; set; }

		/// <summary> The natural height in pixels of the html box. </summary> <doc/>
		/// <value> <para>Integer</para>
		/// <para>Default: 100</para></value>
		int PixelHeight { get; set; }

		/// <summary> An address to a web page or local or network folder 
		/// as would be entered into the Address bar of the Windows Explorer
		/// or Internet Explorer. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		/// <example> http://www.alphora.com </example>
		string URL { get; set; }
	}

	/// <summary> A Notebook is used to provide one or more Tabs on the user
	/// interface form.</summary> <doc/>
	/// <remarks> Each tab is made by adding NotebookPage or NotebookFramePage 
	/// controls as children to the Notebook control.</remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface INotebook : IElement
	{
		// Published
		
		/// <summary> The currently active notebook page. </summary> <doc/>
		/// <value> IBaseNotebookPage</value>
		/// <remarks> The name of one of the child pages. </remarks>
		IBaseNotebookPage ActivePage { get; set; }

		/// <summary> Action triggered when the active page changes. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para></value>
		/// <remarks> An action in the same node tree.</remarks>
		IAction OnActivePageChange { get; set; }
	}

	public interface IBaseNotebookPage : IElement
	{
		// Published

		/// <summary> The title for the notebook page. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string Title { get; set; }
	}

	/// <summary> A notebook page element that contains other elements. </summary> <doc/>
	/// <remarks> May contain one element as a child. </remarks>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface INotebookPage : IBaseNotebookPage
	{

	}

	/// <summary> A notebook page element that embeds another interface document. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface INotebookFramePage : IBaseNotebookPage, ISourceLink
	{
		/// <summary> The root interface element contained in this frame page. </summary>  <doc/>
		IFrameInterface FrameInterfaceNode { get; }

		// Published

		/// <summary> Specifies the document of the interface to embed. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string Document { get; set; }

		/// <summary> Specifies the text for merged menu. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string MenuText { get; set; }

		/// <summary> When true, the frame will only be loaded when the tab is selected; otherwise the 
		/// frame is loaded when activated. </summary> <doc/>
		/// <value> <para>Boolean: True|False</para>
		/// <para>Default: true</para></value>
		bool LoadAsSelected { get; set; }
	}

	/// <summary> </summary> <doc/>
	public interface IFile : ITitledElement
	{
		// Published

		/// <summary> The name of the column in the data source containing the extension 
		/// of the file that this controls is associated with. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> Must be a column from the source specified in the Source property.  This property is 
		/// optional.  If it is set, then the File control will know the type of file which enables certain
		/// functionality (such as Open). </remarks>
		string ExtensionColumnName { get; set; }

		/// <summary> Maximum size in bytes for documents to be loaded into this control. </summary> <doc/>
		/// <value> <para>Long</para> <para> Default: 30000000 </para></value>
		/// <remarks> If a user attempts to load a file larger than this into the control, they will receive and error. </remarks>
		long MaximumContentLength { get; set; }
	}

	/// <summary> Abstract interface which is common to all visible nodes. </summary>
	public interface IVisual
	{
		/// <summary> Specifies whether the node is actually visible. </summary>
		bool GetVisible();

		// Published

		/// <summary> Determines whether the control, 
		/// or controls associated with the action, will be visible.
		/// </summary> <doc/>
		/// <value> <para>Boolean: True|False </para>
		/// <para>Default: true</para></value>
		/// <remarks>
		/// <para>If the component is a control, when Visible is set to false 
		/// the control will not be visible on the form. If the control is the
		/// parent of other controls, all decendent controls will also be hidden.</para>
		/// <para>If the component is an action, when Visible is set to false
		/// the control(s) associated with the action will not be 
		/// visible on the form.</para>
		///	</remarks>
		bool Visible { get; set; }
	}

	/// <summary> Abstract interface for all visual nodes. </summary>
	public interface IElement : INode, IVisual
	{
		/// <summary> Gets the actual hint to use for this element. </summary>
		string GetHint();

		/// <summary> Gets the actual tab stop value for this element. </summary>
		bool GetTabStop();



		// Published

		/// <summary> Text which describes the element to the end-user. </summary> <doc/>
		/// <value> <para>String </para>
		/// <para>Default: empty string</para></value>
		/// <remarks> The Hint text will be displayed in the bar at the bottom of
		/// the form when the control has focus.</remarks>
		string Hint { get; set; }

		/// <summary> Determines if the control can be focused through tab key navigation. </summary>
		/// <value> Boolean: True|False </value>
		/// <remarks> When false, the control will not gain focus using the tab key. </remarks>
		bool TabStop { get; set; }

		/// <summary> The left margin for the control. </summary> <doc/>
		/// <value> Integer: number of pixels </value>
		int MarginLeft { get; set; }

		/// <summary> The right margin for the element. </summary> <doc/>
		/// <value> Integer: number of pixels </value>
		int MarginRight { get; set; }

		/// <summary> The top margin for the element. </summary> <doc/>
		/// <value> Integer: number of pixels </value>
		int MarginTop { get; set; }

		/// <summary> The bottom margin for the element. </summary> <doc/>
		/// <value> Integer: number of pixels </value>
		int MarginBottom { get; set; }

		/// <summary> The help keyword to navigate to when 
		/// the user activates help. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> The keyword will locate help based on an index term, 
		/// topic name, etc. depending on the value 
		/// of <see cref="HelpKeywordBehavior"/>. </remarks>
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

		/// <summary> Specifies the help text to display to the user when 
		/// keyword navigation is not used. </summary> <doc/>
		/// <value><para>String</para>
		/// <para>Default: empty string</para></value>
		string HelpString { get; set; }
		
		/// <summary> The style name to use for this element. </summary> <doc/>
		/// <value><para>String</para><para>Default: empty string</para></value>
		string Style { get; set; }
	}

	/// <summary> FormInterface element node. </summary>
	/// <remarks> A host for a root interface node. </remarks>
	public interface IFormInterface : IInterface
	{
		/// <summary> Shows the form non-modally. </summary>
		void Show();

		/// <summary> Shows the form non-modally using the given data mode. </summary>
		void Show(FormMode AFormMode);

		/// <summary> Shows the form in a certain mode. </summary>
		/// <remarks> 
		///		If AOnAcceptForm or AOnRejectForm are passed, the form will act modally (with 
		///		"Accept/Reject" buttons).  This method will always return right away (the modality 
		///		is simulated). 
		///	</remarks>
		/// <param name="AFormMode"> The data mode of the form. </param>
		void Show(IFormInterface AParentForm, FormInterfaceHandler AOnAcceptForm, FormInterfaceHandler AOnRejectForm, FormMode AFormMode);

		FormMode Mode { get; }

		/// <summary> Notifies the user (in a passive manner) that errors occurred while loading this interface. </summary>
		void EmbedErrors(ErrorList AErrorList);
		
		bool Close(CloseBehavior ABehavior);
		bool IsAcceptReject { get; }
		bool AcceptEnabled { get; set; }

		void Enable();
		void Disable(IFormInterface AChildForm);
		bool GetEnabled();

		event EventHandler OnClosed;
		
		/// <summary> Action taken before when the user interface is Accepted. </summary> <doc/>
		/// <value> <para>IAction</para>
		/// <para>Default: (None)</para> </value>
		IAction OnBeforeAccept { get; set; }			

		// Published

		/// <summary> If true, the accept/reject state is forced. </summary> <doc/>
		/// <value>Default: False</value>
		bool ForceAcceptReject { get; set; }
		
		/// <summary> If true, the form should be remain in front of other non top-most forms. </summary>
		bool TopMost { get; set; }
	}

	/// <summary> A Frame control embeds a user interface document as an element. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface IFrame : IElement, ISourceLink
	{
		/// <summary> The root interface element contained in this frame page. </summary>
		IFrameInterface FrameInterfaceNode { get; }

		// Published

		/// <summary> Determines whether the frame will automatically request a post of an embedded interface before closing it. </summary> <doc/>
		/// <remarks> This auto-posting happens before the BeforeCloseEmbedded event. </remarks>
		/// <value> <para>Boolean</para>
		/// <para>Default: false for Frames, true for NotebookFramePages</para></value>
		bool PostBeforeClosingEmbedded { get; set; }

		/// <summary> An action that will be executed before closing an embedded interface. </summary> <doc/>
		/// <remarks> The AInterface argument contains the frame being closed. </remarks>
		IAction BeforeCloseEmbedded { get; set; }

		/// <summary> The Document of the interface to embed. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string Document { get; set; }

		/// <summary> Filter (string expression) to apply to the main source 
		/// of the target form. </summary> <doc/>
		/// <value> <para>String:</para>
		/// <para>Default: empty string</para></value>
		string Filter { get; set; }

		/// <summary> The text used for the menu in which sub menus of this frame are added. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para>Default: empty string</para></value>
		string MenuText { get; set; }
	}

	/// <summary> Embedded Interface node. </summary>
	public interface IFrameInterface : IInterface 
	{
		IFrame Frame { get; }
	}

	/// <summary> An abstract element which has a data source reference. </summary>
	public interface IDataElement : IElement, ISourceReference, IReadOnly
	{
	}

	/// <summary> Abstract node that is associated with a specific column in 
	/// the data source. </summary>
	public interface IColumnElement : IDataElement
	{
		// Published

		/// <summary> The name of the column in the data source this element 
		/// is associated with. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> Must be a column from the source specified 
		/// in the Source property.</remarks>
		string ColumnName { get; set; }

		/// <summary> Text that describes the control. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para> </value>
		/// <remarks> 
		/// <para>The Title property is used as the title of the control and is
		/// displayed on the form.</para>
		/// <para>If the Title is set to an empty string the name of the column from 
		/// the ColumnName property will be used.</para>
		/// <para>If TitleAlignment is set to None no title will be displayed 
		/// for the control.</para>
		/// </remarks>
		string Title { get; set; }

	}

	public interface ITitledElement : IColumnElement, IVerticalAlignedElement
	{
		// Published

		/// <summary> The alignment of the title relative to the control. </summary> <doc/>
		/// <value> <para>TitleAlignment: Top|Left|None</para>
		/// <para>Default: Top</para></value>
		/// <remarks> When set to Top, the title will be displayed above the control. 
		/// When set to Left, the title will be displayed to the left of the control.
		/// When set to None, the title will not be displayed.
		/// </remarks>
		TitleAlignment TitleAlignment { get; set; }
	}

	public interface IAlignedElement : ITitledElement
	{
		// Published

		/// <summary> Specifies the alignment of the text within the control.
		/// </summary> <doc/>
		/// <value> <para>HorizontalAlignment: Left|Right|Center</para>
		/// <para>Default: Left</para></value>
		HorizontalAlignment TextAlignment { get; set; }

		/// <summary> Specifies the natural display width of the control. 
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

		/// <summary> Specifies the maximum display width of the control. </summary> <doc/>
		/// <value> <para>Integer (number of characters)</para>
		/// <para>Default: -1</para></value>
		/// <remarks> 
		///	As the control grows to display more data, this property limits
		///	the width the control will be allowed to grow to.  MaxWidth does 
		///	not control how many characters are allowed, only how the control 
		///	is displayed.  An value less than 1 allows the control to grow to fill 
		///	all available space.  The width is approximate and based on the average 
		///	character width of the current font.
		///	</remarks>
		int MaxWidth { get; set; }
	}

	public interface ILookup : INode, ISourceReference, IDocumentReference
	{
		void LookupFormInitialize(IFormInterface AForm);

		string GetColumnNames();

		string GetLookupColumnNames();

		// Published

		/// <summary> 
		/// A set of keys (comma or semicolon seperated) which provide the 
		/// values to filter the lookups' MainSource.  Used with DetailKeyNames 
		/// to create a master detail filter on the frames' MainSource. 
		/// </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> 
		/// Should also be set in the Document property if the lookups' 
		/// design is a derived page. 
		/// </remarks>
		string MasterKeyNames { get; set; }

		/// <summary> 
		/// A set of keys (comma or semicolon seperated) by which the lookups' 
		/// MainSource will be filtered. </summary> <doc/>
		/// <value> <para>String</para>
		/// <para> Default: empty string</para></value>
		/// <remarks> Used with MasterKeyNames to create a master detail filter 
		/// on the lookups' MainSource.  Should also be set in the Document 
		/// property if the frame's design is a derived page. </remarks>
		string DetailKeyNames { get; set; }
	}
	
	/// <summary>
	/// Indicates a class has the capability to enable or disable the layout.
	/// </summary>
	public interface ILayoutDisableable
	{
		/// <summary>
		/// Disables layout refreshes.  No layout refreshes should happen 
		/// until layout is reenabled with EnableLayout.  EnableLayout 
		/// must be called for each time DisableLayout is called.
		/// </summary>
		void DisableLayout();

		/// <summary>
		/// Reenables layout refreshes.  If Layout was called while layout 
		/// was disabled it will immideatly call Layout();
		/// </summary>
		void EnableLayout();
	}

	public interface IUpdateHandler
	{
		/// <summary> Indicates the beginning of a large update process. </summary>
		/// <remarks> Must be called symetrically with EndUpdate(). </remarks>
		void BeginUpdate();
		
		/// <summary> Indicates the end of a large update process. </summary>
		void EndUpdate();

		/// <summary> Indicates the end of a large update process. </summary>
		void EndUpdate(bool APerformLayout);
	}

	/**************  Enumerations & Delegates **************/

	// this is a duplication of DAE.Client.Controls.TitleAlignment
	public enum TitleAlignment {Top, Left, None}

	// this is a duplication of DAE.Client.Controls.VerticalAlignment
	public enum VerticalAlignment
	{
		Top,
		Middle,
		Bottom
	}

	// this is a duplication of System.Windows.Forms.HorizontalAlignment
	public enum HorizontalAlignment 
	{
		Center = 2,		//System.Windows.Forms.HorizontalAlignment.Center,
		Left = 0,		//System.Windows.Forms.HorizontalAlignment.Left,
		Right = 1		//System.Windows.Forms.HorizontalAlignment.Right
	}

	// this is a duplication of System.Windows.Forms.HelpNavigator
	public enum HelpKeywordBehavior
	{
		AssociateIndex = -2147483643,	//System.Windows.Forms.HelpNavigator.AssociateIndex
		Find = -2147483644,				//System.Windows.Forms.HelpNavigator.Find
		Index = -2147483645,			//System.Windows.Forms.HelpNavigator.Index
		KeywordIndex = -2147483642,		//System.Windows.Forms.HelpNavigator.KeywordIndex
		TableOfContents = -2147483646,	//System.Windows.Forms.HelpNavigator.TableOfContents
		Topic = -2147483647,			//System.Windows.Forms.HelpNavigator.Topic
		TopicId = -2147483641			//System.Windows.Forms.HelpNavigator.TopicId
	}

	public delegate void FormInterfaceHandler(IFormInterface AForm); 

	/// <summary> Defines the styles for stretching an image. </summary>
	/// <remarks>
	///		<list type="table">
	///			<listheader><term>Value</term><term>Description</term></listheader>
	///			<item><term>StretchRatio</term><term>Stretches the image to fille the specified size while maintaining the image's aspect ratio.</term></item>
	///			<item><term>StretchFill</term><term>Stretches the image to fill the specified size.</term></item>
	///			<item><term>NoStretch</term><term>Maintain original image size.</term></item>
	///		</list>
	/// </remarks>
	// This is a copy of DAE.Client.Controls.StretchStyles
	public enum StretchStyles {StretchRatio, StretchFill, NoStretch}
}
