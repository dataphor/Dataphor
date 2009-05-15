using System;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string ADescription)
        {
            Description = ADescription;
        }

        public string Description { get; private set; }
    }
}