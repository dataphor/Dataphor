
using ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class NextBlock : BaseBlockAction
    {
        public override void Execute(TextArea textArea)
        {
            if (textArea.Document.TextLength > 0)
            {                
                int nextBlock = GetNextBlockOffset(textArea);
                textArea.AutoClearSelection = true;
                textArea.Caret.Position = textArea.Document.OffsetToPosition(nextBlock);
                textArea.SetDesiredColumn();
            }
        }
    }
}