/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Displays the session's settings. </summary>
	public class ServerConnectForm : DialogForm
	{
		private System.ComponentModel.IContainer components;
		private Button EditAliasButton;
		private Button DeleteAliasButton;
		private ListView _serverList;
		private ColumnHeader _iconColumn;
		private ColumnHeader _aliasColumn;
		private System.Windows.Forms.TextBox UserIDTextBox;
		private Label label1;
		private Label label2;
		private System.Windows.Forms.TextBox PasswordTextBox;
		private Button AddAliasButton;
		private System.Windows.Forms.ImageList _imageList;

		public ServerConnectForm() 
		{
			InitializeComponent();
			SuspendLayout();
			try
			{
				SetAcceptReject(true, false);
				FStatusBar.Visible = false;
			}
			finally
			{
				ResumeLayout(false);
			}
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerConnectForm));
			this._imageList = new System.Windows.Forms.ImageList(this.components);
			this._serverList = new System.Windows.Forms.ListView();
			this._iconColumn = new System.Windows.Forms.ColumnHeader();
			this._aliasColumn = new System.Windows.Forms.ColumnHeader();
			this.DeleteAliasButton = new System.Windows.Forms.Button();
			this.EditAliasButton = new System.Windows.Forms.Button();
			this.AddAliasButton = new System.Windows.Forms.Button();
			this.PasswordTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.UserIDTextBox = new System.Windows.Forms.TextBox();
			this.FContentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// FContentPanel
			// 
			this.FContentPanel.AutoScroll = true;
			this.FContentPanel.Controls.Add(this.UserIDTextBox);
			this.FContentPanel.Controls.Add(this.label1);
			this.FContentPanel.Controls.Add(this.label2);
			this.FContentPanel.Controls.Add(this.PasswordTextBox);
			this.FContentPanel.Controls.Add(this.AddAliasButton);
			this.FContentPanel.Controls.Add(this.EditAliasButton);
			this.FContentPanel.Controls.Add(this.DeleteAliasButton);
			this.FContentPanel.Controls.Add(this._serverList);
			this.FContentPanel.Size = new System.Drawing.Size(398, 259);
			// 
			// FImageList
			// 
			this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FImageList.ImageStream")));
			this._imageList.TransparentColor = System.Drawing.Color.White;
			this._imageList.Images.SetKeyName(0, "");
			this._imageList.Images.SetKeyName(1, "");
			this._imageList.Images.SetKeyName(2, "");
			this._imageList.Images.SetKeyName(3, "");
			this._imageList.Images.SetKeyName(4, "");
			this._imageList.Images.SetKeyName(5, "");
			this._imageList.Images.SetKeyName(6, "");
			// 
			// FServerList
			// 
			this._serverList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._serverList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._iconColumn,
            this._aliasColumn});
			this._serverList.FullRowSelect = true;
			this._serverList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this._serverList.HideSelection = false;
			this._serverList.Location = new System.Drawing.Point(7, 6);
			this._serverList.MultiSelect = false;
			this._serverList.Name = "FServerList";
			this._serverList.Size = new System.Drawing.Size(296, 158);
			this._serverList.SmallImageList = this._imageList;
			this._serverList.TabIndex = 7;
			this._serverList.UseCompatibleStateImageBehavior = false;
			this._serverList.View = System.Windows.Forms.View.Details;
			this._serverList.DoubleClick += new System.EventHandler(this.ServerListDoubleClicked);
			this._serverList.SelectedIndexChanged += new System.EventHandler(this.FServerList_SelectedIndexChanged);
			// 
			// FIconColumn
			// 
			this._iconColumn.Text = "";
			this._iconColumn.Width = 24;
			// 
			// FAliasColumn
			// 
			this._aliasColumn.Text = "Server Alias";
			this._aliasColumn.Width = 220;
			// 
			// DeleteAliasButton
			// 
			this.DeleteAliasButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DeleteAliasButton.Image = ((System.Drawing.Image)(resources.GetObject("DeleteAliasButton.Image")));
			this.DeleteAliasButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.DeleteAliasButton.Location = new System.Drawing.Point(309, 56);
			this.DeleteAliasButton.Name = "DeleteAliasButton";
			this.DeleteAliasButton.Size = new System.Drawing.Size(81, 24);
			this.DeleteAliasButton.TabIndex = 6;
			this.DeleteAliasButton.Text = "&Delete";
			this.DeleteAliasButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.DeleteAliasButton.Click += new System.EventHandler(this.DeleteAliasButton_Click);
			// 
			// EditAliasButton
			// 
			this.EditAliasButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.EditAliasButton.Image = ((System.Drawing.Image)(resources.GetObject("EditAliasButton.Image")));
			this.EditAliasButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.EditAliasButton.Location = new System.Drawing.Point(309, 31);
			this.EditAliasButton.Name = "EditAliasButton";
			this.EditAliasButton.Size = new System.Drawing.Size(81, 24);
			this.EditAliasButton.TabIndex = 5;
			this.EditAliasButton.Text = "&Edit...";
			this.EditAliasButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.EditAliasButton.Click += new System.EventHandler(this.EditAliasButton_Click);
			// 
			// AddAliasButton
			// 
			this.AddAliasButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AddAliasButton.Image = ((System.Drawing.Image)(resources.GetObject("AddAliasButton.Image")));
			this.AddAliasButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AddAliasButton.Location = new System.Drawing.Point(309, 6);
			this.AddAliasButton.Name = "AddAliasButton";
			this.AddAliasButton.Size = new System.Drawing.Size(81, 24);
			this.AddAliasButton.TabIndex = 4;
			this.AddAliasButton.Text = "&Add...";
			this.AddAliasButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.AddAliasButton.Click += new System.EventHandler(this.AddAliasButton_Click);
			// 
			// PasswordTextBox
			// 
			this.PasswordTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PasswordTextBox.Location = new System.Drawing.Point(8, 234);
			this.PasswordTextBox.Name = "PasswordTextBox";
			this.PasswordTextBox.PasswordChar = '*';
			this.PasswordTextBox.Size = new System.Drawing.Size(220, 20);
			this.PasswordTextBox.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.Location = new System.Drawing.Point(8, 216);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "&Password";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Location = new System.Drawing.Point(8, 172);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(43, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "&User ID";
			// 
			// UserIDTextBox
			// 
			this.UserIDTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.UserIDTextBox.Location = new System.Drawing.Point(8, 190);
			this.UserIDTextBox.Name = "UserIDTextBox";
			this.UserIDTextBox.Size = new System.Drawing.Size(300, 20);
			this.UserIDTextBox.TabIndex = 0;
			this.UserIDTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UserIDTextBox_KeyDown);
			// 
			// ServerConnectForm
			// 
			this.ClientSize = new System.Drawing.Size(398, 330);
			this.ControlBox = false;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Location = new System.Drawing.Point(0, 0);
			this.MinimumSize = new System.Drawing.Size(250, 228);
			this.Name = "ServerConnectForm";
			this.Text = "Connect to Server";
			this.FContentPanel.ResumeLayout(false);
			this.FContentPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary> Loads, displays, saves and returns an alias configuration. </summary>
		/// <remarks> Any changes to the alias list are saved even if the form is cancelled. </remarks>
		public static AliasConfiguration Execute()
		{
			AliasConfiguration configuration = AliasManager.LoadConfiguration();
			Execute(configuration);
			return configuration;
		}

		public static void Execute(AliasConfiguration configuration)
		{
			using (ServerConnectForm form = new ServerConnectForm())
			{
				form.SetConfiguration(configuration);
				form.EnsureServerSelected();

				DialogResult result = form.ShowDialog();

				ServerAlias selected = form.SelectedAlias;
				if (selected != null)
					selected.SessionInfo.UserID = form.UserIDTextBox.Text;
				
				if (selected != null)
					form.Configuration.DefaultAliasName = selected.Name;
				else
					form.Configuration.DefaultAliasName = String.Empty;
					
				AliasManager.SaveConfiguration(form.Configuration);

				if (result != DialogResult.OK)
					throw new AbortException();

				if (selected != null)
					selected.SessionInfo.Password = form.PasswordTextBox.Text;
			}
		}

		private AliasConfiguration _configuration;
		
		public AliasConfiguration Configuration
		{
			get { return _configuration; }
		}

		public ServerAlias SelectedAlias
		{
			get
			{
				if (_serverList.SelectedItems.Count > 0)
					return (ServerAlias)_serverList.SelectedItems[0].Tag;
				else
					return null;
			}
		}

		private void SetConfiguration(AliasConfiguration configuration)
		{
			_configuration = configuration;
			_serverList.Items.Clear();
			foreach (ServerAlias alias in configuration.Aliases.Values)
				AddItem(alias);
			SelectAlias(configuration.DefaultAliasName);
		}

		/// <remarks> If the named alias is not found, first item is selected. </remarks>
		private void SelectAlias(string aliasName)
		{
			ListViewItem item;
			for (int i = 0; i < _serverList.Items.Count; i++)
			{
				item = _serverList.Items[i];
				if (String.Compare(((ServerAlias)item.Tag).Name, aliasName, true) == 0)
				{
					item.Selected = true;
					return;
				}
			}
			if (_serverList.Items.Count > 0)
				_serverList.Items[0].Selected = true;
		}

		private void AddItem(ServerAlias alias)
		{
			ListViewItem item = new ListViewItem(String.Empty, (alias is InProcessAlias ? 0 : 1));
			item.SubItems.Add(alias.ToString());
			item.Tag = alias;
			_serverList.Items.Add(item);
		}

		/// <remarks> Does nothing if not found. </remarks>
		private void RemoveItem(ServerAlias alias)
		{
			ListViewItem item;
			for (int i = 0; i < _serverList.Items.Count; i++)
			{
				item = _serverList.Items[i];
				if (item.Tag == alias)
					_serverList.Items.RemoveAt(i);
			}
			EnsureServerSelected();
		}

		private void AddAliasButton_Click(object sender, System.EventArgs e)
		{
			ServerAlias alias = EditAliasForm.ExecuteAdd();
			_configuration.Aliases.Add(alias);
			AddItem(alias);
			SelectAlias(alias.Name);
		}

		private void EditAliasButton_Click(object sender, System.EventArgs e)
		{
			ServerAlias oldAlias = SelectedAlias;
			if (oldAlias != null)
			{
				ServerAlias newAlias = EditAliasForm.ExecuteEdit(oldAlias);
				RemoveItem(oldAlias);
				_configuration.Aliases.Remove(oldAlias.Name);
				_configuration.Aliases.Add(newAlias);
				AddItem(newAlias);
				SelectAlias(newAlias.Name);
			}
		}

		private void DeleteAliasButton_Click(object sender, System.EventArgs e)
		{
			ServerAlias alias = SelectedAlias;
			if 
			(
				(alias != null) && 
				(
					MessageBox.Show
					(
						Strings.CConfirmAliasDeleteText, 
						Strings.CConfirmAliasDeleteCaption,
						MessageBoxButtons.YesNo, 
						MessageBoxIcon.Warning, 
						MessageBoxDefaultButton.Button1
					) == DialogResult.Yes
				)
			)
			{
				RemoveItem(alias);
				_configuration.Aliases.Remove(alias.Name);
			}
			EnsureServerSelected();
		}

		private void EnsureServerSelected()
		{
			// Select the first item if nothing is selected
			if ((_serverList.SelectedItems.Count == 0) && (_serverList.Items.Count > 0))
			{
				_serverList.Items[0].Selected = true;
			}
		}

		private void FServerList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Ensure that the selected item is visible
			if (_serverList.SelectedIndices.Count > 0)
				_serverList.EnsureVisible(_serverList.SelectedIndices[0]);

			if (SelectedAlias != null)
			{
				EditAliasButton.Enabled = true;
				DeleteAliasButton.Enabled = true;
				UserIDTextBox.Text = SelectedAlias.SessionInfo.UserID;
			}
			else
			{
				EditAliasButton.Enabled = false;
				DeleteAliasButton.Enabled = false;
			}
		}

		protected override void OnClosing(CancelEventArgs args)
		{
 			base.OnClosing(args);
			args.Cancel = false;
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					try
					{
						PostChanges();
					}
					catch
					{
						args.Cancel = true;
						throw;
					}
				}
				else
					CancelChanges();
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);
			}
		}

		private void CancelChanges()
		{
			// Nothing
		}

		private void PostChanges()
		{
			if (SelectedAlias == null)
			{
				System.Media.SystemSounds.Beep.Play();
				throw new AbortException();
			}
		}

		private bool InternalProcessKey(Keys keyData)
		{
			if (_serverList.SelectedIndices.Count > 0)
			{
				switch (keyData)
				{
					case Keys.Up :
						if (_serverList.SelectedIndices[0] > 0)
							_serverList.Items[_serverList.SelectedIndices[0] - 1].Selected = true;
						return true;
					case Keys.Down :
						if (_serverList.SelectedIndices[0] < (_serverList.Items.Count - 1))
							_serverList.Items[_serverList.SelectedIndices[0] + 1].Selected = true;
						return true;
				}
			}
			return false;
		}

		/// <remarks> Allow the user to scroll the selected server list from anywhere on the form. </remarks>
		protected override bool ProcessDialogKey(Keys keyData)
		{
			return InternalProcessKey(keyData) || base.ProcessDialogKey(keyData);
		}

		private void UserIDTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			e.Handled |= InternalProcessKey(e.KeyData);
		}

		private void ServerListDoubleClicked(object sender, System.EventArgs e)
		{
			Close(CloseBehavior.AcceptOrClose);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// HACK: The first control doesn't have focus when this form is shown (???) so we have to explicitly focus it.
			UserIDTextBox.Focus();
		}
	}
}