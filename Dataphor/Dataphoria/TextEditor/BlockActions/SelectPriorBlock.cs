using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
    public class SelectPriorBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
        {
            if (ATextArea.Document.TextLength > 0)
            {
                int LCurrentOffset = ATextArea.Caret.Offset;
                int LStartOffset = GetPriorBlockOffset(ATextArea);

                // Move the caret
                ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LStartOffset);
                ATextArea.SetDesiredColumn();

                // Set or extend the selection
                ATextArea.AutoClearSelection = false;
                if (ATextArea.SelectionManager.HasSomethingSelected)
                {
                    // Extend the selection
                    ATextArea.SelectionManager.ExtendSelection
                        (
                        ATextArea.Document.OffsetToPosition(LCurrentOffset),
                        ATextArea.Document.OffsetToPosition(LStartOffset)
                        );
                }
                else
                {
                    // Select from the current caret position to the beginning of the block
                    ATextArea.SelectionManager.SetSelection
                        (
                        new DefaultSelection
                            (
                            ATextArea.Document,
                            ATextArea.Document.OffsetToPosition(LStartOffset),
                            ATextArea.Document.OffsetToPosition(LCurrentOffset)
                            )
                        );
                }
            }
        }
    }
}