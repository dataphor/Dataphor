namespace Alphora.Dataphor.Frontend.Client.Windows
{
    partial class ScanForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScanForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.previewToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.StartToolStripButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.scanonePageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scanallFromFeederToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StopToolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.AdvancedtoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.colorsCombobox2 = new Alphora.Dataphor.Frontend.Client.Windows.ColorsComboBox();
            this.scannersCombobox2 = new Alphora.Dataphor.Frontend.Client.Windows.ScannersCombobox();
            this.resolutionsCombobox2 = new Alphora.Dataphor.Frontend.Client.Windows.ResolutionsComboBox();
            this.pictureBox3 = new Alphora.Dataphor.Frontend.Client.Windows.QualityPictureBox();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel1 = new System.Windows.Forms.ToolStripContentPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.FContentPanel.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // FContentPanel
            // 
            this.FContentPanel.AutoScroll = true;
            this.FContentPanel.Dock = System.Windows.Forms.DockStyle.None;
            this.FContentPanel.Location = new System.Drawing.Point(396, 239);
            this.FContentPanel.Size = new System.Drawing.Size(18, 16);
            this.FContentPanel.Visible = false;
            this.FContentPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.FContentPanel_Paint);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.previewToolStripButton,
            this.StartToolStripButton,
            this.StopToolStripButton1,
            this.toolStripSeparator1,
            this.AdvancedtoolStripButton});
            this.toolStrip1.Location = new System.Drawing.Point(137, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(223, 25);
            this.toolStrip1.TabIndex = 18;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // previewToolStripButton
            // 
            this.previewToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("previewToolStripButton.Image")));
            this.previewToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.previewToolStripButton.Name = "previewToolStripButton";
            this.previewToolStripButton.Size = new System.Drawing.Size(65, 22);
            this.previewToolStripButton.Text = "&Preview";
            this.previewToolStripButton.Click += new System.EventHandler(this.previewToolStripButton_Click);
            // 
            // StartToolStripButton
            // 
            this.StartToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.scanonePageToolStripMenuItem,
            this.scanallFromFeederToolStripMenuItem});
            this.StartToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("StartToolStripButton.Image")));
            this.StartToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.StartToolStripButton.Name = "StartToolStripButton";
            this.StartToolStripButton.Size = new System.Drawing.Size(60, 22);
            this.StartToolStripButton.Text = "&Start";
            this.StartToolStripButton.ToolTipText = "Start scanning";
            // 
            // scanonePageToolStripMenuItem
            // 
            this.scanonePageToolStripMenuItem.Name = "scanonePageToolStripMenuItem";
            this.scanonePageToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
            this.scanonePageToolStripMenuItem.Text = "Scan &one page (from flatbed)";
            this.scanonePageToolStripMenuItem.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // scanallFromFeederToolStripMenuItem
            // 
            this.scanallFromFeederToolStripMenuItem.Name = "scanallFromFeederToolStripMenuItem";
            this.scanallFromFeederToolStripMenuItem.Size = new System.Drawing.Size(215, 22);
            this.scanallFromFeederToolStripMenuItem.Text = "Scan &all (from feeder)";
            this.scanallFromFeederToolStripMenuItem.Click += new System.EventHandler(this.scanallFromFeederToolStripMenuItem_Click);
            // 
            // StopToolStripButton1
            // 
            this.StopToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.StopToolStripButton1.Image = global::Alphora.Dataphor.Frontend.Client.Windows.Properties.Resources.Scanner;
            this.StopToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.StopToolStripButton1.Name = "StopToolStripButton1";
            this.StopToolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.StopToolStripButton1.Text = "&Cancel";
            this.StopToolStripButton1.ToolTipText = "Stop scanning from feeder";
            this.StopToolStripButton1.Click += new System.EventHandler(this.StopToolStripButton1_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // AdvancedtoolStripButton
            // 
            this.AdvancedtoolStripButton.CheckOnClick = true;
            this.AdvancedtoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.AdvancedtoolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("AdvancedtoolStripButton.Image")));
            this.AdvancedtoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.AdvancedtoolStripButton.Name = "AdvancedtoolStripButton";
            this.AdvancedtoolStripButton.Size = new System.Drawing.Size(50, 17);
            this.AdvancedtoolStripButton.Text = "S&ettings";
            this.AdvancedtoolStripButton.Click += new System.EventHandler(this.AdvancedtoolStripButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(37, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Select scanner or camera:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(34, 109);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "Select color mode:";
            // 
            // button1
            // 
            this.button1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button1.Location = new System.Drawing.Point(232, 155);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(128, 23);
            this.button1.TabIndex = 31;
            this.button1.Text = "Save these settings";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // colorsCombobox2
            // 
            this.colorsCombobox2.Colors = ImageAcquisitionTAL.ColorMode.None;
            this.colorsCombobox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.colorsCombobox2.FormattingEnabled = true;
            this.colorsCombobox2.Location = new System.Drawing.Point(37, 127);
            this.colorsCombobox2.Name = "colorsCombobox2";
            this.colorsCombobox2.SelectedColorMode = ImageAcquisitionTAL.ColorMode.BW;
            this.colorsCombobox2.Size = new System.Drawing.Size(323, 21);
            this.colorsCombobox2.TabIndex = 32;
            // 
            // scannersCombobox2
            // 
            this.scannersCombobox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.scannersCombobox2.FormattingEnabled = true;
            this.scannersCombobox2.Location = new System.Drawing.Point(36, 43);
            this.scannersCombobox2.Name = "scannersCombobox2";
            this.scannersCombobox2.SelectedDeviceID = "";
            this.scannersCombobox2.Size = new System.Drawing.Size(324, 21);
            this.scannersCombobox2.TabIndex = 33;
            this.scannersCombobox2.SelectedIndexChanged += new System.EventHandler(this.scannersCombobox2_SelectedIndexChanged);
            // 
            // resolutionsCombobox2
            // 
            this.resolutionsCombobox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.resolutionsCombobox2.FormattingEnabled = true;
            this.resolutionsCombobox2.Location = new System.Drawing.Point(37, 85);
            this.resolutionsCombobox2.Name = "resolutionsCombobox2";
            this.resolutionsCombobox2.Resolutions = null;
            this.resolutionsCombobox2.SelectedResolution = 200;
            this.resolutionsCombobox2.Size = new System.Drawing.Size(323, 21);
            this.resolutionsCombobox2.TabIndex = 34;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox3.BackColor = System.Drawing.SystemColors.Control;
            this.pictureBox3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBox3.Location = new System.Drawing.Point(0, 48);
            this.pictureBox3.MinimumSize = new System.Drawing.Size(4, 4);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.OriginalImage = null;
            this.pictureBox3.Size = new System.Drawing.Size(412, 301);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 17;
            this.pictureBox3.TabStop = false;
            this.pictureBox3.Click += new System.EventHandler(this.pictureBox3_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(34, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(135, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Select scanning resolution:";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Alphora.Dataphor.Frontend.Client.Windows.Properties.Resources.Scanner;
            this.pictureBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBox1.Location = new System.Drawing.Point(363, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            // 
            // BottomToolStripPanel
            // 
            this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // TopToolStripPanel
            // 
            this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // RightToolStripPanel
            // 
            this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // ContentPanel1
            // 
            this.ContentPanel1.Size = new System.Drawing.Size(125, 150);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.scannersCombobox2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.colorsCombobox2);
            this.panel1.Controls.Add(this.resolutionsCombobox2);
            this.panel1.Location = new System.Drawing.Point(0, 48);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(411, 190);
            this.panel1.TabIndex = 19;
            // 
            // ScanForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 370);
            this.Controls.Add(this.pictureBox3);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "ScanForm";
            this.ShowInTaskbar = false;
            this.Text = "Scan page(s)";
            this.Load += new System.EventHandler(this.ScanForm_Load);
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.pictureBox1, 0);
            this.Controls.SetChildIndex(this.toolStrip1, 0);
            this.Controls.SetChildIndex(this.pictureBox3, 0);
            this.Controls.SetChildIndex(this.FContentPanel, 0);
            this.FContentPanel.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton previewToolStripButton;
        private System.Windows.Forms.ToolStripDropDownButton StartToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem scanonePageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scanallFromFeederToolStripMenuItem;
        private ResolutionsComboBox resolutionsCombobox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton AdvancedtoolStripButton;
        private ColorsComboBox colorsCombobox2;
        private ScannersCombobox scannersCombobox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolStripButton StopToolStripButton1;
        private QualityPictureBox pictureBox3;
        private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
        private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
        private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
        private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
        private System.Windows.Forms.ToolStripContentPanel ContentPanel1;
        private System.Windows.Forms.Panel panel1;
    }
}
