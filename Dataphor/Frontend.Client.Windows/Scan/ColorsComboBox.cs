using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ImageAcquisitionTAL;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
    public partial class ColorsComboBox : ComboBox
    {
        public ColorsComboBox()
        {
            InitializeComponent();
        }

        public ColorMode SelectedColorMode
        {
            get
            {
                if (SelectedItem == null)
                    return ColorMode.BW;
                else
                    return (ColorMode)SelectedItem;
            }
            set
            {
                SelectedItem = value;
            }
        }

        ColorMode colors;

        public ColorMode Colors
        {
            get { return colors; }
            set 
            { 
                colors = value; 
                Items.Clear();
                IfAddColorMode(value, ColorMode.BW);
                IfAddColorMode(value, ColorMode.Color);
                IfAddColorMode(value, ColorMode.GrayScale);
                SelectedColorMode = ColorMode.BW; // try BW, leave selected index 0 if no BW in list
            }
        }

        public void SetColorsFromScannerInfo(ScannerInfo sinfo)
        {
            Colors |= sinfo.SupportsBW ? ColorMode.BW : 0;
            Colors |= sinfo.SupportsColors ? ColorMode.Color : 0;
            Colors |= sinfo.SupportsGrayScale ? ColorMode.GrayScale : 0;
        }

        private void IfAddColorMode(ColorMode analyseValue, ColorMode colorMode)
        {
            if ((analyseValue & colorMode) == colorMode)
                Items.Add(colorMode);
        }

        protected override void OnFormat(ListControlConvertEventArgs e)
        {
            // Add " DPI"
            e.Value += " DPI";
            // Add user friendly blah-blah-blah to well-known values
            switch ((ColorMode)e.ListItem)
            {
                case ColorMode.BW :
                    e.Value = "Black and white only (minimal space)";
                    break;
                case ColorMode.Color :
                    e.Value = "Full color documents (maximum space)";
                    break;
                case ColorMode.GrayScale :
                    e.Value = "Gray scale documents";
                    break;
            }
        }
    }
}
