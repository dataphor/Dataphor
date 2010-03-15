using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
    class D4CompletionData : ICompletionData
    {
        
        public D4CompletionData(string AText, int AImageIndex)
		{
			FText        = AText;
			FImageIndex  = AImageIndex;
		}

        public D4CompletionData(string AText, string ADescription, int AImageIndex)
		{
			FText        = AText;
			FDescription = ADescription;
			FImageIndex  = AImageIndex;
		}
        
        #region Implementation of ICompletionData

        public virtual bool InsertAction(TextArea ATextArea, char AChar)
        {
            ATextArea.InsertString(FText);
            return false;
        }

        private int FImageIndex;
        public int ImageIndex
        {
            get { return FImageIndex; }
            set { FImageIndex = value; }
        }

        private string FText;
        public string Text
        {
            get { return FText; }
            set { FText = value; }
        }

        private string FDescription;
        public string Description
        {
            get { return FDescription; }
            set { FDescription = value; }
        }

        private double FPriority;
        public double Priority
        {
            get { return FPriority; }
            set { FPriority = value; }
        }

        #endregion
    }
}
