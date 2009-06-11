using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
    public class SelectBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
        {
            if (!ATextArea.SelectionManager.HasSomethingSelected && (ATextArea.Document.TextLength > 0))
            {
                int LCurrentOffset = ATextArea.Caret.Offset;
                bool LAtBlockStart;
                int LEndOffset = GetNextBlockOffset(ATextArea, out LAtBlockStart);
                int LStartOffset;
                if (LAtBlockStart)
                    LStartOffset = LCurrentOffset;
                else
                    LStartOffset = GetPriorBlockOffset(ATextArea);

                ATextArea.SelectionManager.SetSelection
                    (
                    new DefaultSelection
                        (
                        ATextArea.Document,
                        ATextArea.Document.OffsetToPosition(LStartOffset),
                        ATextArea.Document.OffsetToPosition(LEndOffset)
                        )
                    );
            }
        }
    }
}