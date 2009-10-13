using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
    public partial class QualityPictureBox : PictureBox
    {
        public QualityPictureBox()
        {
            InitializeComponent();
        }

        System.Drawing.Image originalImage = null;

        public System.Drawing.Image OriginalImage
        {
            get
            {
                return originalImage;
            }
            set
            {                
                originalImage = value;
                if (originalImage != null)
                    Image = GraphicsHelper.BuildThumbnailImage(originalImage, ClientSize);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (originalImage != null)
            {
                Image = GraphicsHelper.BuildThumbnailImage(originalImage, ClientSize);
            }
        }      
    }
}
