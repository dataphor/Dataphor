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
		public TagCollectionEditor(Type AItemType) : base(AItemType){}
		protected override object CreateInstance(Type AItemType)
		{
			if (typeof(Alphora.Dataphor.DAE.Language.D4.Tag).IsAssignableFrom(AItemType))
			{
				NameValueEdit FNameValueEdit = new NameValueEdit();
				FNameValueEdit.ShowDialog();
				if (FNameValueEdit.DialogResult == DialogResult.OK)
					return new Tag(FNameValueEdit.FTextBoxName.Text,FNameValueEdit.FTextBoxValue.Text);
				else
					throw new  DesignException(DesignException.Codes.AddTagCancelled);
			}
			else
				return base.CreateInstance(AItemType);
		}
	}
	
	public class NameValueEdit : Form
	{
		public const int CTagFormWidth = 300;
		public const int CTagFormHeight = 200;
		public TextBox FTextBoxName;
		public TextBox FTextBoxValue;
		public Label FLabelName;
		public Label FLabelValue;
		public Button FButtonOK;
		public Button FButtonCancel;

		public NameValueEdit()
		{
			Size = new System.Drawing.Size(CTagFormWidth,CTagFormHeight);
			Text = "Tag Editor Dialog";
			SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			FLabelName = new Label();
			FLabelName.Parent = this;
			FLabelName.Text = "Name";
			FLabelName.Left = 20;
			FLabelName.Top = 10;
			FTextBoxName = new TextBox();
			FTextBoxName.Parent = this;
			FTextBoxName.Left = 20;
			FTextBoxName.Top = 35;
			FTextBoxName.Width = 150;
			FLabelValue = new Label();
			FLabelValue.Parent = this;
			FLabelValue.Text = "Value";
			FLabelValue.Left = 20;
			FLabelValue.Top = 70;
			FTextBoxValue = new TextBox();
			FTextBoxValue.Parent = this;
			FTextBoxValue.Left = 20;
			FTextBoxValue.Top = 95;
			FTextBoxValue.Width = 150;
			FButtonOK = new Button();
			FButtonOK.Parent = this;
			FButtonOK.Left = 200;
			FButtonOK.Top = 20;
			FButtonOK.Text = "OK";
			FButtonOK.DialogResult = DialogResult.OK;
			FButtonCancel = new Button();
			FButtonCancel.Parent = this;
			FButtonCancel.Left = 200;
			FButtonCancel.Top = 50;
			FButtonCancel.Text = "Cancel";
			FButtonCancel.DialogResult = DialogResult.Cancel;
			FButtonCancel.Click += new EventHandler(DoButtonCancelClick);
			FButtonOK.Click += new EventHandler(DoButtonOKClick);
		}

		private bool FDisposed;
		protected override void Dispose(bool ADisposing)
		{
			if (!FDisposed)
			{
				try
				{
					FButtonCancel.Click -= new EventHandler(DoButtonCancelClick);
					FButtonOK.Click -= new EventHandler(DoButtonOKClick);
					FLabelName.Dispose();
					FLabelValue.Dispose();
					FTextBoxName.Dispose();
					FTextBoxValue.Dispose();
					FButtonOK.Dispose();
					FButtonCancel.Dispose();
				}
				finally
				{
					FDisposed = true;
				}
			}
		}

		protected virtual void DoButtonCancelClick(object sender, EventArgs AArgs)
		{
			Close();
		}
		protected virtual void DoButtonOKClick(object sender, EventArgs AArgs)
		{
			Close();
		}
	}
	 	 
	public class MetaDataEditor : System.Drawing.Design.UITypeEditor 
	{
		private IWindowsFormsEditorService FEditorService = null;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider provider, object AValue) 
		{	 
			MetaDataEditForm LMetaDataEditForm = new MetaDataEditForm((MetaData)AValue); 
			try
			{
				FEditorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
				FEditorService.ShowDialog(LMetaDataEditForm);
				if (LMetaDataEditForm.DialogResult == DialogResult.OK)
					AValue = LMetaDataEditForm.MetaData;
			}
			finally
			{
				LMetaDataEditForm.Dispose();
			}
			return AValue;
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{ 
			return UITypeEditorEditStyle.Modal;
		}
	}

	public class MetaDataEditForm : Form
	{
		public const int CTagFormWidth = 323;
		public const int CTagFormHeight = 300;
		public CheckBox FCheckBox;
		public RadioButton FRadioButtonNone;
		public RadioButton FRadioButtonMetadata;
		public GroupBox FGroupMetaData;
		public Label FLabelComment;
		public TextBox FTextBoxComment;
		public Label FLabelTags;
		public ListView FListView;
		public Panel FButtonPanel;
		public Button FButtonAdd;
		public Button FButtonEdit;
		public Button FButtonDelete;
		public Panel FPanelDivide;
		public Button FButtonOK;
		public Button FButtonCancel;
		public TagCollectionEditor FTagCollectionEditor;
		public NameValueEdit FNameValueEdit;
		private MetaData FOriginalMetaData;

		public MetaDataEditForm(MetaData AValue)
		{
			Size = new System.Drawing.Size(CTagFormWidth,CTagFormHeight);
			Text = "Metadata Edit";
			FRadioButtonNone = new RadioButton();
			FRadioButtonNone.Text = "None";
			FRadioButtonNone.Parent = this;
			FRadioButtonNone.Location = new Point(16,8);
			FRadioButtonNone.Checked = true;
			FRadioButtonMetadata = new RadioButton();
			FRadioButtonMetadata.Text = "Metadata";
			FRadioButtonMetadata.Parent = this;
			FRadioButtonMetadata.Width = 75;
			FRadioButtonMetadata.Location = new Point(16,32);
			FGroupMetaData = new GroupBox();
			FGroupMetaData.Parent = this;
			FGroupMetaData.Location = new Point(8,36);
			FGroupMetaData.Size = new System.Drawing.Size(299,190);
			FGroupMetaData.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
										System.Windows.Forms.AnchorStyles.Bottom) | 
										System.Windows.Forms.AnchorStyles.Left) | 
										System.Windows.Forms.AnchorStyles.Right);
			FGroupMetaData.Enabled = false;
			FLabelComment = new Label();
			FLabelComment.Parent = FGroupMetaData;
			FLabelComment.Location = new Point(8,20);
			FLabelComment.Height = 15;
			FLabelComment.Text = "Comment";
			FTextBoxComment = new TextBox();
			FTextBoxComment.Parent = FGroupMetaData;
			FTextBoxComment.Location = new Point(8,43);
			FTextBoxComment.Width = 200;
			FTextBoxComment.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
										System.Windows.Forms.AnchorStyles.Bottom) | 
										System.Windows.Forms.AnchorStyles.Left) | 
										System.Windows.Forms.AnchorStyles.Right);
			FLabelTags = new Label();
			FLabelTags.Parent = FGroupMetaData;
			FLabelTags.Location = new Point(8,74);
			FLabelTags.Height = 15;
			FLabelTags.Text = "Tags";
			FListView = new ListView();
			FListView.Parent = FGroupMetaData;
			FListView.Location = new Point(8,97);
			FListView.Size = new System.Drawing.Size(200,85);
			FListView.Anchor = (((System.Windows.Forms.AnchorStyles.Top | 
								  System.Windows.Forms.AnchorStyles.Bottom) | 
								  System.Windows.Forms.AnchorStyles.Left) | 
								  System.Windows.Forms.AnchorStyles.Right);
			FListView.View = View.Details;
			FListView.GridLines = false;
			FListView.Columns.Add("Name", 98, HorizontalAlignment.Left);
			FListView.Columns.Add("Value", 1500, HorizontalAlignment.Left);
			FListView.Scrollable = true;
			FListView.FullRowSelect = true;
			FButtonPanel = new Panel();
			FButtonPanel.Parent = FGroupMetaData;
			FButtonPanel.Location = new Point(216,97);
			FButtonPanel.Size = new System.Drawing.Size(75,85);
			FButtonPanel.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
									 System.Windows.Forms.AnchorStyles.Right)));
			FButtonAdd = new Button();
			FButtonAdd.Parent = FButtonPanel;
			FButtonAdd.Location = new Point(0,0);
			FButtonAdd.Text = "&Add...";
			FButtonEdit = new Button();
			FButtonEdit.Parent = FButtonPanel;
			FButtonEdit.Location = new Point(0,31);
			FButtonEdit.Text = "&Edit...";
			FButtonDelete = new Button();
			FButtonDelete.Parent = FButtonPanel;
			FButtonDelete.Location = new Point(0,62);
			FButtonDelete.Text = "&Delete";
			FButtonOK = new Button();
			FButtonOK.Parent = this;
			FButtonOK.Location = new Point(141,234);
			FButtonOK.Text = "&OK";
			FButtonOK.DialogResult = DialogResult.OK;
			FButtonOK.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
								  System.Windows.Forms.AnchorStyles.Right)));
			FButtonCancel = new Button();
			FButtonCancel.Parent = this;
			FButtonCancel.Location = new Point(224,234);
			FButtonCancel.Text = "&Cancel";
			FButtonCancel.DialogResult = DialogResult.Cancel;
			FButtonCancel.Anchor = (((System.Windows.Forms.AnchorStyles.Bottom | 
									  System.Windows.Forms.AnchorStyles.Right)));
			FButtonCancel.Click += new EventHandler(DoButtonCancelClick);
			FButtonOK.Click += new EventHandler(DoButtonOKClick);
			FButtonAdd.Click += new EventHandler(DoButtonAddClick);
			FButtonEdit.Click += new EventHandler(DoButtonEditClick);
			FButtonDelete.Click += new EventHandler(DoButtonDeleteClick);
			FRadioButtonMetadata.Click += new EventHandler(DoRadioButtonMetadataClick);
			FRadioButtonNone.Click += new EventHandler(DoRadioButtonNoneClick);

			FMetaData = AValue;
			if ( AValue != null	)
			{
				//FTextBoxComment.Text = FMetaData.Comment;
				Changed();
			}
		}

		private bool FDisposed;
		protected override void Dispose(bool ADisposing)
		{
			if(!FDisposed)
			{
				try
				{
					FButtonAdd.Dispose();
					FButtonEdit.Dispose();
					FButtonDelete.Dispose();
					FButtonOK.Dispose();
					FButtonCancel.Dispose();
					FLabelComment.Dispose();
					FTextBoxComment.Dispose();
					FLabelTags.Dispose();
					FListView.Dispose();
					FGroupMetaData.Dispose();
					FButtonPanel.Dispose();
					FRadioButtonNone.Dispose();
					FRadioButtonMetadata.Dispose();
					FButtonCancel.Click -= new EventHandler(DoButtonCancelClick);
					FButtonOK.Click -= new EventHandler(DoButtonOKClick);
					FButtonAdd.Click -= new EventHandler(DoButtonAddClick);
					FButtonEdit.Click -= new EventHandler(DoButtonEditClick);
					FButtonDelete.Click -= new EventHandler(DoButtonDeleteClick);
					FRadioButtonMetadata.Click -= new EventHandler(DoRadioButtonMetadataClick);
					FRadioButtonNone.Click -= new EventHandler(DoRadioButtonNoneClick);
					 
				}
				finally
				{
					FDisposed = true;
				}
			}
		}

		protected virtual void DoRadioButtonMetadataClick(object sender, EventArgs AArgs)
		{
			 if	(MetaData == null)
				 MetaData = new MetaData();
			 else if (FOriginalMetaData != null)
				 MetaData = FOriginalMetaData.Copy();	
		}

		protected virtual void DoRadioButtonNoneClick(object sender, EventArgs AArgs)
		{
			MetaData LMetaData = FMetaData;
			MetaData = null;
			if (LMetaData != null)
				LMetaData = null;
		}
		

		protected virtual void DoButtonAddClick(object sender, EventArgs AArgs)
		{
			NameValueEdit LNameValueEdit = new NameValueEdit();
			try
			{
				LNameValueEdit.ShowDialog();
				if (LNameValueEdit.DialogResult == DialogResult.OK)
				{
					Tag LTag = new  Tag(LNameValueEdit.FTextBoxName.Text,LNameValueEdit.FTextBoxValue.Text);
					FMetaData.Tags.Add(LTag);
					Changed();
				}
				else
					throw new  DesignException(DesignException.Codes.AddTagCancelled);	
			}
			finally
			{
				LNameValueEdit.Dispose();
			}
		}

		protected virtual void DoButtonEditClick(object sender, EventArgs AArgs)
		{
			try
			{
				if(FListView.SelectedItems.Count == 0 && FMetaData.Tags.Count > 0)
				{
					throw new DesignException(DesignException.Codes.NoTagSelected);
				}
				else if (FMetaData.Tags.Count == 0) {}
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
		protected virtual void DoButtonDeleteClick(object sender, EventArgs AArgs)
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

		protected virtual void DoButtonCancelClick(object sender, EventArgs AArgs)
		{
			Close();
		}
		protected virtual void DoButtonOKClick(object sender, EventArgs AArgs)
		{
			//if (FMetaData != null)
			//	MetaData.Comment = FTextBoxComment.Text;
			Close();
		}

		private MetaData FMetaData;
		public MetaData MetaData
		{
			get { return FMetaData; }
			set 
			{
				if(FMetaData != value)
				{
					if (value != null)
						FOriginalMetaData = value.Copy();
					else
						FOriginalMetaData = null;
					FMetaData = value;
					Changed();
				}
			}
		}
		 
		protected void Changed()
		{
			if(FMetaData != null)
			{
				FRadioButtonNone.Checked = false;
				FRadioButtonMetadata.Checked = true;
				FGroupMetaData.Enabled = true;
				FButtonPanel.Enabled = true;
				FListView.BackColor = SystemColors.Window;
				FListView.Items.Clear();
				int i = 0;
				#if USEHASHTABLEFORTAGS
				foreach (Tag LTag in FMetaData.Tags)
				{ 
				#else
				Tag LTag;
				for (int LIndex = 0; LIndex < FMetaData.Tags.Count; LIndex++)
				{
					LTag = FMetaData.Tags[LIndex];
				#endif
					FListView.Items.Add(new ListViewItem());
					FListView.Items[i].Text = LTag.Name;
					FListView.Items[i].SubItems.Add(LTag.Value);
					i = i + 1;
				}
			}
			else
			{
				FTextBoxComment.Clear();
				FListView.Items.Clear();
				FListView.Columns.Add("Name", 98, HorizontalAlignment.Left);
				FListView.Columns.Add("Value", 1500, HorizontalAlignment.Left);
				FRadioButtonNone.Checked = true;
				FRadioButtonMetadata.Checked = false;
				FGroupMetaData.Enabled = false;
				FListView.BackColor = SystemColors.Control;
			}
		}	 
	}	 
}
