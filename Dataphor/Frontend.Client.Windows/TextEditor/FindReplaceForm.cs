/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class FindReplaceForm : System.Windows.Forms.Form
	{
		public const string FindTitle = "Find";

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox FindText;
		private System.Windows.Forms.TextBox ReplaceText;
		private System.Windows.Forms.CheckBox SelectionOnly;
		private System.Windows.Forms.CheckBox UseRegEx;
		private System.Windows.Forms.CheckBox ReverseDirection;
		private System.Windows.Forms.CheckBox WholeWordOnly;
		private System.Windows.Forms.CheckBox WrapAtEnd;
		private System.Windows.Forms.CheckBox CaseSensitive;
		private System.Windows.Forms.Button FindButton;
		private System.Windows.Forms.Button ReplaceAllButton;
		private System.Windows.Forms.Button ReplaceButton;
		private System.Windows.Forms.Button CancelingButton;
		private System.Windows.Forms.Panel panel2;

		public FindReplaceForm(FindAction action)
		{
			InitializeComponent();
			if (action == FindAction.Find)
			{
				AcceptButton = FindButton;
				PerformLayout();
				panel2.Visible = false;
				Height -= panel2.Height;
				Text = FindTitle;
			}
			else
				AcceptButton = ReplaceButton;
		}

		#region InitializeComponent

		private void InitializeComponent()
		{
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.FindText = new System.Windows.Forms.TextBox();
            this.FindButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SelectionOnly = new System.Windows.Forms.CheckBox();
            this.ReplaceAllButton = new System.Windows.Forms.Button();
            this.ReplaceButton = new System.Windows.Forms.Button();
            this.ReplaceText = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.WrapAtEnd = new System.Windows.Forms.CheckBox();
            this.CaseSensitive = new System.Windows.Forms.CheckBox();
            this.WholeWordOnly = new System.Windows.Forms.CheckBox();
            this.ReverseDirection = new System.Windows.Forms.CheckBox();
            this.UseRegEx = new System.Windows.Forms.CheckBox();
            this.CancelingButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.FindText);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(368, 44);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Searc&h Text";
            // 
            // FindText
            // 
            this.FindText.Location = new System.Drawing.Point(8, 20);
            this.FindText.Name = "FindText";
            this.FindText.Size = new System.Drawing.Size(264, 20);
            this.FindText.TabIndex = 0;
            // 
            // FindButton
            // 
            this.FindButton.Location = new System.Drawing.Point(280, 16);
            this.FindButton.Name = "FindButton";
            this.FindButton.Size = new System.Drawing.Size(80, 23);
            this.FindButton.TabIndex = 2;
            this.FindButton.Text = "&Find";
            this.FindButton.Click += new System.EventHandler(this.Find_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(8, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "Re&place Text";
            // 
            // SelectionOnly
            // 
            this.SelectionOnly.Location = new System.Drawing.Point(8, -1);
            this.SelectionOnly.Name = "SelectionOnly";
            this.SelectionOnly.Size = new System.Drawing.Size(160, 24);
            this.SelectionOnly.TabIndex = 0;
            this.SelectionOnly.Text = "Find Selection &Only";
            // 
            // ReplaceAllButton
            // 
            this.ReplaceAllButton.Location = new System.Drawing.Point(280, 24);
            this.ReplaceAllButton.Name = "ReplaceAllButton";
            this.ReplaceAllButton.Size = new System.Drawing.Size(80, 23);
            this.ReplaceAllButton.TabIndex = 2;
            this.ReplaceAllButton.Text = "Replace &All";
            this.ReplaceAllButton.Click += new System.EventHandler(this.ReplaceAll_Click);
            // 
            // ReplaceButton
            // 
            this.ReplaceButton.Location = new System.Drawing.Point(280, 0);
            this.ReplaceButton.Name = "ReplaceButton";
            this.ReplaceButton.Size = new System.Drawing.Size(80, 23);
            this.ReplaceButton.TabIndex = 1;
            this.ReplaceButton.Text = "&Replace";
            this.ReplaceButton.Click += new System.EventHandler(this.Replace_Click);
            // 
            // ReplaceText
            // 
            this.ReplaceText.Location = new System.Drawing.Point(8, 16);
            this.ReplaceText.Name = "ReplaceText";
            this.ReplaceText.Size = new System.Drawing.Size(264, 20);
            this.ReplaceText.TabIndex = 0;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.WrapAtEnd);
            this.panel3.Controls.Add(this.CaseSensitive);
            this.panel3.Controls.Add(this.WholeWordOnly);
            this.panel3.Controls.Add(this.ReverseDirection);
            this.panel3.Controls.Add(this.UseRegEx);
            this.panel3.Controls.Add(this.SelectionOnly);
            this.panel3.Controls.Add(this.CancelingButton);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 95);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(368, 95);
            this.panel3.TabIndex = 3;
            // 
            // WrapAtEnd
            // 
            this.WrapAtEnd.Checked = true;
            this.WrapAtEnd.CheckState = System.Windows.Forms.CheckState.Checked;
            this.WrapAtEnd.Location = new System.Drawing.Point(232, 47);
            this.WrapAtEnd.Name = "WrapAtEnd";
            this.WrapAtEnd.Size = new System.Drawing.Size(96, 24);
            this.WrapAtEnd.TabIndex = 4;
            this.WrapAtEnd.Text = "Wrap At &End";
            // 
            // CaseSensitive
            // 
            this.CaseSensitive.Location = new System.Drawing.Point(232, 70);
            this.CaseSensitive.Name = "CaseSensitive";
            this.CaseSensitive.Size = new System.Drawing.Size(128, 24);
            this.CaseSensitive.TabIndex = 5;
            this.CaseSensitive.Text = "&Case Sensitive";
            // 
            // WholeWordOnly
            // 
            this.WholeWordOnly.Location = new System.Drawing.Point(8, 70);
            this.WholeWordOnly.Name = "WholeWordOnly";
            this.WholeWordOnly.Size = new System.Drawing.Size(128, 24);
            this.WholeWordOnly.TabIndex = 3;
            this.WholeWordOnly.Text = "&Whole Word Only";
            // 
            // ReverseDirection
            // 
            this.ReverseDirection.Location = new System.Drawing.Point(8, 47);
            this.ReverseDirection.Name = "ReverseDirection";
            this.ReverseDirection.Size = new System.Drawing.Size(226, 24);
            this.ReverseDirection.TabIndex = 2;
            this.ReverseDirection.Text = "Reverse Find Direction (Find &Up)";
            // 
            // UseRegEx
            // 
            this.UseRegEx.Location = new System.Drawing.Point(8, 23);
            this.UseRegEx.Name = "UseRegEx";
            this.UseRegEx.Size = new System.Drawing.Size(176, 24);
            this.UseRegEx.TabIndex = 1;
            this.UseRegEx.Text = "Use Re&gular Expressions";
            // 
            // CancelingButton
            // 
            this.CancelingButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelingButton.Location = new System.Drawing.Point(280, 1);
            this.CancelingButton.Name = "CancelingButton";
            this.CancelingButton.Size = new System.Drawing.Size(80, 23);
            this.CancelingButton.TabIndex = 6;
            this.CancelingButton.Text = "Ca&ncel";
            this.CancelingButton.Click += new System.EventHandler(this.button4_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.ReplaceAllButton);
            this.panel2.Controls.Add(this.ReplaceButton);
            this.panel2.Controls.Add(this.ReplaceText);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 44);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(368, 51);
            this.panel2.TabIndex = 1;
            // 
            // FindReplaceForm
            // 
            this.CancelButton = this.CancelingButton;
            this.ClientSize = new System.Drawing.Size(368, 190);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.FindButton);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FindReplaceForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Replace";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

		}	

		#endregion

		// Settings

		public FindSettings Settings
		{
			get 
			{ 
				FindSettings settings = new FindSettings();
				settings.FindText = FindText.Text;
				settings.ReplaceText = ReplaceText.Text;
				settings.WrapAtEnd = WrapAtEnd.Checked;
				settings.CaseSensitive = CaseSensitive.Checked;
				settings.WholeWordOnly = WholeWordOnly.Checked;
				settings.ReverseDirection = ReverseDirection.Checked;
				settings.UseRegEx = UseRegEx.Checked;
				settings.SelectionOnly = SelectionOnly.Checked;
				return settings;
			}
			set
			{
				FindText.Text = value.FindText;
				ReplaceText.Text = value.ReplaceText;
				WrapAtEnd.Checked = value.WrapAtEnd;
				CaseSensitive.Checked = value.CaseSensitive;
				WholeWordOnly.Checked = value.WholeWordOnly;
				ReverseDirection.Checked = value.ReverseDirection;
				UseRegEx.Checked = value.UseRegEx;
				SelectionOnly.Checked = value.SelectionOnly;
			}
		}

		// Action

		private FindAction _action;
		public FindAction Action
		{
			get { return _action; }
			set { _action = value; }
		}

		private void Find_Click(object sender, System.EventArgs e)
		{
			Action = FindAction.Find;
			DialogResult = DialogResult.OK;
		}	

		private void Replace_Click(object sender, System.EventArgs e)
		{
			Action = FindAction.Replace;
			DialogResult = DialogResult.OK;
		}

		private void ReplaceAll_Click(object sender, System.EventArgs e)
		{
			Action = FindAction.ReplaceAll;
			DialogResult = DialogResult.OK;
		}

		private void button4_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

	}

	public enum FindAction
	{
		Find,
		Replace,
		ReplaceAll
	}

	public class FindSettings
	{
		public string FindText = String.Empty;
		public string ReplaceText = String.Empty;
		public bool WrapAtEnd = true;
		public bool CaseSensitive;
		public bool WholeWordOnly;
		public bool ReverseDirection;
		public bool UseRegEx;
		public bool SelectionOnly;
	}
}