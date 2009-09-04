using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
    public interface IStatusBarClient
    {
        void MergeStatusBarWith(StatusStrip AStatusBar);        
    }
}
