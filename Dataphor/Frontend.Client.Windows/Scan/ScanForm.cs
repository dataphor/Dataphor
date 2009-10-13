using System;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ImageAcquisitionTAL;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using Alphora.Dataphor.Frontend.Client.Windows.Properties;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
    public partial class ScanForm : DialogForm, IImageSource
    {
        public ScanForm()
        {
            InitializeComponent();
            SuspendLayout();

            try
            {
                SetAcceptReject(true, false);
            }
            finally
            {
                ResumeLayout(false);
            }
        }

        private bool FLoading;
        public bool Loading
        {
            get { return FLoading; }
        }

        public void LoadImage()
        {
            FLoading = true;
            if (ShowDialog() != DialogResult.OK)
                throw new AbortException();
        }

        private MemoryStream FStream;
        public Stream Stream
        {
            get { return FStream; }
        }

        public event EventHandler GotImage;

        public System.Drawing.Image ScannedImage
        {
            get
            {
                return pictureBox3.OriginalImage;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            AcquireImage(true, false);
        }

        private void DoGotImage()
        {
            if (GotImage != null)
                GotImage(this, EventArgs.Empty);
        }

        private void scanallFromFeederToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AcquireImage(false, false);
            if (FStream != null)
            {
                FStream.Close();
                FStream = null;
            }
            FStream = new MemoryStream();
            pictureBox3.Image.Save(FStream, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        private void AcquireImage(bool fromGlass, bool forPreview)
        {
            System.Drawing.Image img = null;

            stopScanClicked = false;
            ScannerInfo sinfo = scannersCombobox2.SelectedScannerInfo;
            sinfo.CurrentResolution = resolutionsCombobox2.SelectedResolution;
            sinfo.CurrentColorMode = colorsCombobox2.SelectedColorMode;
            sinfo.CurrentSource = fromGlass | forPreview ? ScanSource.Flatbed : ScanSource.Feeder;
            scanningSession = scannersCombobox2.SelectedDeviceTechnology.BeginAcquire(sinfo);
            do
            {
                try
                {
                    if (forPreview)
                        img = scannersCombobox2.SelectedDeviceTechnology.AcquireForPreview(sinfo);
                    else
                        img = scannersCombobox2.SelectedDeviceTechnology.Acquire(sinfo);
                }
                catch (Exception ex)
                {
                    Localizer.MsgBox(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
                if (img != null)
                {
                    pictureBox3.OriginalImage = img;
                    if (!forPreview)
                        DoGotImage();
                }
                Application.DoEvents();
            } while (img != null && !fromGlass && !forPreview && !stopScanClicked); // if img = null -> no more pages to scan
            scannersCombobox2.SelectedDeviceTechnology.EndAcquire(scanningSession);
        }

        object scanningSession = null;

        private void scannersCombobox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool IsDevice = scannersCombobox2.SelectedDeviceID != null;
            previewToolStripButton.Enabled = IsDevice;
            StartToolStripButton.Enabled = IsDevice;
            StopToolStripButton1.Enabled = IsDevice;
            if (IsDevice)
            {
                ScannerLineGuiHelper line = (ScannerLineGuiHelper)scannersCombobox2.SelectedItem;
                ScannerInfo scanner = line.Technology.GetScannerInfo(line.DeviceID);
                scanallFromFeederToolStripMenuItem.Enabled = scanner.SupportsFeeder;
                StopToolStripButton1.Enabled = scanner.SupportsFeeder;
                resolutionsCombobox2.Resolutions = scanner.SupportedResolutions;
                resolutionsCombobox2.SelectedResolution = scanner.CurrentResolution;
                colorsCombobox2.SetColorsFromScannerInfo(scanner);
                colorsCombobox2.SelectedColorMode = scanner.CurrentColorMode;
            }
            this.Activate(); // this is because of some ugly splash screens the datasource can present on start
        }

        private void previewToolStripButton_Click(object sender, EventArgs e)
        {
            AcquireImage(true, true);
        }

        private void AdvancedtoolStripButton_Click(object sender, EventArgs e)
        {
            if (AdvancedtoolStripButton.Checked)
                ExplodePanel();
            else
                ImplodePanel();
        }

        private void ImplodePanel()
        {
            for (int i = 10; i <= 190; i++)
            {
                this.Height = 585 - i;
                Application.DoEvents();
            }
        }

        private void ExplodePanel()
        {
            for (int i = 10; i <= 190; i++)
            {
                this.Height = 585 - 190 + i;
                Application.DoEvents();
            }
        }

        private void ScanForm_Load(object sender, EventArgs e)
        {
            Settings settings = Settings.Default;
            scannersCombobox2.SelectedDeviceID = settings.DefaultScanner;
            colorsCombobox2.SelectedColorMode = settings.DefaultColorMode;
            resolutionsCombobox2.SelectedResolution = settings.DefaultResolution;
            Cursor = Cursors.Default;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Settings settings = Settings.Default;
            settings.DefaultScanner = scannersCombobox2.SelectedDeviceID;
            settings.DefaultColorMode = colorsCombobox2.SelectedColorMode;
            settings.DefaultResolution = resolutionsCombobox2.SelectedResolution;
            settings.Save();
        }

        bool stopScanClicked = false;

        private void StopToolStripButton1_Click(object sender, EventArgs e)
        {
            stopScanClicked = true;
        }

        private void FContentPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        protected override void OnClosing(CancelEventArgs AArgs)
        {
            base.OnClosing(AArgs);
            AArgs.Cancel = false;
            try
            {
                if (DialogResult == DialogResult.OK)
                {
                    try
                    {
                        PostChanges();
                    }
                    catch
                    {
                        AArgs.Cancel = true;
                        throw;
                    }
                }
                else
                    CancelChanges();
            }
            catch (Exception AException)
            {
                Session.HandleException(AException);
            }
            finally
            {
                FLoading = false;
            }
        }
        private void CancelChanges()
        {
            pictureBox3.Image = null;
        }

        private void PostChanges()
        {
            if (pictureBox3.Image == null)
            {
                System.Media.SystemSounds.Beep.Play();
                throw new AbortException();
            }
        }  
    }
    public static class GraphicsHelper
    {
        public static System.Drawing.Image BuildThumbnailImage(System.Drawing.Image originalImage, Size size)
        {
            Size zsize = GetZoomedSize(originalImage, size);
            if (zsize.IsEmpty)
                return null;
            System.Drawing.Image thumb = new Bitmap(zsize.Width, zsize.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(thumb);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle rect = new Rectangle(Point.Empty, zsize);
            g.DrawImage(originalImage, rect);

            return thumb;
        }

        public static System.Drawing.Image BuildThumbnailImage(System.Drawing.Image originalImage, SizeF size)
        {
            SizeF zsize = GetZoomedSize(originalImage, size);
            if (zsize.IsEmpty)
                return null;
            Size zsizePix = new Size(
                (int)Math.Round(zsize.Width * originalImage.HorizontalResolution),
                (int)Math.Round(zsize.Height * originalImage.VerticalResolution));
            System.Drawing.Image thumb = new Bitmap(zsizePix.Width, zsizePix.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(thumb);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            RectangleF rect = new RectangleF(PointF.Empty, zsizePix);
            g.DrawImage(originalImage, rect);

            return thumb;
        }

        public static Size GetZoomedSize(System.Drawing.Image image, Size clientSize)
        {
            float Wrate = (float)clientSize.Width / image.Size.Width;
            float Hrate = (float)clientSize.Height / image.Size.Height;
            float rate = Math.Min(Wrate, Hrate);
            Size size = new Size();
            size.Width = (int)Math.Round(image.Width * rate);
            size.Height = (int)Math.Round(image.Height * rate);
            return size;
        }

        public static Size GetZoomedSizeForPrinter(System.Drawing.Image image, Size clientSize, float xDpi, float yDpi)
        {
            // printer unit is inch/100
            float Wrate = ((float)clientSize.Width / 100f) * xDpi / image.Size.Width;
            float Hrate = ((float)clientSize.Height / 100f) * yDpi / image.Size.Height;
            float rate = Math.Min(Wrate, Hrate);
            Size size = new Size();
            size.Width = (int)Math.Round(image.Width * rate / xDpi * 100);
            size.Height = (int)Math.Round(image.Height * rate / yDpi * 100);
            return size;
        }

        public static SizeF GetZoomedSize(System.Drawing.Image image, SizeF clientSize)
        {
            float imgWidth = image.Size.Width / image.HorizontalResolution;
            float imgHeight = image.Size.Height / image.VerticalResolution;
            float Wrate = clientSize.Width / imgWidth; // in inch
            float Hrate = clientSize.Height / imgHeight; // in inch
            float rate = Math.Min(Wrate, Hrate);
            SizeF size = new SizeF();
            size.Width = imgWidth * rate;
            size.Height = imgHeight * rate;
            return size;
        }
    }
    public static class Localizer
    {
        public static string GetStr(string text, params object[] textParams)
        {
            string locText = Properties.Resources.ResourceManager.GetString(CalcKey(text));
            if (string.IsNullOrEmpty(locText))
                locText = text;
            return string.Format(locText, textParams);
        }

        public static DialogResult MsgBox(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButton, params object[] textParams)
        {
            if (Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft)
                return MessageBox.Show(owner, GetStr(text, textParams), GetStr(caption), buttons, icon, defButton, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            else
                return MessageBox.Show(owner, GetStr(text, textParams), GetStr(caption), buttons, icon, defButton);
        }

        public static DialogResult MsgBox(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon, params object[] textParams)
        {
            return MsgBox(owner, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, textParams);
        }

        public static string CalcKey(string t)
        {
            t = t.Replace("\n", "\\n");
            t = t.Replace("\r", "\\r");
            if (string.IsNullOrEmpty(t))
                return "";
            string key = "K";
            long s = 0;
            foreach (char ch in t)
            {
                s += (int)ch;
            }
            return key + s.ToString();
        }
    }
}
