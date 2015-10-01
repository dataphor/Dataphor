/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	Assumption: this designer assumes that its lifetime is for a single document.  It will 
	 currently not close any existing document before operations such a New(), Open().  This
	 behavior is okay for now because Dataphoria does not ask designers to change documents.
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Cursors = System.Windows.Forms.Cursors;
using Image = System.Drawing.Image;

using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.FormDesigner.ToolBox;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Session = Alphora.Dataphor.Frontend.Client.Windows.Session;

using WeifenLuo.WinFormsUI.Docking;
using System.Xml.Linq;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	// Don't put any definitions above the FormDesiger class

	public partial class FormDesigner : BaseForm, ILiveDesigner, IErrorSource, IServiceProvider, IContainer, IToolBarClient
	{
		protected DockContent _dockContentFormPanel;
		protected DockContent _dockContentNodesTree;
		protected DockContent _dockContentPalettePanel;
		protected DockContent _dockContentPropertyGrid;
		private ToolBox.ToolBox _palettePanel;

		public FormDesigner() // dummy constructor for MDI menu merging?
		{
			InitializeComponent();
			InitializeDocking();
		}

		public FormDesigner(IDataphoria dataphoria, string designerID)
		{
			InitializeComponent();


			InitializeDocking();


			_designerID = designerID;

			FNodesTree.FormDesigner = this;

			InitializeService(dataphoria);


			PrepareSession();
			dataphoria.OnFormDesignerLibrariesChanged += FormDesignerLibrariesChanged;
		}

		protected override void Dispose(bool disposed)
		{
			if (!IsDisposed && (Dataphoria != null))
			{
				try
				{
					SetDesignHost(null, true);
				}
				finally
				{
					try
					{
						_palettePanel.ClearPalette();
					}
					finally
					{
						try
						{
							if (_frontendSession != null)
							{
								_frontendSession.Dispose();
								_frontendSession = null;
							}
						}
						finally
						{
							try
							{
								Dataphoria.OnFormDesignerLibrariesChanged -= new EventHandler(FormDesignerLibrariesChanged);
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
									base.Dispose(disposed);
								}
							}
						}
					}
				}
			}
		}


		// Dataphoria

		[Browsable(false)]
		public IDataphoria Dataphoria
		{
			get { return (_service == null ? null : _service.Dataphoria); }
		}

		void IToolBarClient.MergeToolbarWith(ToolStrip parentToolStrip)
		{
			ToolStripManager.Merge(FToolStrip, parentToolStrip);
		}

		void IDisposable.Dispose()
		{
			// TODO:  Add FormDesigner.System.IDisposable.Dispose implementation
		}

		#region IServiceProvider Members

		public new virtual object GetService(Type serviceType)
		{
			if (serviceType == typeof (IDesignService))
				return Service;
			object result = base.GetService(serviceType);
			if (result != null)
				return result;
			return Dataphoria.GetService(serviceType);
		}

		#endregion

		#region Help

		protected override void OnHelpRequested(HelpEventArgs args)
		{
			base.OnHelpRequested(args);
			string keyword;
			if (SelectedPaletteItem != null)
				keyword = SelectedPaletteItem.ClassName;
			else
			{
				if (ActiveControl.Name == "FNodesTree")
					keyword = FNodesTree.SelectedNode.Node.GetType().Name;
				else
					keyword = FPropertyGrid.SelectedObject.GetType().Name;
			}
			NodeTypeEntry entry = FrontendSession.NodeTypeTable[keyword];
			if (entry != null)
				keyword = entry.Namespace + "." + keyword;
			Dataphoria.InvokeHelp(keyword);
		}

		#endregion

		#region IContainer

		// IContainer is implemented because Sites are required to have containers

		public ComponentCollection Components
		{
			get { return new ComponentCollection(new IComponent[] {}); }
		}

		public void Remove(IComponent component)
		{
			// nadda
		}

		public void Add(IComponent component, string name)
		{
			// nadda
		}

		void IContainer.Add(IComponent component)
		{
			// nadda
		}

		#endregion

		private void InitializeDocking()
		{
			// 
			// FPaletteGroupBar
			// 
			/*FPaletteGroupBar = new GroupBar();
			FPaletteGroupBar.AllowDrop = true;
			FPaletteGroupBar.BackColor = SystemColors.Control;
			FPaletteGroupBar.BorderStyle = BorderStyle.FixedSingle;
			FPaletteGroupBar.Dock = DockStyle.Fill;
			FPaletteGroupBar.Location = new Point(0, 24);
			FPaletteGroupBar.Name = "FPaletteGroupBar";
			FPaletteGroupBar.SelectedItem = 0;
			FPaletteGroupBar.Size = new Size(163, 163);
			FPaletteGroupBar.TabIndex = 1;*/
			// 
			// FPointerGroupView
			// 
			/*FPointerGroupView = new GroupView
									{
										BorderStyle = BorderStyle.None,
										ButtonView = true,
										Dock = DockStyle.Top
									};
			FPointerGroupView.GroupViewItems.AddRange(new[]
														  {
															  new GroupViewItem("Pointer", 0)
														  });
			FPointerGroupView.IntegratedScrolling = true;
			FPointerGroupView.ItemYSpacing = 2;
			FPointerGroupView.LargeImageList = null;
			FPointerGroupView.Location = new Point(0, 0);
			FPointerGroupView.Name = "FPointerGroupView";
			FPointerGroupView.SelectedItem = 0;
			FPointerGroupView.Size = new Size(163, 24);
			FPointerGroupView.SmallImageList = FPointerImageList;
			FPointerGroupView.SmallImageView = true;
			FPointerGroupView.TabIndex = 0;
			FPointerGroupView.Text = "groupView2";
			FPointerGroupView.GroupViewItemSelected += FPointerGroupView_GroupViewItemSelected;
			*/

			// 
			// FNodesTree
			// 
			FNodesTree = new DesignerTree.DesignerTree();
			FNodesTree.AllowDrop = true;
			FNodesTree.BorderStyle = BorderStyle.None;
			FNodesTree.CausesValidation = false;
			FNodesTree.Dock = DockStyle.Fill;
			FNodesTree.HideSelection = false;
			FNodesTree.ImageList = FNodesImageList;
			FNodesTree.Location = new Point(0, 0);
			FNodesTree.Name = "FNodesTree";
			FNodesTree.ShowRootLines = false;
			FNodesTree.Size = new Size(283, 209);
			FNodesTree.TabIndex = 0;
			FNodesTree.AfterSelect += FNodesTree_AfterSelect;
			FNodesTree.Dock = DockStyle.Fill;

			_dockContentNodesTree = new DockContent();
			_dockContentNodesTree.HideOnClose = true;
			_dockContentNodesTree.Controls.Add(FNodesTree);
			_dockContentNodesTree.TabText = "Nodes Tree";
			_dockContentNodesTree.Text = "Nodes Tree - Dataphoria";
			_dockContentNodesTree.ShowHint = DockState.Document;
			_dockContentNodesTree.Show(FDockPanel);
			
			// 
			// FPalettePanel
			// 
			_palettePanel = new ToolBox.ToolBox();
			_palettePanel.NodesTree = this.FNodesTree;
			_palettePanel.Location = new Point(1, 21);
			_palettePanel.Name = "FPalettePanel";
			_palettePanel.Size = new Size(163, 187);
			_palettePanel.TabIndex = 1;
			_palettePanel.Dock = DockStyle.Fill;
			_palettePanel.StatusChanged += FPalettePanel_StatusChanged;

			_dockContentPalettePanel = new DockContent();
			_dockContentPalettePanel.HideOnClose = true;
			_dockContentPalettePanel.Controls.Add(_palettePanel);
			_dockContentPalettePanel.TabText = "Node Palette";
			_dockContentPalettePanel.Text = "Node Palette - Dataphoria";
			_dockContentPalettePanel.ShowHint = DockState.DockLeft;
			_dockContentPalettePanel.Show(FDockPanel);

			// 
			// FFormPanel
			// 
			FFormPanel = new FormPanel();
			FFormPanel.BackColor = SystemColors.ControlDark;
			FFormPanel.Location = new Point(1, 21);
			FFormPanel.Name = "FFormPanel";
			FFormPanel.Size = new Size(685, 283);
			FFormPanel.TabIndex = 3;
			FFormPanel.Dock = DockStyle.Fill;

			_dockContentFormPanel = new DockContent();
			_dockContentFormPanel.HideOnClose = true;
			_dockContentFormPanel.Controls.Add(FFormPanel);
			_dockContentFormPanel.TabText = "Form - Dataphoria";
			_dockContentFormPanel.Text = "Form";
			_dockContentFormPanel.ShowHint = DockState.DockBottom;
			_dockContentFormPanel.Show(FDockPanel);

			// 
			// FPropertyGrid
			// 
			FPropertyGrid = new PropertyGrid();
			FPropertyGrid.BackColor = SystemColors.Control;
			FPropertyGrid.CausesValidation = false;
			FPropertyGrid.CommandsVisibleIfAvailable = true;
			FPropertyGrid.Cursor = Cursors.HSplit;
			FPropertyGrid.LargeButtons = false;
			FPropertyGrid.LineColor = SystemColors.ScrollBar;
			FPropertyGrid.Location = new Point(1, 21);
			FPropertyGrid.Name = "FPropertyGrid";
			FPropertyGrid.PropertySort = PropertySort.Alphabetical;
			FPropertyGrid.TabIndex = 2;
			FPropertyGrid.Text = "Properties of the Currently Selected Node";
			FPropertyGrid.ToolbarVisible = false;
			FPropertyGrid.ViewBackColor = SystemColors.Window;
			FPropertyGrid.ViewForeColor = SystemColors.WindowText;
			FPropertyGrid.PropertyValueChanged +=NodePropertyGrid_PropertyValueChanged;
			FPropertyGrid.Dock = DockStyle.Fill;

			_dockContentPropertyGrid = new DockContent();
			_dockContentPropertyGrid.HideOnClose = true;
			_dockContentPropertyGrid.Controls.Add(FPropertyGrid);
			_dockContentPropertyGrid.TabText = "Properties";
			_dockContentPropertyGrid.Text = "Properties - Dataphoria";
			_dockContentPropertyGrid.ShowHint = DockState.DockRight;
			_dockContentPropertyGrid.Show(FDockPanel);
		}

		private void FPalettePanel_StatusChanged(object sender, StatusEventArgs args)
		{
			this.SetStatus(args.Description);
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			try
			{
				_service.CheckModified();
				if (_isDesignHostOwner && (!FrontendSession.CloseAllForms(_designHost, CloseBehavior.AcceptOrClose)))
					// if we are hosting, close the child forms
					throw new AbortException();
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}

		#region FrontendSession

		private Session _frontendSession;

		[Browsable(false)]
		public Session FrontendSession
		{
			get { return _frontendSession; }
		}

		/// <summary> Prepares (or re-prepares) the frontend session and the component palette </summary>
		private void PrepareSession()
		{
			if (_frontendSession == null)
			{
				_frontendSession = Dataphoria.GetLiveDesignableFrontendSession();
				_palettePanel.FrontendSession = this._frontendSession;
			}
			_frontendSession.SetFormDesigner();
			_palettePanel.ClearPalette();
			_palettePanel.LoadPalette();
		}

		private void FormDesignerLibrariesChanged(object sender, EventArgs args)
		{
			PrepareSession();
		}

		#endregion

		#region Service

		private IDesignService _service;

		[Browsable(false)]
		public IDesignService Service
		{
			get { return _service; }
		}

		public void InitializeService(IDataphoria dataphoria)
		{
			_service = new DesignService(dataphoria, this);
			_service.OnModifiedChanged += NameOrModifiedChanged;
			_service.OnNameChanged += NameOrModifiedChanged;
			_service.OnRequestLoad += RequestLoad;
			_service.OnRequestSave += RequestSave;
		}

		private void NameOrModifiedChanged(object sender, EventArgs args)
		{
			UpdateTitle();
		}

		protected virtual void RequestLoad(DesignService service, DesignBuffer buffer)
		{
			SetDesignHost(HostFromBuffer(buffer), true);
		}

		protected virtual void RequestSave(DesignService service, DesignBuffer buffer)
		{
			Serializer serializer = FrontendSession.CreateSerializer();
			var document = new XDocument();
			serializer.Serialize(document, _designHost.Children[0]);
			Dataphoria.Warnings.AppendErrors(this, serializer.Errors, true);

			var stream = new MemoryStream();
			var xmlTextWriter = XmlWriter.Create(stream, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true });
			document.Save(xmlTextWriter);
			xmlTextWriter.Flush();
			byte[] writerString = stream.ToArray();
			buffer.SaveData(Encoding.UTF8.GetString(writerString, 0, writerString.Length));

			UpdateHostsDocument(buffer);
		}

		#endregion

		#region Tree Nodes

		private void FNodesTree_AfterSelect(object sender, TreeViewEventArgs args)
		{
			ActivateNode((DesignerTree.DesignerNode)args.Node);
		}

		public void ActivateNode(DesignerTree.DesignerNode node)
		{
			if ((FPropertyGrid.SelectedObject != null) && (FPropertyGrid.SelectedObject is IDisposableNotify))
				((IDisposableNotify) FPropertyGrid.SelectedObject).Disposed -= SelectedNodeDisposed;

			bool editsAllowed;
			if (node == null)
			{
				FPropertyGrid.SelectedObject = null;
				editsAllowed = false;
			}
			else
			{
				FPropertyGrid.SelectedObject = node.Node;
				node.Node.Disposed += SelectedNodeDisposed;
				editsAllowed = !node.ReadOnly;
			}
			FDeleteToolStripMenuItem.Enabled = editsAllowed;
			FRenameToolStripMenuItem.Enabled = editsAllowed;
			FCutToolStripMenuItem.Enabled = editsAllowed;
		}

		private void SelectedNodeDisposed(object sender, EventArgs args)
		{
			ActivateNode(FNodesTree.SelectedNode);
		}

		private void NodePropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_service.SetModified(true);
		}

		#endregion

		#region IErrorSource

		void IErrorSource.ErrorHighlighted(Exception exception)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception exception)
		{
			Focus();
		}

		#endregion

		#region Palette

		private Hashtable _imageIndex = new Hashtable();
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
				Dataphoria.Warnings.AppendError(this, exception, true);
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
				object indexResult = _imageIndex[imageAttribute.ImageExpression];
				if (indexResult == null)
				{
					Image image = LoadImage(imageAttribute.ImageExpression);
					if (image != null)
					{
						if (image is Bitmap)
							((Bitmap) image).MakeTransparent();
						FNodesImageList.Images.Add(image);
						int index = FNodesImageList.Images.Count - 1;
						_imageIndex.Add(imageAttribute.ImageExpression, index);
						return index;
					}
					_imageIndex.Add(imageAttribute.ImageExpression, 0);
				}
				else
					return (int) indexResult;
			}
			return 0; // Zero is the reserved index for the default image
		}
			  

		#endregion

		#region IDesigner, New, Loading, Saving

		private string _designerID;

		[Browsable(false)]
		public string DesignerID
		{
			get { return _designerID; }
		}

		public void Open(DesignBuffer buffer)
		{
			_service.Open(buffer);
		}

		/// <remarks> 
		///		Note that this method should not be confused with Form.Close().  
		///		Be sure to deal with a compile-time instance of type IDesigner 
		///		to invoke this method. 
		///	</remarks>
		void IDesigner.Show()
		{
			UpdateTitle();
			Dataphoria.AttachForm(this);

			// HACK: Don't know why, but for some reason, setting the MDIParent of this form collapses the nodes tree.
			FNodesTree.ExpandAll();
		}

		public virtual void New()
		{
			IHost host = FrontendSession.CreateHost();
			try
			{
				INode node = GetNewDesignNode();
				host.Children.Add(node);
				host.Open();
				InternalNew(host, true);
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		public virtual void Open(IHost host)
		{
			DocumentDesignBuffer buffer = BufferFromHost(host);
			_service.ValidateBuffer(buffer);
			SetDesignHost(host, false);
			_service.SetBuffer(buffer);
			_service.SetModified(false);
		}

		protected void InternalNew(IHost host, bool owner)
		{
			_service.SetBuffer(null);
			_service.SetModified(false);
			SetDesignHost(host, owner);
		}

		public void Save()
		{
			_service.Save();
		}

		public void SaveAsFile()
		{
			_service.SaveAsFile();
		}

		public void SaveAsDocument()
		{
			_service.SaveAsDocument();
		}

		protected virtual INode GetNewDesignNode()
		{
			var form = (IWindowsFormInterface) FrontendSession.CreateForm();
			Dataphoria.AddDesignerForm(form, this);
			return form;
		}

		protected DocumentDesignBuffer BufferFromHost(IHost host)
		{
			DocumentExpression expression = Program.GetDocumentExpression(host.Document);
			var buffer = 
				new DocumentDesignBuffer
				(
					Dataphoria, 
					expression.DocumentArgs.LibraryName,
					expression.DocumentArgs.DocumentName
				);
			return buffer;
		}

		public void New(IHost host)
		{
			InternalNew(host, false);
		}

		protected IHost HostFromBuffer(DesignBuffer buffer)
		{
			return HostFromDocumentData(buffer.LoadData(), GetDocumentExpression(buffer));
		}

		protected IHost HostFromDocumentData(XDocument documentData, string documentExpression)
		{
			IHost host = FrontendSession.CreateHost();
			try
			{
				Deserializer deserializer = FrontendSession.CreateDeserializer();
				INode instance = GetNewDesignNode();
				try
				{
					deserializer.Deserialize(documentData, instance);
					Dataphoria.Warnings.AppendErrors(this, deserializer.Errors, true);
					host.Children.Add(instance);
					host.Document = documentExpression;
				}
				catch
				{
					instance.Dispose();
					throw;
				}
				host.Open();

				return host;
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}

		protected IHost HostFromDocumentData(string documentData, string documentExpression)
		{
			IHost host = FrontendSession.CreateHost();
			try
			{
				Deserializer deserializer = FrontendSession.CreateDeserializer();
				INode instance = GetNewDesignNode();
				try
				{
					deserializer.Deserialize(documentData, instance);
					Dataphoria.Warnings.AppendErrors(this, deserializer.Errors, true);
					host.Children.Add(instance);
					host.Document = documentExpression;
				}
				catch
				{
					instance.Dispose();
					throw;
				}
				host.Open();

				return host;
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}

		private void UpdateTitle()
		{
			TabText =
				String.Format
					(
					"{0} - {1}{2}",
					(_isDesignHostOwner ? Strings.Designer : Strings.LiveDesigner),
					_service.GetDescription(),
					(_service.IsModified ? "*" : String.Empty)
					);
		}

		protected string GetDocumentExpression(DesignBuffer buffer)
		{
			var localBuffer = buffer as DocumentDesignBuffer;
			if (localBuffer == null)
				return String.Empty;
			return String.Format(".Frontend.Form('{0}', '{1}')", localBuffer.LibraryName, localBuffer.DocumentName);
		}

		protected void UpdateHostsDocument(DesignBuffer buffer)
		{
			DesignHost.Document = GetDocumentExpression(buffer);
		}

		#endregion

		#region DesignHost

		private bool _designFormClosing;
		private IHost _designHost;

		private bool _isDesignHostOwner;

		[Browsable(false)]
		public IHost DesignHost
		{
			get { return _designHost; }
		}

		[Browsable(false)]
		public bool IsDesignHostOwner
		{
			get { return _isDesignHostOwner; }
		}

		protected virtual void DetachDesignHost()
		{
			var form = _designHost.Children[0] as IWindowsFormInterface;
			if (form != null)
				form.Form.Closing -= DesignFormClosing;
			FFormPanel.ClearHostedForm();
		}

		protected virtual void AttachDesignHost(IHost host)
		{
			var form = host.Children[0] as IWindowsFormInterface;
			if (form != null)
			{
				form.Form.Closing += DesignFormClosing;
				FFormPanel.SetHostedForm(form, _isDesignHostOwner);
			}
		}

		private void ClearNodesTree()
		{
			foreach (DesignerTree.DesignerNode root in FNodesTree.Nodes)
				root.Dispose();
			FNodesTree.Nodes.Clear();
		}

		protected void SetDesignHost(IHost host, bool owner)
		{
			if (host != _designHost)
			{
				SuspendLayout();
				try
				{
					if (_designHost != null)
					{
						ActivateNode(null);
						_palettePanel.SelectPaletteItem(null, false);

						DetachDesignHost();
						if (_isDesignHostOwner && !_designFormClosing)
							((IWindowsFormInterface) _designHost.Children[0]).Close(CloseBehavior.RejectOrClose);
						_designHost = null;

						FNodesTree.BeginUpdate();
						try
						{
							ClearNodesTree();
						}
						finally
						{
							FNodesTree.EndUpdate();
						}
					}

					_designHost = host;
					_isDesignHostOwner = owner;
					try
					{
						if (_designHost != null)
						{
							FNodesTree.BeginUpdate();
							try
							{
								if (_designHost.Children.Count != 0)
								{
									FNodesTree.SelectedNode = FNodesTree.AddNode(_designHost.Children[0]);
									FNodesTree.SelectedNode.SetReadOnly(true, false);
									ActivateNode(FNodesTree.SelectedNode);
										// the tree doesn't initially raise an ActiveChanged event
								}
							}
							finally
							{
								FNodesTree.EndUpdate();
							}

							AttachDesignHost(_designHost);
						}
					}
					catch
					{
						_designHost = null;
						ClearNodesTree();
						throw;
					}
				}
				finally
				{
					ResumeLayout(true);
				}
			}
		}

		protected void DesignFormClosing(object sender, CancelEventArgs args)
		{
			try
			{
				if (!args.Cancel)
				{
					_designFormClosing = true;
					try
					{
						Close();
						if (!IsDisposed) // The abort of the close does not propigate, so we have to check (&%!@#*)
							throw new AbortException();
					}
					finally
					{
						_designFormClosing = false;
					}
				}
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}

		#endregion

		#region Commands

		private void DeleteNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.Delete();
		}

		private void RenameNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.Rename();
		}

		private void PasteNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.PasteFromClipboard();
		}

		private void CopyNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.CopyToClipboard();
		}

		private void CutNode()
		{
			if (FNodesTree.SelectedNode != null)
				FNodesTree.SelectedNode.CutToClipboard();
		}

		private void ShowPalette()
		{
			_dockContentPalettePanel.Show(FDockPanel);						
		}

		private void ShowProperties()
		{
			_dockContentPropertyGrid.Show(FDockPanel);						
		}

		private void ShowForm()
		{
			_dockContentFormPanel.Show(FDockPanel);						
		}


		private void FMainMenuStrip_ItemClicked(object sender, EventArgs args)
		{
			if (sender == FSaveToolStripButton || sender == FSaveToolStripMenuItem)
			{
				Save();
			}
			else if (sender == FSaveAsFileToolStripMenuItem || sender == FSaveAsFileToolStripButton)
			{
				SaveAsFile();
			}
			else if (sender == FSaveAsDocumentToolStripMenuItem || sender == FSaveAsDocumentToolStripButton)
			{
				SaveAsDocument();
			}
			else if (sender == FCloseToolStripMenuItem)
			{
				Close();
			}
			else if (sender == FCutToolStripMenuItem || sender == FCutToolStripButton)
			{
				CutNode();
			}
			else if (sender == FCopyToolStripMenuItem || sender == FCopyToolStripButton)
			{
				CopyNode();
			}
			else if (sender == FPasteToolStripMenuItem || sender == FPasteToolStripButton)
			{
				PasteNode();
			}
			else if (sender == FDeleteToolStripMenuItem || sender == FDeleteToolStripButton)
			{
				DeleteNode();
			}
			else if (sender == FRenameToolStripMenuItem || sender == FRenameToolStripButton)
			{
				RenameNode();
			}
			else if (sender == FPaletteToolStripMenuItem)
			{
				ShowPalette();
			}
			else if (sender == FPropertiesToolStripMenuItem)
			{
				ShowProperties();
			}
			else if (sender == FFormToolStripMenuItem)
			{
				ShowForm();
			}
		}

		#endregion

		public void PaletteItemDropped()
		{
			_palettePanel.PaletteItemDropped();
		}
	}
}