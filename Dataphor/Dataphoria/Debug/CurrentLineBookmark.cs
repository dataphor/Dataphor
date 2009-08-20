using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace Alphora.Dataphor.Dataphoria
{
	public class CurrentLineBookmark : DebugBookmark
	{
		public CurrentLineBookmark(string ALocator, IDocument ADocument, TextLocation ALocation)
			: base(ALocator, ADocument, ALocation)
		{
		}

		public override bool CanToggle
		{
			get { return false; }
		}

		public override void Draw(IconBarMargin margin, Graphics g, Point p)
		{
			margin.DrawArrow(g, p.Y);
		}

		protected override TextMarker CreateMarker()
		{
			LineSegment LLineSeg = Document.GetLineSegment(LineNumber);
			TextMarker LMarker = new TextMarker(LLineSeg.Offset + ColumnNumber, LLineSeg.Length - ColumnNumber, TextMarkerType.SolidBlock, Color.Yellow, Color.Blue);
			Document.MarkerStrategy.InsertMarker(0, LMarker);
			return LMarker;
		}
	}
}
