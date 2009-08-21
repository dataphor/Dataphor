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
		private System.Windows.Forms.ListView FErrorListView;
		private System.Windows.Forms.ColumnHeader iconColumnHeader;
		private System.Windows.Forms.ColumnHeader classColumnHeader;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.Splitter detailSplitter;
		private System.Windows.Forms.TextBox FErrorDetailBox;
		private ContextMenuStrip FContextMenu;
		private ToolStripMenuItem FClearErrorsMenuItem;
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
			this.FErrorListView = new System.Windows.Forms.ListView();
			this.iconColumnHeader = new System.Windows.Forms.ColumnHeader();
			this.classColumnHeader = new System.Windows.Forms.ColumnHeader();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.detailSplitter = new System.Windows.Forms.Splitter();
			this.FErrorDetailBox = new System.Windows.Forms.TextBox();
			this.FContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.FClearErrorsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// descriptionColumnHeader
			// 
			this.descriptionColumnHeader.Text = "Description";
			this.descriptionColumnHeader.Width = 415;
			// 
			// FErrorListView
			// 
			this.FErrorListView.AllowColumnReorder = true;
			this.FErrorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.iconColumnHeader,
            this.descriptionColumnHeader,
            this.classColumnHeader});
			this.FErrorListView.ContextMenuStrip = this.FContextMenu;
			this.FErrorListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FErrorListView.FullRowSelect = true;
			this.FErrorListView.GridLines = true;
			this.FErrorListView.Location = new System.Drawing.Point(0, 0);
			this.FErrorListView.MultiSelect = false;
			this.FErrorListView.Name = "FErrorListView";
			this.FErrorListView.Size = new System.Drawing.Size(512, 165);
			this.FErrorListView.SmallImageList = this.imageList;
			this.FErrorListView.TabIndex = 0;
			this.FErrorListView.UseCompatibleStateImageBehavior = false;
			this.FErrorListView.View = System.Windows.Forms.View.Details;
			this.FErrorListView.SelectedIndexChanged += new System.EventHandler(this.FErrorListView_SelectedIndexChanged);
			this.FErrorListView.DoubleClick += new System.EventHandler(this.FErrorListView_DoubleClick);
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
			this.FErrorDetailBox.BackColor = System.Drawing.SystemColors.Control;
			this.FErrorDetailBox.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.FErrorDetailBox.Location = new System.Drawing.Point(0, 168);
			this.FErrorDetailBox.Multiline = true;
			this.FErrorDetailBox.Name = "FErrorDetailBox";
			this.FErrorDetailBox.ReadOnly = true;
			this.FErrorDetailBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.FErrorDetailBox.Size = new System.Drawing.Size(512, 88);
			this.FErrorDetailBox.TabIndex = 1;
			this.FErrorDetailBox.WordWrap = false;
			// 
			// FContextMenu
			// 
			this.FContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FClearErrorsMenuItem});
			this.FContextMenu.Name = "contextMenuStrip1";
			this.FContextMenu.Size = new System.Drawing.Size(190, 26);
			this.FContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.FContextMenu_ItemClicked);
			// 
			// FClearErrorsMenuItem
			// 
			this.FClearErrorsMenuItem.Name = "FClearErrorsMenuItem";
			this.FClearErrorsMenuItem.Size = new System.Drawing.Size(189, 22);
			this.FClearErrorsMenuItem.Text = "Clear Errors/Warnings";
			// 
			// ErrorListView
			// 
			this.Controls.Add(this.FErrorListView);
			this.Controls.Add(this.detailSplitter);
			this.Controls.Add(this.FErrorDetailBox);
			this.Name = "ErrorListView";
			this.Size = new System.Drawing.Size(512, 256);
			this.FContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private Hashtable FErrorSources = new Hashtable();

		public void ClearErrors()
		{
			if (FErrorListView.Items.Count > 0)
			{
				FErrorListView.Items.Clear();
				foreach (DictionaryEntry LItem in FErrorSources)
					((IErrorSource)LItem.Key).Disposed -= new EventHandler(ErrorSourceDisposed);
				FErrorSources.Clear();
				FErrorDetailBox.Text = String.Empty;
			}
		}

		public void ClearErrors(IErrorSource ASource)
		{
			bool LChanged = false;
			FErrorListView.BeginUpdate();
			try
			{
				for (int i = FErrorListView.Items.Count - 1; i >= 0; i--)
				{
					if (Object.ReferenceEquals(((ErrorItem)FErrorListView.Items[i].Tag).Source, ASource))
					{
						FErrorListView.Items.RemoveAt(i);
						LChanged = true;
					}
				}
			}
			finally
			{
				FErrorListView.EndUpdate();
			}
			
			if (LChanged)
			{
				ASource.Disposed -= new EventHandler(ErrorSourceDisposed);
				FErrorSources.Remove(ASource);
			}
		}

		public void AppendErrors(IErrorSource ASource, ErrorList AList)
		{
			AppendErrors(ASource, AList, true);
		}

		public void AppendErrors(IErrorSource ASource, ErrorList AList, bool AWarning)
		{
			if ((AList != null) && (AList.Count > 0))
			{
				FErrorListView.BeginUpdate();
				try
				{
					for (int i = AList.Count - 1; i >= 0; i--)
						InternalAppendError(ASource, AList[i], AWarning);
				}
				finally
				{
					FErrorListView.EndUpdate();
				}
				ErrorsAdded(AWarning);
			}
		}

		public void AppendError(IErrorSource ASource, Exception AException, bool AWarning)
		{
			InternalAppendError(ASource, AException, AWarning);
			ErrorsAdded(AWarning);
		}

		private void InternalAppendError(IErrorSource ASource, Exception AException, bool AWarning)
		{
			if (AException != null)
			{
				ListViewItem LItem = new ListViewItem();
				
				LItem.Tag = new ErrorItem(AException, ASource);
				LItem.ImageIndex = (AWarning ? 0 : 1);
				// if this is a DataphorException add the exception code to the description
				DataphorException LException = AException as DataphorException;
				LItem.SubItems.Add(String.Format("{0}{1}", LException != null ? String.Format("({0}:{1}) ", LException.Severity.ToString(), LException.Code.ToString()) : "", ExceptionUtility.BriefDescription(AException)));
				LItem.SubItems.Add(AException.GetType().ToString());
				FErrorListView.Items.Insert(0, LItem);
				if ((ASource != null) && (FErrorSources[ASource] == null))
				{
					FErrorSources.Add(ASource, ASource);
					ASource.Disposed += new EventHandler(ErrorSourceDisposed);
				}
			}
		}

		private void ErrorSourceDisposed(object ASender, EventArgs AArgs)
		{
			ClearErrors((IErrorSource)ASender);
		}

		private void ErrorsAdded(bool AWarning)
		{
			if (FErrorListView.Items.Count > 0)
				FErrorListView.Items[0].Selected = true;
			if (AWarning)
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
			if (FErrorListView.SelectedItems.Count > 0)
			{
				ErrorItem LItem = (ErrorItem)FErrorListView.SelectedItems[0].Tag;
				FErrorDetailBox.Text = ExceptionUtility.DetailedDescription(LItem.Exception);
				if (LItem.Source != null)
					LItem.Source.ErrorHighlighted(LItem.Exception);
			}
			else
				FErrorDetailBox.Text = String.Empty;
		}

		private void FErrorListView_DoubleClick(object sender, System.EventArgs e)
		{
			ItemSelected();
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if (AKey == Keys.Enter)
			{
				ItemSelected();
				return true;
			}
			else
				return base.ProcessDialogKey(AKey);
		}


		private void ItemSelected()
		{
			if (FErrorListView.SelectedItems.Count > 0)
			{
				ErrorItem LItem = (ErrorItem)FErrorListView.SelectedItems[0].Tag;
				if (LItem.Source != null)
					LItem.Source.ErrorSelected(LItem.Exception);
			}
		}

		// F1 help, for setting selected error to get code
		public Exception SelectedError
		{
			get
			{
				if (FErrorListView.SelectedItems.Count > 0)
					return ((ErrorItem)FErrorListView.SelectedItems[0].Tag).Exception;
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

		private void FContextMenu_ItemClicked(object ASender, ToolStripItemClickedEventArgs AArgs)
		{
			if (AArgs.ClickedItem.Name == "FClearErrorsMenuItem")
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
