using System.Drawing;

using Alphora.Dataphor.DAE.Debug;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public class BreakpointBookmark : DebugBookmark
	{
		public BreakpointBookmark(IDocument ADocument, TextLocation ALocation, DebugLocator ALocator)
			: base(ADocument, ALocation, ALocator)
		{
		}

		static readonly Color CMarkerColor = Color.FromArgb(180, 38, 38);

		public override void Draw(IconBarMargin margin, Graphics g, Point p)
		{
			margin.DrawBreakpoint(g, p.Y, IsEnabled, true);
		}

		protected override TextMarker CreateMarker()
		{
			LineSegment ALineSeg = Anchor.Line;
			TextMarker AMarker = new TextMarker(ALineSeg.Offset, ALineSeg.Length, TextMarkerType.SolidBlock, CMarkerColor, Color.White);
			Document.MarkerStrategy.AddMarker(AMarker);
			return AMarker;
		}
	}
}
