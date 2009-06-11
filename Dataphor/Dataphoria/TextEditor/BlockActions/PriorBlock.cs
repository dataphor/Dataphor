using ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class PriorBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
        {
            if (ATextArea.Document.TextLength > 0)
            {                
                int LPriorBlock = GetPriorBlockOffset(ATextArea);
                ATextArea.AutoClearSelection = true;
                ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LPriorBlock);
                ATextArea.SetDesiredColumn();
            }
        }
    }
}