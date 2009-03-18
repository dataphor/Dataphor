/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Data;
using System.Text;

using SD = ICSharpCode.TextEditor;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client.Provider;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;

using Syncfusion.Windows.Forms.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
    partial class D4Editor : TextEditor, IErrorSource
    {
        private System.ComponentModel.IContainer components;
        private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FScriptMenu;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExecuteMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExecuteLineMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPrepareLineMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FSelectBlockMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPriorBlockMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FNextBlockMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FPrepareMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FAnalyzeMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FAnalyzeLineMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FInjectMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExecuteSchemaMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExportDataMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FExecuteBothMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FShowResultsMenuItem;
        private Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem FExportMenu;
        private Syncfusion.Windows.Forms.Tools.XPMenus.Bar FScriptBar;
        private System.Windows.Forms.ImageList FD4EditorImageList;
        private Syncfusion.Windows.Forms.Tools.XPMenus.BarItem FCancelMenuItem;
        






        protected override void Dispose(bool disposing)
        {
            try
            {
                components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(D4Editor));

            this.FScriptMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FExecuteMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FD4EditorImageList = new System.Windows.Forms.ImageList(this.components);
            this.FCancelMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPrepareMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FAnalyzeMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FInjectMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FExportMenu = new Syncfusion.Windows.Forms.Tools.XPMenus.ParentBarItem();
            this.FExecuteSchemaMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FExportDataMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FExecuteBothMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FExecuteLineMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPrepareLineMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FAnalyzeLineMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FSelectBlockMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FPriorBlockMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FNextBlockMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FShowResultsMenuItem = new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem();
            this.FScriptBar = new Syncfusion.Windows.Forms.Tools.XPMenus.Bar(this.FFrameBarManager, "ScriptBar");
            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).BeginInit();

            ((System.ComponentModel.ISupportInitialize)(this.FPositionStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // FFrameBarManager
            // 
            this.FFrameBarManager.BarPositionInfo = ((System.IO.MemoryStream)(resources.GetObject("FFrameBarManager.BarPositionInfo")));
            this.FFrameBarManager.Bars.Add(this.FScriptBar);
            this.FFrameBarManager.Categories.Add("Script");
            this.FFrameBarManager.CategoriesToIgnoreInCustDialog.AddRange(new int[] {
            3});
            this.FFrameBarManager.CurrentBaseFormType = "Alphora.Dataphor.Dataphoria.TextEditor.TextEditor";
            this.FFrameBarManager.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FScriptMenu,
            this.FSelectBlockMenuItem,
            this.FPriorBlockMenuItem,
            this.FNextBlockMenuItem,
            this.FExecuteMenuItem,
            this.FExecuteLineMenuItem,
            this.FPrepareMenuItem,
            this.FPrepareLineMenuItem,
            this.FAnalyzeMenuItem,
            this.FAnalyzeLineMenuItem,
            this.FExportMenu,
            this.FInjectMenuItem,
            this.FExecuteSchemaMenuItem,
            this.FExportDataMenuItem,
            this.FExecuteBothMenuItem,
            this.FShowResultsMenuItem,
            this.FCancelMenuItem});
            this.FFrameBarManager.ItemClicked += new Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventHandler(this.FrameBarManagerItemClicked);
            // 
            // FFileMenu
            // 
            this.FFileMenu.UpdatedBarItemPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0,
            1,
            2,
            3,
            4});
            this.FFileMenu.UpdatedSeparatorPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0});
            // 
            // FMainMenu
            // 
            this.FMainMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FScriptMenu});
            this.FMainMenu.UpdatedBarItemPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0,
            1,
            2,
            3});
            // 
            // FFileBar
            // 
            this.FFileBar.UpdatedSeparatorPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0});
            // 
            // FEditBar
            // 
            this.FEditBar.UpdatedSeparatorPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0});
            // 
            // FEditMenu
            // 
            this.FEditMenu.UpdatedBarItemPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8});
            this.FEditMenu.UpdatedSeparatorPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0,
            1,
            2});

            // 
            // FViewMenu
            // 
            this.FViewMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FShowResultsMenuItem});
            this.FViewMenu.SeparatorIndices.AddRange(new int[] {
            0,
            1,
            2});
            this.FViewMenu.UpdatedBarItemPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            2,
            1,
            0});
            this.FViewMenu.UpdatedSeparatorPositions = new Syncfusion.Collections.IntListDesignTime(new int[] {
            0,
            1,
            2});

            // 
            // FTextEdit
            // 
            this.FTextEdit.Size = new System.Drawing.Size(455, 138);

            // 
            // FScriptMenu
            // 
            this.FScriptMenu.CategoryIndex = 4;
            this.FScriptMenu.ID = "Script";
            this.FScriptMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FExecuteMenuItem,
            this.FCancelMenuItem,
            this.FPrepareMenuItem,
            this.FAnalyzeMenuItem,
            this.FInjectMenuItem,
            this.FExportMenu});
            this.FScriptMenu.MergeOrder = 30;
            this.FScriptMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FScriptMenu.Text = "&Script";
            // 
            // FExecuteMenuItem
            // 
            this.FExecuteMenuItem.CategoryIndex = 4;
            this.FExecuteMenuItem.ID = "Execute";
            this.FExecuteMenuItem.ImageIndex = 0;
            this.FExecuteMenuItem.ImageList = this.FD4EditorImageList;
            this.FExecuteMenuItem.MergeOrder = 10;
            this.FExecuteMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlE;
            this.FExecuteMenuItem.Text = "&Execute";
            // 
            // FD4EditorImageList
            // 
            this.FD4EditorImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FD4EditorImageList.ImageStream")));
            this.FD4EditorImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.FD4EditorImageList.Images.SetKeyName(0, "");
            this.FD4EditorImageList.Images.SetKeyName(1, "");
            this.FD4EditorImageList.Images.SetKeyName(2, "");
            this.FD4EditorImageList.Images.SetKeyName(3, "");
            this.FD4EditorImageList.Images.SetKeyName(4, "Tree View-Search.png");
            // 
            // FCancelMenuItem
            // 
            this.FCancelMenuItem.CategoryIndex = 4;
            this.FCancelMenuItem.Enabled = false;
            this.FCancelMenuItem.ID = "Cancel";
            this.FCancelMenuItem.ImageIndex = 2;
            this.FCancelMenuItem.ImageList = this.FD4EditorImageList;
            this.FCancelMenuItem.MergeOrder = 10;
            this.FCancelMenuItem.Text = "&Cancel Execute";
            // 
            // FPrepareMenuItem
            // 
            this.FPrepareMenuItem.CategoryIndex = 4;
            this.FPrepareMenuItem.ID = "Prepare";
            this.FPrepareMenuItem.ImageIndex = 1;
            this.FPrepareMenuItem.ImageList = this.FD4EditorImageList;
            this.FPrepareMenuItem.MergeOrder = 10;
            this.FPrepareMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlR;
            this.FPrepareMenuItem.Text = "&Prepare";
            // 
            // FAnalyzeMenuItem
            // 
            this.FAnalyzeMenuItem.CategoryIndex = 4;
            this.FAnalyzeMenuItem.ID = "Analyze";
            this.FAnalyzeMenuItem.ImageIndex = 4;
            this.FAnalyzeMenuItem.ImageList = this.FD4EditorImageList;
            this.FAnalyzeMenuItem.MergeOrder = 10;
            this.FAnalyzeMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlT;
            this.FAnalyzeMenuItem.Text = "&Analyze";
            // 
            // FInjectMenuItem
            // 
            this.FInjectMenuItem.CategoryIndex = 4;
            this.FInjectMenuItem.ID = "Inject";
            this.FInjectMenuItem.ImageIndex = 3;
            this.FInjectMenuItem.ImageList = this.FD4EditorImageList;
            this.FInjectMenuItem.MergeOrder = 10;
            this.FInjectMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlI;
            this.FInjectMenuItem.Text = "&Inject As Upgrade";
            // 
            // FExportMenu
            // 
            this.FExportMenu.CategoryIndex = 4;
            this.FExportMenu.ID = "Export";
            this.FExportMenu.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FExecuteSchemaMenuItem,
            this.FExportDataMenuItem,
            this.FExecuteBothMenuItem});
            this.FExportMenu.MergeOrder = 10;
            this.FExportMenu.MergeType = System.Windows.Forms.MenuMerge.MergeItems;
            this.FExportMenu.Text = "E&xport";
            this.FExportMenu.Visible = false;
            // 
            // FExecuteSchemaMenuItem
            // 
            this.FExecuteSchemaMenuItem.CategoryIndex = 4;
            this.FExecuteSchemaMenuItem.ID = "ExportSchema";
            this.FExecuteSchemaMenuItem.MergeOrder = 10;
            this.FExecuteSchemaMenuItem.Text = "&Schema Only...";
            // 
            // FExportDataMenuItem
            // 
            this.FExportDataMenuItem.CategoryIndex = 4;
            this.FExportDataMenuItem.ID = "ExportData";
            this.FExportDataMenuItem.MergeOrder = 10;
            this.FExportDataMenuItem.Text = "&Data Only...";
            // 
            // FExecuteBothMenuItem
            // 
            this.FExecuteBothMenuItem.CategoryIndex = 4;
            this.FExecuteBothMenuItem.ID = "ExportBoth";
            this.FExecuteBothMenuItem.MergeOrder = 10;
            this.FExecuteBothMenuItem.Text = "S&chema and Data...";
            // 
            // FExecuteLineMenuItem
            // 
            this.FExecuteLineMenuItem.CategoryIndex = 4;
            this.FExecuteLineMenuItem.ID = "ExecuteLine";
            this.FExecuteLineMenuItem.ImageIndex = 0;
            this.FExecuteLineMenuItem.ImageList = this.FD4EditorImageList;
            this.FExecuteLineMenuItem.MergeOrder = 10;
            this.FExecuteLineMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftE;
            this.FExecuteLineMenuItem.Text = "E&xecute Line";
            // 
            // FPrepareLineMenuItem
            // 
            this.FPrepareLineMenuItem.CategoryIndex = 4;
            this.FPrepareLineMenuItem.ID = "PrepareLine";
            this.FPrepareLineMenuItem.ImageIndex = 1;
            this.FPrepareLineMenuItem.ImageList = this.FD4EditorImageList;
            this.FPrepareLineMenuItem.MergeOrder = 10;
            this.FPrepareLineMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftR;
            this.FPrepareLineMenuItem.Text = "P&repare Line";
            // 
            // FAnalyzeLineMenuItem
            // 
            this.FAnalyzeLineMenuItem.CategoryIndex = 4;
            this.FAnalyzeLineMenuItem.ID = "AnalyzeLine";
            this.FAnalyzeLineMenuItem.ImageIndex = 4;
            this.FAnalyzeLineMenuItem.ImageList = this.FD4EditorImageList;
            this.FAnalyzeLineMenuItem.MergeOrder = 10;
            this.FAnalyzeLineMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftT;
            this.FAnalyzeLineMenuItem.Text = "A&nalyze Line";
            // 
            // FSelectBlockMenuItem
            // 
            this.FSelectBlockMenuItem.CategoryIndex = 1;
            this.FSelectBlockMenuItem.ID = "SelectBlock";
            this.FSelectBlockMenuItem.ImageIndex = 4;
            this.FSelectBlockMenuItem.ImageList = this.FD4EditorImageList;
            this.FSelectBlockMenuItem.MergeOrder = 10;
            this.FSelectBlockMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlD;
            this.FSelectBlockMenuItem.Text = "Select &Block";
            // 
            // FPriorBlockMenuItem
            // 
            this.FPriorBlockMenuItem.CategoryIndex = 1;
            this.FPriorBlockMenuItem.ID = "PriorBlock";
            this.FPriorBlockMenuItem.MergeOrder = 10;
            this.FPriorBlockMenuItem.Text = "&Prior Block";
            // 
            // FNextBlockMenuItem
            // 
            this.FNextBlockMenuItem.CategoryIndex = 1;
            this.FNextBlockMenuItem.ID = "NextBlock";
            this.FNextBlockMenuItem.MergeOrder = 10;
            this.FNextBlockMenuItem.Text = "&Next Block";
            // 
            // FShowResultsMenuItem
            // 
            this.FShowResultsMenuItem.CategoryIndex = 2;
            this.FShowResultsMenuItem.ID = "ShowResults";
            this.FShowResultsMenuItem.MergeOrder = 10;
            this.FShowResultsMenuItem.Shortcut = System.Windows.Forms.Shortcut.F7;
            this.FShowResultsMenuItem.Text = "&Results";
            // 
            // FScriptBar
            // 
            this.FScriptBar.BarName = "ScriptBar";
            this.FScriptBar.Items.AddRange(new Syncfusion.Windows.Forms.Tools.XPMenus.BarItem[] {
            this.FPrepareMenuItem,
            this.FAnalyzeMenuItem,
            this.FExecuteMenuItem,
            this.FCancelMenuItem,
            this.FInjectMenuItem});
            this.FScriptBar.Manager = this.FFrameBarManager;
            this.FScriptBar.SeparatorIndices.AddRange(new int[] {
            3});
            // 
            // D4Editor
            // 
            this.ClientSize = new System.Drawing.Size(455, 376);
            this.Name = "D4Editor";

            ((System.ComponentModel.ISupportInitialize)(this.FFrameBarManager)).EndInit();


            ((System.ComponentModel.ISupportInitialize)(this.FPositionStatus)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

    }
}
