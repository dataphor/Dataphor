using System;
using System.Drawing;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    public class FormPanel : ContainerControl
    {
        private Form _hostedForm;
        private HScrollBar _hScrollBar;
        private bool _isOwner;
        private Point _originalLocation;
        private VScrollBar _vScrollBar;

        public FormPanel()
        {
            BackColor = SystemColors.ControlDark;

            SuspendLayout();

            _hScrollBar = new HScrollBar();
            _hScrollBar.Dock = DockStyle.Bottom;
            _hScrollBar.SmallChange = 5;
            _hScrollBar.Scroll += HScrollBarScroll;
            Controls.Add(_hScrollBar);

            _vScrollBar = new VScrollBar();
            _vScrollBar.Dock = DockStyle.Right;
            _vScrollBar.SmallChange = 5;
            _vScrollBar.Scroll += VScrollBarScroll;
            Controls.Add(_vScrollBar);

            ResumeLayout(false);
        }

        public Form HostedForm
        {
            get { return _hostedForm; }
        }

        public void SetHostedForm(IWindowsFormInterface form, bool isOwner)
        {
            InternalClear();
            _hostedForm = (Form) form.Form;
            if (_hostedForm != null)
            {
                _isOwner = isOwner;
                if (!isOwner)
                    _originalLocation = _hostedForm.Location;
                SuspendLayout();
                try
                {
                    form.BeginUpdate();
                    try
                    {
                        _hostedForm.TopLevel = false;
                        Controls.Add(_hostedForm);
                        _hostedForm.SendToBack();
                        if (isOwner)
                            form.Show();
                    }
                    finally
                    {
                        form.EndUpdate(false);
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
            _hostedForm = null;
        }

        private void InternalClear()
        {
            if (_hostedForm != null)
            {
                _hostedForm.Hide();
                Controls.Remove(_hostedForm);
                if (!_isOwner)
                {
                    _hostedForm.TopLevel = true;
                    _hostedForm.Location = _originalLocation;
                    _hostedForm.Show();
                    _hostedForm.BringToFront();
                }
            }
        }

        protected override void OnLayout(LayoutEventArgs args)
        {
            if ((args.AffectedControl == null) || (args.AffectedControl == _hostedForm) ||
                (args.AffectedControl == this))
            {
                // Prepare the "adjusted" clients size
                Size adjustedClientSize = ClientSize;
                adjustedClientSize.Width -= _vScrollBar.Width;
                adjustedClientSize.Height -= _hScrollBar.Height;
                // Ensure a minimum client size to avoid errors setting scrollbar limits etc.
                if (adjustedClientSize.Width <= 0)
                    adjustedClientSize.Width = 1;
                if (adjustedClientSize.Height <= 0)
                    adjustedClientSize.Height = 1;

                if (_hostedForm != null)
                {
                    int maxValue;

                    maxValue = Math.Max(0, _hostedForm.Width - adjustedClientSize.Width);
                    if (_hScrollBar.Value > maxValue)
                        _hScrollBar.Value = maxValue;
                    _hScrollBar.Maximum = Math.Max(0, _hostedForm.Width);
                    _hScrollBar.Visible = (_hScrollBar.Maximum - adjustedClientSize.Width) > 0;
                    if (_hScrollBar.Visible)
                        _hScrollBar.LargeChange = adjustedClientSize.Width;

                    maxValue = Math.Max(0, _hostedForm.Height - adjustedClientSize.Height);
                    if (_vScrollBar.Value > maxValue)
                        _vScrollBar.Value = maxValue;
                    _vScrollBar.Maximum = Math.Max(0, _hostedForm.Height);
                    _vScrollBar.Visible = (_vScrollBar.Maximum - adjustedClientSize.Height) > 0;
                    if (_vScrollBar.Visible)
                        _vScrollBar.LargeChange = adjustedClientSize.Height;

                    _hostedForm.Location = new Point(-_hScrollBar.Value, -_vScrollBar.Value);
                    _hostedForm.SendToBack();
                }
                else
                {
                    _hScrollBar.Visible = false;
                    _vScrollBar.Visible = false;
                }
            }
            base.OnLayout(args);
        }

        protected override void OnControlAdded(ControlEventArgs args)
        {
            base.OnControlAdded(args);
            args.Control.Move += ControlMove;
        }

        protected override void OnControlRemoved(ControlEventArgs args)
        {
            args.Control.Move -= ControlMove;
            base.OnControlRemoved(args);
        }

        private void ControlMove(object sender, EventArgs args)
        {
            var control = (Control) sender;
            if ((control.IsHandleCreated) && (control.Location != Point.Empty))
                PerformLayout();
        }

        private void HScrollBarScroll(object sender, ScrollEventArgs args)
        {
            PerformLayout(_hostedForm, "Location");
        }

        private void VScrollBarScroll(object sender, ScrollEventArgs args)
        {
            PerformLayout(_hostedForm, "Location");
        }
    }
}