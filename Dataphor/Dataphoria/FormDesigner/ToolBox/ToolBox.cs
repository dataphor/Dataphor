using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Ascend.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;
using Image=System.Drawing.Image;
using Session=Alphora.Dataphor.Frontend.Client.Windows.Session;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    public partial class ToolBox : UserControl, IErrorSource
    {
        //GroupBar.GroupBarItems->GroupBarItem
        private NavigationPane FPaletteGroupBar;
        //GroupView.GroupViewItem->GroupViewItem
        private ListView FPointerGroupView;
        


        public ToolBox()
        {
            InitializeComponent();
            InitializeGroupView();
        }

        #region IErrorSource Members

        public void ErrorHighlighted(Exception AException)
        {
        }

        public void ErrorSelected(Exception AException)
        {
            throw new NotImplementedException();
        }

        #endregion

        private EventHandler<StatusEventArgs> FStatusChanged;
        
        private void SetStatus(string ADescription)
        {
            EventHandler<StatusEventArgs> LChanged = FStatusChanged;
            if (LChanged != null) LChanged(this, new StatusEventArgs(ADescription));
        }

        public event EventHandler<StatusEventArgs> StatusChanged
        {
            add { FStatusChanged += value; }
            remove { FStatusChanged -= value; }
        }        


        private void InitializeGroupView()
        {
            // 
            // FPaletteGroupBar
            // 
            FPaletteGroupBar = new NavigationPane();
            FPaletteGroupBar.AllowDrop = true;
            FPaletteGroupBar.BackColor = SystemColors.Control;
            //FPaletteGroupBar.BorderStyle = BorderStyle.FixedSingle;
            FPaletteGroupBar.Dock = DockStyle.Fill;
            FPaletteGroupBar.Location = new Point(0, 24);
            FPaletteGroupBar.Name = "FPaletteGroupBar";
            //FPaletteGroupBar.SelectedItem = 0;
            FPaletteGroupBar.Size = new Size(163, 163);
            FPaletteGroupBar.TabIndex = 1;
            // 
            // FPointerGroupView
            // 
            FPointerGroupView = new ListView
                                    {
                                        BorderStyle = BorderStyle.None,
                                        //ButtonView = true,
                                        View=View.List,                                        
                                        Dock = DockStyle.Top
                                    };
            
            FPointerGroupView.Items.AddRange(new[]
                                                          {
                                                              new ListViewItem("Pointer", 0)
                                                          });
            
            //FPointerGroupView.IntegratedScrolling = true;
            //FPointerGroupView.ItemYSpacing = 2;
            FPointerGroupView.LargeImageList = null;
            FPointerGroupView.Location = new Point(0, 0);
            FPointerGroupView.Name = "FPointerGroupView";
            //FPointerGroupView.SelectedItem = 0;
            FPointerGroupView.Items[0].Selected = true;
            FPointerGroupView.MultiSelect = false;
            
            FPointerGroupView.Size = new Size(163, 24);
            FPointerGroupView.SmallImageList = FPointerImageList;
            //FPointerGroupView.SmallImageView = true;
            FPointerGroupView.TabIndex = 0;
            FPointerGroupView.Text = "groupView2";
            //FPointerGroupView.GroupViewItemSelected += FPointerGroupView_GroupViewItemSelected;
            FPointerGroupView.ItemSelectionChanged+=FPointerGroupView_ItemSelectionChanged;

            Controls.Add(FPaletteGroupBar);
            Controls.Add(FPointerGroupView);
        }

        

        internal void ClearPalette()
        {
            FPaletteGroupBar.NavigationPages.Clear();
        }

        private ListView EnsureCategory(string ACategoryName)
        {
            NavigationPanePage LItem = FindPaletteBarItem(ACategoryName);
            if (LItem == null)
            {
                var LView = new ListView
                                {
                                    BorderStyle = BorderStyle.None,
                                    View = View.List,                                    
                                   // IntegratedScrolling = false,
                                    //ItemYSpacing = 2,
                                    SmallImageList = FNodesImageList,
                                    Dock = DockStyle.Fill,
                                  //  SmallImageView = true,
                                  //  SelectedTextColor = Color.Navy
                                };
                //LView.GroupViewItemSelected += new EventHandler(CategoryGroupViewItemSelected);
                //LView.Columns.Add(new ColumnHeader());
                LView.ItemSelectionChanged += LView_ItemSelectionChanged;

                LItem = new NavigationPanePage();
                //LItem.Client = LView;
                LItem.Controls.Add(LView);
                LItem.Text = ACategoryName;
                //HACK: It is very important to set the Name of the NavigationPanePage or the NavigationPane will get confused
                LItem.Name = "NavigationPanePage" + FPaletteGroupBar.NavigationPages.Count;
                FPaletteGroupBar.NavigationPages.Add(LItem);
            }
            return (ListView)LItem.Controls[0];
        }

       

        internal void LoadPalette()
        {
            PaletteItem LItem;
            Type LType;

            foreach (String LName in FrontendSession.NodeTypeTable.Keys)
            {
                LType = FrontendSession.NodeTypeTable.GetClassType(LName);

                if (IsTypeListed(LType))
                {
                    LItem = new PaletteItem
                                {
                                    ClassName = LType.Name,
                                    Text = LType.Name,
                                    Description = GetDescription(LType),
                                    ImageIndex = GetDesignerImage(LType)
                                };
                    ListView LCategory = EnsureCategory(GetDesignerCategory(LType));
                    LCategory.Items.Add(LItem);
                }
            }
        }

        public void SelectPaletteItem(PaletteItem AItem, bool AIsMultiDrop)
        {
            if (AItem != FSelectedPaletteItem)
            {
                FIsMultiDrop = AIsMultiDrop && (AItem != null);

                if (FSelectedPaletteItem != null)
                {
                    //FSelectedPaletteItem.ListView.ButtonView = false;
                   // FSelectedPaletteItem.ListView.SelectedTextColor = Color.Navy;                    
                }

                FSelectedPaletteItem = AItem;

                if (FSelectedPaletteItem != null)
                {
                    //FSelectedPaletteItem.ListView.ButtonView = true;
                    //FSelectedPaletteItem.ListView.SelectedItem =
                    //    FSelectedPaletteItem.ListView.GroupViewItems.IndexOf(FSelectedPaletteItem);

                    FSelectedPaletteItem.Selected = true;

                   // if (FIsMultiDrop)
                   //     FSelectedPaletteItem.ListView.SelectedTextColor = Color.Blue;*/

                    NodesTree.PaletteItem = FSelectedPaletteItem;
                    SetStatus(FSelectedPaletteItem.Description);
                    //FPointerGroupView.ButtonView = false;
                }
                else
                {
                    NodesTree.PaletteItem = null;
                    SetStatus(String.Empty);
                    //FPointerGroupView.ButtonView = true;
                }

                NodesTree.Select();
            }
        }
       

        public void PaletteItemDropped()
        {
            if (!IsMultiDrop)
                SelectPaletteItem(null, false);
        }

        private NavigationPanePage FindPaletteBarItem(string AText)
        {
            foreach (NavigationPanePage LItem in FPaletteGroupBar.NavigationPages)
            {
                if (String.Compare(LItem.Text, AText, true) == 0)
                    return LItem;
            }
            return null;
        }

        protected override bool ProcessDialogKey(Keys AKey)
        {
            if
                (
                ((AKey & Keys.Modifiers) == Keys.None) &&
                ((AKey & Keys.KeyCode) == Keys.Escape) &&
                (FSelectedPaletteItem != null)
                )
            {
                SelectPaletteItem(null, false);
                return true;
            }
            return base.ProcessDialogKey(AKey);
        }

        /*private void FPointerGroupView_GroupViewItemSelected(object ASender, EventArgs AArgs)
        {
            SelectPaletteItem(null, false);
        }*/

        private void FPointerGroupView_ItemSelectionChanged(object ASender, ListViewItemSelectionChangedEventArgs AE)
        {
            
            SelectPaletteItem(null, false);
        }

        /*private void CategoryGroupViewItemSelected(object ASender, EventArgs AArgs)
        {
            var LView = (GroupView) ASender;
            SelectPaletteItem
                (
                (PaletteItem) LView.GroupViewItems[LView.SelectedItem],
                ModifierKeys == Keys.Shift
                );
        }*/

        void LView_ItemSelectionChanged(object ASender, ListViewItemSelectionChangedEventArgs AArgs)
        {
            var LView = (ListView)ASender;
            /*SelectPaletteItem((PaletteItem)LView.GroupViewItems[LView.SelectedItem],
                ModifierKeys == Keys.Shift);*/

            var LSelectedItem = (PaletteItem) AArgs.Item;
            SelectPaletteItem(LSelectedItem,ModifierKeys == Keys.Shift);
        }

        #region Palette

        private readonly Hashtable FImageIndex = new Hashtable();
        private bool FIsMultiDrop;
        private PaletteItem FSelectedPaletteItem;

        [Browsable(false)]
        public PaletteItem SelectedPaletteItem
        {
            get { return FSelectedPaletteItem; }
        }

        [Browsable(false)]
        public bool IsMultiDrop
        {
            get { return FIsMultiDrop; }
        }
        


        private bool IsTypeListed(Type AType)
        {
            var LListIn =
                (ListInDesignerAttribute) ReflectionUtility.GetAttribute(AType, typeof (ListInDesignerAttribute));
            if (LListIn != null)
                return LListIn.IsListed;
            return true;
        }

        private string GetDescription(Type AType)
        {
            var LDescription =
                (DescriptionAttribute) ReflectionUtility.GetAttribute(AType, typeof (DescriptionAttribute));
            if (LDescription != null)
                return LDescription.Description;
            return String.Empty;
        }

        private string GetDesignerCategory(Type AType)
        {
            var LCategory =
                (DesignerCategoryAttribute) ReflectionUtility.GetAttribute(AType, typeof (DesignerCategoryAttribute));
            if (LCategory != null)
                return LCategory.Category;
            return Strings.UnspecifiedCategory;
        }

        private Image LoadImage(string AImageExpression)
        {
            try
            {
                using (DataValue LImageData = FrontendSession.Pipe.RequestDocument(AImageExpression))
                {
                    var LStreamCopy = new MemoryStream();
                    Stream LStream = LImageData.OpenStream();
                    try
                    {
                        StreamUtility.CopyStream(LStream, LStreamCopy);
                    }
                    finally
                    {
                        LStream.Close();
                    }
                    return Image.FromStream(LStreamCopy);
                }
            }
            catch (Exception LException)
            {
                Program.DataphoriaInstance.Warnings.AppendError(this, LException, true);
                // Don't rethrow
            }
            return null;
        }

        public int GetDesignerImage(Type AType)
        {
            var LImageAttribute =
                (DesignerImageAttribute) ReflectionUtility.GetAttribute(AType, typeof (DesignerImageAttribute));
            if (LImageAttribute != null)
            {
                object LIndexResult = FImageIndex[LImageAttribute.ImageExpression];
                if (LIndexResult == null)
                {
                    Image LImage = LoadImage(LImageAttribute.ImageExpression);
                    if (LImage != null)
                    {
                        if (LImage is Bitmap)
                            ((Bitmap) LImage).MakeTransparent();
                        FNodesImageList.Images.Add(LImage);
                        int LIndex = FNodesImageList.Images.Count - 1;
                        FImageIndex.Add(LImageAttribute.ImageExpression, LIndex);
                        return LIndex;                        
                    }
                    FImageIndex.Add(LImageAttribute.ImageExpression, 0);
                }
                else
                    return (int) LIndexResult;
            }
            return 0; // Zero is the reserved index for the default image
        }

        #endregion

        #region FrontendSession

        [Browsable(false)]
        public Session FrontendSession { get; set; }

        public DesignerTree.DesignerTree NodesTree { get; set; }

        #endregion
    }
}
