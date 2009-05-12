namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    partial class ToolBox
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ImageList FPointerImageList;

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
            components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.FPointerImageList = new System.Windows.Forms.ImageList(this.components);

            // 
            // FPointerImageList
            // 
            //this.FPointerImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FPointerImageList.ImageStream")));
            this.FPointerImageList.TransparentColor = System.Drawing.Color.Lime;
            //this.FPointerImageList.Images.SetKeyName(0, "");
        }

        #endregion
    }
}
