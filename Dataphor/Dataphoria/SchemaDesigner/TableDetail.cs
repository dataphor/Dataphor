/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public class TableDetailSurface  : ObjectDetailSurface
	{
		public TableDetailSurface(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			AutoScroll = true;
			FDetail = new TableDetail(AObject, ADesigner);
			Controls.Add(FDetail);
		}

		private TableDetail FDetail;

		public TableSchema BaseTableVar
		{
			get { return (TableSchema)Object; }
		}

		public override void Details(Control AControl)
		{
			if ((AControl == null) || (AControl == FDetail))
				FDetail.Details();
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			Size LClientSize = ClientSize;
			if (FDetail != null)
			{
				FDetail.Left = (LClientSize.Width / 2) - (FDetail.Width / 2);
				FDetail.Top = (LClientSize.Height / 2) - (FDetail.Height / 2);
			}
		}
	}

	public class TableDetail : TableDesigner
	{
		public const int CVColumnSpacing = 2;
		public const int CLinkLineSpacing = 4;
		public const int CLinkLineGap = 2;
		public const int CLinkLineTickWidth = 2;
		public const int CHPadding = 4;
		public const int CVPadding = 3;
		public const int CVSectionGap = 4;

		public TableDetail(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			Size = new Size(300, 350);
			TextVAlign = VerticalAlignment.Top;

			// Get all columns
			TableSchema.ColumnSchemaEnumerator LColumns = BaseTableVar.GetColumns();
			while (LColumns.MoveNext())
			{
				AddColumnDesigner(LColumns.Current);
			}

			// Get all keys
			TableSchema.KeySchemaEnumerator LKeys = BaseTableVar.GetKeys();
			while (LKeys.MoveNext())
			{
				AddKeyDesigner(LKeys.Current);
			}

			// Get all Orders
			TableSchema.OrderSchemaEnumerator LOrders = BaseTableVar.GetOrders();
			while (LOrders.MoveNext())
			{
				AddOrderDesigner(LOrders.Current);
			}
		}

		public TableSchema BaseTableVar
		{
			get { return (TableSchema)Object; }
		}

		// Column Designers

		private ArrayList FColumnDesigners = new ArrayList();

		public void AddColumnDesigner(ColumnSchema AColumn)
		{
			ColumnDesigner LDesigner = new ColumnDesigner(AColumn);
			FColumnDesigners.Add(LDesigner);
			LDesigner.Disposed += new EventHandler(ColumnDisposed);
			Controls.Add(LDesigner);
		}

		private void ColumnDisposed(object ASender, EventArgs AArgs)
		{
			((Control)ASender).Disposed -= new EventHandler(ColumnDisposed);
			FColumnDesigners.Remove(ASender);
		}

		// Key Designers

		private ArrayList FKeyDesigners = new ArrayList();

		public void AddKeyDesigner(KeySchema AKey)
		{
			KeyDesigner LDesigner = new KeyDesigner(AKey);
			FKeyDesigners.Add(LDesigner);
			LDesigner.Disposed += new EventHandler(KeyDisposed);
			Controls.Add(LDesigner);
		}

		private void KeyDisposed(object ASender, EventArgs AArgs)
		{
			((Control)ASender).Disposed -= new EventHandler(KeyDisposed);
			FKeyDesigners.Remove(ASender);
		}

		// Order Designers

		private ArrayList FOrderDesigners = new ArrayList();

		public void AddOrderDesigner(OrderSchema AOrder)
		{
			OrderDesigner LDesigner = new OrderDesigner(AOrder);
			FOrderDesigners.Add(LDesigner);
			LDesigner.Disposed += new EventHandler(OrderDisposed);
			Controls.Add(LDesigner);
		}

		private void OrderDisposed(object ASender, EventArgs AArgs)
		{
			((Control)ASender).Disposed -= new EventHandler(OrderDisposed);
			FOrderDesigners.Remove(ASender);
		}

		// Layout

		private int GetLinkXOffset()
		{
			return (((FKeyDesigners.Count + FOrderDesigners.Count) - 1) * CLinkLineSpacing) + 
				((FKeyDesigners.Count > 0) && (FOrderDesigners.Count > 0) ? CLinkLineGap : 0) +
				((FKeyDesigners.Count > 0) || (FOrderDesigners.Count > 0) ? CHPadding : 0);
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			Rectangle LBounds = GetInnerBounds();
			int LYOffset = (GetTextBounds().Bottom - LBounds.Y);
			int LXOffset = GetLinkXOffset();
			LBounds.Y += LYOffset;
			LBounds.Height -= LYOffset;
			LBounds.X += LXOffset;
			LBounds.Width -= LXOffset;
			LBounds.Inflate(-CHPadding, -CVPadding);
			foreach (ColumnDesigner LDesigner in FColumnDesigners)
			{
				LDesigner.Bounds = new Rectangle(LBounds.Location, new Size(LBounds.Width, LDesigner.Height));
				LBounds.Y += LDesigner.Height + CVColumnSpacing;
			}
			LBounds.Y += CVSectionGap;
			foreach (KeyDesigner LDesigner in FKeyDesigners)
			{
				LDesigner.Bounds = new Rectangle(LBounds.Location, new Size(LBounds.Width, LDesigner.Height));
				LBounds.Y += LDesigner.Height + CVColumnSpacing;
				LBounds.X -= CLinkLineSpacing;
			}
			LBounds.Y += CVSectionGap;
			LBounds.X -= CLinkLineGap;
			foreach (OrderDesigner LDesigner in FOrderDesigners)
			{
				LDesigner.Bounds = new Rectangle(LBounds.Location, new Size(LBounds.Width, LDesigner.Height));
				LBounds.Y += LDesigner.Height + CVColumnSpacing;
				LBounds.X -= CLinkLineSpacing;
			}
		}

		private ColumnDesigner FindColumnDesigner(string AName)
		{
			foreach (ColumnDesigner LDesigner in FColumnDesigners)
			{
				if (String.Compare(LDesigner.Column.Name, AName, true) == 0)
					return LDesigner;
			}
			return null;
		}

		// Paint

		private void PaintColumnLinks(ColumnListDesignerBase ADesigner, Pen APen, Graphics AGraphics, int ACurrentX)
		{
			if (ADesigner.ColumnList.ColumnCount > 0)
			{
				int LCurrentY;
				int LMinY = Int32.MaxValue;
				ColumnDesigner LColumn;
				for (int LColumnIndex = 0; LColumnIndex < ADesigner.ColumnList.ColumnCount; LColumnIndex++)
				{
					LColumn = FindColumnDesigner(ADesigner.ColumnList.GetColumnName(LColumnIndex));
					LCurrentY = LColumn.Top + (LColumn.Height / 2);
					AGraphics.DrawLine(APen, new Point(ACurrentX, LCurrentY), new Point(ACurrentX + CLinkLineTickWidth, LCurrentY));
					if (LMinY > LCurrentY)
						LMinY = LCurrentY;
				}
				LCurrentY = ADesigner.Top + (ADesigner.Height / 2);
				AGraphics.DrawLine(APen, new Point(ACurrentX, LCurrentY), new Point(ACurrentX + CLinkLineTickWidth, LCurrentY));
				AGraphics.DrawLine(APen, new Point(ACurrentX, LMinY), new Point(ACurrentX, LCurrentY));
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			Rectangle LBounds = GetInnerBounds();
			using (Pen LPen = new Pen(Color.Black))
			{
				int LCurrentX = LBounds.X + GetLinkXOffset();	// + CHPadding - CHPadding
				foreach (KeyDesigner LDesigner in FKeyDesigners)
				{
					PaintColumnLinks(LDesigner, LPen, AArgs.Graphics, LCurrentX);
					LCurrentX -= CLinkLineSpacing;
				}
				if (FKeyDesigners.Count > 0)
					LCurrentX -= CLinkLineGap;
				foreach (OrderDesigner LDesigner in FOrderDesigners)
				{
					PaintColumnLinks(LDesigner, LPen, AArgs.Graphics, LCurrentX);
					LCurrentX -= CLinkLineSpacing;
				}
			}
		}
	}

	public class ColumnDesigner : TextDesignerBox
	{
		public const int CVPadding = 5;
		
		public ColumnDesigner(ColumnSchema AColumn)
		{
			FColumn = AColumn;
			FColumn.OnModified += new SchemaHandler(ColumnModified);
			FColumn.OnDeleted += new SchemaHandler(ColumnDeleted);

			TextVAlign = VerticalAlignment.Middle;
			TextHAlign = HorizontalAlignment.Left;
			TextHPadding = (Height / 2) + 1;	// Capsule
			Height = GetTextBounds().Height + CVPadding + CMaxDepth;
			RoundRadius = TextHPadding;
			
			UpdateText();
		}

		protected override void Dispose(bool ADisposing)
		{
			FColumn.OnModified -= new SchemaHandler(ColumnModified);
			FColumn.OnDeleted -= new SchemaHandler(ColumnDeleted);
			base.Dispose(ADisposing);
		}

		private ColumnSchema FColumn;
		public ColumnSchema Column
		{
			get { return FColumn; }
		}

		private void ColumnModified(BaseSchema ASchema)
		{
			UpdateText();
		}

		private void ColumnDeleted(BaseSchema ASchema)
		{
			Dispose();
		}

		private void UpdateText()
		{
			this.Text = FColumn.Name;
		}
	
		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			if (AArgs.Button == MouseButtons.Left)
				DoDragDrop(new ColumnListDesignerData(new string[] {Column.Name}), DragDropEffects.Copy | DragDropEffects.Move);
		}
	}

	public abstract class ColumnListDesignerBase : DesignerBox
	{
		public const int CHColPadding = 2;
		public const int CHPadding = 2;
		public const int CVPadding = 5;

		public ColumnListDesignerBase(BaseColumnListSchema AColumnList)
		{
			FColumnList = AColumnList;
			FColumnList.OnModified += new SchemaHandler(ColumnListModified);
			FColumnList.OnDeleted += new SchemaHandler(ColumnListDeleted);
			Height = Font.Height + (Height - GetInnerBounds().Height) + CVPadding;
		}

		protected override void Dispose(bool ADisposing)
		{
			FColumnList.OnModified -= new SchemaHandler(ColumnListModified);
			FColumnList.OnDeleted -= new SchemaHandler(ColumnListDeleted);
			base.Dispose(ADisposing);
		}

		private BaseColumnListSchema FColumnList;
		public BaseColumnListSchema ColumnList
		{
			get { return FColumnList; }
		}

		protected virtual void ColumnListModified(BaseSchema ASchema)
		{
			Invalidate();
		}

		protected virtual void ColumnListDeleted(BaseSchema ASchema)
		{
			Dispose();
		}

		private Color FItemColor = Color.Navy;
		public Color ItemColor
		{
			get { return FItemColor; }
			set
			{
				if (FItemColor != value)
				{
					FItemColor = value;
					Invalidate();
				}
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			using (Pen LPen = new Pen(ForeColor))
			{
				using (SolidBrush LBrush = new SolidBrush(ItemColor))
				{
					Rectangle LRect = GetInnerBounds();
					Rectangle LColumnRect;
					Size LTextSize;
					string LColumnName;
					LRect.Inflate(-1, -1);

					for (int i = 0; i < ColumnList.ColumnCount; i++)
					{
						LColumnName = ColumnList.GetColumnName(i);
						LTextSize = AArgs.Graphics.MeasureString(LColumnName, Font).ToSize();
						LColumnRect = 
							new Rectangle
							(
								LRect.Location, 
								new Size
								(
									LTextSize.Width + (CHColPadding * 2),
									LRect.Height
								)
							);
						if (AArgs.Graphics.Clip.IsVisible(LColumnRect))
						{
							LBrush.Color = ItemColor;
							AArgs.Graphics.FillRectangle(LBrush, LColumnRect);
							AArgs.Graphics.DrawRectangle(LPen, LColumnRect);
							LBrush.Color = ForeColor;
							AArgs.Graphics.DrawString
							(
								LColumnName, 
								Font, 
								LBrush,
								LColumnRect.Left + CHColPadding,
								LRect.Y + ((LRect.Height / 2) - (LTextSize.Height / 2))
							);
						}
						LRect.X += LColumnRect.Width + CHPadding;
						LRect.Width -= LColumnRect.Width;
					}
				}
			}
		}

		// Drag / Drop

		private string GetColumnAt(Point APoint)
		{
			Rectangle LBounds = GetInnerBounds();
			Rectangle LColumnRect;
			string LColumnName;
			Size LTextSize;
			LBounds.Inflate(-1, -1);
			if ((APoint.Y < LBounds.Y) || (APoint.Y > LBounds.Y))	// Optimization - don't bother going on
				return String.Empty;
			using (Graphics LGraphics = CreateGraphics())
			{
				for (int i = 0; i < ColumnList.ColumnCount; i++)
				{
					LColumnName = ColumnList.GetColumnName(i);
					LTextSize = LGraphics.MeasureString(LColumnName, Font).ToSize();
					LColumnRect = 
						new Rectangle
						(
							LBounds.Location, 
							new Size
							(
								LTextSize.Width + (CHColPadding * 2),
								LBounds.Height
							)
						);
					if (LColumnRect.Contains(APoint))
						return LColumnName;
					LBounds.X += LColumnRect.Width + CHPadding;
					LBounds.Width -= LColumnRect.Width;
					if (LBounds.X > APoint.X)	// Optimization - don't bother going on
						return String.Empty;
				}
			}
			return String.Empty;
		}

		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			if (AArgs.Button == MouseButtons.Left)
			{
				string LColumnName = GetColumnAt(new Point(AArgs.X, AArgs.Y));
				if (LColumnName != String.Empty)
				{
					DoDragDrop
					(
						new ColumnListDesignerData(new string[] {LColumnName}), 
						DragDropEffects.Move
					);
				}
			}
		}

		//		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs AArgs)
		//		{
		//			base.OnQueryContinueDrag(AArgs);
		//			if (AArgs.EscapePressed)
		//				AArgs.Action = DragAction.Cancel;
		//		}


		protected override void OnDragOver(DragEventArgs AArgs)
		{
			base.OnDragEnter(AArgs);

			bool LIsDragTarget = false;
			if (LIsDragTarget)
				AArgs.Effect = DragDropEffects.Move;
			else
				AArgs.Effect = DragDropEffects.None;
		}

		protected override void OnDragLeave(EventArgs AArgs)
		{
			base.OnDragLeave(AArgs);
		}

		protected override void OnDragDrop(DragEventArgs AArgs)
		{
			base.OnDragDrop(AArgs);

			bool LIsDragTarget = false;
			if (LIsDragTarget)
			{
				ColumnListDesignerData LData = AArgs.Data as ColumnListDesignerData;
				if (LData != null)
				{
					Invalidate();
				}
			}
		}

	}

	public class ColumnListDesignerData : DataObject
	{
		public ColumnListDesignerData(string[] AColumnNames)
		{
			FColumnNames = AColumnNames;
		}

		private string[] FColumnNames;
		public string[] ColumnNames
		{
			get { return FColumnNames; }
		}
	}

	public class KeyDesigner : ColumnListDesignerBase
	{
		public KeyDesigner(BaseColumnListSchema AColumnList) : base(AColumnList)
		{
			SurfaceColor = Color.FromArgb(255, 234, 233);
			ForeColor = Color.FromArgb(70, 3, 0);
			ItemColor = Color.WhiteSmoke;
			HighlightColor = Color.FromArgb(138, 7, 0);
		}

		public KeySchema Key
		{
			get { return (KeySchema)ColumnList; }
		}
	}

	public class OrderDesigner : ColumnListDesignerBase
	{
		public OrderDesigner(BaseColumnListSchema AColumnList) : base(AColumnList)
		{
			SurfaceColor = Color.FromArgb(182, 218, 194);
			ForeColor = Color.FromArgb(20, 39, 27);
			ItemColor = Color.WhiteSmoke;
			HighlightColor = Color.FromArgb(52, 103, 70);
		}

		public OrderSchema Order
		{
			get { return (OrderSchema)ColumnList; }
		}
	}
}
