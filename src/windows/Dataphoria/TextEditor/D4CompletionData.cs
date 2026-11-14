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
        
        public D4CompletionData(string text, int imageIndex)
		{
			_text        = text;
			_imageIndex  = imageIndex;
		}

        public D4CompletionData(string text, string description, int imageIndex)
		{
			_text        = text;
			_description = description;
			_imageIndex  = imageIndex;
		}
        
        #region Implementation of ICompletionData

        public virtual bool InsertAction(TextArea textArea, char charValue)
        {
            textArea.InsertString(_text);
            return false;
        }

        private int _imageIndex;
        public int ImageIndex
        {
            get { return _imageIndex; }
            set { _imageIndex = value; }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        private double _priority;
        public double Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        #endregion
    }
}
