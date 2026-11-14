using ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class PriorBlock : BaseBlockAction
    {
        public override void Execute(TextArea textArea)
        {
            if (textArea.Document.TextLength > 0)
            {                
                int priorBlock = GetPriorBlockOffset(textArea);
                textArea.AutoClearSelection = true;
                textArea.Caret.Position = textArea.Document.OffsetToPosition(priorBlock);
                textArea.SetDesiredColumn();
            }
        }
    }
}