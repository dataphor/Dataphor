/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Windows.Forms;
using System.Collections;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public abstract class ListNode : DataNode
	{
		public override BuildMode BuildMode
		{
			get { return BuildMode.OnExpand; }
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_RefreshMenuText, new EventHandler(RefreshClicked), Shortcut.F5));
			return menu;
		}

		private void RefreshClicked(object sender, EventArgs args)
		{
			Refresh();
		}
		
		protected virtual bool SortChildren
		{
			get { return true; }
		}

		protected abstract string GetChildExpression();

		protected virtual DAE.Runtime.DataParams GetParams()
		{
			return null;
		}
		
		protected abstract BaseNode CreateChildNode(DAE.Runtime.Data.IRow row);

        protected override void InternalReconcileChildren()
        {
            ArrayList items = new ArrayList(Nodes.Count);
            foreach (TreeNode node in Nodes)
                items.Add(node);
            Dataphoria.Execute(GetChildExpression(), GetParams(), ARow =>
                                                                      {
                                                                          TreeNode node = FindByKey(ARow);
                                                                          if (node != null)
                                                                          {
                                                                              items.Remove(node);
                                                                              ReconcileNode((BaseNode)node, ARow);
                                                                          }
                                                                          else
                                                                              AddNode(CreateChildNode(ARow));
                                                                      });
            foreach (TreeNode node in items)
                Nodes.Remove(node);
        }

 		/// <summary> Finds the first node using the specified row. </summary>
		/// <returns> The matching node reference or null (if not found). </returns>
		public BaseNode FindByKey(DAE.Runtime.Data.IRow row)
		{
			ItemNode itemNode;
			foreach (TreeNode node in Nodes)
			{
				itemNode = node as ItemNode;
				if ((itemNode != null) && itemNode.IsEqual(row))
					return itemNode;
			}
			return null;
		}

 		/// <summary> Finds the first node using the specified text. </summary>
		/// <returns> The matching node reference or null (if not found). </returns>
		public BaseNode FindByText(string text)
		{
			foreach (TreeNode node in Nodes)
			{
				if (node.Text.Trim() == text)
					return node as BaseNode;
			}
			return null;
		}

		public void AddNode(BaseNode node)
		{
			if (SortChildren)
			{
				// Insertion sort
				for (int i = Nodes.Count - 1; i >= 0; i--)
				{
					if (String.Compare(node.Text, Nodes[i].Text) > 0)
					{
						InsertBaseNode(i + 1, node);
						return;
					}
				}
				InsertBaseNode(0, node);
			}
			else
				AddBaseNode(node);
		}
		
		protected virtual void ReconcileNode(BaseNode node, DAE.Runtime.Data.IRow row)
		{
			// Stub for node-level reconciliation
		}
	}
	
	public abstract class BrowseNode : ListNode
	{
		protected MenuItem _addSeparator;
		protected MenuItem _addMenuItem;
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			_addSeparator = new MenuItem("-");
			menu.MenuItems.Add(0, _addSeparator);
			_addMenuItem = new MenuItem(Strings.ObjectTree_AddMenuText, new EventHandler(AddClicked), Shortcut.Ins);
			menu.MenuItems.Add(0, _addMenuItem);
			return menu;
		}

		protected void SetInsertOpenState(Frontend.Client.IFormInterface form)
		{
			form.MainSource.OpenState = DAE.Client.DataSetState.Insert;
		}

		protected virtual void AddClicked(object sender, EventArgs args)
		{
			Frontend.Client.Windows.IWindowsFormInterface form = Dataphoria.FrontendSession.LoadForm(null, AddDocument(), new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (form.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
				BaseNode node = CreateChildNode(form.MainSource.DataView.ActiveRow);
				AddNode(node);
				TreeView.SelectedNode = node;
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		protected abstract string AddDocument();
	}

	public abstract class ItemNode : DataNode
	{
		public override BuildMode BuildMode
		{
			get { return BuildMode.Never; }
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewMenuText, new EventHandler(ViewClicked), Shortcut.CtrlF2));
			return menu;
		}

		public ListNode ParentList
		{
			get { return (ListNode)Parent; }
		}

		protected abstract string ViewDocument();

		protected virtual void UpdateText()
		{
		}

		public abstract string GetFilter();

		public abstract bool IsEqual(DAE.Runtime.Data.IRow row);

		public virtual void View()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(ViewDocument(), form);
				form.MainSource.Filter = GetFilter();
				form.HostNode.Open();
				form.ShowModal(Frontend.Client.FormMode.None);
			}
		}

		private void ViewClicked(object sender, EventArgs args)
		{
			View();
		}
	}

	public abstract class EditableItemNode : ItemNode
	{
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			menu.MenuItems.Add(0, new MenuItem(Strings.ObjectTree_EditMenuText, new EventHandler(EditClicked), Shortcut.F2));
			menu.MenuItems.Add(0, new MenuItem(Strings.ObjectTree_DeleteMenuText, new EventHandler(DeleteClicked), Shortcut.Del));
			return menu;
		}

		protected virtual string EditDocument()
		{
			return String.Empty;
		}
		
		protected virtual string DeleteDocument()
		{
			return String.Empty;
		}
		
		private string _newText;
		
		protected virtual string KeyColumnName()
		{
			return null;
		}

		private void EditDataViewOnValidate(object sender, EventArgs args)
		{
			string AKey = KeyColumnName();
			if (AKey != null)
				_newText = ((DAE.Client.DataView)sender)[AKey].AsDisplayString;
			else
				_newText = Text;
		}

		protected virtual void PrepareEditForm(Frontend.Client.Windows.IWindowsFormInterface form)
		{
			form.MainSource.Filter = GetFilter();
		}

		public virtual void Edit()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(EditDocument(), form);
				PrepareEditForm(form);
				form.HostNode.Open();
				form.MainSource.DataView.OnValidate += new EventHandler(EditDataViewOnValidate);
				if (form.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				if (_newText != Text.Trim())
					NameChanged(_newText);
			}
		}

		protected virtual void NameChanged(string newName)
		{
			ListNode parent = ParentList;
			Parent.Nodes.Remove(this);
			UpdateText();
			parent.AddNode(this);
			TreeView.SelectedNode = this;
		}

		private void EditClicked(object sender, EventArgs args)
		{
			Edit();
		}
		
		public virtual void Delete()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface form = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(DeleteDocument(), form);
				form.MainSource.Filter = GetFilter();
				form.HostNode.Open();
				if (form.ShowModal(Frontend.Client.FormMode.Delete) != DialogResult.OK)
					throw new AbortException();
			}
			Remove();
		}

		private void DeleteClicked(object sender, EventArgs args)
		{
			Delete();
		}
	}

	public abstract class SchemaListNode : ListNode
	{
		protected const string SchemaListFilter = 
			@"
				where (Library_Name = ALibraryName) 
				where (AShowGenerated or not(IsGenerated)) 
					and (AShowSystem or not(IsSystem))
			";

		public SchemaListNode(string libraryName)
		{
			_libraryName = libraryName;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			MenuItem menuItem = new MenuItem(Strings.ObjectTree_ShowGeneratedObjects, new EventHandler(ShowGeneratedObjectsClicked));
			menuItem.Checked = false;
			menu.MenuItems.Add(0, menuItem);
			menuItem = new MenuItem(Strings.ObjectTree_ShowSystemObjects, new EventHandler(ShowSystemObjectsClicked));
			menuItem.Checked = true;
			menu.MenuItems.Add(1, menuItem);
			menu.MenuItems.Add(2, new MenuItem("-"));
			return menu;
		}

		private bool _showGeneratedObjects;
		public bool ShowGeneratedObjects { get { return _showGeneratedObjects; } }
		
		private bool _showSystemObjects = true;
		public bool ShowSystemObjects { get { return _showSystemObjects; } }

		protected override Alphora.Dataphor.DAE.Runtime.DataParams GetParams()
		{
			DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
			paramsValue.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "AShowGenerated", _showGeneratedObjects));
			paramsValue.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "AShowSystem", _showSystemObjects));
			paramsValue.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "ALibraryName", _libraryName));
			return paramsValue;
		}
		
		private void ShowGeneratedObjectsClicked(object sender, EventArgs args)
		{
			MenuItem menuItem = (MenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			_showGeneratedObjects = menuItem.Checked;
			Refresh();
		}
		
		private void ShowSystemObjectsClicked(object sender, EventArgs args)
		{
			MenuItem menuItem = (MenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			_showSystemObjects = menuItem.Checked;
			Refresh();
		}
		
		private string _libraryName;
		public string LibraryName
		{
			get { return _libraryName; }
		}

		public string QualifyObjectName(string name)
		{
			if (name.StartsWith("."))
				return name;
			else
				return _libraryName + "." + name;
		}

		public string UnqualifyObjectName(string name)
		{
			if (name.IndexOf(_libraryName) == 0)
				return name.Substring(_libraryName.Length + 1);
			else
				return "." + name;
		}
	}

	public abstract class SchemaItemNode : ItemNode
	{
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_DropMenuText, new EventHandler(DropClicked), Shortcut.Del));
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewDependencies, new EventHandler(ViewDependenciesClicked)));
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewDependents, new EventHandler(ViewDependentsClicked)));
			menu.MenuItems.Add(new MenuItem("-"));
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateScriptClicked)));
			menu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitDropScriptMenuText, new EventHandler(EmitDropScriptClicked)));
			return menu;
		}

		private SchemaListNode _parentSchemaList;
		public SchemaListNode ParentSchemaList
		{
			get { return _parentSchemaList; }
			set { _parentSchemaList = value; }
		}

		private string _objectName;
		public string ObjectName
		{
			get { return _objectName; }
			set
			{
				_objectName = value;
				UpdateText();
			}
		}

		public override bool IsEqual(DAE.Runtime.Data.IRow row)
		{
			return ((string)row["Name"] == _objectName);
		}

		public override string GetFilter()
		{
			return String.Format("Name = '{0}'", _objectName);
		}

		protected override void UpdateText()
		{
			Text = ParentSchemaList.UnqualifyObjectName(_objectName);
		}

		private void ViewDependentsClicked(object sender, EventArgs args)
		{
			ViewDependents();
		}
		
		private void ViewDependenciesClicked(object sender, EventArgs args)
		{
			ViewDependencies();
		}

		private void DropClicked(object sender, EventArgs args)
		{
			Drop();
		}

		private void EmitCreateScriptClicked(object sender, EventArgs args)
		{
			EmitCreate();
		}

		private void EmitDropScriptClicked(object sender, EventArgs args)
		{
			EmitDrop();
		}

		protected virtual void Drop()
		{
			// Confirm the deletion
			Frontend.Client.Windows.IWindowsFormInterface form = 
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					".Frontend.Form('Frontend', 'DropDependents')"
				);
			try
			{
				Frontend.Client.ISource source = (Frontend.Client.ISource)form.FindNode("Dependents");
				source.Expression = 
					String.Format
					(
						@"	
							DependentObjects('{0}')
								over {{ Level, Sequence, Object_Description }}
								rename {{ Object_Description Description }}
								order by {{ Level desc, Sequence }}
						",
						ObjectName
					);
				source.Enabled = true;
				if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				form.HostNode.Dispose();
			}

			// Emit and execute the drop script
			using (DAE.Runtime.Data.Scalar script = (DAE.Runtime.Data.Scalar)Dataphoria.EvaluateQuery(GetScriptDropExpression()))
				Dataphoria.ExecuteScript(script.AsString);

			ParentList.Refresh();
		}

		private string GetScriptDropExpression()
		{
			return String.Format(".System.ScriptDrop('{0}')", ObjectName);
		}

		protected virtual void EmitCreate()
		{
			Dataphoria.EvaluateAndEdit(String.Format(".System.Script('{0}')", ObjectName), "d4");
		}

		protected virtual void EmitDrop()
		{
			Dataphoria.EvaluateAndEdit(GetScriptDropExpression(), "d4");
		}

		protected virtual void ViewDependents()
		{
			Frontend.Client.Windows.IWindowsFormInterface form = 
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					String.Format
					(
						@"
							Frontend.Derive
							(
								'
									DependentObjects(''{0}'')
										over {{ Level, Sequence, Object_Description }}
										rename {{ Object_Description Description }}
										adorn
										{{
											Description tags {{ Frontend.Priority = ''50'', Frontend.Width = ''60'' }},
											Level tags {{ Frontend.Priority = ''100'', Frontend.Width = ''7'' }},
											Sequence tags {{ Frontend.Priority = ''150'', Frontend.Width = ''12'' }}
										}}
											tags {{ Frontend.Title = ''{1}'' }}
										order by {{ Level desc, Sequence }}
								', 
								'List'
							)
						",
						ObjectName,
						Strings.DependentObjects
					)
				);
			try
			{
				if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		protected virtual void ViewDependencies()
		{
			Frontend.Client.Windows.IWindowsFormInterface form = 
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					String.Format
					(
						@"
							Frontend.Derive
							(
								'
									RequiredObjects(''{0}'')
										over {{ Level, Sequence, Object_Description }}
										rename {{ Object_Description Description }}
										adorn
										{{
											Description tags {{ Frontend.Priority = ''50'', Frontend.Width = ''60'' }},
											Level tags {{ Frontend.Priority = ''100'', Frontend.Width = ''7'' }},
											Sequence tags {{ Frontend.Priority = ''150'', Frontend.Width = ''12'' }}
										}}
											tags {{ Frontend.Title = ''{1}'' }}
										order by {{ Level desc, Sequence }}
								', 
								'List'
							)
						",
						ObjectName,
						Strings.RequiredObjects
					)
				);
			try
			{
				if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		protected abstract string GetViewExpression();

		protected override string ViewDocument()
		{
			return String.Format(".Frontend.Derive('{0} adorn tags {{ Frontend.Title = ''{1}'' }}', 'View', false)", GetViewExpression(), Text);
		}
	}
}
