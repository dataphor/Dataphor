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
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	public class MultiLineEditForm : System.Windows.Forms.Form
	{
		//private System.ComponentModel.IContainer components;
		private System.Windows.Forms.TextBox tbValue;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;

		public MultiLineEditForm(string tempValue)
		{
			InitializeComponent();
			Value = tempValue;
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
            this.tbValue = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbValue
            // 
            this.tbValue.AcceptsReturn = true;
            this.tbValue.AcceptsTab = true;
            this.tbValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbValue.Font = new System.Drawing.Font("Courier New", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbValue.Location = new System.Drawing.Point(5, 9);
            this.tbValue.Multiline = true;
            this.tbValue.Name = "tbValue";
            this.tbValue.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbValue.Size = new System.Drawing.Size(495, 272);
            this.tbValue.TabIndex = 0;
            this.tbValue.WordWrap = false;
            this.tbValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextKeyDown);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(310, 288);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(90, 26);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "&OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(406, 288);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 26);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // MultiLineEditForm
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(504, 324);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tbValue);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "MultiLineEditForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		protected override void Dispose( bool disposing )
		{
//			if( ADisposing )
//			{
//				if (components != null)
//					components.Dispose();
//			}
			base.Dispose( disposing );
		}

		private string _value;
		public string Value
		{
			get { return _value; }
			set
			{
				if (_value != value)
				{
					_value = value;
					tbValue.Text = value;
				}
			}
		}

		protected virtual void TextKeyDown(object sender, KeyEventArgs args)
		{
			switch (Keys.KeyCode & args.KeyCode)
			{
				case Keys.Enter :
					if (args.Control)
					{
						args.Handled = true;
						btnOK.PerformClick();
					}
					break;
			}
		}

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			_value = tbValue.Text;
			Close();
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			Close();
		}

	}

	public interface IPropertyTextEditorService
	{
		void EditProperty(object AInstance, PropertyDescriptor ADescriptor);
	}

	/// <summary> Enables multi-line editing of a string value. </summary>
	public class MultiLineEditor : UITypeEditor
	{
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				ISite site = ((IComponent)context.Instance).Site;
				if (site != null)
				{
					IPropertyTextEditorService service = (IPropertyTextEditorService)site.GetService(typeof(IPropertyTextEditorService));
					if (service != null)
					{
						service.EditProperty(context.Instance, context.PropertyDescriptor);
						return tempValue;
					}
				}

				IWindowsFormsEditorService editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (editorService != null)
				{
					MultiLineEditForm form = new MultiLineEditForm((string)tempValue);
					try
					{
						form.Closing += new CancelEventHandler(Form_Closing);
						if (context.PropertyDescriptor != null)
							form.Text = context.PropertyDescriptor.DisplayName;
						editorService.ShowDialog(form);
						if (form.DialogResult == DialogResult.OK)
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
			if (((MultiLineEditForm)sender).DialogResult == DialogResult.OK)
			{
				try
				{
					Validate(((MultiLineEditForm)sender).Value);
				}
				catch
				{
					args.Cancel = true;
					throw;
				}
			}
		}

		protected virtual void Validate(string tempValue)
		{
			//Abstract validation.
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
