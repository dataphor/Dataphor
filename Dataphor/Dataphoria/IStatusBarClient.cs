using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
    public interface IStatusBarClient
    {
        void Merge(Control AStatusBar);
        void Unmerge(Control AStatusBar);
    }
}
