using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class SelectBlock : BaseBlockAction
    {
        public override void Execute(TextArea textArea)
        {
            if (!textArea.SelectionManager.HasSomethingSelected && (textArea.Document.TextLength > 0))
            {
                int currentOffset = textArea.Caret.Offset;
                bool atBlockStart;
                int endOffset = GetNextBlockOffset(textArea, out atBlockStart);
                int startOffset;
                if (atBlockStart)
                    startOffset = currentOffset;
                else
                    startOffset = GetPriorBlockOffset(textArea);

                textArea.SelectionManager.SetSelection
                    (
                    new DefaultSelection
                        (
                        textArea.Document,
                        textArea.Document.OffsetToPosition(startOffset),
                        textArea.Document.OffsetToPosition(endOffset)
                        )
                    );
            }
        }
    }
}