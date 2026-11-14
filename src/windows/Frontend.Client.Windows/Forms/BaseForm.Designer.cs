/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	partial class BaseForm
	{
		protected StatusStrip FStatusBar;
		protected ToolStripStatusLabel FHintPanel;
		protected MenuStrip FMainMenu;
		protected ToolStripStatusLabel FStatusPanel;
		protected ToolStrip FToolBar;
		protected ToolStripMenuItem FFormMenu;
		protected Panel FContentPanel;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseForm));
			this.FMainMenu = new System.Windows.Forms.MenuStrip();
			this.FFormMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.FStatusBar = new System.Windows.Forms.StatusStrip();
			this.FHintPanel = new System.Windows.Forms.ToolStripStatusLabel();
			this.FStatusPanel = new System.Windows.Forms.ToolStripStatusLabel();
			this.FToolBar = new System.Windows.Forms.ToolStrip();
			this.FContentPanel = new Alphora.Dataphor.Frontend.Client.Windows.Panel();
			this.FMainMenu.SuspendLayout();
			this.FStatusBar.SuspendLayout();
			this.FContentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// FMainMenu
			// 
			this.FMainMenu.AllowItemReorder = true;
			this.FMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FFormMenu});
			this.FMainMenu.Location = new System.Drawing.Point(0, 0);
			this.FMainMenu.Name = "FMainMenu";
			this.FMainMenu.Size = new System.Drawing.Size(272, 24);
			this.FMainMenu.TabIndex = 1;
			// 
			// FFormMenu
			// 
			this.FFormMenu.MergeIndex = 100;
			this.FFormMenu.Name = "FFormMenu";
			this.FFormMenu.Size = new System.Drawing.Size(43, 20);
			this.FFormMenu.Text = "&Form";
			// 
			// FStatusBar
			// 
			this.FStatusBar.AllowItemReorder = true;
			this.FStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FHintPanel,
            this.FStatusPanel});
			this.FStatusBar.Location = new System.Drawing.Point(0, 179);
			this.FStatusBar.Name = "FStatusBar";
			this.FStatusBar.RenderMode = System.Windows.Forms.ToolStripRenderMode.ManagerRenderMode;
			this.FStatusBar.Size = new System.Drawing.Size(272, 22);
			this.FStatusBar.TabIndex = 3;
			// 
			// FHintPanel
			// 
			this.FHintPanel.AutoSize = false;
			this.FHintPanel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)
						| System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.FHintPanel.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
			this.FHintPanel.MergeIndex = 100;
			this.FHintPanel.Name = "FHintPanel";
			this.FHintPanel.Size = new System.Drawing.Size(257, 17);
			this.FHintPanel.Spring = true;
			this.FHintPanel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// FStatusPanel
			// 
			this.FStatusPanel.MergeIndex = 200;
			this.FStatusPanel.Name = "FStatusPanel";
			this.FStatusPanel.Size = new System.Drawing.Size(0, 17);
			// 
			// FToolBar
			// 
			this.FToolBar.Location = new System.Drawing.Point(0, 24);
			this.FToolBar.Name = "FToolBar";
			this.FToolBar.Size = new System.Drawing.Size(272, 25);
			this.FToolBar.TabIndex = 2;
			this.FToolBar.ItemAdded += new System.Windows.Forms.ToolStripItemEventHandler(this.ToolBarItemAdded);
			this.FToolBar.ItemRemoved += new System.Windows.Forms.ToolStripItemEventHandler(this.ToolBarItemDeleted);
			// 
			// FContentPanel
			// 
			this.FContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FContentPanel.Paint += new PaintEventHandler(ContentPanelPaint);
			this.FContentPanel.Location = new System.Drawing.Point(0, 49);
			this.FContentPanel.Name = "FContentPanel";
			this.FContentPanel.Size = new System.Drawing.Size(272, 130);
			this.FContentPanel.TabIndex = 0;
			// 
			// BaseForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(272, 201);
			this.Controls.Add(this.FContentPanel);
			this.Controls.Add(this.FToolBar);
			this.Controls.Add(this.FMainMenu);
			this.Controls.Add(this.FStatusBar);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.FMainMenu;
			this.Name = "BaseForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.FMainMenu.ResumeLayout(false);
			this.FMainMenu.PerformLayout();
			this.FStatusBar.ResumeLayout(false);
			this.FStatusBar.PerformLayout();
			this.FContentPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}