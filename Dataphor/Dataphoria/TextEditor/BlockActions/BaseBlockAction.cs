using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public abstract class BaseBlockAction : AbstractEditAction
    {
        protected int GetPriorBlockOffset(TextArea ATextArea)
        {
            string LTextContent = ATextArea.Document.TextContent.Substring(0, ATextArea.Caret.Offset);
            int LPriorBlock = LTextContent.LastIndexOf("//*");
            if (LPriorBlock < 0)
                LPriorBlock = 0;

            return LPriorBlock;
        }

        protected int GetNextBlockOffset(TextArea ATextArea)
        {
            bool LAtBlockStart;
            return GetNextBlockOffset(ATextArea, out LAtBlockStart);
        }

        protected int GetNextBlockOffset(TextArea ATextArea, out bool AAtBlockStart)
        {
            AAtBlockStart = false;
            string LTextContent = ATextArea.Document.TextContent.Substring(ATextArea.Caret.Offset);
            int LNextBlock = LTextContent.IndexOf("//*");
            if (LNextBlock == 0)
            {
                AAtBlockStart = true;
                LTextContent = LTextContent.Substring(LNextBlock + 3);
                LNextBlock = LTextContent.IndexOf("//*");
                if (LNextBlock >= 0)
                    LNextBlock += 3;
            }

            if (LNextBlock < 0)
                LNextBlock = ATextArea.Document.TextLength;
            else
                LNextBlock = ATextArea.Caret.Offset + LNextBlock;

            return LNextBlock;
        }
    }
}