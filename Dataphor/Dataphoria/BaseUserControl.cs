using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
    public partial class BaseUserControl : UserControl, IStatusBarClient
    {
        public BaseUserControl()
        {
            InitializeComponent();
        }
    }
}
