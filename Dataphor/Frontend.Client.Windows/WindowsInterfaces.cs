/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public interface IWindowsFormInterface : IFormInterface, IWindowsElement, IUpdateHandler
	{
		/// <summary> Shows the form modally. </summary>
		/// <remarks> The method does not return until the form is closed. </remarks>
		WinForms.DialogResult ShowModal(FormMode AMode);

		/// <summary> Shows the form non-modally. </summary>
		/// <param name="AOnCloseForm"> Callback when the form closes. </param>
		void Show(FormInterfaceHandler AOnCloseForm);

		/// <summary> Adds an available action to the form. </summary>
		object AddCustomAction(string AText, System.Drawing.Image AImage, FormInterfaceHandler AHandler);
		void RemoveCustomAction(object AAction);
		void ClearCustomActions();

		/// <summary> Gets a description of the current default action. </summary>
		string GetDefaultActionDescription();

		IBaseForm Form { get; }
		bool AutoSize { get; set; }
		bool IsLookup { get; set; }
		Size FormMinSize();
		Size FormNaturalSize();
		Size FormMaxSize();
		bool SupressCloseButton { get; set; }
		void RootLayout();
		void FormClosed();
		bool FormClosing();
	}

	public interface IAccelerates
	{
		/// <summary> Gets the accelerator manager associated with the node. </summary>
		AcceleratorManager Accelerators { get; }
	}

	public interface IWindowsBarItem : IDisposable
	{
		bool Visible { get; set; }
	}

	public interface IWindowsBarSeparator : IWindowsBarItem
	{
	}

	public interface IWindowsBarButton : IWindowsBarItem, IToolStripItemContainer
	{
		string Text { get; set; }
		System.Drawing.Image Image { get; set; }
		bool Enabled { get; set; }
	}

	public interface IWindowsBarContainer : IWindowsBarItem
	{
		IWindowsBarContainer CreateContainer();
		IWindowsBarButton CreateMenuItem(EventHandler AHandler);
		IWindowsBarSeparator CreateSeparator();

		/// <summary> Adds a bar item . </summary>
		/// <remarks> A priority order can optionally be assigned to the item through the AGetPriorityHandler callback. </remarks>
		void AddBarItem(IWindowsBarItem AItem, GetPriorityHandler AGetPriority);
		/// <summary> Removes a bar item. </summary>
		void RemoveBarItem(IWindowsBarItem AItem);
	}

	public delegate int GetPriorityHandler(IWindowsBarItem AItem);

	public interface IWindowsExposedHost
	{
		IWindowsBarContainer ExposedContainer { get; }
	}

	public interface IWindowsMenuHost
	{
		IWindowsBarContainer MenuContainer { get; }
	}
	
	public interface IWindowsSearch : ISearch
	{
		DAE.Client.Controls.IncrementalSearch SearchControl { get; }
		int AverageCharPixelWidth { get; }
	}

	public interface IWindowsSearchColumn : ISearchColumn
	{
		int PixelWidth { get; }
	}

	public interface IWindowsContainerElement : IWindowsControlElement
	{
	}

	public interface IWindowsElement : IElement
	{
		/// <summary> Adjust the size of the element. </summary>
		/// <remarks> SetSize may be called with an extent which is outside of the MinSize/MaxSize values. </remarks>
		void Layout(Rectangle ABounds);

		/// <value> The minimum size at which this element is usable. </value>
		/// <remarks> The control may be asked to size itself to an extent less than this value. </remarks>
		Size MinSize { get; }

		/// <value> The maximum size to which it is useful to have this element. </value>
		/// <remarks> The control may be asked to size itself to an extent larger than this value. </remarks>
		Size MaxSize { get; }

		/// <value> The 'natural' size of the control, or the size which is most useful. </value>
		/// <remarks> This value shouldn't be greater than Max or less than Min but it can be. </remarks>
		Size NaturalSize { get; }

		/// <summary> Returns the total "overhead" of the element, including margins. </summary>
		Size GetOverhead();

		/// <summary> Updates the appearance and size of the element. </summary>
		void UpdateLayout();

		/// <summary> Called when the visibility changes. </summary>
		void VisibleChanged();

		/// <summary> When true, the usage of margins for the element is suppressed. </summary>
		bool SuppressMargins { get; set; }
	}

	public interface IWindowsControlElement : IWindowsElement
	{
		WinForms.Control Control { get; }
	}

	public class FocusChangedEvent : NodeEvent
	{
		public FocusChangedEvent(IWindowsElement node)
		{
			_node = node;
		}

		private IWindowsElement _node;
		public IWindowsElement Node { get { return _node; } }
	}

	public class AdvanceFocusEvent : NodeEvent
	{
		public AdvanceFocusEvent(bool forward)
		{
			_forward = forward;
		}

		private bool _forward;
		public bool Forward
		{
			get { return _forward; }
			set { _forward = value; }
		}
	}

	public interface IBaseForm : IDisposable, IWindowsExposedHost, IWindowsMenuHost
	{
		bool ShowInTaskbar { get; set; }
		void Close();
		int Height { get; set; }
		int Width { get; set; }
		string Text { get; set; }
		Rectangle ClientRectangle { get; }
		Size Size { get; set; }
		Size ClientSize { get; set; }
		Rectangle Bounds { get; set; }
		WinForms.DialogResult ShowDialog();
		WinForms.Control ActiveControl { get; set; }
		void Show(IFormInterface AParent);
		void Hide();
		void Activate();
		bool Enabled { get; set; }
		WinForms.DialogResult DialogResult { get; set; }
		bool Modal { get; }
		WinForms.FormWindowState WindowState { get; set; }
		bool Visible { get; set; }
		event EventHandler Activated;
		event EventHandler Accepting;
		event CancelEventHandler Closing;
		event EventHandler Closed;
		event EventHandler Resize;
		event EventHandler Shown;
		event WinForms.LayoutEventHandler Layout;
		WinForms.FormStartPosition StartPosition { get; set; }
		System.Drawing.Color ForeColor { get; set; }
		void SuspendLayout();
		void BeginUpdate();
		void EndUpdate();
		void ResumeLayout(bool APerformLayout);
		void PerformLayout();
		System.Drawing.Image BackgroundImage { get; set; }
		Point Location { get; set; }
		System.Drawing.Icon Icon { get; set; }
		bool TopLevel { get; set; }
		bool TopMost { get; set; }
		bool HelpButton { get; set; }
		WinForms.Form Owner { get; set; }

		void AdvanceFocus(bool AForward);
		bool LastControlActive();
		bool ActiveControlProcessesEnter();
		WinForms.ScrollableControl ContentPanel { get; }
		Size GetBorderSize();
		bool EnterNavigates { get; set; }
		void SetHintText(string AText);
		void UpdateStatusText();
		void SetAcceptReject(bool AIsAcceptReject, bool ASupressCloseButton);
		void EmbedErrors(ErrorList AErrorList);
		object AddCustomAction(string AText, System.Drawing.Image AImage, EventHandler AHandler);
		void RemoveCustomAction(object AAction);
		void ClearCustomActions();
		void ResetIcon();
		WinForms.StatusStrip StatusBar { get; }
		bool AutoResize { get; set; }
		event PaintHandledEventHandler PaintBackground;
		event EventHandler LayoutContents;
		event GetSizeHandler GetNaturalSize;
		event EventHandler DefaultAction;
		event GetStringHandler GetDefaultActionDescription;
		Dictionary<WinForms.Keys, DialogKeyHandler> DialogKeys { get; }
		void Close(CloseBehavior ABehavior);
		bool IsLookup { get; set; }
		bool AcceptEnabled { get; set; }
	}
}
