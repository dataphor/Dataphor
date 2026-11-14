/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Windows.Forms;

using Crownwood.Magic.Controls;
using Crownwood.Magic.Common;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class SchemaForm : DataphoriaForm
	{
		private System.ComponentModel.Container components = null;

		public SchemaForm(string AScript)
		{
			InitializeComponent();

			FScript = AScript;

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			// Init controls
			SuspendLayout();

			FDesignerPage = new Crownwood.Magic.Controls.TabPage();
			FDesignerPage.Title = "Designer";
			FDesignerPage.TabStop = false;

			FEditorPage = new Crownwood.Magic.Controls.TabPage();
			FEditorPage.Title = "Editor";
			FEditorPage.TabStop = false;

			FTabControl = new Crownwood.Magic.Controls.TabControl();
			FTabControl.Height = FTabControl.TabsAreaRect.Height;
			FTabControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			FTabControl.TabPages.AddRange(new Crownwood.Magic.Controls.TabPage[] {FDesignerPage, FEditorPage});
			FTabControl.SelectionChanging += new EventHandler(TabSelectionChanging);
			FTabControl.TabStop = false;

			Controls.Add(FTabControl);

			InitializeDesigner();

			ResumeLayout(false);

			UpdateTitle();
		}

		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SchemaForm));
			// 
			// SchemaForm
			// 
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SchemaForm";
		}
		#endregion

		private Crownwood.Magic.Controls.TabControl FTabControl;
		private Crownwood.Magic.Controls.TabPage FDesignerPage;
		private Crownwood.Magic.Controls.TabPage FEditorPage;

		// Script

		private string FScript;
		public string Script
		{
			get { return FScript; }
		}

		// Tab selection

		private void TabSelectionChanging(object ASender, EventArgs AArgs)
		{
			if (FTabControl.SelectedIndex == 1)	// The SelectedIndex hasn't been updated at this point
				InitializeDesigner();
			else
				InitializeEditor();
		}

		// Designer control

		private DesignerControl FDesignerControl;

		private void InitializeDesigner()
		{
			string LScript;
			if (FEditorControl != null)
				LScript = FEditorControl.Script;
			else
				LScript = FScript;
			FDesignerControl = new DesignerControl(LScript);
			try
			{
				FDesignerControl.OnModified += new EventHandler(DesignerModified);
				Controls.Add(FDesignerControl);
				FDesignerControl.BringToFront();
				FDesignerControl.Focus();
			}
			catch
			{
				FDesignerControl.Dispose();
				throw;
			}
			if (FEditorControl != null)
			{
				FEditorControl.OnModified -= new EventHandler(EditorModified);
				FEditorControl.Dispose();
				FEditorControl = null;
			}
		}

		private EditorControl FEditorControl;

		private void InitializeEditor()
		{
			string LScript;
			if (FDesignerControl != null)
				LScript = FDesignerControl.Script;
			else
				LScript = FScript;
			FEditorControl = new EditorControl(LScript);
			try
			{
				FEditorControl.OnModified += new EventHandler(EditorModified);
				Controls.Add(FEditorControl);
				FEditorControl.BringToFront();
				ActiveControl = FEditorControl;
			}
			catch
			{
				FEditorControl.Dispose();
				throw;
			}
			if (FDesignerControl != null)
			{
				FDesignerControl.OnModified -= new EventHandler(DesignerModified);
				FDesignerControl.Dispose();
				FDesignerControl = null;
			}
		}

		// "Modified" handling

		private void UpdateTitle()
		{
			Text = Strings.Get("SchemaFormTitle") + (FIsModified ? "*" : String.Empty);
		}
        
		private bool FIsModified;

		public bool IsModified
		{
			get { return FIsModified; }
		}

		private void DesignerModified(object ASender, EventArgs AArgs)
		{
			Modified();
		}

		private void EditorModified(object ASender, EventArgs AArgs)
		{
			Modified();
		}

		public void Modified()
		{
			if (!FIsModified)
			{
				FIsModified = true;
				UpdateTitle();
			}
		}

		private void ResetModified()
		{
			if (FIsModified)
			{
				FIsModified = false;
				UpdateTitle();
			}
		}

		private void CheckModified()
		{
			if (FIsModified)
			{
				switch (MessageBox.Show(Strings.Get("SaveChanges"), Strings.Get("SaveChangesCaption"), MessageBoxButtons.YesNoCancel))
				{
					case (DialogResult.Yes) :
						Save();
						break;
					case (DialogResult.Cancel) :
						throw new AbortException();
				}
			}
		}

		public void Save()
		{
			// TODO: Save
		}

		public void SaveAs()
		{
			// TODO: SaveAs
		}

		// Painting

		protected override void OnPaintBackground(PaintEventArgs AArgs)
		{
			// Optimization: because the background of this form will always be 
			// covered, don't paint it.
		}
	}
}
