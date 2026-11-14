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
using Image=System.Drawing.Image;
using Session=Alphora.Dataphor.Frontend.Client.Windows.Session;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox
{
    public partial class ToolBox : UserControl, IErrorSource
    {
        //GroupBar.GroupBarItems->GroupBarItem
        private NavigationPane _paletteGroupBar;
        //GroupView.GroupViewItem->GroupViewItem
        private ListView _pointerGroupView;
        


        public ToolBox()
        {
            InitializeComponent();
            InitializeGroupView();
        }

        #region IErrorSource Members

        public void ErrorHighlighted(Exception exception)
        {
        }

        public void ErrorSelected(Exception exception)
        {
            throw new NotImplementedException();
        }

        #endregion

        private EventHandler<StatusEventArgs> _statusChanged;
        
        private void SetStatus(string description)
        {
            EventHandler<StatusEventArgs> changed = _statusChanged;
            if (changed != null) changed(this, new StatusEventArgs(description));
        }

        public event EventHandler<StatusEventArgs> StatusChanged
        {
            add { _statusChanged += value; }
            remove { _statusChanged -= value; }
        }        


        private void InitializeGroupView()
        {
            // 
            // FPaletteGroupBar
            // 
            _paletteGroupBar = new NavigationPane();
            _paletteGroupBar.AllowDrop = true;
            _paletteGroupBar.BackColor = SystemColors.Control;
            //FPaletteGroupBar.BorderStyle = BorderStyle.FixedSingle;
            _paletteGroupBar.Dock = DockStyle.Fill;
            _paletteGroupBar.Location = new Point(0, 24);
            _paletteGroupBar.Name = "FPaletteGroupBar";
            //FPaletteGroupBar.SelectedItem = 0;
            _paletteGroupBar.Size = new Size(163, 163);
            _paletteGroupBar.TabIndex = 1;
            // 
            // FPointerGroupView
            // 
            _pointerGroupView = new ListView
                                    {
                                        BorderStyle = BorderStyle.None,
                                        //ButtonView = true,
                                        View=View.List,                                        
                                        Dock = DockStyle.Top
                                    };
            
            _pointerGroupView.Items.AddRange(new[]
                                                          {
                                                              new ListViewItem("Pointer", 0)
                                                          });
            
            //FPointerGroupView.IntegratedScrolling = true;
            //FPointerGroupView.ItemYSpacing = 2;
            _pointerGroupView.LargeImageList = null;
            _pointerGroupView.Location = new Point(0, 0);
            _pointerGroupView.Name = "FPointerGroupView";
            //FPointerGroupView.SelectedItem = 0;
            _pointerGroupView.Items[0].Selected = true;
            _pointerGroupView.MultiSelect = false;
            
            _pointerGroupView.Size = new Size(163, 24);
            _pointerGroupView.SmallImageList = FPointerImageList;
            //FPointerGroupView.SmallImageView = true;
            _pointerGroupView.TabIndex = 0;
            _pointerGroupView.Text = "groupView2";
            //FPointerGroupView.GroupViewItemSelected += FPointerGroupView_GroupViewItemSelected;
            _pointerGroupView.ItemSelectionChanged+=FPointerGroupView_ItemSelectionChanged;

            Controls.Add(_paletteGroupBar);
            Controls.Add(_pointerGroupView);
        }

        

        internal void ClearPalette()
        {
            _paletteGroupBar.NavigationPages.Clear();
        }

        private ListView EnsureCategory(string categoryName)
        {
            NavigationPanePage item = FindPaletteBarItem(categoryName);
            if (item == null)
            {
                var view = new ListView
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
                view.ItemSelectionChanged += LView_ItemSelectionChanged;

                item = new NavigationPanePage();
                //LItem.Client = LView;
                item.Controls.Add(view);
                item.Text = categoryName;
                //HACK: It is very important to set the Name of the NavigationPanePage or the NavigationPane will get confused
                item.Name = "NavigationPanePage" + _paletteGroupBar.NavigationPages.Count;
                _paletteGroupBar.NavigationPages.Add(item);
            }
            return (ListView)item.Controls[0];
        }

       

        internal void LoadPalette()
        {
            PaletteItem item;
            Type type;

            foreach (String name in FrontendSession.NodeTypeTable.Keys)
            {
                type = FrontendSession.NodeTypeTable.GetClassType(name);

                if (IsTypeListed(type))
                {
                    item = new PaletteItem
                                {
                                    ClassName = type.Name,
                                    Text = type.Name,
                                    Description = GetDescription(type),
                                    ImageIndex = GetDesignerImage(type)
                                };
                    ListView category = EnsureCategory(GetDesignerCategory(type));
                    category.Items.Add(item);
                }
            }
        }

        public void SelectPaletteItem(PaletteItem item, bool isMultiDrop)
        {
            if (item != _selectedPaletteItem)
            {
                _isMultiDrop = isMultiDrop && (item != null);

                if (_selectedPaletteItem != null)
                {
                    //FSelectedPaletteItem.ListView.ButtonView = false;
                   // FSelectedPaletteItem.ListView.SelectedTextColor = Color.Navy;                    
                }

                _selectedPaletteItem = item;

                if (_selectedPaletteItem != null)
                {
                    //FSelectedPaletteItem.ListView.ButtonView = true;
                    //FSelectedPaletteItem.ListView.SelectedItem =
                    //    FSelectedPaletteItem.ListView.GroupViewItems.IndexOf(FSelectedPaletteItem);

                    _selectedPaletteItem.Selected = true;

                   // if (FIsMultiDrop)
                   //     FSelectedPaletteItem.ListView.SelectedTextColor = Color.Blue;*/

                    NodesTree.PaletteItem = _selectedPaletteItem;
                    SetStatus(_selectedPaletteItem.Description);
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

        private NavigationPanePage FindPaletteBarItem(string text)
        {
            foreach (NavigationPanePage item in _paletteGroupBar.NavigationPages)
            {
                if (String.Compare(item.Text, text, true) == 0)
                    return item;
            }
            return null;
        }

        protected override bool ProcessDialogKey(Keys key)
        {
            if
                (
                ((key & Keys.Modifiers) == Keys.None) &&
                ((key & Keys.KeyCode) == Keys.Escape) &&
                (_selectedPaletteItem != null)
                )
            {
                SelectPaletteItem(null, false);
                return true;
            }
            return base.ProcessDialogKey(key);
        }

        /*private void FPointerGroupView_GroupViewItemSelected(object ASender, EventArgs AArgs)
        {
            SelectPaletteItem(null, false);
        }*/

        private void FPointerGroupView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
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

        void LView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs args)
        {
            var view = (ListView)sender;
            /*SelectPaletteItem((PaletteItem)LView.GroupViewItems[LView.SelectedItem],
                ModifierKeys == Keys.Shift);*/

            var selectedItem = (PaletteItem) args.Item;
            SelectPaletteItem(selectedItem,ModifierKeys == Keys.Shift);
        }

        #region Palette

        private readonly Hashtable FImageIndex = new Hashtable();
        private bool _isMultiDrop;
        private PaletteItem _selectedPaletteItem;

        [Browsable(false)]
        public PaletteItem SelectedPaletteItem
        {
            get { return _selectedPaletteItem; }
        }

        [Browsable(false)]
        public bool IsMultiDrop
        {
            get { return _isMultiDrop; }
        }
        


        private bool IsTypeListed(Type type)
        {
            var listIn =
                (ListInDesignerAttribute) ReflectionUtility.GetAttribute(type, typeof (ListInDesignerAttribute));
            if (listIn != null)
                return listIn.IsListed;
            return true;
        }

        private string GetDescription(Type type)
        {
            var description =
                (DescriptionAttribute) ReflectionUtility.GetAttribute(type, typeof (DescriptionAttribute));
            if (description != null)
                return description.Description;
            return String.Empty;
        }

        private string GetDesignerCategory(Type type)
        {
            var category =
                (DesignerCategoryAttribute) ReflectionUtility.GetAttribute(type, typeof (DesignerCategoryAttribute));
            if (category != null)
                return category.Category;
            return Strings.UnspecifiedCategory;
        }

        private Image LoadImage(string imageExpression)
        {
            try
            {
                using (IDataValue imageData = FrontendSession.Pipe.RequestDocument(imageExpression))
                {
                    var streamCopy = new MemoryStream();
                    Stream stream = imageData.OpenStream();
                    try
                    {
                        StreamUtility.CopyStream(stream, streamCopy);
                    }
                    finally
                    {
                        stream.Close();
                    }
                    return Image.FromStream(streamCopy);
                }
            }
            catch (Exception exception)
            {
                Program.DataphoriaInstance.Warnings.AppendError(this, exception, true);
                // Don't rethrow
            }
            return null;
        }

        public int GetDesignerImage(Type type)
        {
            var imageAttribute =
                (DesignerImageAttribute) ReflectionUtility.GetAttribute(type, typeof (DesignerImageAttribute));
            if (imageAttribute != null)
            {
                object indexResult = FImageIndex[imageAttribute.ImageExpression];
                if (indexResult == null)
                {
                    Image image = LoadImage(imageAttribute.ImageExpression);
                    if (image != null)
                    {
                        if (image is Bitmap)
                            ((Bitmap) image).MakeTransparent();
                        FNodesImageList.Images.Add(image);
                        int index = FNodesImageList.Images.Count - 1;
                        FImageIndex.Add(imageAttribute.ImageExpression, index);
                        return index;                        
                    }
                    FImageIndex.Add(imageAttribute.ImageExpression, 0);
                }
                else
                    return (int) indexResult;
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
