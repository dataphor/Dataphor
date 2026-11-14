using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class SelectNextBlock : BaseBlockAction
    {
        public override void Execute(TextArea textArea)
        {
            if (textArea.Document.TextLength > 0)
            {
                int currentOffset = textArea.Caret.Offset;
                int endOffset = GetNextBlockOffset(textArea);

                // Move the caret
                textArea.Caret.Position = textArea.Document.OffsetToPosition(endOffset);
                textArea.SetDesiredColumn();

                // Set or extend the selection
                textArea.AutoClearSelection = false;
                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    // Extend the selection
                    textArea.SelectionManager.ExtendSelection
                        (
                        textArea.Document.OffsetToPosition(currentOffset),
                        textArea.Document.OffsetToPosition(endOffset)
                        );
                }
                else
                {
                    // Select from the current position to the end of the block
                    textArea.SelectionManager.SetSelection
                        (
                        new DefaultSelection
                            (
                            textArea.Document,
                            textArea.Document.OffsetToPosition(currentOffset),
                            textArea.Document.OffsetToPosition(endOffset)
                            )
                        );
                }
            }
        }
    }
}