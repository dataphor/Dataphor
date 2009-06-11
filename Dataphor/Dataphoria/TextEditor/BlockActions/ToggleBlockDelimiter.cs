using System.Drawing;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class ToggleBlockDelimiter : AbstractEditAction
    {
        public override void Execute(TextArea ATextArea)
        {
            // Add a block delimiter on the current line

            Point LPosition = ATextArea.Caret.Position;
            LineSegment LSegment = ATextArea.Document.GetLineSegment(LPosition.Y);
            string LTextContent = ATextArea.Document.GetText(LSegment);

            if (LTextContent.IndexOf("//*") == 0)
            {
                ATextArea.Document.Remove(LSegment.Offset, 3);
                ATextArea.Refresh(); // Doesn't refresh properly (if on the last line) without this
            }
            else
            {
                ATextArea.Document.Insert(LSegment.Offset, "//*");
                if (LPosition.X == 0)
                {
                    ATextArea.Caret.Position = new Point(3, LPosition.Y);
                    ATextArea.SetDesiredColumn();
                }
            }
        }
    }
}