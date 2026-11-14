using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class SelectPriorBlock : BaseBlockAction
    {
        public override void Execute(TextArea textArea)
        {
            if (textArea.Document.TextLength > 0)
            {
                int currentOffset = textArea.Caret.Offset;
                int startOffset = GetPriorBlockOffset(textArea);

                // Move the caret
                textArea.Caret.Position = textArea.Document.OffsetToPosition(startOffset);
                textArea.SetDesiredColumn();

                // Set or extend the selection
                textArea.AutoClearSelection = false;
                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    // Extend the selection
                    textArea.SelectionManager.ExtendSelection
                        (
                        textArea.Document.OffsetToPosition(currentOffset),
                        textArea.Document.OffsetToPosition(startOffset)
                        );
                }
                else
                {
                    // Select from the current caret position to the beginning of the block
                    textArea.SelectionManager.SetSelection
                        (
                        new DefaultSelection
                            (
                            textArea.Document,
                            textArea.Document.OffsetToPosition(startOffset),
                            textArea.Document.OffsetToPosition(currentOffset)
                            )
                        );
                }
            }
        }
    }
}