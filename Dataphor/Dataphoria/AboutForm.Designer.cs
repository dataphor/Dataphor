/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary> Dataphoria "about" form. </summary>
	partial class AboutForm 
	{
		private System.Windows.Forms.PictureBox FPictureBox1;
		private System.Windows.Forms.Label FLabel1;
		private System.Windows.Forms.LinkLabel FLinkLabel1;
		private System.Windows.Forms.Label FLabel3;
		private System.Windows.Forms.Label FLabel4;
		private System.Windows.Forms.LinkLabel FLinkLabel2;
		private System.Windows.Forms.GroupBox FGroupBox1;
		private System.Windows.Forms.ListView FLvModules;
		private System.Windows.Forms.ColumnHeader FColumnHeader1;
		private System.Windows.Forms.ColumnHeader FColumnHeader2;
		private System.Windows.Forms.ToolTip FToolTip;
		private TextBox FTxtVersion;
		private Label FLabel2;
		private LinkLabel FLinkLabel3;
		private System.ComponentModel.IContainer components;



		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.FPictureBox1 = new System.Windows.Forms.PictureBox();
            this.FLabel1 = new System.Windows.Forms.Label();
            this.FLinkLabel1 = new System.Windows.Forms.LinkLabel();
            this.FLabel3 = new System.Windows.Forms.Label();
            this.FLabel4 = new System.Windows.Forms.Label();
            this.FLinkLabel2 = new System.Windows.Forms.LinkLabel();
            this.FGroupBox1 = new System.Windows.Forms.GroupBox();
            this.FTxtVersion = new System.Windows.Forms.TextBox();
            this.FLvModules = new System.Windows.Forms.ListView();
            this.FColumnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.FColumnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.FToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.FLabel2 = new System.Windows.Forms.Label();
            this.FLinkLabel3 = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.FPictureBox1)).BeginInit();
            this.FGroupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // FPictureBox1
            // 
            this.FPictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FPictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("FPictureBox1.Image")));
            this.FPictureBox1.Location = new System.Drawing.Point(8, 8);
            this.FPictureBox1.Name = "FPictureBox1";
            this.FPictureBox1.Size = new System.Drawing.Size(336, 120);
            this.FPictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.FPictureBox1.TabIndex = 0;
            this.FPictureBox1.TabStop = false;
            // 
            // FLabel1
            // 
            this.FLabel1.Location = new System.Drawing.Point(8, 200);
            this.FLabel1.Name = "FLabel1";
            this.FLabel1.Size = new System.Drawing.Size(336, 26);
            this.FLabel1.TabIndex = 1;
            this.FLabel1.Text = "Dataphoria � 2001-2006 Alphora.  All Rights Reserved.  For more information:";
            // 
            // FLinkLabel1
            // 
            this.FLinkLabel1.Location = new System.Drawing.Point(8, 226);
            this.FLinkLabel1.Name = "FLinkLabel1";
            this.FLinkLabel1.Size = new System.Drawing.Size(100, 23);
            this.FLinkLabel1.TabIndex = 3;
            this.FLinkLabel1.TabStop = true;
            this.FLinkLabel1.Text = "www.alphora.com";
            this.FLinkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // FLabel3
            // 
            this.FLabel3.Location = new System.Drawing.Point(8, 278);
            this.FLabel3.Name = "FLabel3";
            this.FLabel3.Size = new System.Drawing.Size(304, 32);
            this.FLabel3.TabIndex = 4;
            this.FLabel3.Text = "The text editor control is from the SharpDevelop IDE. Used under license. For mor" +
                "e information:";
            // 
            // FLabel4
            // 
            this.FLabel4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FLabel4.Location = new System.Drawing.Point(8, 128);
            this.FLabel4.Name = "FLabel4";
            this.FLabel4.Size = new System.Drawing.Size(336, 16);
            this.FLabel4.TabIndex = 5;
            this.FLabel4.Text = "Dataphor Integrated Development Environment";
            this.FLabel4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FLinkLabel2
            // 
            this.FLinkLabel2.Location = new System.Drawing.Point(9, 304);
            this.FLinkLabel2.Name = "FLinkLabel2";
            this.FLinkLabel2.Size = new System.Drawing.Size(136, 23);
            this.FLinkLabel2.TabIndex = 6;
            this.FLinkLabel2.TabStop = true;
            this.FLinkLabel2.Text = "www.icsharpcode.net";
            this.FLinkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
            // 
            // FGroupBox1
            // 
            this.FGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FGroupBox1.Controls.Add(this.FTxtVersion);
            this.FGroupBox1.Location = new System.Drawing.Point(8, 152);
            this.FGroupBox1.Name = "FGroupBox1";
            this.FGroupBox1.Size = new System.Drawing.Size(336, 40);
            this.FGroupBox1.TabIndex = 7;
            this.FGroupBox1.TabStop = false;
            this.FGroupBox1.Text = "Version";
            // 
            // FTxtVersion
            // 
            this.FTxtVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FTxtVersion.Location = new System.Drawing.Point(5, 14);
            this.FTxtVersion.MaxLength = 32;
            this.FTxtVersion.Name = "FTxtVersion";
            this.FTxtVersion.ReadOnly = true;
            this.FTxtVersion.Size = new System.Drawing.Size(326, 20);
            this.FTxtVersion.TabIndex = 1;
            this.FTxtVersion.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // FLvModules
            // 
            this.FLvModules.AllowColumnReorder = true;
            this.FLvModules.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FLvModules.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FLvModules.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FColumnHeader1,
            this.FColumnHeader2});
            this.FLvModules.FullRowSelect = true;
            this.FLvModules.GridLines = true;
            this.FLvModules.LabelWrap = false;
            this.FLvModules.Location = new System.Drawing.Point(8, 331);
            this.FLvModules.MultiSelect = false;
            this.FLvModules.Name = "FLvModules";
            this.FLvModules.Size = new System.Drawing.Size(336, 131);
            this.FLvModules.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.FLvModules.TabIndex = 8;
            this.FToolTip.SetToolTip(this.FLvModules, "Loaded Modules");
            this.FLvModules.UseCompatibleStateImageBehavior = false;
            this.FLvModules.View = System.Windows.Forms.View.Details;
            // 
            // FColumnHeader1
            // 
            this.FColumnHeader1.Text = "Name";
            this.FColumnHeader1.Width = 230;
            // 
            // FColumnHeader2
            // 
            this.FColumnHeader2.Text = "Version";
            this.FColumnHeader2.Width = 89;
            // 
            // FLabel2
            // 
            this.FLabel2.AutoSize = true;
            this.FLabel2.Location = new System.Drawing.Point(8, 246);
            this.FLabel2.Name = "FLabel2";
            this.FLabel2.Size = new System.Drawing.Size(190, 13);
            this.FLabel2.TabIndex = 9;
            this.FLabel2.Text = "For online discussions about Dataphor:";
            // 
            // FLinkLabel3
            // 
            this.FLinkLabel3.AutoSize = true;
            this.FLinkLabel3.Location = new System.Drawing.Point(8, 260);
            this.FLinkLabel3.Name = "FLinkLabel3";
            this.FLinkLabel3.Size = new System.Drawing.Size(131, 13);
            this.FLinkLabel3.TabIndex = 10;
            this.FLinkLabel3.TabStop = true;
            this.FLinkLabel3.Text = "news://news.alphora.com";
            this.FLinkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel3_LinkClicked);
            // 
            // AboutForm
            // 
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(354, 471);
            this.Controls.Add(this.FLinkLabel3);
            this.Controls.Add(this.FLabel2);
            this.Controls.Add(this.FLvModules);
            this.Controls.Add(this.FGroupBox1);
            this.Controls.Add(this.FLinkLabel2);
            this.Controls.Add(this.FLabel3);
            this.Controls.Add(this.FLinkLabel1);
            this.Controls.Add(this.FLabel1);
            this.Controls.Add(this.FPictureBox1);
            this.Controls.Add(this.FLabel4);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Dataphoria";
            ((System.ComponentModel.ISupportInitialize)(this.FPictureBox1)).EndInit();
            this.FGroupBox1.ResumeLayout(false);
            this.FGroupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		
	}
}
