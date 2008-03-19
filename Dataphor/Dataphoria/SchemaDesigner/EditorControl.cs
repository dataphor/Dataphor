/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

using Crownwood.Magic.Controls;

using Alphora.Dataphor.Dataphoria.TextEditor;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class EditorControl : ContainerControl
	{
		public EditorControl(string AScript)
		{
			FEditStatusPanel = new StatusBarPanel();
			FEditStatusPanel.AutoSize = StatusBarPanelAutoSize.Spring;

			FEditPositionPanel = new StatusBarPanel();
			FEditPositionPanel.Alignment = HorizontalAlignment.Right;
			FEditPositionPanel.AutoSize = StatusBarPanelAutoSize.Contents;

			FEditStatusBar = new StatusBar();
			FEditStatusBar.Panels.AddRange(new StatusBarPanel[] {FEditStatusPanel, FEditPositionPanel});
			FEditStatusBar.ShowPanels = true;
			FEditStatusBar.SizingGrip = false;

			FTextEdit = new TextEdit();
			FTextEdit.Dock = DockStyle.Fill;
			FTextEdit.Document.TextContent = AScript;
			FTextEdit.Refresh();

			SuspendLayout();

			Dock = DockStyle.Fill;
			Controls.AddRange(new Control[] {FTextEdit, FEditStatusBar});

			ResumeLayout(false);
		}

		private StatusBar FEditStatusBar;
		private StatusBarPanel FEditStatusPanel;
		private StatusBarPanel FEditPositionPanel;
		private TextEdit FTextEdit;

		// Modified

		public event EventHandler OnModified;

		public void Modified()
		{
			if (OnModified != null)
				OnModified(this, EventArgs.Empty);
		}

		// Script

		public string Script
		{
			get { return FTextEdit.Document.TextContent; }
		}
	}
}
