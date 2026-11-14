using System.Windows.Forms;
namespace Alphora.Dataphor.Dataphoria
{
    partial class BaseForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

       
       
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			this.FToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.FStatusHighlightTimer = new System.Windows.Forms.Timer(this.components);
			this.FBottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.FStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.FStatusStrip = new System.Windows.Forms.StatusStrip();
			this.FBottomToolStripPanel.SuspendLayout();
			this.FStatusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FStatusHighlightTimer
			// 
			this.FStatusHighlightTimer.Interval = 1000;
			this.FStatusHighlightTimer.Tick += new System.EventHandler(this.StatusHighlightTimerTick);
			// 
			// FBottomToolStripPanel
			// 
			this.FBottomToolStripPanel.Controls.Add(this.FStatusStrip);
			this.FBottomToolStripPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.FBottomToolStripPanel.Location = new System.Drawing.Point(0, 242);
			this.FBottomToolStripPanel.Name = "FBottomToolStripPanel";
			this.FBottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.FBottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.FBottomToolStripPanel.Size = new System.Drawing.Size(284, 22);
			// 
			// FStatusLabel
			// 
			this.FStatusLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.FStatusLabel.MergeIndex = 100;
			this.FStatusLabel.Name = "FStatusLabel";
			this.FStatusLabel.Size = new System.Drawing.Size(269, 17);
			this.FStatusLabel.Spring = true;
			this.FStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// FStatusStrip
			// 
			this.FStatusStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.FStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FStatusLabel});
			this.FStatusStrip.Location = new System.Drawing.Point(0, 0);
			this.FStatusStrip.Name = "FStatusStrip";
			this.FStatusStrip.Size = new System.Drawing.Size(284, 22);
			this.FStatusStrip.TabIndex = 0;
			// 
			// BaseForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 264);
			this.Controls.Add(this.FBottomToolStripPanel);
			this.Name = "BaseForm";
			this.TabText = "BaseForm";
			this.Text = "BaseForm";
			this.FBottomToolStripPanel.ResumeLayout(false);
			this.FBottomToolStripPanel.PerformLayout();
			this.FStatusStrip.ResumeLayout(false);
			this.FStatusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

        #endregion

		private System.Windows.Forms.ToolTip FToolTip;
		private Timer FStatusHighlightTimer;
		protected ToolStripPanel FBottomToolStripPanel;
		protected StatusStrip FStatusStrip;
		private ToolStripStatusLabel FStatusLabel;
	}
}