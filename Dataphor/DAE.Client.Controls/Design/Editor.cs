/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;

using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	/// <summary> Editor for the ColumnName property of IncrementalSearchColumn's. </summary>
	public class GridColumnNameEditor : DAE.Client.Design.ColumnNameEditor
	{
		public override DataSet DataSet(ITypeDescriptorContext AContext)
		{
			if 
				(
				(AContext != null) && 
				(AContext.Instance != null) &&
				(AContext.Instance is GridColumn)
				)
			{
				GridColumn LColumn = (GridColumn)AContext.Instance;
				if ((LColumn.Grid != null) && (LColumn.Grid.Source != null))
					return LColumn.Grid.Source.DataSet;
			}
			return null;
		}	 
	}

	/// <summary> Provides an interface to edit GridColumns </summary>
	public class GridColumnCollectionEditor : CollectionEditor
	{
		public GridColumnCollectionEditor(Type type) : base(type) {}

		protected override object CreateInstance(Type AItemType)
		{
			if (AItemType == typeof(Alphora.Dataphor.DAE.Client.Controls.GridColumn))
				return base.CreateInstance(typeof(Alphora.Dataphor.DAE.Client.Controls.TextColumn));
			else
				return base.CreateInstance(AItemType);
		}

		protected override Type[] CreateNewItemTypes()
		{
			Type[] LItemTypes = 
				{
					typeof(Alphora.Dataphor.DAE.Client.Controls.TextColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.ActionColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.CheckBoxColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.ImageColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.LinkColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.SequenceColumn)
				};
			return LItemTypes;
		}

		private int NonDefaultGridColumnCount(Alphora.Dataphor.DisposableList AColumnsArray)
		{
			int LObjectCount = 0;
			for (int i = 0; i < AColumnsArray.Count; i++)
				if (AColumnsArray[i] is Alphora.Dataphor.DAE.Client.Controls.DataColumn)
				{
					if (!((Alphora.Dataphor.DAE.Client.Controls.DataColumn)AColumnsArray[i]).IsDefaultGridColumn)
						LObjectCount++;
				}
				else
					LObjectCount++;
			return LObjectCount;
		}

		protected override object[] GetItems(object AEditValue)
		{
			Alphora.Dataphor.DisposableList LArray = (Alphora.Dataphor.DisposableList)AEditValue;
			int LObjectCount = NonDefaultGridColumnCount(LArray);
			if (LObjectCount > 0)
			{
				object[] LNonDefaultColumns = new object[LObjectCount];
				int LInsertIndex = 0;
				for (int i = 0; i < LArray.Count; i++)
					if (LArray[i] is Alphora.Dataphor.DAE.Client.Controls.DataColumn)
					{
						if (!((Alphora.Dataphor.DAE.Client.Controls.DataColumn)LArray[i]).IsDefaultGridColumn)
							LNonDefaultColumns[LInsertIndex++] = LArray[i];
					}
					else
						LNonDefaultColumns[LInsertIndex++] = LArray[i];
				return LNonDefaultColumns;
			}
			else
				return new object[] {};
			
		}

		private CollectionForm FForm;
		protected override CollectionForm CreateCollectionForm()
		{
			CollectionForm LForm = base.CreateCollectionForm();
			FForm = LForm;
			return LForm;
		}
											   
		protected override object SetItems(object AEditValue, object[] AValue)
		{
			object LResult = base.SetItems(AEditValue, AValue);		
			if ((AEditValue is GridColumns) && (FForm != null))
				foreach (Control LControl in FForm.Controls)
					if (LControl is PropertyGrid)
						((PropertyGrid)LControl).Refresh();
			return LResult;
		}
	}

	public class AbstractCollectionBrowse : Form
	{
		protected System.Windows.Forms.ListView FListView;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;

		private System.ComponentModel.Container components = null;

		public AbstractCollectionBrowse()
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

		protected virtual void InitializeComponent()
		{
			this.FListView = new System.Windows.Forms.ListView();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// FListView
			// 
			this.FListView.AllowColumnReorder = true;
			this.FListView.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.FListView.FullRowSelect = true;
			this.FListView.HideSelection = false;
			this.FListView.Location = new System.Drawing.Point(8, 8);
			this.FListView.MultiSelect = false;
			this.FListView.Name = "FListView";
			this.FListView.Size = new System.Drawing.Size(272, 132);
			this.FListView.TabIndex = 0;
			this.FListView.View = System.Windows.Forms.View.Details;
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.btnAdd.Location = new System.Drawing.Point(288, 8);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.TabIndex = 1;
			this.btnAdd.Text = "&Add...";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.btnEdit.Location = new System.Drawing.Point(288, 32);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.TabIndex = 2;
			this.btnEdit.Text = "&Edit...";
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.btnDelete.Location = new System.Drawing.Point(288, 56);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.TabIndex = 3;
			this.btnDelete.Text = "&Delete";
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// AbstractCollectionBrowse
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(368, 150);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnDelete,
																		  this.btnEdit,
																		  this.btnAdd,
																		  this.FListView});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "AbstractCollectionBrowse";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = " Edit Statements";
			this.ResumeLayout(false);

		}

		protected virtual void InitColumns()
		{
			//Abstract
		}

		protected virtual void btnAdd_Click(object ASender, System.EventArgs AArgs)
		{
			//Abstract
		}

		protected virtual void btnEdit_Click(object ASender, System.EventArgs AArgs)
		{
			//Abstract
		}

		protected virtual void btnDelete_Click(object ASender, System.EventArgs AArgs)
		{
			//Abstract
		}
	}

	public class AdornColumnExpressionBrowse : AbstractCollectionBrowse
	{
		private AdornColumnExpressions FExpressions;

		public AdornColumnExpressionBrowse(AdornColumnExpressions ACollection)
		{
			this.Text = "Column Expressions";
			FExpressions = ACollection;
			ExpressionsChanged(FExpressions, null);
		}

		protected override void Dispose( bool ADisposing )
		{
			if (FExpressions != null)
			{
				FExpressions = null;
			}
			base.Dispose( ADisposing );
		}

		protected override void InitColumns()
		{
			FListView.Columns.Add("Column Name", 100, HorizontalAlignment.Left);
			FListView.Columns.Add("Default", 110, HorizontalAlignment.Left);
			FListView.Columns.Add("Constraint", 180, HorizontalAlignment.Left);
		}

		private void ExpressionsChanged(object ASender, object AItem)
		{
			if (FListView != null)
			{
				FListView.BeginUpdate();
				try
				{
					FListView.Items.Clear();
					int i = 0;
					foreach (AdornColumnExpression LExpression in FExpressions)
					{
						FListView.Items.Add(new ListViewItem());
						FListView.Items[i].Tag = LExpression;
						FListView.Items[i].Text = LExpression.ColumnName;
						FListView.Items[i].SubItems.Add(LExpression.Default != null ? LExpression.Default.ExpressionString : String.Empty);
						FListView.Items[i].SubItems.Add(LExpression.Constraints.Count > 0 ? LExpression.Constraints[0].ExpressionString : String.Empty); 
						++i;
					}
					if (FListView.Items.Count > 0)
						FListView.Items[0].Focused = true;
				}
				finally
				{
					FListView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnAdd_Click(ASender, AArgs);
			AdornColumnExpression LExpression = new AdornColumnExpression();
			AdornColumnExpressionEdit LEditForm = new AdornColumnExpressionEdit(LExpression);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
					FExpressions.Add(LExpression);
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (FListView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnEdit_Click(ASender, AArgs);
			CheckItemSelected();
			ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
			AdornColumnExpression LExpression = (AdornColumnExpression)LListViewItem.Tag;
			AdornColumnExpressionEdit LEditForm = new AdornColumnExpressionEdit(LExpression);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
					ExpressionsChanged(FExpressions, null);
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnDelete_Click(ASender, AArgs);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
				FExpressions.RemoveAt(FExpressions.IndexOf(LListViewItem.Tag));
				LListViewItem.Tag = null;
				ExpressionsChanged(FExpressions, null);
			}
		}
	}

	internal class AdornColumnExpressionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox tbColumnName;
		private System.Windows.Forms.Label lblColumnName;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.RadioButton rbDefault;
		private System.Windows.Forms.RadioButton rbConstraintNone;
		private System.Windows.Forms.GroupBox gbConstraints;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbDefaultExpression;
		private System.Windows.Forms.Label lblDefaultMetaData;
		private System.Windows.Forms.TextBox tbDefaultMetaData;
		private System.Windows.Forms.TextBox tbName;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox tbConstraintExpression;
		private System.Windows.Forms.TextBox tbConstraintMetaData;
		private System.Windows.Forms.Button btnConstraintExpression;
		private System.Windows.Forms.Button btnConstraintMetaData;
		private System.Windows.Forms.RadioButton rbDefaultNone;
		private System.Windows.Forms.RadioButton rbConstraint;
		private System.Windows.Forms.Button btnDefaultExpression;
		private System.Windows.Forms.Button btnDefaultMetaData;
		private System.ComponentModel.Container components = null;

		public AdornColumnExpressionEdit(AdornColumnExpression AExpression)
		{
			InitializeComponent();
			Value = AExpression;
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

		private void InitializeComponent()
		{
			this.tbColumnName = new System.Windows.Forms.TextBox();
			this.lblColumnName = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.rbDefault = new System.Windows.Forms.RadioButton();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.rbDefaultNone = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.rbConstraint = new System.Windows.Forms.RadioButton();
			this.rbConstraintNone = new System.Windows.Forms.RadioButton();
			this.gbConstraints = new System.Windows.Forms.GroupBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.tbDefaultExpression = new System.Windows.Forms.TextBox();
			this.btnDefaultExpression = new System.Windows.Forms.Button();
			this.lblDefaultMetaData = new System.Windows.Forms.Label();
			this.tbDefaultMetaData = new System.Windows.Forms.TextBox();
			this.btnDefaultMetaData = new System.Windows.Forms.Button();
			this.tbName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.tbConstraintExpression = new System.Windows.Forms.TextBox();
			this.tbConstraintMetaData = new System.Windows.Forms.TextBox();
			this.btnConstraintExpression = new System.Windows.Forms.Button();
			this.btnConstraintMetaData = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.gbConstraints.SuspendLayout();
			this.SuspendLayout();
			// 
			// tbColumnName
			// 
			this.tbColumnName.Location = new System.Drawing.Point(8, 24);
			this.tbColumnName.Name = "tbColumnName";
			this.tbColumnName.Size = new System.Drawing.Size(160, 20);
			this.tbColumnName.TabIndex = 0;
			this.tbColumnName.Text = "";
			// 
			// lblColumnName
			// 
			this.lblColumnName.Location = new System.Drawing.Point(8, 8);
			this.lblColumnName.Name = "lblColumnName";
			this.lblColumnName.Size = new System.Drawing.Size(100, 16);
			this.lblColumnName.TabIndex = 1;
			this.lblColumnName.Text = "Column Name";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.AddRange(new System.Windows.Forms.Control[] {
																					this.rbDefault,
																					this.groupBox3,
																					this.rbDefaultNone});
			this.groupBox1.Location = new System.Drawing.Point(8, 56);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(216, 224);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = " Default Definition";
			// 
			// rbDefault
			// 
			this.rbDefault.Location = new System.Drawing.Point(16, 40);
			this.rbDefault.Name = "rbDefault";
			this.rbDefault.Size = new System.Drawing.Size(64, 24);
			this.rbDefault.TabIndex = 3;
			this.rbDefault.Text = "Default";
			this.rbDefault.CheckedChanged += new System.EventHandler(this.rbDefault_CheckedChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.AddRange(new System.Windows.Forms.Control[] {
																					this.btnDefaultMetaData,
																					this.tbDefaultMetaData,
																					this.lblDefaultMetaData,
																					this.btnDefaultExpression,
																					this.tbDefaultExpression,
																					this.label1});
			this.groupBox3.Enabled = false;
			this.groupBox3.Location = new System.Drawing.Point(8, 48);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(200, 168);
			this.groupBox3.TabIndex = 2;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "groupBox3";
			// 
			// rbDefaultNone
			// 
			this.rbDefaultNone.Checked = true;
			this.rbDefaultNone.Location = new System.Drawing.Point(16, 16);
			this.rbDefaultNone.Name = "rbDefaultNone";
			this.rbDefaultNone.Size = new System.Drawing.Size(56, 24);
			this.rbDefaultNone.TabIndex = 1;
			this.rbDefaultNone.TabStop = true;
			this.rbDefaultNone.Text = "None";
			this.rbDefaultNone.CheckedChanged += new System.EventHandler(this.rbDefaultNone_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.AddRange(new System.Windows.Forms.Control[] {
																					this.rbConstraint,
																					this.rbConstraintNone,
																					this.gbConstraints});
			this.groupBox2.Location = new System.Drawing.Point(232, 56);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(216, 224);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = " Constraint ";
			// 
			// rbConstraint
			// 
			this.rbConstraint.Location = new System.Drawing.Point(16, 40);
			this.rbConstraint.Name = "rbConstraint";
			this.rbConstraint.Size = new System.Drawing.Size(88, 24);
			this.rbConstraint.TabIndex = 1;
			this.rbConstraint.Text = "Constraint";
			this.rbConstraint.CheckedChanged += new System.EventHandler(this.rbConstraint_CheckedChanged);
			// 
			// rbConstraintNone
			// 
			this.rbConstraintNone.Checked = true;
			this.rbConstraintNone.Location = new System.Drawing.Point(16, 16);
			this.rbConstraintNone.Name = "rbConstraintNone";
			this.rbConstraintNone.Size = new System.Drawing.Size(64, 24);
			this.rbConstraintNone.TabIndex = 0;
			this.rbConstraintNone.TabStop = true;
			this.rbConstraintNone.Text = "None";
			this.rbConstraintNone.CheckedChanged += new System.EventHandler(this.rbConstraintNone_CheckedChanged);
			// 
			// gbConstraints
			// 
			this.gbConstraints.Controls.AddRange(new System.Windows.Forms.Control[] {
																						this.btnConstraintMetaData,
																						this.tbConstraintMetaData,
																						this.tbConstraintExpression,
																						this.label4,
																						this.label3,
																						this.label2,
																						this.tbName,
																						this.btnConstraintExpression});
			this.gbConstraints.Enabled = false;
			this.gbConstraints.Location = new System.Drawing.Point(8, 48);
			this.gbConstraints.Name = "gbConstraints";
			this.gbConstraints.Size = new System.Drawing.Size(200, 168);
			this.gbConstraints.TabIndex = 2;
			this.gbConstraints.TabStop = false;
			this.gbConstraints.Text = " Constraint";
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(288, 288);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 4;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(368, 288);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(64, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Expression";
			// 
			// tbDefaultExpression
			// 
			this.tbDefaultExpression.Location = new System.Drawing.Point(8, 40);
			this.tbDefaultExpression.Name = "tbDefaultExpression";
			this.tbDefaultExpression.Size = new System.Drawing.Size(144, 20);
			this.tbDefaultExpression.TabIndex = 1;
			this.tbDefaultExpression.Text = "";
			// 
			// btnDefaultExpression
			// 
			this.btnDefaultExpression.Location = new System.Drawing.Point(160, 40);
			this.btnDefaultExpression.Name = "btnDefaultExpression";
			this.btnDefaultExpression.Size = new System.Drawing.Size(24, 20);
			this.btnDefaultExpression.TabIndex = 2;
			this.btnDefaultExpression.Text = "...";
			this.btnDefaultExpression.Click += new System.EventHandler(this.btnDefaultExpression_Click);
			// 
			// lblDefaultMetaData
			// 
			this.lblDefaultMetaData.Location = new System.Drawing.Point(8, 72);
			this.lblDefaultMetaData.Name = "lblDefaultMetaData";
			this.lblDefaultMetaData.Size = new System.Drawing.Size(100, 16);
			this.lblDefaultMetaData.TabIndex = 3;
			this.lblDefaultMetaData.Text = "MetaData";
			// 
			// tbDefaultMetaData
			// 
			this.tbDefaultMetaData.Location = new System.Drawing.Point(8, 88);
			this.tbDefaultMetaData.Name = "tbDefaultMetaData";
			this.tbDefaultMetaData.ReadOnly = true;
			this.tbDefaultMetaData.Size = new System.Drawing.Size(144, 20);
			this.tbDefaultMetaData.TabIndex = 4;
			this.tbDefaultMetaData.Text = "";
			// 
			// btnDefaultMetaData
			// 
			this.btnDefaultMetaData.Location = new System.Drawing.Point(160, 88);
			this.btnDefaultMetaData.Name = "btnDefaultMetaData";
			this.btnDefaultMetaData.Size = new System.Drawing.Size(24, 20);
			this.btnDefaultMetaData.TabIndex = 5;
			this.btnDefaultMetaData.Text = "...";
			this.btnDefaultMetaData.Click += new System.EventHandler(this.btnDefaultMetaData_Click);
			// 
			// tbName
			// 
			this.tbName.Location = new System.Drawing.Point(8, 40);
			this.tbName.Name = "tbName";
			this.tbName.Size = new System.Drawing.Size(152, 20);
			this.tbName.TabIndex = 0;
			this.tbName.Text = "";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(100, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "Name";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(8, 72);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(100, 16);
			this.label3.TabIndex = 2;
			this.label3.Text = "Expression";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 120);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(100, 16);
			this.label4.TabIndex = 3;
			this.label4.Text = "MetaData";
			// 
			// tbConstraintExpression
			// 
			this.tbConstraintExpression.Location = new System.Drawing.Point(8, 88);
			this.tbConstraintExpression.Name = "tbConstraintExpression";
			this.tbConstraintExpression.Size = new System.Drawing.Size(152, 20);
			this.tbConstraintExpression.TabIndex = 4;
			this.tbConstraintExpression.Text = "";
			// 
			// tbConstraintMetaData
			// 
			this.tbConstraintMetaData.Location = new System.Drawing.Point(8, 136);
			this.tbConstraintMetaData.Name = "tbConstraintMetaData";
			this.tbConstraintMetaData.ReadOnly = true;
			this.tbConstraintMetaData.Size = new System.Drawing.Size(152, 20);
			this.tbConstraintMetaData.TabIndex = 5;
			this.tbConstraintMetaData.Text = "";
			// 
			// btnConstraintExpression
			// 
			this.btnConstraintExpression.Location = new System.Drawing.Point(168, 88);
			this.btnConstraintExpression.Name = "btnConstraintExpression";
			this.btnConstraintExpression.Size = new System.Drawing.Size(24, 20);
			this.btnConstraintExpression.TabIndex = 6;
			this.btnConstraintExpression.Text = "...";
			this.btnConstraintExpression.Click += new System.EventHandler(this.btnConstraintExpression_Click);
			// 
			// btnConstraintMetaData
			// 
			this.btnConstraintMetaData.Location = new System.Drawing.Point(168, 136);
			this.btnConstraintMetaData.Name = "btnConstraintMetaData";
			this.btnConstraintMetaData.Size = new System.Drawing.Size(24, 20);
			this.btnConstraintMetaData.TabIndex = 7;
			this.btnConstraintMetaData.Text = "...";
			this.btnConstraintMetaData.Click += new System.EventHandler(this.btnConstraintMetaData_Click);
			// 
			// Form
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(456, 318);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnCancel,
																		  this.btnOK,
																		  this.groupBox2,
																		  this.groupBox1,
																		  this.lblColumnName,
																		  this.tbColumnName});
			this.Name = "Form4";
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Text = "AdornColumnExpression Edit";
			this.groupBox1.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.gbConstraints.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		private AdornColumnExpression FValue;
		public AdornColumnExpression Value
		{
			get { return FValue; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.AdornColumnExpressionNotNull);
				FValue = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			tbColumnName.Text = FValue.ColumnName;
			rbDefault.Checked = FValue.Default != null;
			rbConstraint.Checked = FValue.Constraints.Count > 0;
		}

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs e)

		{
			Close();
		}

		private void EditMetaData(ref MetaData AValue)
		{
			MetaDataEditForm LMetaDataEditForm = new MetaDataEditForm(AValue); 
			try
			{
				LMetaDataEditForm.ShowDialog((IWin32Window)this);
				if (LMetaDataEditForm.DialogResult == DialogResult.OK)
					AValue = LMetaDataEditForm.MetaData;
			}
			finally
			{
				LMetaDataEditForm.Dispose();
			}
		}

		private void btnDefaultMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref FDefaultMetaData);
			if (FDefaultMetaData == null)
				tbDefaultMetaData.Text = "(none)";
			else
				tbDefaultMetaData.Text = FDefaultMetaData.ToString();

		}

		private void btnConstraintMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref FConstraintMetaData);
			if (FConstraintMetaData == null)
				tbConstraintMetaData.Text = "(none)";
			else
				tbConstraintMetaData.Text = FConstraintMetaData.ToString();
		}

		protected virtual void ExpressionClosing(object ASender, CancelEventArgs AArgs)
		{
			if (((MultiLineEditForm)ASender).Value != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				LParser.ParseExpression(((MultiLineEditForm)ASender).Value);
			}
		}

		private void EditExpression(System.Windows.Forms.TextBox ATextBoxExpression)
		{
			MultiLineEditForm LForm = new MultiLineEditForm(ATextBoxExpression.Text);
			try
			{
				LForm.Closing += new CancelEventHandler(ExpressionClosing);
				LForm.Text = "Expression";
				LForm.ShowDialog((IWin32Window)this);
				if (LForm.DialogResult == DialogResult.OK)
					ATextBoxExpression.Text = LForm.Value;
			}
			finally
			{
				LForm.Closing -= new  CancelEventHandler(ExpressionClosing);
				LForm.Dispose();
			}
		}

		private void btnDefaultExpression_Click(object sender, System.EventArgs e)
		{
			EditExpression(tbDefaultExpression);
		}

		private void btnConstraintExpression_Click(object sender, System.EventArgs e)
		{
			EditExpression(tbConstraintExpression);
		}

		private MetaData FDefaultMetaData;
		public MetaData DefaultMetaData
		{
			get { return FDefaultMetaData; }
		}

		private void rbDefaultNone_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbDefaultNone.Checked)
			{
				groupBox3.Enabled = false;
				FDefaultMetaData = null;
			}
		}

		private void rbDefault_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbDefault.Checked)
			{
				groupBox3.Enabled = true;
				if (FValue.Default != null)
				{
					tbDefaultExpression.Text = FValue.Default.ExpressionString;
					if (FValue.Default.MetaData != null)
					{
						FDefaultMetaData = FValue.Default.MetaData.Copy();
						tbDefaultMetaData.Text = FDefaultMetaData.ToString();
					}
					else
					{
						FDefaultMetaData = null;
						tbDefaultMetaData.Text = "(none)";
					}
				}
			}
		}

		private MetaData FConstraintMetaData;
		public MetaData ConstraintMetaData
		{
			get { return FConstraintMetaData; }
		}

		private void rbConstraintNone_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbConstraintNone.Checked)
			{
				gbConstraints.Enabled = false;
				FConstraintMetaData = null;
			}
		}

		private void rbConstraint_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbConstraint.Checked)
			{
				gbConstraints.Enabled = true;
				if (FValue.Constraints.Count > 0)
				{
					ConstraintDefinition LConstraint = FValue.Constraints[0];
					tbName.Text = LConstraint.ConstraintName;
					tbConstraintExpression.Text = LConstraint.ExpressionString;
					if (LConstraint.MetaData != null)
					{
						FConstraintMetaData = LConstraint.MetaData.Copy();
						tbConstraintMetaData.Text = FConstraintMetaData.ToString();
					}
					else
					{
						tbConstraintMetaData.Text = "(none)";
						FConstraintMetaData = null;
					}
				}
			}
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			if (DialogResult == DialogResult.OK)
			{
				try
				{
					if (tbColumnName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ColumnNameNeeded);
					if (rbDefault.Checked)
					{
						if (tbDefaultExpression.Text == String.Empty)
							throw new DesignException(DesignException.Codes.ExpressionRequired);
					}
					if (rbConstraint.Checked)
					{
						if (tbName.Text == String.Empty)
							throw new DesignException(DesignException.Codes.ConstraintNameRequired);
						if (tbConstraintExpression.Text == String.Empty)
							throw new DesignException(DesignException.Codes.ConstraintExpressionRequired);
					}
				
					//Assign values...
					FValue.ColumnName = tbColumnName.Text;
					if (rbDefault.Checked)
					{
						DefaultDefinition LDefault = new DefaultDefinition();
						LDefault.ExpressionString = tbDefaultExpression.Text;
						LDefault.MetaData = DefaultMetaData;
						FValue.Default = LDefault;
					}
					else
						FValue.Default = null;

					if (rbConstraint.Checked)
					{
						ConstraintDefinition LConstraint = new ConstraintDefinition();
						LConstraint.ConstraintName = tbName.Text;
						LConstraint.ExpressionString = tbConstraintExpression.Text;
						LConstraint.MetaData = ConstraintMetaData;
						if (FValue.Constraints.Count > 0)
							FValue.Constraints.Remove(FValue.Constraints[0]);
						FValue.Constraints.Add(LConstraint);
					}
					else
						FValue.Constraints.Clear();
				}
				catch
				{
					AArgs.Cancel = true;
					throw;
				}
			}
		}
	}

	public class AdornColumnExpressionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		private ITypeDescriptorContext FContext;

		private void ExpressionChanged(object ASender, object AItem)
		{
			if (FContext != null)
				FContext.OnComponentChanged();
		}
	
		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FContext = AContext;
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					AdornColumnExpressionBrowse LForm = new AdornColumnExpressionBrowse((AdornColumnExpressions)AValue);
					try
					{
						FEditorService.ShowDialog(LForm);
					}
					finally
					{
						FContext = null;
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class ConstraintsBrowse : AbstractCollectionBrowse
	{
		private ConstraintDefinitions FConstraints;

		public ConstraintsBrowse(ConstraintDefinitions ACollection)
		{
			this.Text = "Constraints";
			FConstraints = ACollection;
		}

		protected override void Dispose( bool ADisposing )
		{
			if (FConstraints != null)
			{
				FConstraints = null;
			}
			base.Dispose( ADisposing );
		}

		protected override void InitColumns()
		{
			FListView.Columns.Add("Constraint Name", 100, HorizontalAlignment.Left);
			FListView.Columns.Add("Expression", 120, HorizontalAlignment.Left);
		}

		private void ConstraintsChanged(object ASender, object AItem)
		{
			if (FListView != null)
			{
				FListView.BeginUpdate();
				try
				{
					FListView.Items.Clear();
					int i = 0;
					foreach (ConstraintDefinition LConstraint in FConstraints)
					{
						FListView.Items.Add(new ListViewItem());
						FListView.Items[i].Tag = LConstraint;
						FListView.Items[i].Text = LConstraint.ConstraintName;
						FListView.Items[i].SubItems.Add(LConstraint.ExpressionString);
						++i;
					}
					if (FListView.Items.Count > 0)
						FListView.Items[0].Focused = true;
				}
				finally
				{
					FListView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnAdd_Click(ASender, AArgs);
			ConstraintDefinition LConstraint = new ConstraintDefinition();
			ConstraintEdit LEditForm = new ConstraintEdit(LConstraint);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
					FConstraints.Add(LConstraint);
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (FListView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnEdit_Click(ASender, AArgs);
			CheckItemSelected();
			ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
			ConstraintDefinition LConstraint = (ConstraintDefinition)LListViewItem.Tag;
			ConstraintEdit LEditForm = new ConstraintEdit(LConstraint);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnDelete_Click(ASender, AArgs);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
				FConstraints.RemoveAt(FConstraints.IndexOf(LListViewItem.Tag));
				LListViewItem.Tag = null;
				ConstraintsChanged(FConstraints, null);
			}
		}
	}	

	public class ConstraintEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnConstraintMetaData;
		private System.Windows.Forms.TextBox tbConstraintMetaData;
		private System.Windows.Forms.TextBox tbConstraintExpression;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbName;
		private System.Windows.Forms.Button btnConstraintExpression;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.ComponentModel.Container components = null;

		public ConstraintEdit(ConstraintDefinition AValue)
		{
			InitializeComponent();
			Value = AValue;
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

		private void InitializeComponent()
		{
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnConstraintMetaData = new System.Windows.Forms.Button();
			this.tbConstraintMetaData = new System.Windows.Forms.TextBox();
			this.tbConstraintExpression = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.tbName = new System.Windows.Forms.TextBox();
			this.btnConstraintExpression = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(136, 160);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 6;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(56, 160);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 5;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnConstraintMetaData
			// 
			this.btnConstraintMetaData.Location = new System.Drawing.Point(168, 120);
			this.btnConstraintMetaData.Name = "btnConstraintMetaData";
			this.btnConstraintMetaData.Size = new System.Drawing.Size(24, 20);
			this.btnConstraintMetaData.TabIndex = 4;
			this.btnConstraintMetaData.Text = "...";
			this.btnConstraintMetaData.Click += new System.EventHandler(this.btnConstraintMetaData_Click);
			// 
			// tbConstraintMetaData
			// 
			this.tbConstraintMetaData.Location = new System.Drawing.Point(8, 120);
			this.tbConstraintMetaData.Name = "tbConstraintMetaData";
			this.tbConstraintMetaData.ReadOnly = true;
			this.tbConstraintMetaData.Size = new System.Drawing.Size(152, 20);
			this.tbConstraintMetaData.TabIndex = 3;
			this.tbConstraintMetaData.Text = "";
			// 
			// tbConstraintExpression
			// 
			this.tbConstraintExpression.Location = new System.Drawing.Point(8, 72);
			this.tbConstraintExpression.Name = "tbConstraintExpression";
			this.tbConstraintExpression.Size = new System.Drawing.Size(152, 20);
			this.tbConstraintExpression.TabIndex = 1;
			this.tbConstraintExpression.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 104);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(100, 16);
			this.label4.TabIndex = 14;
			this.label4.Text = "MetaData";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(8, 56);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(100, 16);
			this.label3.TabIndex = 13;
			this.label3.Text = "Expression";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(100, 16);
			this.label2.TabIndex = 12;
			this.label2.Text = "Name";
			// 
			// tbName
			// 
			this.tbName.Location = new System.Drawing.Point(8, 24);
			this.tbName.Name = "tbName";
			this.tbName.Size = new System.Drawing.Size(152, 20);
			this.tbName.TabIndex = 0;
			this.tbName.Text = "";
			// 
			// btnConstraintExpression
			// 
			this.btnConstraintExpression.Location = new System.Drawing.Point(168, 72);
			this.btnConstraintExpression.Name = "btnConstraintExpression";
			this.btnConstraintExpression.Size = new System.Drawing.Size(24, 20);
			this.btnConstraintExpression.TabIndex = 2;
			this.btnConstraintExpression.Text = "...";
			this.btnConstraintExpression.Click += new System.EventHandler(this.btnConstraintExpression_Click);
			// 
			// Form5
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(216, 190);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnConstraintMetaData,
																		  this.tbConstraintMetaData,
																		  this.tbConstraintExpression,
																		  this.label4,
																		  this.label3,
																		  this.label2,
																		  this.tbName,
																		  this.btnConstraintExpression,
																		  this.btnCancel,
																		  this.btnOK});
			this.Name = "Form5";
			this.Text = "Constraint";
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.ResumeLayout(false);

		}

		private ConstraintDefinition FValue;
		public ConstraintDefinition Value
		{
			get { return FValue; }
			set
			{
				FValue = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			if (FValue != null)
			{
				tbName.Text = FValue.ConstraintName;
				tbConstraintExpression.Text = FValue.ExpressionString;
				if (FValue.MetaData != null)
				{
					FMetaData = FValue.MetaData.Copy();
					tbConstraintMetaData.Text = FMetaData.ToString();
				}
				else
				{
					tbConstraintMetaData.Text = "(none)";
					FMetaData = null;
				}
			}
		}

		private void btnOK_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		private void btnCancel_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		protected virtual void ExpressionClosing(object ASender, CancelEventArgs AArgs)
		{
			if (((MultiLineEditForm)ASender).Value != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				LParser.ParseExpression(((MultiLineEditForm)ASender).Value);
			}
		}

		private void EditExpression(System.Windows.Forms.TextBox ATextBoxExpression)
		{
			MultiLineEditForm LForm = new MultiLineEditForm(ATextBoxExpression.Text);
			try
			{
				LForm.Closing += new CancelEventHandler(ExpressionClosing);
				LForm.Text = "Expression";
				LForm.ShowDialog((IWin32Window)this);
				if (LForm.DialogResult == DialogResult.OK)
					ATextBoxExpression.Text = LForm.Value;
			}
			finally
			{
				LForm.Closing -= new  CancelEventHandler(ExpressionClosing);
				LForm.Dispose();
			}
		}

		private void btnConstraintExpression_Click(object ASender, System.EventArgs AArgs)
		{
			EditExpression(tbConstraintExpression);
		}

		private MetaData FMetaData;
		public MetaData MetaData
		{
			get { return FMetaData; }
		}

		private void EditMetaData(ref MetaData AValue)
		{
			MetaDataEditForm LMetaDataEditForm = new MetaDataEditForm(AValue); 
			try
			{
				LMetaDataEditForm.ShowDialog((IWin32Window)this);
				if (LMetaDataEditForm.DialogResult == DialogResult.OK)
					AValue = LMetaDataEditForm.MetaData;
			}
			finally
			{
				LMetaDataEditForm.Dispose();
			}
		}

		private void btnConstraintMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref FMetaData);
			if (FMetaData == null)
				tbConstraintMetaData.Text = "(none)";
			else
				tbConstraintMetaData.Text = FMetaData.ToString();
		}

		
		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			if (DialogResult == DialogResult.OK)
			{
				try
				{
					if (tbName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ConstraintNameRequired);
					if (tbConstraintExpression.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ConstraintExpressionRequired);
				
					//Assign values...
					FValue.ConstraintName = tbName.Text;
					FValue.ExpressionString = tbConstraintExpression.Text;
					FValue.MetaData = MetaData;
				}
				catch
				{
					AArgs.Cancel = true;
					throw;
				}
			}
		}

	}

	public class ConstraintsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		private ITypeDescriptorContext FContext;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FContext = AContext;
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					ConstraintsBrowse LForm = new ConstraintsBrowse((ConstraintDefinitions)AValue);
					try
					{
						FEditorService.ShowDialog(LForm);
					}
					finally
					{
						FContext = null;
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class OrderColumnDefinitionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.TextBox tbColumnName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton rbAscending;
		private System.Windows.Forms.RadioButton rbDescending;
		private System.ComponentModel.Container components = null;

		public OrderColumnDefinitionEdit(OrderColumnDefinition AValue)
		{
			InitializeComponent();
			Value = AValue;
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

		private void InitializeComponent()
		{
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.tbColumnName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.rbAscending = new System.Windows.Forms.RadioButton();
			this.rbDescending = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(120, 104);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(40, 104);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 3;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// tbColumnName
			// 
			this.tbColumnName.Location = new System.Drawing.Point(8, 24);
			this.tbColumnName.Name = "tbColumnName";
			this.tbColumnName.Size = new System.Drawing.Size(152, 20);
			this.tbColumnName.TabIndex = 0;
			this.tbColumnName.Text = "";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 10;
			this.label1.Text = "Column Name";
			// 
			// rbAscending
			// 
			this.rbAscending.Location = new System.Drawing.Point(8, 48);
			this.rbAscending.Name = "rbAscending";
			this.rbAscending.TabIndex = 1;
			this.rbAscending.Text = "&Ascending";
			this.rbAscending.Checked = true;
			// 
			// rbDescending
			// 
			this.rbDescending.Location = new System.Drawing.Point(8, 72);
			this.rbDescending.Name = "rbDescending";
			this.rbDescending.TabIndex = 2;
			this.rbDescending.Text = "&Descending";
			// 
			// Form
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(200, 134);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.rbDescending,
																		  this.rbAscending,
																		  this.label1,
																		  this.tbColumnName,
																		  this.btnCancel,
																		  this.btnOK});
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form6";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Text = "Column Edit";
			this.ResumeLayout(false);

		}

		private OrderColumnDefinition FValue;
		public OrderColumnDefinition Value
		{
			get { return FValue; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.InvalidOrderColumnDefinition);
				FValue = value;
				DefinitionChanged();
			}
		}

		private void DefinitionChanged()
		{
			tbColumnName.Text = FValue.ColumnName;
			rbAscending.Checked = FValue.Ascending;
			rbDescending.Checked = !FValue.Ascending;
		}

		private void btnOK_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		private void btnCancel_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					if (tbColumnName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ColumnNameNeeded);
					FValue.ColumnName = tbColumnName.Text;
					FValue.Ascending = rbAscending.Checked;
				}
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}
	}

	public class OrderDefinitionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;
		private System.Windows.Forms.ColumnHeader chColumnName;
		private System.Windows.Forms.ColumnHeader chColumnDir;
		private System.Windows.Forms.Button btnUp;
		private System.Windows.Forms.Button btnDown;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.ComponentModel.Container components = null;

		public OrderDefinitionEdit(OrderDefinition AValue)
		{
			InitializeComponent();
			Value = AValue;
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

		private void InitializeComponent()
		{
			this.listView1 = new System.Windows.Forms.ListView();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.chColumnName = new System.Windows.Forms.ColumnHeader();
			this.chColumnDir = new System.Windows.Forms.ColumnHeader();
			this.btnUp = new System.Windows.Forms.Button();
			this.btnDown = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listView1
			// 
			this.listView1.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																						this.chColumnName,
																						this.chColumnDir});
			this.listView1.FullRowSelect = true;
			this.listView1.Location = new System.Drawing.Point(32, 8);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(248, 132);
			this.listView1.TabIndex = 0;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.HideSelection = false;
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnAdd.Location = new System.Drawing.Point(288, 8);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.TabIndex = 1;
			this.btnAdd.Text = "&Add...";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnEdit.Location = new System.Drawing.Point(288, 32);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.TabIndex = 2;
			this.btnEdit.Text = "&Edit...";
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnDelete.Location = new System.Drawing.Point(288, 56);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.TabIndex = 3;
			this.btnDelete.Text = "&Delete";
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// chColumnName
			// 
			this.chColumnName.Text = "Column Name";
			this.chColumnName.Width = 180;
			// 
			// chColumnDir
			// 
			this.chColumnDir.Text = "Direction";
			// 
			// btnUp
			// 
			this.btnUp.Location = new System.Drawing.Point(8, 8);
			this.btnUp.Name = "btnUp";
			this.btnUp.Size = new System.Drawing.Size(24, 23);
			this.btnUp.TabIndex = 4;
			this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
			this.btnUp.Image = LoadBitmap("Alphora.Dataphor.DAE.Design.UpArrow.bmp");
			// 
			// btnDown
			// 
			this.btnDown.Location = new System.Drawing.Point(8, 32);
			this.btnDown.Name = "btnDown";
			this.btnDown.Size = new System.Drawing.Size(24, 23);
			this.btnDown.TabIndex = 5;
			this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
			this.btnDown.Image = LoadBitmap("Alphora.Dataphor.DAE.Design.DownArrow.bmp");
			// 
			// btnOK
			// 
			this.btnOK.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(208, 152);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 6;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(288, 152);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// Form3
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(368, 182);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnCancel,
																		  this.btnOK,
																		  this.btnDown,
																		  this.btnUp,
																		  this.btnDelete,
																		  this.btnEdit,
																		  this.btnAdd,
																		  this.listView1});
			this.Name = "Form3";
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Text = "Order Columns";
			this.ResumeLayout(false);

		}

		protected System.Drawing.Bitmap LoadBitmap(string AResourceName)
		{
			System.Drawing.Bitmap LResult = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream(AResourceName));
			LResult.MakeTransparent();
			return LResult;
		}

		private OrderDefinition FValue;
		protected internal OrderDefinition Value
		{
			get { return FValue; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.NullOrderDefinition);
				FValue = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			ListViewItem LItem;
			listView1.Items.Clear();
			foreach(OrderColumnDefinition LColumn in FValue.Columns)
			{
				LItem = listView1.Items.Add(new ListViewItem());
				LItem.Tag = LColumn;
				LItem.Text = LColumn.ColumnName;
				if (LColumn.Ascending)
					LItem.SubItems.Add("asc");
				else
					LItem.SubItems.Add("desc");
			}
		}

		private void CheckItemSelected()
		{
			if (listView1.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			OrderColumnDefinition LDefinition = new OrderColumnDefinition();
			OrderColumnDefinitionEdit LEditForm = new OrderColumnDefinitionEdit(LDefinition);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					ListViewItem LItem = new ListViewItem();
					LItem.Text = LDefinition.ColumnName;
					if (LDefinition.Ascending)
						LItem.SubItems.Add("asc");
					else
						LItem.SubItems.Add("desc");
					listView1.Items.Add(LItem);
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void btnEdit_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			ListViewItem LItem = listView1.SelectedItems[0];
			OrderColumnDefinition LDefinition = new OrderColumnDefinition(LItem.Text, LItem.SubItems[1].Text != "desc");
			OrderColumnDefinitionEdit LEditForm = new OrderColumnDefinitionEdit(LDefinition);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					LItem.Text = LDefinition.ColumnName;
					if (LDefinition.Ascending)
						LItem.SubItems[1].Text = "asc";
					else
						LItem.SubItems[1].Text = "desc";
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void btnDelete_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				listView1.Items.RemoveAt(listView1.SelectedIndices[0]);
		}

		private void btnUp_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int LSelectedIndex = listView1.SelectedIndices[0];
			ListViewItem LItem = listView1.SelectedItems[0];
			if (LSelectedIndex > 0)
			{
				listView1.Items.Remove(LItem);
				listView1.Items.Insert(LSelectedIndex - 1, LItem);
			}
		}

		private void btnDown_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int LSelectedIndex = listView1.SelectedIndices[0];
			ListViewItem LItem = listView1.SelectedItems[0];
			if (LSelectedIndex < (listView1.Items.Count - 1))
			{
				listView1.Items.Remove(LItem);
				listView1.Items.Insert(LSelectedIndex + 1, LItem);
			}
		}

		private void btnOK_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		private void btnCancel_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					FValue.Columns.Clear();
					foreach(ListViewItem LItem in listView1.Items)
						FValue.Columns.Add(new OrderColumnDefinition(LItem.Text, LItem.SubItems[1].Text != "desc"));
				}
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}
	}

	public class OrderDefinitionsBrowse : AbstractCollectionBrowse
	{
		private OrderDefinitions FOrders;

		public OrderDefinitionsBrowse(OrderDefinitions ACollection)
		{
			this.Text = "Order Definitions";
			FOrders = ACollection;
		}

		protected override void Dispose( bool ADisposing )
		{
			if (FOrders != null)
			{
				FOrders = null;
			}
			base.Dispose( ADisposing );
		}

		protected override void InitColumns()
		{
			FListView.Columns.Add("Order", 250, HorizontalAlignment.Left);
		}

		private void OrdersChanged(object ASender, object AItem)
		{
			if (FListView != null)
			{
				FListView.BeginUpdate();
				try
				{
					FListView.Items.Clear();
					int i = 0;
					string LCaption;
					foreach (OrderDefinition LOrder in FOrders)
					{
						FListView.Items.Add(new ListViewItem());
						LCaption = String.Empty;
						foreach(OrderColumnDefinition LColumn in LOrder.Columns)
						{
							if (LCaption != String.Empty)
								LCaption += ", ";
							if (LColumn.Ascending)
								LCaption += LColumn.ColumnName;
							else
								LCaption += LColumn.ColumnName + " desc"; 
						}
						FListView.Items[i].Tag = LOrder;
						FListView.Items[i].Text = LCaption;
						++i;
					}
					if (FListView.Items.Count > 0)
						FListView.Items[0].Focused = true;
				}
				finally
				{
					FListView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnAdd_Click(ASender, AArgs);
			OrderDefinition LOrder = new OrderDefinition();
			OrderDefinitionEdit LEditForm = new OrderDefinitionEdit(LOrder);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					if (LOrder.Columns.Count > 0)
						FOrders.Add(LOrder);
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (FListView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		public event EventHandler OnOrdersEdited;

		protected override void btnEdit_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnEdit_Click(ASender, AArgs);
			CheckItemSelected();
			ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
			OrderDefinition LOrder = (OrderDefinition)LListViewItem.Tag;
			OrderDefinitionEdit LEditForm = new OrderDefinitionEdit(LOrder);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					if (LOrder.Columns.Count == 0)
						FOrders.Remove(LOrder);
					OrdersChanged(FOrders, LOrder);
					if (OnOrdersEdited != null)
						OnOrdersEdited(this, EventArgs.Empty);
					
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnDelete_Click(ASender, AArgs);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
				FOrders.RemoveAt(FOrders.IndexOf(LListViewItem.Tag));
				LListViewItem.Tag = null;
				OrdersChanged(FOrders, null);
			}
		}
	}

	public class OrderDefinitionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		private ITypeDescriptorContext FContext;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FContext = AContext;
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					OrderDefinitionsBrowse LForm = new OrderDefinitionsBrowse((OrderDefinitions)AValue);
					try
					{
						FEditorService.ShowDialog(LForm);
					}
					finally
					{
						FContext = null;
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class OrderEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				OrderDefinition LOrder = new OrderDefinition();
				if (AValue != null)
				{
					if (((OrderDefinition)AValue).MetaData != null)
						LOrder.MetaData = ((OrderDefinition)AValue).MetaData.Copy();
					foreach (OrderColumnDefinition LColumn in ((OrderDefinition)AValue).Columns)
						LOrder.Columns.Add(new OrderColumnDefinition(LColumn.ColumnName, LColumn.Ascending));
				}
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					OrderDefinitionEdit LForm = new OrderDefinitionEdit(LOrder);
					try
					{
						FEditorService.ShowDialog(LForm);
						if (LForm.DialogResult == DialogResult.OK)
						{
							if (LOrder.Columns.Count == 0)
								AValue = null;
							else
								AValue = LOrder;
						}
					}
					finally
					{
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class KeyColumnDefinitionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.TextBox tbColumnName;
		private System.Windows.Forms.Label label1;
		private System.ComponentModel.Container components = null;

		public KeyColumnDefinitionEdit(KeyColumnDefinition AValue)
		{
			InitializeComponent();
			Value = AValue;
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

		private void InitializeComponent()
		{
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.tbColumnName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(120, 52);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(40, 52);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 3;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// tbColumnName
			// 
			this.tbColumnName.Location = new System.Drawing.Point(8, 24);
			this.tbColumnName.Name = "tbColumnName";
			this.tbColumnName.Size = new System.Drawing.Size(152, 20);
			this.tbColumnName.TabIndex = 0;
			this.tbColumnName.Text = "";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 10;
			this.label1.Text = "Column Name";
			// 
			// Form
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(200, 82);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.label1,
																		  this.tbColumnName,
																		  this.btnCancel,
																		  this.btnOK});
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmColumnNameEdit";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Text = "Column Name Edit";
			this.ResumeLayout(false);

		}

		private KeyColumnDefinition FValue;
		public KeyColumnDefinition Value
		{
			get { return FValue; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.InvalidKeyColumnDefinition);
				FValue = value;
				DefinitionChanged();
			}
		}

		private void DefinitionChanged()
		{
			tbColumnName.Text = FValue.ColumnName;
		}

		private void btnOK_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		private void btnCancel_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					if (tbColumnName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ColumnNameNeeded);
					FValue.ColumnName = tbColumnName.Text;
				}
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}
	}

	public class KeyDefinitionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;
		private System.Windows.Forms.ColumnHeader chColumnName;
		private System.Windows.Forms.Button btnUp;
		private System.Windows.Forms.Button btnDown;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.ComponentModel.Container components = null;

		public KeyDefinitionEdit(KeyDefinition AValue)
		{
			InitializeComponent();
			Value = AValue;
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

		private void InitializeComponent()
		{
			this.listView1 = new System.Windows.Forms.ListView();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.chColumnName = new System.Windows.Forms.ColumnHeader();
			this.btnUp = new System.Windows.Forms.Button();
			this.btnDown = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listView1
			// 
			this.listView1.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {this.chColumnName});
			this.listView1.FullRowSelect = true;
			this.listView1.Location = new System.Drawing.Point(32, 8);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(248, 132);
			this.listView1.TabIndex = 0;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.HideSelection = false;
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnAdd.Location = new System.Drawing.Point(288, 8);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.TabIndex = 1;
			this.btnAdd.Text = "&Add...";
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnEdit.Location = new System.Drawing.Point(288, 32);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.TabIndex = 2;
			this.btnEdit.Text = "&Edit...";
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnDelete.Location = new System.Drawing.Point(288, 56);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.TabIndex = 3;
			this.btnDelete.Text = "&Delete";
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// chColumnName
			// 
			this.chColumnName.Text = "Column Name";
			this.chColumnName.Width = 180;
			// 
			// btnUp
			// 
			this.btnUp.Location = new System.Drawing.Point(8, 8);
			this.btnUp.Name = "btnUp";
			this.btnUp.Size = new System.Drawing.Size(24, 23);
			this.btnUp.TabIndex = 4;
			this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
			this.btnUp.Image = LoadBitmap("Alphora.Dataphor.DAE.Design.UpArrow.bmp");
			// 
			// btnDown
			// 
			this.btnDown.Location = new System.Drawing.Point(8, 32);
			this.btnDown.Name = "btnDown";
			this.btnDown.Size = new System.Drawing.Size(24, 23);
			this.btnDown.TabIndex = 5;
			this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
			this.btnDown.Image = LoadBitmap("Alphora.Dataphor.DAE.Design.DownArrow.bmp");
			// 
			// btnOK
			// 
			this.btnOK.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(208, 152);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 6;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(288, 152);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// Form3
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(368, 182);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnCancel,
																		  this.btnOK,
																		  this.btnDown,
																		  this.btnUp,
																		  this.btnDelete,
																		  this.btnEdit,
																		  this.btnAdd,
																		  this.listView1});
			this.Name = "Form3";
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Text = "Key Columns";
			this.ResumeLayout(false);

		}

		protected System.Drawing.Bitmap LoadBitmap(string AResourceName)
		{
			System.Drawing.Bitmap LResult = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream(AResourceName));
			LResult.MakeTransparent();
			return LResult;
		}

		private KeyDefinition FValue;
		protected internal KeyDefinition Value
		{
			get { return FValue; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.NullKeyDefinition);
				FValue = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			ListViewItem LItem;
			listView1.Items.Clear();
			foreach(KeyColumnDefinition LColumn in FValue.Columns)
			{
				LItem = listView1.Items.Add(new ListViewItem());
				LItem.Tag = LColumn;
				LItem.Text = LColumn.ColumnName;
			}
		}

		private void CheckItemSelected()
		{
			if (listView1.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			KeyColumnDefinition LDefinition = new KeyColumnDefinition();
			KeyColumnDefinitionEdit LEditForm = new KeyColumnDefinitionEdit(LDefinition);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					ListViewItem LItem = new ListViewItem();
					LItem.Text = LDefinition.ColumnName;
					listView1.Items.Add(LItem);
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void btnEdit_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			ListViewItem LItem = listView1.SelectedItems[0];
			KeyColumnDefinition LDefinition = new KeyColumnDefinition(LItem.Text);
			KeyColumnDefinitionEdit LEditForm = new KeyColumnDefinitionEdit(LDefinition);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
					LItem.Text = LDefinition.ColumnName;
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void btnDelete_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				listView1.Items.RemoveAt(listView1.SelectedIndices[0]);
		}

		private void btnUp_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int LSelectedIndex = listView1.SelectedIndices[0];
			ListViewItem LItem = listView1.SelectedItems[0];
			if (LSelectedIndex > 0)
			{
				listView1.Items.Remove(LItem);
				listView1.Items.Insert(LSelectedIndex - 1, LItem);
			}
		}

		private void btnDown_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int LSelectedIndex = listView1.SelectedIndices[0];
			ListViewItem LItem = listView1.SelectedItems[0];
			if (LSelectedIndex < (listView1.Items.Count - 1))
			{
				listView1.Items.Remove(LItem);
				listView1.Items.Insert(LSelectedIndex + 1, LItem);
			}
		}

		private void btnOK_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		private void btnCancel_Click(object ASender, System.EventArgs AArgs)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					FValue.Columns.Clear();
					foreach(ListViewItem LItem in listView1.Items)
						FValue.Columns.Add(new KeyColumnDefinition(LItem.Text));
				}
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}
	}

	public class KeyDefinitionsBrowse : AbstractCollectionBrowse
	{

		private KeyDefinitions FKeys;

		public KeyDefinitionsBrowse(KeyDefinitions ACollection)
		{
			this.Text = "Key Definitions";
			FKeys = ACollection;
		}

		protected override void Dispose( bool ADisposing )
		{
			if (FKeys != null)
			{
				FKeys = null;
			}
			base.Dispose( ADisposing );
		}

		protected override void InitColumns()
		{
			FListView.Columns.Add("Order", 250, HorizontalAlignment.Left);
		}

		protected override void btnAdd_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnAdd_Click(ASender, AArgs);
			KeyDefinition LKey = new KeyDefinition();
			KeyDefinitionEdit LEditForm = new KeyDefinitionEdit(LKey);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					if (LKey.Columns.Count > 0)
						FKeys.Add(LKey);
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (FListView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnEdit_Click(ASender, AArgs);
			CheckItemSelected();
			ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
			KeyDefinition LKey = (KeyDefinition)LListViewItem.Tag;
			KeyDefinitionEdit LEditForm = new KeyDefinitionEdit(LKey);
			try
			{
				LEditForm.ShowDialog((IWin32Window)this);
				if (LEditForm.DialogResult == DialogResult.OK)
				{
					if (LKey.Columns.Count == 0)
						FKeys.Remove(LKey);
				}
			}
			finally
			{
				LEditForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object ASender, System.EventArgs AArgs)
		{
			base.btnDelete_Click(ASender, AArgs);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem LListViewItem = FListView.Items[FListView.SelectedIndices[0]];
				FKeys.RemoveAt(FKeys.IndexOf(LListViewItem.Tag));
				LListViewItem.Tag = null;
			}
		}
	}

	public class KeyDefinitionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		private ITypeDescriptorContext FContext;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FContext = AContext;
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					KeyDefinitionsBrowse LForm = new KeyDefinitionsBrowse((KeyDefinitions)AValue);
					try
					{
						FEditorService.ShowDialog(LForm);
					}
					finally
					{
						FContext = null;
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class KeyEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				KeyDefinition LKey = new KeyDefinition();
				if (AValue != null)
				{
					if (((KeyDefinition)AValue).MetaData != null)
						LKey.MetaData = ((KeyDefinition)AValue).MetaData.Copy();
					foreach (KeyColumnDefinition LColumn in ((KeyDefinition)AValue).Columns)
						LKey.Columns.Add(new KeyColumnDefinition(LColumn.ColumnName));
				}
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					KeyDefinitionEdit LForm = new KeyDefinitionEdit(LKey);
					try
					{
						FEditorService.ShowDialog(LForm);
						if (LForm.DialogResult == DialogResult.OK)
						{
							if (LKey.Columns.Count == 0)
								AValue = null;
							else
								AValue = LKey;
						}
					}
					finally
					{
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}

	public class D4ExpressionEmitEdit : MultiLineEditor
	{
		protected override void Validate(string AValue)
		{
			if (AValue != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				LParser.ParseExpression(AValue);
			}
		}
	}

	public class DefaultDefinitionEditForm : Form
	{
		private System.Windows.Forms.RadioButton rbNone;
		private System.Windows.Forms.GroupBox FGroupBox1;
		private System.Windows.Forms.RadioButton rbDefault;
		private System.Windows.Forms.PropertyGrid FPropertyGrid;
		private System.ComponentModel.IContainer FComponents;
		private DefaultDefinition FOriginalValue;

		public DefaultDefinitionEditForm(DefaultDefinition AValue)
		{
			InitializeForm();
			FOriginalValue = AValue;
			Value = AValue;
		}

		private void InitializeForm()
		{
			FComponents = new System.ComponentModel.Container();
			this.rbNone = new System.Windows.Forms.RadioButton();
			this.FGroupBox1 = new System.Windows.Forms.GroupBox();
			this.rbDefault = new System.Windows.Forms.RadioButton();
			this.FPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.FGroupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// rbNone
			// 
			this.rbNone.Checked = true;
			this.rbNone.Location = new System.Drawing.Point(16, 8);
			this.rbNone.Name = "rbNone";
			this.rbNone.TabIndex = 0;
			this.rbNone.Text = "None";
			this.rbNone.CheckedChanged += new System.EventHandler(this.rbNone_CheckedChanged);
			// 
			// FGroupBox1
			// 
			this.FGroupBox1.Controls.AddRange(new System.Windows.Forms.Control[] {
																					 this.FPropertyGrid});
			this.FGroupBox1.Enabled = false;
			this.FGroupBox1.Location = new System.Drawing.Point(8, 40);
			this.FGroupBox1.Name = "FGroupBox1";
			this.FGroupBox1.Size = new System.Drawing.Size(216, 184);
			this.FGroupBox1.TabIndex = 1;
			this.FGroupBox1.TabStop = false;
			// 
			// rbDefault
			// 
			this.rbDefault.Location = new System.Drawing.Point(16, 32);
			this.rbDefault.Name = "rbDefault";
			this.rbDefault.Size = new System.Drawing.Size(120, 24);
			this.rbDefault.TabIndex = 2;
			this.rbDefault.Text = "Default Definition";
			this.rbDefault.CheckedChanged += new System.EventHandler(this.rbDefault_CheckedChanged);
			// 
			// FPropertyGrid
			// 
			this.FPropertyGrid.CommandsVisibleIfAvailable = true;
			this.FPropertyGrid.LargeButtons = false;
			this.FPropertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.FPropertyGrid.Location = new System.Drawing.Point(8, 16);
			this.FPropertyGrid.Name = "FPropertyGrid";
			this.FPropertyGrid.Size = new System.Drawing.Size(200, 160);
			this.FPropertyGrid.TabIndex = 0;
			this.FPropertyGrid.Text = "propertyGrid1";
			this.FPropertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
			this.FPropertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;			
			// 
			// Form
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(232, 230);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.rbNone,
																		  this.rbDefault,
																		  this.FGroupBox1});
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormDefault";
			this.ShowInTaskbar = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Default Definition";
			this.FGroupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected override void Dispose( bool ADisposing )
		{
			if( ADisposing )
			{
				if(FComponents != null)
					FComponents.Dispose();
			}
			base.Dispose( ADisposing );
		}

		private DefaultDefinition FValue;
		public DefaultDefinition Value
		{
			get { return FValue; }
			set
			{
				if (FValue != value)
				{
					FValue = value;
					Changed();
				}
			}
		}

		protected void Changed()
		{
			if (FValue != null)
			{
				FGroupBox1.Enabled = true;
				rbDefault.Checked = true;
			}
			else
			{
				FGroupBox1.Enabled = false;
				rbDefault.Checked = false;
			}
			FPropertyGrid.SelectedObject = FValue;
		}

		private void rbNone_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbNone.Checked)
				Value = null;
		}

		private void rbDefault_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbDefault.Checked)
			{
				if (Value == null)
				{
					if (FOriginalValue != null)
						Value = FOriginalValue;
					else
						Value = new DefaultDefinition();
				}
			}
		}
	}

	public class DefaultDefinitionEdit : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;
	
		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					DefaultDefinitionEditForm LForm = new DefaultDefinitionEditForm(AValue as DefaultDefinition);
					try
					{
						LForm.Closing += new CancelEventHandler(Form_Closing);
						FEditorService.ShowDialog(LForm);
						if (LForm.Value != AValue)
							AValue = LForm.Value;
					}
					finally
					{
						LForm.Closing -= new CancelEventHandler(Form_Closing);
						LForm.Dispose();
					}
				}
			}
			return AValue;
		}

		private void Form_Closing(object ASender, System.ComponentModel.CancelEventArgs AArgs)
		{
			try
			{
				Validate(((DefaultDefinitionEditForm)ASender).Value);
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}

		protected virtual void Validate(DefaultDefinition AValue)
		{
			if (AValue != null)
			{
				if (AValue.Expression == null)
					throw new DesignException(DesignException.Codes.InvalidDefaultDefinition);
			}
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}
}
