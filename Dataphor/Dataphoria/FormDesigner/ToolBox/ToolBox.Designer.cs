namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    partial class ToolBox
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolBox));
            this.FNodesImageList = new System.Windows.Forms.ImageList(this.components);
            this.FPointerImageList = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // FNodesImageList
            // 
            this.FNodesImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FNodesImageList.ImageStream")));
            this.FNodesImageList.TransparentColor = System.Drawing.Color.LimeGreen;
            this.FNodesImageList.Images.SetKeyName(0, "");
            // 
            // FPointerImageList
            // 
            this.FPointerImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FPointerImageList.ImageStream")));
            this.FPointerImageList.TransparentColor = System.Drawing.Color.Lime;
            this.FPointerImageList.Images.SetKeyName(0, "");
            // 
            // ToolBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ToolBox";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList FNodesImageList;
        private System.Windows.Forms.ImageList FPointerImageList;
    }
}
