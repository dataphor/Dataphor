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
    public partial class ScannersCombobox : ComboBox
    {
        public ScannersCombobox()
        {
            InitializeComponent();
        }
        protected override void OnValidating(CancelEventArgs e)
        {
            base.OnValidating(e);            
            if (SelectedDeviceID == null)
            {
                Localizer.MsgBox(this, "You have to select the scanner, not a technology or '(No scanners installed)'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        [Browsable(false)]
        public ScannerInfo SelectedScannerInfo
        {
            get
            {
                if (DesignMode)
                    return null;
                if (SelectedDeviceTechnology == null)
                    return null;
                try
                {
                    return SelectedDeviceTechnology.GetScannerInfo(SelectedDeviceID);
                }
                catch (Exception ex)
                {
                    Localizer.MsgBox(this, 
    @"Error {0} accessing scanner information. Please, pay attention, that WIA 1.0, SANE and SIS specifications is not yet implemented. 
Try selecting your scnanner device under TWAIN or WIA 2.0 specifications.
If you cannot see your scanner under TWAIN or WIA 2.0, you can try to obtain proper drivers from your device vendor.", "Device error", MessageBoxButtons.OK, MessageBoxIcon.Error, ex.Message);
                    return null;
                }
            }
        }

        [Browsable(false)]
        public string SelectedDeviceID 
        {
            get
            {
                if (DesignMode)
                    return "";
                if (SelectedItem == null)
                    return null;
                ScannerLineGuiHelper sl = (ScannerLineGuiHelper)SelectedItem;
                return sl.DeviceID;
            }
            set
            {
                if (DesignMode)
                    return;
                SelectedItem = ScannerLineGuiHelper.FindScanner(value);
            }
        }

        public IAcquisitionTechnology SelectedDeviceTechnology
        {
            get
            {
                if (DesignMode)
                    return null;
                if (SelectedItem == null)
                    return null;
                ScannerLineGuiHelper sl = (ScannerLineGuiHelper)SelectedItem;
                return sl.Technology;
            }
        }

        protected override void InitLayout()
        {
            base.InitLayout();
            if (!DesignMode)
                DataSource = ScannerLineGuiHelper.GetScanners();
        }
    }

    internal struct ScannerLineGuiHelper
    {

        public override string ToString()
        {
            return DisplayString;
        }

        internal static readonly List<ScannerLineGuiHelper> Scanners = new List<ScannerLineGuiHelper>();

        internal static List<ScannerLineGuiHelper> GetScanners()
        {
            Scanners.Clear();
            IAcquisitionTechnology[] techs = ImageAcquisitionTechnologyFactory.GetSupportedTechnologies();            
            foreach (IAcquisitionTechnology t in techs)
            {
                AddScannerLine(t, null, null);
                bool ThereIsScanner = false;
                foreach (ScannerInfo s in t.GetInstalledScanners())
                {
                    ThereIsScanner = true;
                    AddScannerLine(t, s.DisplayName, s.DeviceID);
                }
                if (!ThereIsScanner)
                    AddScannerLine(t, "NOSCANNERS", null);
            }
            return Scanners;
        }

        private static void AddScannerLine(IAcquisitionTechnology technology, string deviceName, string deviceId)
        {
            ScannerLineGuiHelper line = new ScannerLineGuiHelper();
            if (deviceName == null)
                line.DisplayString = string.Format("• Supported by {0}", technology.Name);
            else if (deviceName == "NOSCANNERS")
                line.DisplayString = "        (No scanners installed)";
            else
                line.DisplayString = string.Format("        {0}", deviceName);
            line.DeviceID = deviceId;
            line.Technology = technology;
            Scanners.Add(line);
        }

        internal static ScannerLineGuiHelper? FindScanner(string which)
        {
            foreach (ScannerLineGuiHelper s in Scanners)
            {
                if (s.DeviceID == which)
                    return s;
            }
            return null;
        }

        internal string DisplayString;
        internal string DeviceID; // if not technology line, otherwise null
        internal IAcquisitionTechnology Technology; 
    }
}
