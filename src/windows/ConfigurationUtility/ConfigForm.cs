/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Microsoft.Samples.WinForms.Extras;

namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
	/// <summary>
	/// Summary description for ConfigForm.
	/// </summary>
	public class ConfigForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button CatalogBrowse;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox PortNumber;
		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TextBox CatalogDirectoryTextBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtLibraryDirectory;
		private System.Windows.Forms.Button btnLibraryBrowse;
		private System.Windows.Forms.CheckBox chkLogErrors;
		private CheckBox cbCatalogStoreShared;
		private TextBox tbCatalogStorePassword;
		private TextBox tbCatalogStoreDatabaseName;
		private Label label1;
		private Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ConfigForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Don't know why this is all messed up, but I'll put it here.
			this.Icon = new Icon(this.GetType().Assembly.GetManifestResourceStream(Alphora.Dataphor.DAE.Service.ConfigurationUtility.MainForm.WindowIconName));;
		}

		public int Port
		{
			get { return Convert.ToInt32(PortNumber.Text); }
			set { PortNumber.Text = value.ToString(); }
		}

		public string CatalogDirectory
		{
			get { return CatalogDirectoryTextBox.Text; }
			set { CatalogDirectoryTextBox.Text = value; }
		}
		
		public string CatalogStoreDatabaseName
		{
			get { return this.tbCatalogStoreDatabaseName.Text; }
			set { tbCatalogStoreDatabaseName.Text = value; }
		}
		
		public string CatalogStorePassword
		{
			get { return tbCatalogStorePassword.Text; }
			set { tbCatalogStorePassword.Text = value; }
		}
		
		public bool CatalogStoreShared
		{
			get { return cbCatalogStoreShared.Checked; }
			set { cbCatalogStoreShared.Checked = value; }
		}

		public string StartupScriptFile
		{
			get { return String.Empty; }
			set { }
		}

		public string LibraryDirectory
		{
			get { return txtLibraryDirectory.Text; }
			set { txtLibraryDirectory.Text = value; }
		}

		public bool LogErrors
		{
			get { return chkLogErrors.Checked; }
			set { chkLogErrors.Checked = value; }
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
			this.CatalogBrowse = new System.Windows.Forms.Button();
			this.CatalogDirectoryTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.PortNumber = new System.Windows.Forms.TextBox();
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.txtLibraryDirectory = new System.Windows.Forms.TextBox();
			this.btnLibraryBrowse = new System.Windows.Forms.Button();
			this.chkLogErrors = new System.Windows.Forms.CheckBox();
			this.cbCatalogStoreShared = new System.Windows.Forms.CheckBox();
			this.tbCatalogStorePassword = new System.Windows.Forms.TextBox();
			this.tbCatalogStoreDatabaseName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// CatalogBrowse
			// 
			this.CatalogBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.CatalogBrowse.Location = new System.Drawing.Point(272, 72);
			this.CatalogBrowse.Name = "CatalogBrowse";
			this.CatalogBrowse.Size = new System.Drawing.Size(24, 20);
			this.CatalogBrowse.TabIndex = 3;
			this.CatalogBrowse.Text = "...";
			this.CatalogBrowse.Click += new System.EventHandler(this.CatalogBrowse_Click);
			// 
			// CatalogDirectoryTextBox
			// 
			this.CatalogDirectoryTextBox.Location = new System.Drawing.Point(8, 72);
			this.CatalogDirectoryTextBox.Name = "CatalogDirectoryTextBox";
			this.CatalogDirectoryTextBox.Size = new System.Drawing.Size(264, 20);
			this.CatalogDirectoryTextBox.TabIndex = 2;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label4.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label4.Location = new System.Drawing.Point(8, 56);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(88, 13);
			this.label4.TabIndex = 18;
			this.label4.Text = "Catalog Directory";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(36, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "Port #";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// PortNumber
			// 
			this.PortNumber.Location = new System.Drawing.Point(8, 24);
			this.PortNumber.MaxLength = 5;
			this.PortNumber.Name = "PortNumber";
			this.PortNumber.Size = new System.Drawing.Size(56, 20);
			this.PortNumber.TabIndex = 1;
			this.PortNumber.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.PortNumber_OnKeyPress);
			// 
			// Ok
			// 
			this.Ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Ok.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.Ok.Location = new System.Drawing.Point(144, 235);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(75, 23);
			this.Ok.TabIndex = 8;
			this.Ok.Text = "OK";
			// 
			// Cancel
			// 
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.Cancel.Location = new System.Drawing.Point(221, 235);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 9;
			this.Cancel.Text = "Cancel";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label5.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label5.Location = new System.Drawing.Point(8, 163);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(83, 13);
			this.label5.TabIndex = 22;
			this.label5.Text = "Library Directory";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtLibraryDirectory
			// 
			this.txtLibraryDirectory.Location = new System.Drawing.Point(8, 179);
			this.txtLibraryDirectory.Name = "txtLibraryDirectory";
			this.txtLibraryDirectory.Size = new System.Drawing.Size(264, 20);
			this.txtLibraryDirectory.TabIndex = 4;
			// 
			// btnLibraryBrowse
			// 
			this.btnLibraryBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnLibraryBrowse.Location = new System.Drawing.Point(272, 179);
			this.btnLibraryBrowse.Name = "btnLibraryBrowse";
			this.btnLibraryBrowse.Size = new System.Drawing.Size(24, 20);
			this.btnLibraryBrowse.TabIndex = 5;
			this.btnLibraryBrowse.Text = "...";
			this.btnLibraryBrowse.Click += new System.EventHandler(this.button1_Click);
			// 
			// chkLogErrors
			// 
			this.chkLogErrors.Location = new System.Drawing.Point(115, 205);
			this.chkLogErrors.Name = "chkLogErrors";
			this.chkLogErrors.Size = new System.Drawing.Size(104, 24);
			this.chkLogErrors.TabIndex = 7;
			this.chkLogErrors.Text = "Log Errors";
			// 
			// cbCatalogStoreShared
			// 
			this.cbCatalogStoreShared.AutoSize = true;
			this.cbCatalogStoreShared.Location = new System.Drawing.Point(8, 144);
			this.cbCatalogStoreShared.Name = "cbCatalogStoreShared";
			this.cbCatalogStoreShared.Size = new System.Drawing.Size(127, 17);
			this.cbCatalogStoreShared.TabIndex = 42;
			this.cbCatalogStoreShared.Text = "Shared Catalog Store";
			this.cbCatalogStoreShared.UseVisualStyleBackColor = true;
			this.cbCatalogStoreShared.Visible = false;
			// 
			// tbCatalogStorePassword
			// 
			this.tbCatalogStorePassword.Location = new System.Drawing.Point(166, 118);
			this.tbCatalogStorePassword.Name = "tbCatalogStorePassword";
			this.tbCatalogStorePassword.PasswordChar = '*';
			this.tbCatalogStorePassword.Size = new System.Drawing.Size(98, 20);
			this.tbCatalogStorePassword.TabIndex = 43;
			// 
			// tbCatalogStoreDatabaseName
			// 
			this.tbCatalogStoreDatabaseName.Location = new System.Drawing.Point(8, 118);
			this.tbCatalogStoreDatabaseName.Name = "tbCatalogStoreDatabaseName";
			this.tbCatalogStoreDatabaseName.Size = new System.Drawing.Size(151, 20);
			this.tbCatalogStoreDatabaseName.TabIndex = 41;
			this.tbCatalogStoreDatabaseName.Text = "DAECatalog";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(163, 102);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 13);
			this.label1.TabIndex = 45;
			this.label1.Text = "Password";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 102);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(151, 13);
			this.label3.TabIndex = 44;
			this.label3.Text = "Catalog Store Database Name";
			// 
			// ConfigForm
			// 
			this.AcceptButton = this.Ok;
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(306, 268);
			this.Controls.Add(this.cbCatalogStoreShared);
			this.Controls.Add(this.tbCatalogStorePassword);
			this.Controls.Add(this.tbCatalogStoreDatabaseName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.chkLogErrors);
			this.Controls.Add(this.btnLibraryBrowse);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.txtLibraryDirectory);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.CatalogDirectoryTextBox);
			this.Controls.Add(this.PortNumber);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.Controls.Add(this.CatalogBrowse);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "ConfigForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Dataphor Server Configuration";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void CatalogBrowse_Click(object sender, System.EventArgs e)
		{
			CatalogDirectoryTextBox.Text = GetDirectory(MainForm.CatalogBrowseTitle, CatalogDirectoryTextBox.Text);
		}

		private string GetDirectory(string description, string directory)
		{
			FolderBrowserDialog browser = new FolderBrowserDialog();
			browser.Description = "Select directory";
			if (directory != String.Empty)
				browser.SelectedPath = directory;
			else
				browser.RootFolder = Environment.SpecialFolder.MyComputer;
			browser.ShowNewFolderButton = true;
			if (browser.ShowDialog(this) != DialogResult.OK)
				throw new AbortException();
			return browser.SelectedPath;
		}

/*
		private void StartupScriptBrowse_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.OpenFileDialog LFileDlg = new System.Windows.Forms.OpenFileDialog();

			LFileDlg.InitialDirectory = StartupScriptFileName.Text;
			LFileDlg.FileName = StartupScriptFileName.Text;
			LFileDlg.Title = Alphora.Dataphor.DAE.Service.ConfigurationUtility.MainForm.CStartupScriptBrowseTitle;
			LFileDlg.DefaultExt = Alphora.Dataphor.DAE.Service.ConfigurationUtility.MainForm.CStartupScriptDefaultExtension;
			LFileDlg.DereferenceLinks = true;
			LFileDlg.Filter = Alphora.Dataphor.DAE.Service.ConfigurationUtility.MainForm.CStartupScriptBrowseFilter;
			LFileDlg.Multiselect = false;
			System.Windows.Forms.DialogResult LResult = LFileDlg.ShowDialog();

			if (LResult == System.Windows.Forms.DialogResult.OK)
			{
				StartupScriptFileName.Text = LFileDlg.FileName;
			}
		}
*/

		// This makes sure that you can't enter anything but 0-9 in the port #
		private void PortNumber_OnKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs args)
		{
			if (args.KeyChar > 32)
			{
				if ((args.KeyChar < '0') || (args.KeyChar > '9'))
					args.Handled = true;
			}

			base.OnKeyPress(args);
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			txtLibraryDirectory.Text = GetDirectory(MainForm.CatalogBrowseTitle, txtLibraryDirectory.Text);
		}
	}
}
