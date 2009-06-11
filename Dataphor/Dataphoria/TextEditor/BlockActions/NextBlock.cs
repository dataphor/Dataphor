
using ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class NextBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
        {
            if (ATextArea.Document.TextLength > 0)
            {                
                int LNextBlock = GetNextBlockOffset(ATextArea);
                ATextArea.AutoClearSelection = true;
                ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LNextBlock);
                ATextArea.SetDesiredColumn();
            }
        }
    }
}