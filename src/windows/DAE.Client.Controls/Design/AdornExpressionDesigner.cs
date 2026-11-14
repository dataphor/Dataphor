/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	public class TagCollectionEditor : CollectionEditor
	{
		public TagCollectionEditor(Type itemType) : base(itemType){}
		protected override object CreateInstance(Type itemType)
		{
			if (typeof(Alphora.Dataphor.DAE.Language.D4.Tag).IsAssignableFrom(itemType))
			{
				NameValueEdit FNameValueEdit = new NameValueEdit();
				FNameValueEdit.ShowDialog();
				if (FNameValueEdit.DialogResult == DialogResult.OK)
					return new Tag(FNameValueEdit._textBoxName.Text,FNameValueEdit._textBoxValue.Text);
				else
					throw new  DesignException(DesignException.Codes.AddTagCancelled);
			}
			else
				return base.CreateInstance(itemType);
		}
	}
	
	public class NameValueEdit : Form
	{
		public const int TagFormWidth = 300;
		public const int TagFormHeight = 200;
		public TextBox _textBoxName;
		public TextBox _textBoxValue;
		public Label _labelName;
		public Label _labelValue;
		public Button _buttonOK;
		public Button _buttonCancel;

		public NameValueEdit()
		{
			Size = new System.Drawing.Size(TagFormWidth,TagFormHeight);
			Text = "Tag Editor Dialog";
			SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			_labelName = new Label();
			_labelName.Parent = this;
			_labelName.Text = "Name";
			_labelName.Left = 20;
			_labelName.Top = 10;
			_textBoxName = new TextBox();
			_textBoxName.Parent = this;
			_textBoxName.Left = 20;
			_textBoxName.Top = 35;
			_textBoxName.Width = 150;
			_labelValue = new Label();
			_labelValue.Parent = this;
			_labelValue.Text = "Value";
			_labelValue.Left = 20;
			_labelValue.Top = 70;
			_textBoxValue = new TextBox();
			_textBoxValue.Parent = this;
			_textBoxValue.Left = 20;
			_textBoxValue.Top = 95;
			_textBoxValue.Width = 150;
			_buttonOK = new Button();
			_buttonOK.Parent = this;
			_buttonOK.Left = 200;
			_buttonOK.Top = 20;
			_buttonOK.Text = "OK";
			_buttonOK.DialogResult = DialogResult.OK;
			_buttonCancel = new Button();
			_buttonCancel.Parent = this;
			_buttonCancel.Left = 200;
			_buttonCancel.Top = 50;
			_buttonCancel.Text = "Cancel";
			_buttonCancel.DialogResult = DialogResult.Cancel;
			_buttonCancel.Click += new EventHandler(DoButtonCancelClick);
			_buttonOK.Click += new EventHandler(DoButtonOKClick);
		}

		private bool _disposed;
		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				try
				{
					_buttonCancel.Click -= new EventHandler(DoButtonCancelClick);
					_buttonOK.Click -= new EventHandler(DoButtonOKClick);
					_labelName.Dispose();
					_labelValue.Dispose();
					_textBoxName.Dispose();
					_textBoxValue.Dispose();
					_buttonOK.Dispose();
					_buttonCancel.Dispose();
				}
				finally
				{
					_disposed = true;
				}
			}
		}

		protected virtual void DoButtonCancelClick(object sender, EventArgs args)
		{
			Close();
		}
		protected virtual void DoButtonOKClick(object sender, EventArgs args)
		{
			Close();
		}
	}
	 	 
	public class MetaDataEditor : System.Drawing.Design.UITypeEditor 
	{
		private IWindowsFormsEditorService _editorService = null;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			MetaDataEditForm metaDataEditForm = new MetaDataEditForm((MetaData)tempValue); 
			try
			{
				_editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
				_editorService.ShowDialog(metaDataEditForm);
				if (metaDataEditForm.DialogResult == DialogResult.OK)
					tempValue = metaDataEditForm.MetaData;
			}
			finally
			{
				metaDataEditForm.Dispose();
			}
			return tempValue;
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{ 
			return UITypeEditorEditStyle.Modal;
		}
	}

	public class MetaDataEditForm : Form
	{
		public const int TagFormWidth = 323;
		public const int TagFormHeight = 300;
		public CheckBox _checkBox;
		public RadioButton _radioButtonNone;
		public RadioButton _radioButtonMetadata;
		public GroupBox _groupMetaData;
		public Label _labelComment;
		public TextBox _textBoxComment;
		public Label _labelTags;
		public ListView _listView;
		public Panel _buttonPanel;
		public Button _buttonAdd;
		public Button _buttonEdit;
		public Button _buttonDelete;
		public Panel _panelDivide;
		public Button _buttonOK;
		public Button _buttonCancel;
		public TagCollectionEditor _tagCollectionEditor;
		public NameValueEdit _nameValueEdit;
		private MetaData _originalMetaData;

		public MetaDataEditForm(MetaData tempValue)
		{
			Size = new System.Drawing.Size(TagFormWidth,TagFormHeight);
			Text = "Metadata Edit";
			_radioButtonNone = new RadioButton();
			_radioButtonNone.Text = "None";
			_radioButtonNone.Parent = this;
			_radioButtonNone.Location = new Point(16,8);
			_radioButtonNone.Checked = true;
			_radioButtonMetadata = new RadioButton();
			_radioButtonMetadata.Text = "Metadata";
			_radioButtonMetadata.Parent = this;
			_radioButtonMetadata.Width = 75;
			_radioButtonMetadata.Location = new Point(16,32);
			_groupMetaData = new GroupBox();
			_groupMetaData.Parent = this;
			_groupMetaData.Location = new Point(8,36);
			_groupMetaData.Size = new System.Drawing.Size(299,190);
			_groupMetaData.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
										System.Windows.Forms.AnchorStyles.Bottom) | 
										System.Windows.Forms.AnchorStyles.Left) | 
										System.Windows.Forms.AnchorStyles.Right);
			_groupMetaData.Enabled = false;
			_labelComment = new Label();
			_labelComment.Parent = _groupMetaData;
			_labelComment.Location = new Point(8,20);
			_labelComment.Height = 15;
			_labelComment.Text = "Comment";
			_textBoxComment = new TextBox();
			_textBoxComment.Parent = _groupMetaData;
			_textBoxComment.Location = new Point(8,43);
			_textBoxComment.Width = 200;
			_textBoxComment.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
										System.Windows.Forms.AnchorStyles.Bottom) | 
										System.Windows.Forms.AnchorStyles.Left) | 
										System.Windows.Forms.AnchorStyles.Right);
			_labelTags = new Label();
			_labelTags.Parent = _groupMetaData;
			_labelTags.Location = new Point(8,74);
			_labelTags.Height = 15;
			_labelTags.Text = "Tags";
			_listView = new ListView();
			_listView.Parent = _groupMetaData;
			_listView.Location = new Point(8,97);
			_listView.Size = new System.Drawing.Size(200,85);
			_listView.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
								  System.Windows.Forms.AnchorStyles.Bottom) | 
								  System.Windows.Forms.AnchorStyles.Left) | 
								  System.Windows.Forms.AnchorStyles.Right);
			_listView.View = View.Details;
			_listView.GridLines = false;
			_listView.Columns.Add("Name", 98, HorizontalAlignment.Left);
			_listView.Columns.Add("Value", 1500, HorizontalAlignment.Left);
			_listView.Scrollable = true;
			_listView.FullRowSelect = true;
			_buttonPanel = new Panel();
			_buttonPanel.Parent = _groupMetaData;
			_buttonPanel.Location = new Point(216,97);
			_buttonPanel.Size = new System.Drawing.Size(75,85);
			_buttonPanel.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
									 System.Windows.Forms.AnchorStyles.Right)));
			_buttonAdd = new Button();
			_buttonAdd.Parent = _buttonPanel;
			_buttonAdd.Location = new Point(0,0);
			_buttonAdd.Text = "&Add...";
			_buttonEdit = new Button();
			_buttonEdit.Parent = _buttonPanel;
			_buttonEdit.Location = new Point(0,31);
			_buttonEdit.Text = "&Edit...";
			_buttonDelete = new Button();
			_buttonDelete.Parent = _buttonPanel;
			_buttonDelete.Location = new Point(0,62);
			_buttonDelete.Text = "&Delete";
			_buttonOK = new Button();
			_buttonOK.Parent = this;
			_buttonOK.Location = new Point(141,234);
			_buttonOK.Text = "&OK";
			_buttonOK.DialogResult = DialogResult.OK;
			_buttonOK.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
								  System.Windows.Forms.AnchorStyles.Right)));
			_buttonCancel = new Button();
			_buttonCancel.Parent = this;
			_buttonCancel.Location = new Point(224,234);
			_buttonCancel.Text = "&Cancel";
			_buttonCancel.DialogResult = DialogResult.Cancel;
			_buttonCancel.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
									  System.Windows.Forms.AnchorStyles.Right)));
			_buttonCancel.Click += new EventHandler(DoButtonCancelClick);
			_buttonOK.Click += new EventHandler(DoButtonOKClick);
			_buttonAdd.Click += new EventHandler(DoButtonAddClick);
			_buttonEdit.Click += new EventHandler(DoButtonEditClick);
			_buttonDelete.Click += new EventHandler(DoButtonDeleteClick);
			_radioButtonMetadata.Click += new EventHandler(DoRadioButtonMetadataClick);
			_radioButtonNone.Click += new EventHandler(DoRadioButtonNoneClick);

			_metaData = tempValue;
			if ( tempValue != null	)
			{
				//FTextBoxComment.Text = FMetaData.Comment;
				Changed();
			}
		}

		private bool _disposed;
		protected override void Dispose(bool disposing)
		{
			if(!_disposed)
			{
				try
				{
					_buttonAdd.Dispose();
					_buttonEdit.Dispose();
					_buttonDelete.Dispose();
					_buttonOK.Dispose();
					_buttonCancel.Dispose();
					_labelComment.Dispose();
					_textBoxComment.Dispose();
					_labelTags.Dispose();
					_listView.Dispose();
					_groupMetaData.Dispose();
					_buttonPanel.Dispose();
					_radioButtonNone.Dispose();
					_radioButtonMetadata.Dispose();
					_buttonCancel.Click -= new EventHandler(DoButtonCancelClick);
					_buttonOK.Click -= new EventHandler(DoButtonOKClick);
					_buttonAdd.Click -= new EventHandler(DoButtonAddClick);
					_buttonEdit.Click -= new EventHandler(DoButtonEditClick);
					_buttonDelete.Click -= new EventHandler(DoButtonDeleteClick);
					_radioButtonMetadata.Click -= new EventHandler(DoRadioButtonMetadataClick);
					_radioButtonNone.Click -= new EventHandler(DoRadioButtonNoneClick);
					 
				}
				finally
				{
					_disposed = true;
				}
			}
		}

		protected virtual void DoRadioButtonMetadataClick(object sender, EventArgs args)
		{
			 if	(MetaData == null)
				 MetaData = new MetaData();
			 else if (_originalMetaData != null)
				 MetaData = _originalMetaData.Copy();	
		}

		protected virtual void DoRadioButtonNoneClick(object sender, EventArgs args)
		{
			MetaData metaData = _metaData;
			MetaData = null;
			if (metaData != null)
				metaData = null;
		}
		

		protected virtual void DoButtonAddClick(object sender, EventArgs args)
		{
			NameValueEdit nameValueEdit = new NameValueEdit();
			try
			{
				nameValueEdit.ShowDialog();
				if (nameValueEdit.DialogResult == DialogResult.OK)
				{
					Tag tag = new  Tag(nameValueEdit._textBoxName.Text,nameValueEdit._textBoxValue.Text);
					_metaData.Tags.Add(tag);
					Changed();
				}
				else
					throw new  DesignException(DesignException.Codes.AddTagCancelled);	
			}
			finally
			{
				nameValueEdit.Dispose();
			}
		}

		protected virtual void DoButtonEditClick(object sender, EventArgs args)
		{
			try
			{
				if(_listView.SelectedItems.Count == 0 && _metaData.Tags.Count > 0)
				{
					throw new DesignException(DesignException.Codes.NoTagSelected);
				}
				else if (_metaData.Tags.Count == 0) {}
				else
				{
					/*
					NameValueEdit LNameValueEdit = new NameValueEdit();
					LNameValueEdit.FTextBoxName.Text = FMetaData.Tags[FListView.SelectedIndices[0]].Name;
					LNameValueEdit.FTextBoxValue.Text = FMetaData.Tags[FListView.SelectedIndices[0]].Value;
					LNameValueEdit.FTextBoxValue.Select();
					LNameValueEdit.FTextBoxName.Enabled = false;
					LNameValueEdit.FTextBoxName.BackColor = SystemColors.Control;
					LNameValueEdit.ShowDialog();
					
					if(LNameValueEdit.DialogResult == DialogResult.OK)
					{
						FMetaData.Tags[FListView.SelectedIndices[0]].Value = LNameValueEdit.FTextBoxValue.Text;
						Changed();
					}
					LNameValueEdit.Dispose();
					*/
				}
			}
			finally{}
		}
		protected virtual void DoButtonDeleteClick(object sender, EventArgs args)
		{
/*
			try
			{
				if (FListView.SelectedItems.Count == 0 && FMetaData.Tags.Count > 0)
				{
					throw new DesignException(DesignException.Codes.NoTagSelected);			 
				}
				else if (FMetaData.Tags.Count == 0) {}
				else if (MessageBox.Show((IWin32Window)this,"Delete   '" + FMetaData.Tags[FListView.SelectedIndices[0]].Name + "' ?","Delete Tag Dialog",MessageBoxButtons.OKCancel,MessageBoxIcon.Question) == DialogResult.OK)
				{
					FMetaData.Tags.RemoveAt(FListView.SelectedIndices[0]);
					Changed();
				}
			}
			finally{} 
*/
		}

		protected virtual void DoButtonCancelClick(object sender, EventArgs args)
		{
			Close();
		}
		protected virtual void DoButtonOKClick(object sender, EventArgs args)
		{
			//if (FMetaData != null)
			//	MetaData.Comment = FTextBoxComment.Text;
			Close();
		}

		private MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set 
			{
				if(_metaData != value)
				{
					if (value != null)
						_originalMetaData = value.Copy();
					else
						_originalMetaData = null;
					_metaData = value;
					Changed();
				}
			}
		}
		 
		protected void Changed()
		{
			if(_metaData != null)
			{
				_radioButtonNone.Checked = false;
				_radioButtonMetadata.Checked = true;
				_groupMetaData.Enabled = true;
				_buttonPanel.Enabled = true;
				_listView.BackColor = SystemColors.Window;
				_listView.Items.Clear();
				int i = 0;
				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in FMetaData.Tags)
				{ 
				#else
				Tag tag;
				for (int index = 0; index < _metaData.Tags.Count; index++)
				{
					tag = _metaData.Tags[index];
				#endif
					_listView.Items.Add(new ListViewItem());
					_listView.Items[i].Text = tag.Name;
					_listView.Items[i].SubItems.Add(tag.Value);
					i = i + 1;
				}
			}
			else
			{
				_textBoxComment.Clear();
				_listView.Items.Clear();
				_listView.Columns.Add("Name", 98, HorizontalAlignment.Left);
				_listView.Columns.Add("Value", 1500, HorizontalAlignment.Left);
				_radioButtonNone.Checked = true;
				_radioButtonMetadata.Checked = false;
				_groupMetaData.Enabled = false;
				_listView.BackColor = SystemColors.Control;
			}
		}	 
	}	 
}
