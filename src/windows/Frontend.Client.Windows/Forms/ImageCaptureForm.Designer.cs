namespace Alphora.Dataphor.Frontend.Client.Windows
{
    partial class ImageCaptureForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FCaptureFrame = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.FPreviewImage = new System.Windows.Forms.PictureBox();
            this.FDevices = new System.Windows.Forms.ComboBox();
            this.FCaptureImage = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.FSettings = new System.Windows.Forms.Button();
            this.FContentPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FPreviewImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FCaptureImage)).BeginInit();
            this.SuspendLayout();
            // 
            // FContentPanel
            // 
            this.FContentPanel.AutoScroll = true;
            this.FContentPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.FContentPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.FContentPanel.Controls.Add(this.label1);
            this.FContentPanel.Controls.Add(this.FSettings);
            this.FContentPanel.Controls.Add(this.FDevices);
            this.FContentPanel.Controls.Add(this.FPreviewImage);
            this.FContentPanel.Controls.Add(this.FCaptureFrame);
            this.FContentPanel.Controls.Add(this.FCaptureImage);
            this.FContentPanel.Controls.Add(this.label2);
            this.FContentPanel.Size = new System.Drawing.Size(627, 331);
            this.FContentPanel.Controls.SetChildIndex(this.label2, 0);
            this.FContentPanel.Controls.SetChildIndex(this.FCaptureImage, 0);
            this.FContentPanel.Controls.SetChildIndex(this.FCaptureFrame, 0);
            this.FContentPanel.Controls.SetChildIndex(this.FPreviewImage, 0);
            this.FContentPanel.Controls.SetChildIndex(this.FDevices, 0);
            this.FContentPanel.Controls.SetChildIndex(this.FSettings, 0);
            this.FContentPanel.Controls.SetChildIndex(this.label1, 0);
            //this.FContentPanel.Controls.SetChildIndex(this.FBarToggleButton, 0);
            // 
            // FBarToggleButton
            // 
            //this.FBarToggleButton.Location = new System.Drawing.Point(617, 0);
            // 
            // FCaptureFrame
            // 
            this.FCaptureFrame.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.FCaptureFrame.Enabled = false;
            this.FCaptureFrame.Location = new System.Drawing.Point(131, 291);
            this.FCaptureFrame.Name = "FCaptureFrame";
            this.FCaptureFrame.Size = new System.Drawing.Size(75, 23);
            this.FCaptureFrame.TabIndex = 7;
            this.FCaptureFrame.Text = "Capture";
            this.FCaptureFrame.UseVisualStyleBackColor = true;
            this.FCaptureFrame.Click += new System.EventHandler(this.FCapture_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Device";
            // 
            // FPreviewImage
            // 
            this.FPreviewImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.FPreviewImage.Location = new System.Drawing.Point(23, 65);
            this.FPreviewImage.Name = "FPreviewImage";
            this.FPreviewImage.Size = new System.Drawing.Size(280, 210);
            this.FPreviewImage.TabIndex = 5;
            this.FPreviewImage.TabStop = false;
            // 
            // FDevices
            // 
            this.FDevices.FormattingEnabled = true;
            this.FDevices.Location = new System.Drawing.Point(67, 7);
            this.FDevices.Name = "FDevices";
            this.FDevices.Size = new System.Drawing.Size(236, 21);
            this.FDevices.TabIndex = 4;
            this.FDevices.Text = "<select a device>";
            this.FDevices.SelectedIndexChanged += new System.EventHandler(this.FDevices_SelectedIndexChanged);
            // 
            // FCaptureImage
            // 
            this.FCaptureImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FCaptureImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.FCaptureImage.Image = global::Alphora.Dataphor.Frontend.Client.Windows.Strings.NoPhoto;
            this.FCaptureImage.Location = new System.Drawing.Point(325, 65);
            this.FCaptureImage.Name = "FCaptureImage";
            this.FCaptureImage.Size = new System.Drawing.Size(280, 210);
            this.FCaptureImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.FCaptureImage.TabIndex = 8;
            this.FCaptureImage.TabStop = false;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(437, 296);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Image";
            // 
            // FSettings
            // 
            this.FSettings.Enabled = false;
            this.FSettings.Location = new System.Drawing.Point(67, 32);
            this.FSettings.Name = "FSettings";
            this.FSettings.Size = new System.Drawing.Size(75, 23);
            this.FSettings.TabIndex = 11;
            this.FSettings.Text = "Settings";
            this.FSettings.UseVisualStyleBackColor = true;
            this.FSettings.Click += new System.EventHandler(this.FSettings_Click);
            // 
            // ImageCaptureForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(627, 402);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "ImageCaptureForm";
            this.Text = "Image Capture";
            this.FContentPanel.ResumeLayout(false);
            this.FContentPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FPreviewImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FCaptureImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button FCaptureFrame;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox FPreviewImage;
        private System.Windows.Forms.ComboBox FDevices;
        private System.Windows.Forms.PictureBox FCaptureImage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button FSettings;
    }
}