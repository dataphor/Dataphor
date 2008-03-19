/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.Visual;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.BOP;

using Syncfusion.Windows.Forms.Tools;

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	// Don't put any definitions above the Analyzer class

	public class Analyzer : DataphoriaForm, Alphora.Dataphor.Dataphoria.Designers.IDesigner
	{
		private System.ComponentModel.IContainer components;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager FFrameBarManager;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsFile;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveAsDocumentMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCloseMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FFileMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSaveMenuItem;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FMainMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FFileBar;
		private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FViewMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu FNodesPopupMenu;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FZoomIn;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FZoomOut;
		private AnalyzerControl FAnalyzerControl;
		private System.Windows.Forms.ImageList ToolBarImageList;
		private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExpandOnDemand;
		private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FView;

		public Analyzer()	// dummy constructor for SyncFusion's MDI menu merging
		{
			InitializeComponent();
		}

		public Analyzer(Dataphoria ADataphoria, string ADesignerID)
		{
			InitializeComponent();

			FDesignerID = ADesignerID;

			InitializeService(ADataphoria);

			SetExpandOnDemand((bool)ADataphoria.Settings.GetSetting("Analyzer.ExpandOnDemand", typeof(bool), true));
		}

		protected override void Dispose(bool ADisposed)
		{
			if (!IsDisposed && (Dataphoria != null))
			{
				try
				{
					Dataphoria.Settings.SetSetting("Analyzer.ExpandOnDemand", FAnalyzerControl.MainSurface.ExpandOnDemand);
				}
				finally
				{
					try
					{
						if (components != null)
							components.Dispose();
					}
					finally
					{
						base.Dispose(ADisposed);
					}
				}
			}
		}

		// Dataphoria

		[Browsable(false)]
		public Dataphoria Dataphoria
		{
			get { return (FService == null ? null : FService.Dataphoria); }
		}

		#region Windows Form Designer generated code
		/// <summary>
		///		Required method for Designer support - do not modify
		///		the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Analyzer));
			this.FFrameBarManager = new Syncfusion.Windows.Forms.Tools.XPMenus.ChildFrameBarManager(this.components, this);
			this.FMainMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "MainMenu");
			this.FFileMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FSaveMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FSaveAsFile = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FSaveAsDocumentMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FCloseMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FViewMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
			this.FZoomIn = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FZoomOut = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			this.FFileBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "FileBar");
			this.FView = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "ViewBar");
			this.ToolBarImageList = new System.Windows.Forms.ImageList(this.components);
			this.FNodesPopupMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.PopupMenu(this.components);
			this.FAnalyzerControl = new Alphora.Dataphor.Dataphoria.Analyzer.AnalyzerControl();
			this.FExpandOnDemand = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
			((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).BeginInit();
			this.SuspendLayout();
			// 
			// FFrameBarManager
			// 
			this.FFrameBarManager.BarPositionInfo = ((System.IO.MemoryStream)(resources.GetObject("FFrameBarManager.BarPositionInfo")));
			this.FFrameBarManager.Bars.Add(this.FMainMenu);
			this.FFrameBarManager.Bars.Add(this.FFileBar);
			this.FFrameBarManager.Bars.Add(this.FView);
			this.FFrameBarManager.Categories.Add("File");
			this.FFrameBarManager.Categories.Add("View");
			this.FFrameBarManager.CurrentBaseFormType = "Alphora.Dataphor.Dataphoria.DataphoriaForm";
			this.FFrameBarManager.Form = this;
			this.FFrameBarManager.FormName = "Form Designer";
			this.FFrameBarManager.ImageList = this.ToolBarImageList;
			this.FFrameBarManager.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																										  this.FSaveAsFile,
																										  this.FSaveAsDocumentMenuItem,
																										  this.FCloseMenuItem,
																										  this.FFileMenu,
																										  this.FSaveMenuItem,
																										  this.FViewMenu,
																										  this.FZoomIn,
																										  this.FZoomOut,
																										  this.FExpandOnDemand});
			this.FFrameBarManager.LargeImageList = null;
			this.FFrameBarManager.UsePartialMenus = false;
			this.FFrameBarManager.ItemClicked += new Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventHandler(this.FrameBarManagerItemClicked);
			// 
			// FMainMenu
			// 
			this.FMainMenu.BarName = "MainMenu";
			this.FMainMenu.BarStyle = ((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle)(((((Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.AllowQuickCustomizing | Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.IsMainMenu) 
				| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.Visible) 
				| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.UseWholeRow) 
				| Syncfusion.Windows.Forms.Tools.XPMenus.BarStyle.DrawDragBorder)));
			this.FMainMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																								   this.FFileMenu,
																								   this.FViewMenu});
			this.FMainMenu.Manager = this.FFrameBarManager;
			// 
			// FFileMenu
			// 
			this.FFileMenu.CategoryIndex = 0;
			this.FFileMenu.ID = "File";
			this.FFileMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																								   this.FSaveMenuItem,
																								   this.FSaveAsFile,
																								   this.FSaveAsDocumentMenuItem,
																								   this.FCloseMenuItem});
			this.FFileMenu.MergeOrder = 1;
			this.FFileMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FFileMenu.Text = "&File";
			// 
			// FSaveMenuItem
			// 
			this.FSaveMenuItem.CategoryIndex = 0;
			this.FSaveMenuItem.ID = "Save";
			this.FSaveMenuItem.ImageIndex = 1;
			this.FSaveMenuItem.MergeOrder = 20;
			this.FSaveMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.FSaveMenuItem.Text = "&Save";
			// 
			// FSaveAsFile
			// 
			this.FSaveAsFile.CategoryIndex = 0;
			this.FSaveAsFile.ID = "SaveAsFile";
			this.FSaveAsFile.ImageIndex = 3;
			this.FSaveAsFile.MergeOrder = 20;
			this.FSaveAsFile.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftF;
			this.FSaveAsFile.Text = "Save As &File...";
			// 
			// FSaveAsDocumentMenuItem
			// 
			this.FSaveAsDocumentMenuItem.CategoryIndex = 0;
			this.FSaveAsDocumentMenuItem.ID = "SaveAsDocument";
			this.FSaveAsDocumentMenuItem.ImageIndex = 2;
			this.FSaveAsDocumentMenuItem.MergeOrder = 20;
			this.FSaveAsDocumentMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftD;
			this.FSaveAsDocumentMenuItem.Text = "Save As &Document...";
			// 
			// FCloseMenuItem
			// 
			this.FCloseMenuItem.CategoryIndex = 0;
			this.FCloseMenuItem.ID = "Close";
			this.FCloseMenuItem.ImageIndex = 0;
			this.FCloseMenuItem.MergeOrder = 20;
			this.FCloseMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF4;
			this.FCloseMenuItem.Text = "&Close";
			// 
			// FViewMenu
			// 
			this.FViewMenu.CategoryIndex = 1;
			this.FViewMenu.ID = "View";
			this.FViewMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																								   this.FZoomIn,
																								   this.FZoomOut,
																								   this.FExpandOnDemand});
			this.FViewMenu.MergeOrder = 10;
			this.FViewMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
			this.FViewMenu.SeparatorIndices.AddRange(new int[] {
																   0});
			this.FViewMenu.Text = "&View";
			// 
			// FZoomIn
			// 
			this.FZoomIn.CategoryIndex = 1;
			this.FZoomIn.ID = "ZoomIn";
			this.FZoomIn.ImageIndex = 4;
			this.FZoomIn.MergeOrder = 20;
			this.FZoomIn.ShortcutText = "+";
			this.FZoomIn.Text = "Zoom In";
			// 
			// FZoomOut
			// 
			this.FZoomOut.CategoryIndex = 1;
			this.FZoomOut.ID = "ZoomOut";
			this.FZoomOut.ImageIndex = 5;
			this.FZoomOut.MergeOrder = 20;
			this.FZoomOut.ShortcutText = "-";
			this.FZoomOut.Text = "Zoom Out";
			// 
			// FFileBar
			// 
			this.FFileBar.BarName = "FileBar";
			this.FFileBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																								  this.FSaveMenuItem,
																								  this.FSaveAsFile,
																								  this.FSaveAsDocumentMenuItem});
			this.FFileBar.Manager = this.FFrameBarManager;
			// 
			// FView
			// 
			this.FView.BarName = "ViewBar";
			this.FView.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
																							   this.FZoomIn,
																							   this.FZoomOut,
																							   this.FExpandOnDemand});
			this.FView.Manager = this.FFrameBarManager;
			// 
			// ToolBarImageList
			// 
			this.ToolBarImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.ToolBarImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.ToolBarImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ToolBarImageList.ImageStream")));
			this.ToolBarImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// FAnalyzerControl
			// 
			this.FAnalyzerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FAnalyzerControl.Location = new System.Drawing.Point(0, 0);
			this.FAnalyzerControl.Name = "FAnalyzerControl";
			this.FAnalyzerControl.Size = new System.Drawing.Size(687, 518);
			this.FAnalyzerControl.TabIndex = 4;
			// 
			// FExpandOnDemand
			// 
			this.FExpandOnDemand.CategoryIndex = 1;
			this.FExpandOnDemand.Checked = true;
			this.FExpandOnDemand.ID = "ExpandOnDemand";
			this.FExpandOnDemand.ImageIndex = 6;
			this.FExpandOnDemand.MergeOrder = 20;
			this.FExpandOnDemand.Text = "&Expand On Demand";
			// 
			// Analyzer
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(687, 518);
			this.Controls.Add(this.FAnalyzerControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Analyzer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		protected override void OnClosing(System.ComponentModel.CancelEventArgs AArgs) 
		{
			base.OnClosing(AArgs);
			try
			{
				FService.CheckModified();
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}

		#region Service

		public void InitializeService(Dataphoria ADataphoria)
		{
			FService = new DesignService(ADataphoria, this);
			FService.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnNameChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnRequestLoad += new RequestHandler(RequestLoad);
			FService.OnRequestSave += new RequestHandler(RequestSave);
		}

		private IDesignService FService;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return FService; }
		}

		private void NameOrModifiedChanged(object ASender, EventArgs AArgs)
		{
			UpdateTitle();
		}

		protected virtual void RequestLoad(DesignService AService, DesignBuffer ABuffer)
		{
			LoadPlan(ABuffer.LoadData());
		}

		protected virtual void RequestSave(DesignService AService, DesignBuffer ABuffer)
		{
			ABuffer.SaveData(SavePlan());
		}

		#endregion

		#region IDesigner, New, Loading, Saving

		private string FDesignerID;
		[Browsable(false)]
		public string DesignerID
		{
			get { return FDesignerID; }
		}

		public void Open(DesignBuffer ABuffer)
		{
			FService.Open(ABuffer);
		}

		/// <remarks> 
		///		Note that this method should not be confused with Form.Close().  
		///		Be sure to deal with a compile-time instance of type IDesigner 
		///		to invoke this method. 
		///	</remarks>
		void Dataphor.Dataphoria.Designers.IDesigner.Show()
		{
			UpdateTitle();
			Dataphoria.AttachForm(this);
		}

		public virtual void New()
		{
			FAnalyzerControl.Clear();
		}

		public virtual void LoadPlan(string APlan)
		{
			FAnalyzerControl.Load(APlan);
		}

		public virtual string SavePlan()
		{
			return FAnalyzerControl.Save();
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		public void Save()
		{
			FService.Save();
		}

		public void SaveAsFile()
		{
			FService.SaveAsFile();
		}

		public void SaveAsDocument()
		{
			FService.SaveAsDocument();
		}

		protected void InternalNew(IHost AHost, bool AOwner)
		{
			FService.SetBuffer(null);
			FService.SetModified(false);
		}

		public void New(IHost AHost)
		{
			InternalNew(AHost, false);
		}

		private void UpdateTitle()
		{
			Text = FService.GetDescription() + (FService.IsModified ? "*" : "");
		}
		
		#endregion

		#region Commands

		private void ZoomIn()
		{
			FAnalyzerControl.ZoomIn();
		}

		private void ZoomOut()
		{
			FAnalyzerControl.ZoomOut();
		}

		private void SetExpandOnDemand(bool AValue)
		{
			FAnalyzerControl.MainSurface.ExpandOnDemand = AValue;
			FExpandOnDemand.Checked = AValue;
		}

		private void ToggleExpandOnDemand()
		{
			SetExpandOnDemand(!FAnalyzerControl.MainSurface.ExpandOnDemand);
		}

		protected override void OnHelpRequested(HelpEventArgs AArgs)
		{
			base.OnHelpRequested(AArgs);
			Dataphoria.InvokeHelp("Analyzer");
			
		}

		private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
		{
			switch (AArgs.ClickedBarItem.ID)
			{
				case "Save" : Save(); break;
				case "SaveAsFile" : SaveAsFile(); break;
				case "SaveAsDocument" : SaveAsDocument(); break;
				case "Close" : Close(); break;
				case "ZoomIn" : ZoomIn(); break;
				case "ZoomOut" : ZoomOut(); break;
				case "ExpandOnDemand" : ToggleExpandOnDemand(); break;
			}
		}

		#endregion

		#region StatusBar

		protected Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel FPositionStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();

			FPositionStatus = new Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel();
			FPositionStatus.Text = "0:0";
			FPositionStatus.SizeToContent = true;
			FPositionStatus.HAlign = Syncfusion.Windows.Forms.Tools.HorzFlowAlign.Right;
			FPositionStatus.Alignment = System.Windows.Forms.HorizontalAlignment.Center;
			FPositionStatus.BorderStyle = BorderStyle.FixedSingle;
			FPositionStatus.BorderColor = Color.Gray;
		}

		protected override void DisposeStatusBar()
		{
			FPositionStatus.Dispose();
			FPositionStatus = null;

			base.DisposeStatusBar();
		}

		public override void Merge(Control AStatusBar)
		{
			base.Merge(AStatusBar);

			AStatusBar.Controls.Add(FPositionStatus);
		}

		public override void Unmerge(Control AStatusBar)
		{
			base.Unmerge(AStatusBar);

			AStatusBar.Controls.Remove(FPositionStatus);
		}

		#endregion
	}
}