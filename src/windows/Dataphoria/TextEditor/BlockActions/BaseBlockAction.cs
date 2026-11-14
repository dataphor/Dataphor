using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public abstract class BaseBlockAction : AbstractEditAction
    {
        protected int GetPriorBlockOffset(TextArea textArea)
        {
            string textContent = textArea.Document.TextContent.Substring(0, textArea.Caret.Offset);
            int priorBlock = textContent.LastIndexOf("//*");
            if (priorBlock < 0)
                priorBlock = 0;

            return priorBlock;
        }

        protected int GetNextBlockOffset(TextArea textArea)
        {
            bool atBlockStart;
            return GetNextBlockOffset(textArea, out atBlockStart);
        }

        protected int GetNextBlockOffset(TextArea textArea, out bool atBlockStart)
        {
            atBlockStart = false;
            string textContent = textArea.Document.TextContent.Substring(textArea.Caret.Offset);
            int nextBlock = textContent.IndexOf("//*");
            if (nextBlock == 0)
            {
                atBlockStart = true;
                textContent = textContent.Substring(nextBlock + 3);
                nextBlock = textContent.IndexOf("//*");
                if (nextBlock >= 0)
                    nextBlock += 3;
            }

            if (nextBlock < 0)
                nextBlock = textArea.Document.TextLength;
            else
                nextBlock = textArea.Caret.Offset + nextBlock;

            return nextBlock;
        }
    }
}