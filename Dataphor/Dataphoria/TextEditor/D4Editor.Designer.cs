/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/


using SD = ICSharpCode.TextEditor;



namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
    partial class D4Editor 
    {
        private System.ComponentModel.IContainer components;
        

        protected override void Dispose(bool disposing)
        {
            try
            {
                components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(D4Editor));
            
            this.FDockContentTextEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FPositionStatus)).BeginInit();
            this.SuspendLayout();
            
            this.ClientSize = new System.Drawing.Size(455, 376);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "D4Editor";
            this.FDockContentTextEdit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.FPositionStatus)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion


    }
}
