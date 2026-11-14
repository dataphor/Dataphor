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
		public override DataSet DataSet(ITypeDescriptorContext context)
		{
			if 
				(
				(context != null) && 
				(context.Instance != null) &&
				(context.Instance is GridColumn)
				)
			{
				GridColumn column = (GridColumn)context.Instance;
				if ((column.Grid != null) && (column.Grid.Source != null))
					return column.Grid.Source.DataSet;
			}
			return null;
		}	 
	}

	/// <summary> Provides an interface to edit GridColumns </summary>
	public class GridColumnCollectionEditor : CollectionEditor
	{
		public GridColumnCollectionEditor(Type type) : base(type) {}

		protected override object CreateInstance(Type itemType)
		{
			if (itemType == typeof(Alphora.Dataphor.DAE.Client.Controls.GridColumn))
				return base.CreateInstance(typeof(Alphora.Dataphor.DAE.Client.Controls.TextColumn));
			else
				return base.CreateInstance(itemType);
		}

		protected override Type[] CreateNewItemTypes()
		{
			Type[] itemTypes = 
				{
					typeof(Alphora.Dataphor.DAE.Client.Controls.TextColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.ActionColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.CheckBoxColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.ImageColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.LinkColumn),
					typeof(Alphora.Dataphor.DAE.Client.Controls.SequenceColumn)
				};
			return itemTypes;
		}

		private int NonDefaultGridColumnCount(Alphora.Dataphor.DisposableList columnsArray)
		{
			int objectCount = 0;
			for (int i = 0; i < columnsArray.Count; i++)
				if (columnsArray[i] is Alphora.Dataphor.DAE.Client.Controls.DataColumn)
				{
					if (!((Alphora.Dataphor.DAE.Client.Controls.DataColumn)columnsArray[i]).IsDefaultGridColumn)
						objectCount++;
				}
				else
					objectCount++;
			return objectCount;
		}

		protected override object[] GetItems(object editValue)
		{
			Alphora.Dataphor.DisposableList array = (Alphora.Dataphor.DisposableList)editValue;
			int objectCount = NonDefaultGridColumnCount(array);
			if (objectCount > 0)
			{
				object[] nonDefaultColumns = new object[objectCount];
				int insertIndex = 0;
				for (int i = 0; i < array.Count; i++)
					if (array[i] is Alphora.Dataphor.DAE.Client.Controls.DataColumn)
					{
						if (!((Alphora.Dataphor.DAE.Client.Controls.DataColumn)array[i]).IsDefaultGridColumn)
							nonDefaultColumns[insertIndex++] = array[i];
					}
					else
						nonDefaultColumns[insertIndex++] = array[i];
				return nonDefaultColumns;
			}
			else
				return new object[] {};
			
		}

		private CollectionForm _form;
		protected override CollectionForm CreateCollectionForm()
		{
			CollectionForm form = base.CreateCollectionForm();
			_form = form;
			return form;
		}
											   
		protected override object SetItems(object editValue, object[] tempValue)
		{
			object result = base.SetItems(editValue, tempValue);		
			if ((editValue is GridColumns) && (_form != null))
				foreach (Control control in _form.Controls)
					if (control is PropertyGrid)
						((PropertyGrid)control).Refresh();
			return result;
		}
	}

	public class AbstractCollectionBrowse : Form
	{
		protected System.Windows.Forms.ListView _listView;
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
			this._listView = new System.Windows.Forms.ListView();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// FListView
			// 
			this._listView.AllowColumnReorder = true;
			this._listView.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this._listView.FullRowSelect = true;
			this._listView.HideSelection = false;
			this._listView.Location = new System.Drawing.Point(8, 8);
			this._listView.MultiSelect = false;
			this._listView.Name = "FListView";
			this._listView.Size = new System.Drawing.Size(272, 132);
			this._listView.TabIndex = 0;
			this._listView.View = System.Windows.Forms.View.Details;
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
																		  this._listView});
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

		protected virtual void btnAdd_Click(object sender, System.EventArgs args)
		{
			//Abstract
		}

		protected virtual void btnEdit_Click(object sender, System.EventArgs args)
		{
			//Abstract
		}

		protected virtual void btnDelete_Click(object sender, System.EventArgs args)
		{
			//Abstract
		}
	}

	public class AdornColumnExpressionBrowse : AbstractCollectionBrowse
	{
		private AdornColumnExpressions _expressions;

		public AdornColumnExpressionBrowse(AdornColumnExpressions collection)
		{
			this.Text = "Column Expressions";
			_expressions = collection;
			ExpressionsChanged(_expressions, null);
		}

		protected override void Dispose( bool disposing )
		{
			if (_expressions != null)
			{
				_expressions = null;
			}
			base.Dispose( disposing );
		}

		protected override void InitColumns()
		{
			_listView.Columns.Add("Column Name", 100, HorizontalAlignment.Left);
			_listView.Columns.Add("Default", 110, HorizontalAlignment.Left);
			_listView.Columns.Add("Constraint", 180, HorizontalAlignment.Left);
		}

		private void ExpressionsChanged(object sender, object item)
		{
			if (_listView != null)
			{
				_listView.BeginUpdate();
				try
				{
					_listView.Items.Clear();
					int i = 0;
					foreach (AdornColumnExpression expression in _expressions)
					{
						_listView.Items.Add(new ListViewItem());
						_listView.Items[i].Tag = expression;
						_listView.Items[i].Text = expression.ColumnName;
						_listView.Items[i].SubItems.Add(expression.Default != null ? expression.Default.ExpressionString : String.Empty);
						_listView.Items[i].SubItems.Add(expression.Constraints.Count > 0 ? expression.Constraints[0].ExpressionString : String.Empty); 
						++i;
					}
					if (_listView.Items.Count > 0)
						_listView.Items[0].Focused = true;
				}
				finally
				{
					_listView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object sender, System.EventArgs args)
		{
			base.btnAdd_Click(sender, args);
			AdornColumnExpression expression = new AdornColumnExpression();
			AdornColumnExpressionEdit editForm = new AdornColumnExpressionEdit(expression);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
					_expressions.Add(expression);
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (_listView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object sender, System.EventArgs args)
		{
			base.btnEdit_Click(sender, args);
			CheckItemSelected();
			ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
			AdornColumnExpression expression = (AdornColumnExpression)listViewItem.Tag;
			AdornColumnExpressionEdit editForm = new AdornColumnExpressionEdit(expression);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
					ExpressionsChanged(_expressions, null);
			}
			finally
			{
				editForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object sender, System.EventArgs args)
		{
			base.btnDelete_Click(sender, args);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
				_expressions.RemoveAt(_expressions.IndexOf(listViewItem.Tag));
				listViewItem.Tag = null;
				ExpressionsChanged(_expressions, null);
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

		public AdornColumnExpressionEdit(AdornColumnExpression expression)
		{
			InitializeComponent();
			Value = expression;
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

		private AdornColumnExpression _value;
		public AdornColumnExpression Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.AdornColumnExpressionNotNull);
				_value = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			tbColumnName.Text = _value.ColumnName;
			rbDefault.Checked = _value.Default != null;
			rbConstraint.Checked = _value.Constraints.Count > 0;
		}

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs e)

		{
			Close();
		}

		private void EditMetaData(ref MetaData tempValue)
		{
			MetaDataEditForm metaDataEditForm = new MetaDataEditForm(tempValue); 
			try
			{
				metaDataEditForm.ShowDialog((IWin32Window)this);
				if (metaDataEditForm.DialogResult == DialogResult.OK)
					tempValue = metaDataEditForm.MetaData;
			}
			finally
			{
				metaDataEditForm.Dispose();
			}
		}

		private void btnDefaultMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref _defaultMetaData);
			if (_defaultMetaData == null)
				tbDefaultMetaData.Text = "(none)";
			else
				tbDefaultMetaData.Text = _defaultMetaData.ToString();

		}

		private void btnConstraintMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref _constraintMetaData);
			if (_constraintMetaData == null)
				tbConstraintMetaData.Text = "(none)";
			else
				tbConstraintMetaData.Text = _constraintMetaData.ToString();
		}

		protected virtual void ExpressionClosing(object sender, CancelEventArgs args)
		{
			if (((MultiLineEditForm)sender).Value != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				parser.ParseExpression(((MultiLineEditForm)sender).Value);
			}
		}

		private void EditExpression(System.Windows.Forms.TextBox textBoxExpression)
		{
			MultiLineEditForm form = new MultiLineEditForm(textBoxExpression.Text);
			try
			{
				form.Closing += new CancelEventHandler(ExpressionClosing);
				form.Text = "Expression";
				form.ShowDialog((IWin32Window)this);
				if (form.DialogResult == DialogResult.OK)
					textBoxExpression.Text = form.Value;
			}
			finally
			{
				form.Closing -= new  CancelEventHandler(ExpressionClosing);
				form.Dispose();
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

		private MetaData _defaultMetaData;
		public MetaData DefaultMetaData
		{
			get { return _defaultMetaData; }
		}

		private void rbDefaultNone_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbDefaultNone.Checked)
			{
				groupBox3.Enabled = false;
				_defaultMetaData = null;
			}
		}

		private void rbDefault_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbDefault.Checked)
			{
				groupBox3.Enabled = true;
				if (_value.Default != null)
				{
					tbDefaultExpression.Text = _value.Default.ExpressionString;
					if (_value.Default.MetaData != null)
					{
						_defaultMetaData = _value.Default.MetaData.Copy();
						tbDefaultMetaData.Text = _defaultMetaData.ToString();
					}
					else
					{
						_defaultMetaData = null;
						tbDefaultMetaData.Text = "(none)";
					}
				}
			}
		}

		private MetaData _constraintMetaData;
		public MetaData ConstraintMetaData
		{
			get { return _constraintMetaData; }
		}

		private void rbConstraintNone_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbConstraintNone.Checked)
			{
				gbConstraints.Enabled = false;
				_constraintMetaData = null;
			}
		}

		private void rbConstraint_CheckedChanged(object sender, System.EventArgs e)
		{
			if (rbConstraint.Checked)
			{
				gbConstraints.Enabled = true;
				if (_value.Constraints.Count > 0)
				{
					ConstraintDefinition constraint = _value.Constraints[0];
					tbName.Text = constraint.ConstraintName;
					tbConstraintExpression.Text = constraint.ExpressionString;
					if (constraint.MetaData != null)
					{
						_constraintMetaData = constraint.MetaData.Copy();
						tbConstraintMetaData.Text = _constraintMetaData.ToString();
					}
					else
					{
						tbConstraintMetaData.Text = "(none)";
						_constraintMetaData = null;
					}
				}
			}
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
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
					_value.ColumnName = tbColumnName.Text;
					if (rbDefault.Checked)
					{
						DefaultDefinition defaultValue = new DefaultDefinition();
						defaultValue.ExpressionString = tbDefaultExpression.Text;
						defaultValue.MetaData = DefaultMetaData;
						_value.Default = defaultValue;
					}
					else
						_value.Default = null;

					if (rbConstraint.Checked)
					{
						ConstraintDefinition constraint = new ConstraintDefinition();
						constraint.ConstraintName = tbName.Text;
						constraint.ExpressionString = tbConstraintExpression.Text;
						constraint.MetaData = ConstraintMetaData;
						if (_value.Constraints.Count > 0)
							_value.Constraints.Remove(_value.Constraints[0]);
						_value.Constraints.Add(constraint);
					}
					else
						_value.Constraints.Clear();
				}
				catch
				{
					args.Cancel = true;
					throw;
				}
			}
		}
	}

	public class AdornColumnExpressionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		private ITypeDescriptorContext _context;

		private void ExpressionChanged(object sender, object item)
		{
			if (_context != null)
				_context.OnComponentChanged();
		}
	
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_context = context;
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					AdornColumnExpressionBrowse form = new AdornColumnExpressionBrowse((AdornColumnExpressions)tempValue);
					try
					{
						_editorService.ShowDialog(form);
					}
					finally
					{
						_context = null;
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}

	public class ConstraintsBrowse : AbstractCollectionBrowse
	{
		private ConstraintDefinitions _constraints;

		public ConstraintsBrowse(ConstraintDefinitions collection)
		{
			this.Text = "Constraints";
			_constraints = collection;
		}

		protected override void Dispose( bool disposing )
		{
			if (_constraints != null)
			{
				_constraints = null;
			}
			base.Dispose( disposing );
		}

		protected override void InitColumns()
		{
			_listView.Columns.Add("Constraint Name", 100, HorizontalAlignment.Left);
			_listView.Columns.Add("Expression", 120, HorizontalAlignment.Left);
		}

		private void ConstraintsChanged(object sender, object item)
		{
			if (_listView != null)
			{
				_listView.BeginUpdate();
				try
				{
					_listView.Items.Clear();
					int i = 0;
					foreach (ConstraintDefinition constraint in _constraints)
					{
						_listView.Items.Add(new ListViewItem());
						_listView.Items[i].Tag = constraint;
						_listView.Items[i].Text = constraint.ConstraintName;
						_listView.Items[i].SubItems.Add(constraint.ExpressionString);
						++i;
					}
					if (_listView.Items.Count > 0)
						_listView.Items[0].Focused = true;
				}
				finally
				{
					_listView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object sender, System.EventArgs args)
		{
			base.btnAdd_Click(sender, args);
			ConstraintDefinition constraint = new ConstraintDefinition();
			ConstraintEdit editForm = new ConstraintEdit(constraint);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
					_constraints.Add(constraint);
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (_listView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object sender, System.EventArgs args)
		{
			base.btnEdit_Click(sender, args);
			CheckItemSelected();
			ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
			ConstraintDefinition constraint = (ConstraintDefinition)listViewItem.Tag;
			ConstraintEdit editForm = new ConstraintEdit(constraint);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
			}
			finally
			{
				editForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object sender, System.EventArgs args)
		{
			base.btnDelete_Click(sender, args);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
				_constraints.RemoveAt(_constraints.IndexOf(listViewItem.Tag));
				listViewItem.Tag = null;
				ConstraintsChanged(_constraints, null);
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

		public ConstraintEdit(ConstraintDefinition tempValue)
		{
			InitializeComponent();
			Value = tempValue;
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

		private ConstraintDefinition _value;
		public ConstraintDefinition Value
		{
			get { return _value; }
			set
			{
				_value = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			if (_value != null)
			{
				tbName.Text = _value.ConstraintName;
				tbConstraintExpression.Text = _value.ExpressionString;
				if (_value.MetaData != null)
				{
					_metaData = _value.MetaData.Copy();
					tbConstraintMetaData.Text = _metaData.ToString();
				}
				else
				{
					tbConstraintMetaData.Text = "(none)";
					_metaData = null;
				}
			}
		}

		private void btnOK_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		protected virtual void ExpressionClosing(object sender, CancelEventArgs args)
		{
			if (((MultiLineEditForm)sender).Value != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				parser.ParseExpression(((MultiLineEditForm)sender).Value);
			}
		}

		private void EditExpression(System.Windows.Forms.TextBox textBoxExpression)
		{
			MultiLineEditForm form = new MultiLineEditForm(textBoxExpression.Text);
			try
			{
				form.Closing += new CancelEventHandler(ExpressionClosing);
				form.Text = "Expression";
				form.ShowDialog((IWin32Window)this);
				if (form.DialogResult == DialogResult.OK)
					textBoxExpression.Text = form.Value;
			}
			finally
			{
				form.Closing -= new  CancelEventHandler(ExpressionClosing);
				form.Dispose();
			}
		}

		private void btnConstraintExpression_Click(object sender, System.EventArgs args)
		{
			EditExpression(tbConstraintExpression);
		}

		private MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
		}

		private void EditMetaData(ref MetaData tempValue)
		{
			MetaDataEditForm metaDataEditForm = new MetaDataEditForm(tempValue); 
			try
			{
				metaDataEditForm.ShowDialog((IWin32Window)this);
				if (metaDataEditForm.DialogResult == DialogResult.OK)
					tempValue = metaDataEditForm.MetaData;
			}
			finally
			{
				metaDataEditForm.Dispose();
			}
		}

		private void btnConstraintMetaData_Click(object sender, System.EventArgs e)
		{
			EditMetaData(ref _metaData);
			if (_metaData == null)
				tbConstraintMetaData.Text = "(none)";
			else
				tbConstraintMetaData.Text = _metaData.ToString();
		}

		
		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			if (DialogResult == DialogResult.OK)
			{
				try
				{
					if (tbName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ConstraintNameRequired);
					if (tbConstraintExpression.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ConstraintExpressionRequired);
				
					//Assign values...
					_value.ConstraintName = tbName.Text;
					_value.ExpressionString = tbConstraintExpression.Text;
					_value.MetaData = MetaData;
				}
				catch
				{
					args.Cancel = true;
					throw;
				}
			}
		}

	}

	public class ConstraintsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		private ITypeDescriptorContext _context;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_context = context;
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					ConstraintsBrowse form = new ConstraintsBrowse((ConstraintDefinitions)tempValue);
					try
					{
						_editorService.ShowDialog(form);
					}
					finally
					{
						_context = null;
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
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

		public OrderColumnDefinitionEdit(OrderColumnDefinition tempValue)
		{
			InitializeComponent();
			Value = tempValue;
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

		private OrderColumnDefinition _value;
		public OrderColumnDefinition Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.InvalidOrderColumnDefinition);
				_value = value;
				DefinitionChanged();
			}
		}

		private void DefinitionChanged()
		{
			tbColumnName.Text = _value.ColumnName;
			rbAscending.Checked = _value.Ascending;
			rbDescending.Checked = !_value.Ascending;
		}

		private void btnOK_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					if (tbColumnName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ColumnNameNeeded);
					_value.ColumnName = tbColumnName.Text;
					_value.Ascending = rbAscending.Checked;
				}
			}
			catch
			{
				args.Cancel = true;
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

		public OrderDefinitionEdit(OrderDefinition tempValue)
		{
			InitializeComponent();
			Value = tempValue;
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

		protected System.Drawing.Bitmap LoadBitmap(string resourceName)
		{
			System.Drawing.Bitmap result = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream(resourceName));
			result.MakeTransparent();
			return result;
		}

		private OrderDefinition _value;
		protected internal OrderDefinition Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.NullOrderDefinition);
				_value = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			ListViewItem item;
			listView1.Items.Clear();
			foreach(OrderColumnDefinition column in _value.Columns)
			{
				item = listView1.Items.Add(new ListViewItem());
				item.Tag = column;
				item.Text = column.ColumnName;
				if (column.Ascending)
					item.SubItems.Add("asc");
				else
					item.SubItems.Add("desc");
			}
		}

		private void CheckItemSelected()
		{
			if (listView1.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			OrderColumnDefinition definition = new OrderColumnDefinition();
			OrderColumnDefinitionEdit editForm = new OrderColumnDefinitionEdit(definition);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					ListViewItem item = new ListViewItem();
					item.Text = definition.ColumnName;
					if (definition.Ascending)
						item.SubItems.Add("asc");
					else
						item.SubItems.Add("desc");
					listView1.Items.Add(item);
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void btnEdit_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			ListViewItem item = listView1.SelectedItems[0];
			OrderColumnDefinition definition = new OrderColumnDefinition(item.Text, item.SubItems[1].Text != "desc");
			OrderColumnDefinitionEdit editForm = new OrderColumnDefinitionEdit(definition);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					item.Text = definition.ColumnName;
					if (definition.Ascending)
						item.SubItems[1].Text = "asc";
					else
						item.SubItems[1].Text = "desc";
				}
			}
			finally
			{
				editForm.Dispose();
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
			int selectedIndex = listView1.SelectedIndices[0];
			ListViewItem item = listView1.SelectedItems[0];
			if (selectedIndex > 0)
			{
				listView1.Items.Remove(item);
				listView1.Items.Insert(selectedIndex - 1, item);
			}
		}

		private void btnDown_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int selectedIndex = listView1.SelectedIndices[0];
			ListViewItem item = listView1.SelectedItems[0];
			if (selectedIndex < (listView1.Items.Count - 1))
			{
				listView1.Items.Remove(item);
				listView1.Items.Insert(selectedIndex + 1, item);
			}
		}

		private void btnOK_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					_value.Columns.Clear();
					foreach(ListViewItem item in listView1.Items)
						_value.Columns.Add(new OrderColumnDefinition(item.Text, item.SubItems[1].Text != "desc"));
				}
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}
	}

	public class OrderDefinitionsBrowse : AbstractCollectionBrowse
	{
		private OrderDefinitions _orders;

		public OrderDefinitionsBrowse(OrderDefinitions collection)
		{
			this.Text = "Order Definitions";
			_orders = collection;
		}

		protected override void Dispose( bool disposing )
		{
			if (_orders != null)
			{
				_orders = null;
			}
			base.Dispose( disposing );
		}

		protected override void InitColumns()
		{
			_listView.Columns.Add("Order", 250, HorizontalAlignment.Left);
		}

		private void OrdersChanged(object sender, object item)
		{
			if (_listView != null)
			{
				_listView.BeginUpdate();
				try
				{
					_listView.Items.Clear();
					int i = 0;
					string caption;
					foreach (OrderDefinition order in _orders)
					{
						_listView.Items.Add(new ListViewItem());
						caption = String.Empty;
						foreach(OrderColumnDefinition column in order.Columns)
						{
							if (caption != String.Empty)
								caption += ", ";
							if (column.Ascending)
								caption += column.ColumnName;
							else
								caption += column.ColumnName + " desc"; 
						}
						_listView.Items[i].Tag = order;
						_listView.Items[i].Text = caption;
						++i;
					}
					if (_listView.Items.Count > 0)
						_listView.Items[0].Focused = true;
				}
				finally
				{
					_listView.EndUpdate();
				}
				
			}
		}
		
		protected override void btnAdd_Click(object sender, System.EventArgs args)
		{
			base.btnAdd_Click(sender, args);
			OrderDefinition order = new OrderDefinition();
			OrderDefinitionEdit editForm = new OrderDefinitionEdit(order);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					if (order.Columns.Count > 0)
						_orders.Add(order);
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (_listView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		public event EventHandler OnOrdersEdited;

		protected override void btnEdit_Click(object sender, System.EventArgs args)
		{
			base.btnEdit_Click(sender, args);
			CheckItemSelected();
			ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
			OrderDefinition order = (OrderDefinition)listViewItem.Tag;
			OrderDefinitionEdit editForm = new OrderDefinitionEdit(order);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					if (order.Columns.Count == 0)
						_orders.Remove(order);
					OrdersChanged(_orders, order);
					if (OnOrdersEdited != null)
						OnOrdersEdited(this, EventArgs.Empty);
					
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object sender, System.EventArgs args)
		{
			base.btnDelete_Click(sender, args);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
				_orders.RemoveAt(_orders.IndexOf(listViewItem.Tag));
				listViewItem.Tag = null;
				OrdersChanged(_orders, null);
			}
		}
	}

	public class OrderDefinitionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		private ITypeDescriptorContext _context;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_context = context;
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					OrderDefinitionsBrowse form = new OrderDefinitionsBrowse((OrderDefinitions)tempValue);
					try
					{
						_editorService.ShowDialog(form);
					}
					finally
					{
						_context = null;
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}

	public class OrderEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				OrderDefinition order = new OrderDefinition();
				if (tempValue != null)
				{
					if (((OrderDefinition)tempValue).MetaData != null)
						order.MetaData = ((OrderDefinition)tempValue).MetaData.Copy();
					foreach (OrderColumnDefinition column in ((OrderDefinition)tempValue).Columns)
						order.Columns.Add(new OrderColumnDefinition(column.ColumnName, column.Ascending));
				}
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					OrderDefinitionEdit form = new OrderDefinitionEdit(order);
					try
					{
						_editorService.ShowDialog(form);
						if (form.DialogResult == DialogResult.OK)
						{
							if (order.Columns.Count == 0)
								tempValue = null;
							else
								tempValue = order;
						}
					}
					finally
					{
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}

	public class KeyColumnDefinitionEdit : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.TextBox tbColumnName;
		private System.Windows.Forms.Label label1;
		private System.ComponentModel.Container components = null;

		public KeyColumnDefinitionEdit(KeyColumnDefinition tempValue)
		{
			InitializeComponent();
			Value = tempValue;
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

		private KeyColumnDefinition _value;
		public KeyColumnDefinition Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.InvalidKeyColumnDefinition);
				_value = value;
				DefinitionChanged();
			}
		}

		private void DefinitionChanged()
		{
			tbColumnName.Text = _value.ColumnName;
		}

		private void btnOK_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					if (tbColumnName.Text == String.Empty)
						throw new DesignException(DesignException.Codes.ColumnNameNeeded);
					_value.ColumnName = tbColumnName.Text;
				}
			}
			catch
			{
				args.Cancel = true;
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

		public KeyDefinitionEdit(KeyDefinition tempValue)
		{
			InitializeComponent();
			Value = tempValue;
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

		protected System.Drawing.Bitmap LoadBitmap(string resourceName)
		{
			System.Drawing.Bitmap result = new System.Drawing.Bitmap(GetType().Assembly.GetManifestResourceStream(resourceName));
			result.MakeTransparent();
			return result;
		}

		private KeyDefinition _value;
		protected internal KeyDefinition Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new DesignException(DesignException.Codes.NullKeyDefinition);
				_value = value;
				ValueChanged();
			}
		}

		protected virtual void ValueChanged()
		{
			ListViewItem item;
			listView1.Items.Clear();
			foreach(KeyColumnDefinition column in _value.Columns)
			{
				item = listView1.Items.Add(new ListViewItem());
				item.Tag = column;
				item.Text = column.ColumnName;
			}
		}

		private void CheckItemSelected()
		{
			if (listView1.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			KeyColumnDefinition definition = new KeyColumnDefinition();
			KeyColumnDefinitionEdit editForm = new KeyColumnDefinitionEdit(definition);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					ListViewItem item = new ListViewItem();
					item.Text = definition.ColumnName;
					listView1.Items.Add(item);
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void btnEdit_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			ListViewItem item = listView1.SelectedItems[0];
			KeyColumnDefinition definition = new KeyColumnDefinition(item.Text);
			KeyColumnDefinitionEdit editForm = new KeyColumnDefinitionEdit(definition);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
					item.Text = definition.ColumnName;
			}
			finally
			{
				editForm.Dispose();
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
			int selectedIndex = listView1.SelectedIndices[0];
			ListViewItem item = listView1.SelectedItems[0];
			if (selectedIndex > 0)
			{
				listView1.Items.Remove(item);
				listView1.Items.Insert(selectedIndex - 1, item);
			}
		}

		private void btnDown_Click(object sender, System.EventArgs e)
		{
			CheckItemSelected();
			int selectedIndex = listView1.SelectedIndices[0];
			ListViewItem item = listView1.SelectedItems[0];
			if (selectedIndex < (listView1.Items.Count - 1))
			{
				listView1.Items.Remove(item);
				listView1.Items.Insert(selectedIndex + 1, item);
			}
		}

		private void btnOK_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs args)
		{
			Close();
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			try
			{
				if (DialogResult == DialogResult.OK)
				{
					_value.Columns.Clear();
					foreach(ListViewItem item in listView1.Items)
						_value.Columns.Add(new KeyColumnDefinition(item.Text));
				}
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}
	}

	public class KeyDefinitionsBrowse : AbstractCollectionBrowse
	{

		private KeyDefinitions _keys;

		public KeyDefinitionsBrowse(KeyDefinitions collection)
		{
			this.Text = "Key Definitions";
			_keys = collection;
		}

		protected override void Dispose( bool disposing )
		{
			if (_keys != null)
			{
				_keys = null;
			}
			base.Dispose( disposing );
		}

		protected override void InitColumns()
		{
			_listView.Columns.Add("Order", 250, HorizontalAlignment.Left);
		}

		protected override void btnAdd_Click(object sender, System.EventArgs args)
		{
			base.btnAdd_Click(sender, args);
			KeyDefinition key = new KeyDefinition();
			KeyDefinitionEdit editForm = new KeyDefinitionEdit(key);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					if (key.Columns.Count > 0)
						_keys.Add(key);
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		private void CheckItemSelected()
		{
			if (_listView.SelectedItems.Count == 0)
				throw new DesignException(DesignException.Codes.NoItemSelected);
		}

		protected override void btnEdit_Click(object sender, System.EventArgs args)
		{
			base.btnEdit_Click(sender, args);
			CheckItemSelected();
			ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
			KeyDefinition key = (KeyDefinition)listViewItem.Tag;
			KeyDefinitionEdit editForm = new KeyDefinitionEdit(key);
			try
			{
				editForm.ShowDialog((IWin32Window)this);
				if (editForm.DialogResult == DialogResult.OK)
				{
					if (key.Columns.Count == 0)
						_keys.Remove(key);
				}
			}
			finally
			{
				editForm.Dispose();
			}
		}

		protected override void btnDelete_Click(object sender, System.EventArgs args)
		{
			base.btnDelete_Click(sender, args);
			CheckItemSelected();
			if (MessageBox.Show("Delete Item?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				ListViewItem listViewItem = _listView.Items[_listView.SelectedIndices[0]];
				_keys.RemoveAt(_keys.IndexOf(listViewItem.Tag));
				listViewItem.Tag = null;
			}
		}
	}

	public class KeyDefinitionsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		private ITypeDescriptorContext _context;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_context = context;
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					KeyDefinitionsBrowse form = new KeyDefinitionsBrowse((KeyDefinitions)tempValue);
					try
					{
						_editorService.ShowDialog(form);
					}
					finally
					{
						_context = null;
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}

	public class KeyEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				KeyDefinition key = new KeyDefinition();
				if (tempValue != null)
				{
					if (((KeyDefinition)tempValue).MetaData != null)
						key.MetaData = ((KeyDefinition)tempValue).MetaData.Copy();
					foreach (KeyColumnDefinition column in ((KeyDefinition)tempValue).Columns)
						key.Columns.Add(new KeyColumnDefinition(column.ColumnName));
				}
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					KeyDefinitionEdit form = new KeyDefinitionEdit(key);
					try
					{
						_editorService.ShowDialog(form);
						if (form.DialogResult == DialogResult.OK)
						{
							if (key.Columns.Count == 0)
								tempValue = null;
							else
								tempValue = key;
						}
					}
					finally
					{
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}

	public class D4ExpressionEmitEdit : MultiLineEditor
	{
		protected override void Validate(string tempValue)
		{
			if (tempValue != String.Empty)
			{
				Alphora.Dataphor.DAE.Language.D4.Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				parser.ParseExpression(tempValue);
			}
		}
	}

	public class DefaultDefinitionEditForm : Form
	{
		private System.Windows.Forms.RadioButton rbNone;
		private System.Windows.Forms.GroupBox _groupBox1;
		private System.Windows.Forms.RadioButton rbDefault;
		private System.Windows.Forms.PropertyGrid _propertyGrid;
		private System.ComponentModel.IContainer _components;
		private DefaultDefinition _originalValue;

		public DefaultDefinitionEditForm(DefaultDefinition tempValue)
		{
			InitializeForm();
			_originalValue = tempValue;
			Value = tempValue;
		}

		private void InitializeForm()
		{
			_components = new System.ComponentModel.Container();
			this.rbNone = new System.Windows.Forms.RadioButton();
			this._groupBox1 = new System.Windows.Forms.GroupBox();
			this.rbDefault = new System.Windows.Forms.RadioButton();
			this._propertyGrid = new System.Windows.Forms.PropertyGrid();
			this._groupBox1.SuspendLayout();
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
			this._groupBox1.Controls.AddRange(new System.Windows.Forms.Control[] {
																					 this._propertyGrid});
			this._groupBox1.Enabled = false;
			this._groupBox1.Location = new System.Drawing.Point(8, 40);
			this._groupBox1.Name = "FGroupBox1";
			this._groupBox1.Size = new System.Drawing.Size(216, 184);
			this._groupBox1.TabIndex = 1;
			this._groupBox1.TabStop = false;
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
			this._propertyGrid.CommandsVisibleIfAvailable = true;
			this._propertyGrid.LargeButtons = false;
			this._propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this._propertyGrid.Location = new System.Drawing.Point(8, 16);
			this._propertyGrid.Name = "FPropertyGrid";
			this._propertyGrid.Size = new System.Drawing.Size(200, 160);
			this._propertyGrid.TabIndex = 0;
			this._propertyGrid.Text = "propertyGrid1";
			this._propertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
			this._propertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;			
			// 
			// Form
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(232, 230);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.rbNone,
																		  this.rbDefault,
																		  this._groupBox1});
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormDefault";
			this.ShowInTaskbar = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Default Definition";
			this._groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(_components != null)
					_components.Dispose();
			}
			base.Dispose( disposing );
		}

		private DefaultDefinition _value;
		public DefaultDefinition Value
		{
			get { return _value; }
			set
			{
				if (_value != value)
				{
					_value = value;
					Changed();
				}
			}
		}

		protected void Changed()
		{
			if (_value != null)
			{
				_groupBox1.Enabled = true;
				rbDefault.Checked = true;
			}
			else
			{
				_groupBox1.Enabled = false;
				rbDefault.Checked = false;
			}
			_propertyGrid.SelectedObject = _value;
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
					if (_originalValue != null)
						Value = _originalValue;
					else
						Value = new DefaultDefinition();
				}
			}
		}
	}

	public class DefaultDefinitionEdit : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;
	
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					DefaultDefinitionEditForm form = new DefaultDefinitionEditForm(tempValue as DefaultDefinition);
					try
					{
						form.Closing += new CancelEventHandler(Form_Closing);
						_editorService.ShowDialog(form);
						if (form.Value != tempValue)
							tempValue = form.Value;
					}
					finally
					{
						form.Closing -= new CancelEventHandler(Form_Closing);
						form.Dispose();
					}
				}
			}
			return tempValue;
		}

		private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs args)
		{
			try
			{
				Validate(((DefaultDefinitionEditForm)sender).Value);
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}

		protected virtual void Validate(DefaultDefinition tempValue)
		{
			if (tempValue != null)
			{
				if (tempValue.Expression == null)
					throw new DesignException(DesignException.Codes.InvalidDefaultDefinition);
			}
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}
}
