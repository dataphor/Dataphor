namespace Alphora.Dataphor.Dataphoria
{
    partial class DataphoriaMainControl
    {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ToolStripProfessionalRenderer toolStripProfessionalRenderer1 = new System.Windows.Forms.ToolStripProfessionalRenderer();
            this.FTabbedDocumentControl = new Darwen.Windows.Forms.Controls.TabbedDocuments.TabbedDocumentControl();
            this.SuspendLayout();
            // 
            // FTabbedDocumentControl
            // 
            this.FTabbedDocumentControl.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.FTabbedDocumentControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FTabbedDocumentControl.Location = new System.Drawing.Point(30, 24);
            this.FTabbedDocumentControl.Name = "FTabbedDocumentControl";
            toolStripProfessionalRenderer1.RoundedEdges = true;
            this.FTabbedDocumentControl.Renderer = toolStripProfessionalRenderer1;
            this.FTabbedDocumentControl.SelectedControl = null;
            this.FTabbedDocumentControl.Size = new System.Drawing.Size(309, 307);
            this.FTabbedDocumentControl.TabIndex = 14;
            // 
            // DataphoriaMainControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.FTabbedDocumentControl);
            this.Name = "DataphoriaMainControl";
            this.Size = new System.Drawing.Size(369, 355);
            this.Controls.SetChildIndex(this.FTabbedDocumentControl, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Darwen.Windows.Forms.Controls.TabbedDocuments.TabbedDocumentControl FTabbedDocumentControl;


    }
}
