/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Error / warning  list view control. </summary>
	public class ErrorListView : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.ColumnHeader descriptionColumnHeader;
		private System.Windows.Forms.ListView _errorListView;
		private System.Windows.Forms.ColumnHeader iconColumnHeader;
		private System.Windows.Forms.ColumnHeader classColumnHeader;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.Splitter detailSplitter;
		private System.Windows.Forms.TextBox _errorDetailBox;
		private ContextMenuStrip _contextMenu;
		private ToolStripMenuItem _clearErrorsMenuItem;
		private System.ComponentModel.IContainer components;

		public ErrorListView()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorListView));
			this.descriptionColumnHeader = new System.Windows.Forms.ColumnHeader();
			this._errorListView = new System.Windows.Forms.ListView();
			this.iconColumnHeader = new System.Windows.Forms.ColumnHeader();
			this.classColumnHeader = new System.Windows.Forms.ColumnHeader();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.detailSplitter = new System.Windows.Forms.Splitter();
			this._errorDetailBox = new System.Windows.Forms.TextBox();
			this._contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this._clearErrorsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._contextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// descriptionColumnHeader
			// 
			this.descriptionColumnHeader.Text = "Description";
			this.descriptionColumnHeader.Width = 415;
			// 
			// FErrorListView
			// 
			this._errorListView.AllowColumnReorder = true;
			this._errorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.iconColumnHeader,
            this.descriptionColumnHeader,
            this.classColumnHeader});
			this._errorListView.ContextMenuStrip = this._contextMenu;
			this._errorListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._errorListView.FullRowSelect = true;
			this._errorListView.GridLines = true;
			this._errorListView.Location = new System.Drawing.Point(0, 0);
			this._errorListView.MultiSelect = false;
			this._errorListView.Name = "FErrorListView";
			this._errorListView.Size = new System.Drawing.Size(512, 165);
			this._errorListView.SmallImageList = this.imageList;
			this._errorListView.TabIndex = 0;
			this._errorListView.UseCompatibleStateImageBehavior = false;
			this._errorListView.View = System.Windows.Forms.View.Details;
			this._errorListView.SelectedIndexChanged += new System.EventHandler(this.FErrorListView_SelectedIndexChanged);
			this._errorListView.DoubleClick += new System.EventHandler(this.FErrorListView_DoubleClick);
			// 
			// iconColumnHeader
			// 
			this.iconColumnHeader.Text = "";
			this.iconColumnHeader.Width = 20;
			// 
			// classColumnHeader
			// 
			this.classColumnHeader.Text = "Class";
			this.classColumnHeader.Width = 146;
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "");
			this.imageList.Images.SetKeyName(1, "");
			// 
			// detailSplitter
			// 
			this.detailSplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.detailSplitter.Location = new System.Drawing.Point(0, 165);
			this.detailSplitter.Name = "detailSplitter";
			this.detailSplitter.Size = new System.Drawing.Size(512, 3);
			this.detailSplitter.TabIndex = 5;
			this.detailSplitter.TabStop = false;
			// 
			// FErrorDetailBox
			// 
			this._errorDetailBox.BackColor = System.Drawing.SystemColors.Control;
			this._errorDetailBox.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._errorDetailBox.Location = new System.Drawing.Point(0, 168);
			this._errorDetailBox.Multiline = true;
			this._errorDetailBox.Name = "FErrorDetailBox";
			this._errorDetailBox.ReadOnly = true;
			this._errorDetailBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this._errorDetailBox.Size = new System.Drawing.Size(512, 88);
			this._errorDetailBox.TabIndex = 1;
			this._errorDetailBox.WordWrap = false;
			// 
			// FContextMenu
			// 
			this._contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._clearErrorsMenuItem});
			this._contextMenu.Name = "contextMenuStrip1";
			this._contextMenu.Size = new System.Drawing.Size(190, 26);
			this._contextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FContextMenu_ItemClicked);
			// 
			// FClearErrorsMenuItem
			// 
			this._clearErrorsMenuItem.Name = "FClearErrorsMenuItem";
			this._clearErrorsMenuItem.Size = new System.Drawing.Size(189, 22);
			this._clearErrorsMenuItem.Text = "Clear Errors/Warnings";
			// 
			// ErrorListView
			// 
			this.Controls.Add(this._errorListView);
			this.Controls.Add(this.detailSplitter);
			this.Controls.Add(this._errorDetailBox);
			this.Name = "ErrorListView";
			this.Size = new System.Drawing.Size(512, 256);
			this._contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private Hashtable _errorSources = new Hashtable();

		public void ClearErrors()
		{
			if (_errorListView.Items.Count > 0)
			{
				_errorListView.Items.Clear();
				foreach (DictionaryEntry item in _errorSources)
					((IErrorSource)item.Key).Disposed -= new EventHandler(ErrorSourceDisposed);
				_errorSources.Clear();
				_errorDetailBox.Text = String.Empty;
			}
		}

		public void ClearErrors(IErrorSource source)
		{
			bool changed = false;
			_errorListView.BeginUpdate();
			try
			{
				for (int i = _errorListView.Items.Count - 1; i >= 0; i--)
				{
					if (Object.ReferenceEquals(((ErrorItem)_errorListView.Items[i].Tag).Source, source))
					{
						_errorListView.Items.RemoveAt(i);
						changed = true;
					}
				}
			}
			finally
			{
				_errorListView.EndUpdate();
			}
			
			if (changed)
			{
				source.Disposed -= new EventHandler(ErrorSourceDisposed);
				_errorSources.Remove(source);
			}
		}

		public void AppendErrors(IErrorSource source, ErrorList list)
		{
			AppendErrors(source, list, true);
		}

		public void AppendErrors(IErrorSource source, ErrorList list, bool warning)
		{
			if ((list != null) && (list.Count > 0))
			{
				_errorListView.BeginUpdate();
				try
				{
					for (int i = list.Count - 1; i >= 0; i--)
						InternalAppendError(source, list[i], warning);
				}
				finally
				{
					_errorListView.EndUpdate();
				}
				ErrorsAdded(warning);
			}
		}

		public void AppendError(IErrorSource source, Exception exception, bool warning)
		{
			InternalAppendError(source, exception, warning);
			ErrorsAdded(warning);
		}

		private void InternalAppendError(IErrorSource source, Exception exception, bool warning)
		{
			if (exception != null)
			{
				ListViewItem item = new ListViewItem();
				
				item.Tag = new ErrorItem(exception, source);
				item.ImageIndex = (warning ? 0 : 1);
				// if this is a DataphorException add the exception code to the description
				DataphorException localException = exception as DataphorException;
				item.SubItems.Add(String.Format("{0}{1}", localException != null ? String.Format("({0}:{1}) ", localException.Severity.ToString(), localException.Code.ToString()) : "", ExceptionUtility.BriefDescription(exception)));
				item.SubItems.Add(exception.GetType().ToString());
				_errorListView.Items.Insert(0, item);
				if ((source != null) && (_errorSources[source] == null))
				{
					_errorSources.Add(source, source);
					source.Disposed += new EventHandler(ErrorSourceDisposed);
				}
			}
		}

		private void ErrorSourceDisposed(object sender, EventArgs args)
		{
			ClearErrors((IErrorSource)sender);
		}

		private void ErrorsAdded(bool warning)
		{
			if (_errorListView.Items.Count > 0)
				_errorListView.Items[0].Selected = true;
			if (warning)
			{
				if (OnWarningsAdded != null)
					OnWarningsAdded(this, EventArgs.Empty);
			}
			else
			{
				if (OnErrorsAdded != null)
					OnErrorsAdded(this, EventArgs.Empty);
			}
		}

		public event EventHandler OnErrorsAdded;
		public event EventHandler OnWarningsAdded;

		private void FErrorListView_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (_errorListView.SelectedItems.Count > 0)
			{
				ErrorItem item = (ErrorItem)_errorListView.SelectedItems[0].Tag;
				_errorDetailBox.Text = ExceptionUtility.DetailedDescription(item.Exception);
				if (item.Source != null)
					item.Source.ErrorHighlighted(item.Exception);
			}
			else
				_errorDetailBox.Text = String.Empty;
		}

		private void FErrorListView_DoubleClick(object sender, System.EventArgs e)
		{
			ItemSelected();
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			if (key == Keys.Enter)
			{
				ItemSelected();
				return true;
			}
			else
				return base.ProcessDialogKey(key);
		}


		private void ItemSelected()
		{
			if (_errorListView.SelectedItems.Count > 0)
			{
				ErrorItem item = (ErrorItem)_errorListView.SelectedItems[0].Tag;
				if (item.Source != null)
					item.Source.ErrorSelected(item.Exception);
			}
		}

		// F1 help, for setting selected error to get code
		public Exception SelectedError
		{
			get
			{
				if (_errorListView.SelectedItems.Count > 0)
					return ((ErrorItem)_errorListView.SelectedItems[0].Tag).Exception;
				return null;
			}
		}

		private struct ErrorItem
		{
			public ErrorItem(Exception AException, IErrorSource ASource)
			{
				Exception = AException;
				Source = ASource;
			}

			public Exception Exception;
			public IErrorSource Source;
		}

		private void FContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs args)
		{
			if (args.ClickedItem.Name == "FClearErrorsMenuItem")
				ClearErrors();
		}
	}

	public interface IErrorSource : IDisposableNotify
	{
		/// <summary> This method is invoked when an item is highlighted in the errors pane. </summary>
		void ErrorHighlighted(Exception AException);

		/// <summary> This method is invoked when an item is selected (double-click or Enter key). </summary>
		void ErrorSelected(Exception AException);
	}
}
