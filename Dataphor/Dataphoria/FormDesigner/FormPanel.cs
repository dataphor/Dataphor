using System;
using System.Drawing;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    public class FormPanel : ContainerControl
    {
        private Form FHostedForm;
        private HScrollBar FHScrollBar;
        private bool FIsOwner;
        private Point FOriginalLocation;
        private VScrollBar FVScrollBar;

        public FormPanel()
        {
            BackColor = SystemColors.ControlDark;

            SuspendLayout();

            FHScrollBar = new HScrollBar();
            FHScrollBar.Dock = DockStyle.Bottom;
            FHScrollBar.SmallChange = 5;
            FHScrollBar.Scroll += HScrollBarScroll;
            Controls.Add(FHScrollBar);

            FVScrollBar = new VScrollBar();
            FVScrollBar.Dock = DockStyle.Right;
            FVScrollBar.SmallChange = 5;
            FVScrollBar.Scroll += VScrollBarScroll;
            Controls.Add(FVScrollBar);

            ResumeLayout(false);
        }

        public Form HostedForm
        {
            get { return FHostedForm; }
        }

        public void SetHostedForm(IWindowsFormInterface AForm, bool AIsOwner)
        {
            InternalClear();
            FHostedForm = (Form) AForm.Form;
            if (FHostedForm != null)
            {
                FIsOwner = AIsOwner;
                if (!AIsOwner)
                    FOriginalLocation = FHostedForm.Location;
                SuspendLayout();
                try
                {
                    AForm.BeginUpdate();
                    try
                    {
                        FHostedForm.TopLevel = false;
                        Controls.Add(FHostedForm);
                        FHostedForm.SendToBack();
                        if (AIsOwner)
                            AForm.Show();
                    }
                    finally
                    {
                        AForm.EndUpdate(false);
                    }
                }
                finally
                {
                    ResumeLayout(true);
                }
            }
        }

        public void ClearHostedForm()
        {
            InternalClear();
            FHostedForm = null;
        }

        private void InternalClear()
        {
            if (FHostedForm != null)
            {
                FHostedForm.Hide();
                Controls.Remove(FHostedForm);
                if (!FIsOwner)
                {
                    FHostedForm.TopLevel = true;
                    FHostedForm.Location = FOriginalLocation;
                    FHostedForm.Show();
                    FHostedForm.BringToFront();
                }
            }
        }

        protected override void OnLayout(LayoutEventArgs AArgs)
        {
            if ((AArgs.AffectedControl == null) || (AArgs.AffectedControl == FHostedForm) ||
                (AArgs.AffectedControl == this))
            {
                // Prepare the "adjusted" clients size
                Size LAdjustedClientSize = ClientSize;
                LAdjustedClientSize.Width -= FVScrollBar.Width;
                LAdjustedClientSize.Height -= FHScrollBar.Height;
                // Ensure a minimum client size to avoid errors setting scrollbar limits etc.
                if (LAdjustedClientSize.Width <= 0)
                    LAdjustedClientSize.Width = 1;
                if (LAdjustedClientSize.Height <= 0)
                    LAdjustedClientSize.Height = 1;

                if (FHostedForm != null)
                {
                    int LMaxValue;

                    LMaxValue = Math.Max(0, FHostedForm.Width - LAdjustedClientSize.Width);
                    if (FHScrollBar.Value > LMaxValue)
                        FHScrollBar.Value = LMaxValue;
                    FHScrollBar.Maximum = Math.Max(0, FHostedForm.Width);
                    FHScrollBar.Visible = (FHScrollBar.Maximum - LAdjustedClientSize.Width) > 0;
                    if (FHScrollBar.Visible)
                        FHScrollBar.LargeChange = LAdjustedClientSize.Width;

                    LMaxValue = Math.Max(0, FHostedForm.Height - LAdjustedClientSize.Height);
                    if (FVScrollBar.Value > LMaxValue)
                        FVScrollBar.Value = LMaxValue;
                    FVScrollBar.Maximum = Math.Max(0, FHostedForm.Height);
                    FVScrollBar.Visible = (FVScrollBar.Maximum - LAdjustedClientSize.Height) > 0;
                    if (FVScrollBar.Visible)
                        FVScrollBar.LargeChange = LAdjustedClientSize.Height;

                    FHostedForm.Location = new Point(-FHScrollBar.Value, -FVScrollBar.Value);
                    FHostedForm.SendToBack();
                }
                else
                {
                    FHScrollBar.Visible = false;
                    FVScrollBar.Visible = false;
                }
            }
            base.OnLayout(AArgs);
        }

        protected override void OnControlAdded(ControlEventArgs AArgs)
        {
            base.OnControlAdded(AArgs);
            AArgs.Control.Move += ControlMove;
        }

        protected override void OnControlRemoved(ControlEventArgs AArgs)
        {
            AArgs.Control.Move -= ControlMove;
            base.OnControlRemoved(AArgs);
        }

        private void ControlMove(object ASender, EventArgs AArgs)
        {
            var LControl = (Control) ASender;
            if ((LControl.IsHandleCreated) && (LControl.Location != Point.Empty))
                PerformLayout();
        }

        private void HScrollBarScroll(object ASender, ScrollEventArgs AArgs)
        {
            PerformLayout(FHostedForm, "Location");
        }

        private void VScrollBarScroll(object ASender, ScrollEventArgs AArgs)
        {
            PerformLayout(FHostedForm, "Location");
        }
    }
}