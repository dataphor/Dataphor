using System;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string description)
        {
            Description = description;
        }

        public string Description { get; private set; }
    }
}