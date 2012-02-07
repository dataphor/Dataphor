using System.Drawing;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria.TextEditor.BlockActions
{
    public class ToggleBlockDelimiter : AbstractEditAction
    {
        public override void Execute(TextArea textArea)
        {
            // Add a block delimiter on the current line

            TextLocation position = textArea.Caret.Position;
            LineSegment segment = textArea.Document.GetLineSegment(position.Y);
            string textContent = textArea.Document.GetText(segment);

            if (textContent.IndexOf("//*") == 0)
            {
                textArea.Document.Remove(segment.Offset, 3);
                textArea.Refresh(); // Doesn't refresh properly (if on the last line) without this
            }
            else
            {
                textArea.Document.Insert(segment.Offset, "//*");
                if (position.X == 0)
                {
                    textArea.Caret.Position = new TextLocation(3, position.Y);
                    textArea.SetDesiredColumn();
                }
            }
        }
    }
}