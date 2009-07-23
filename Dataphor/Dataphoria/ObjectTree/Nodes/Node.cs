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
			ContextMenu LMenu = new ContextMenu();
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_RefreshMenuText, new EventHandler(RefreshClicked), Shortcut.F5));
			return LMenu;
		}

		private void RefreshClicked(object ASender, EventArgs AArgs)
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
		
		protected abstract BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow);
		
		protected override void InternalReconcileChildren()
		{
			ArrayList LItems = new ArrayList(Nodes.Count);
			foreach (TreeNode LNode in Nodes)
				LItems.Add(LNode);

			DAE.IServerCursor LCursor = Dataphoria.OpenCursor(GetChildExpression(), GetParams());
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					TreeNode LNode;
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						LNode = FindByKey(LRow);
						if (LNode != null)
						{
							LItems.Remove(LNode);
							ReconcileNode((BaseNode)LNode, LRow);
						}
						else
							AddNode(CreateChildNode(LRow));
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				Dataphoria.CloseCursor(LCursor);
			}

			foreach (TreeNode LNode in LItems)
				Nodes.Remove(LNode);
		}

 		/// <summary> Finds the first node using the specified row. </summary>
		/// <returns> The matching node reference or null (if not found). </returns>
		public BaseNode FindByKey(DAE.Runtime.Data.Row ARow)
		{
			ItemNode LItemNode;
			foreach (TreeNode LNode in Nodes)
			{
				LItemNode = LNode as ItemNode;
				if ((LItemNode != null) && LItemNode.IsEqual(ARow))
					return LItemNode;
			}
			return null;
		}

 		/// <summary> Finds the first node using the specified text. </summary>
		/// <returns> The matching node reference or null (if not found). </returns>
		public BaseNode FindByText(string AText)
		{
			foreach (TreeNode LNode in Nodes)
			{
				if (LNode.Text.Trim() == AText)
					return LNode as BaseNode;
			}
			return null;
		}

		public void AddNode(BaseNode ANode)
		{
			if (SortChildren)
			{
				// Insertion sort
				for (int i = Nodes.Count - 1; i >= 0; i--)
				{
					if (String.Compare(ANode.Text, Nodes[i].Text) > 0)
					{
						InsertBaseNode(i + 1, ANode);
						return;
					}
				}
				InsertBaseNode(0, ANode);
			}
			else
				AddBaseNode(ANode);
		}
		
		protected virtual void ReconcileNode(BaseNode ANode, DAE.Runtime.Data.Row ARow)
		{
			// Stub for node-level reconciliation
		}
	}
	
	public abstract class BrowseNode : ListNode
	{
		protected MenuItem FAddSeparator;
		protected MenuItem FAddMenuItem;
		
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			FAddSeparator = new MenuItem("-");
			LMenu.MenuItems.Add(0, FAddSeparator);
			FAddMenuItem = new MenuItem(Strings.ObjectTree_AddMenuText, new EventHandler(AddClicked), Shortcut.Ins);
			LMenu.MenuItems.Add(0, FAddMenuItem);
			return LMenu;
		}

		protected void SetInsertOpenState(Frontend.Client.IFormInterface AForm)
		{
			AForm.MainSource.OpenState = DAE.Client.DataSetState.Insert;
		}

		protected virtual void AddClicked(object ASender, EventArgs AArgs)
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = Dataphoria.FrontendSession.LoadForm(null, AddDocument(), new Frontend.Client.FormInterfaceHandler(SetInsertOpenState));
			try
			{
				if (LForm.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
					throw new AbortException();
				BaseNode LNode = CreateChildNode(LForm.MainSource.DataView.ActiveRow);
				AddNode(LNode);
				TreeView.SelectedNode = LNode;
			}
			finally
			{
				LForm.HostNode.Dispose();
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
			ContextMenu LMenu = new ContextMenu();
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewMenuText, new EventHandler(ViewClicked), Shortcut.CtrlF2));
			return LMenu;
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

		public abstract bool IsEqual(DAE.Runtime.Data.Row ARow);

		public virtual void View()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(ViewDocument(), LForm);
				LForm.MainSource.Filter = GetFilter();
				LForm.HostNode.Open();
				LForm.ShowModal(Frontend.Client.FormMode.None);
			}
		}

		private void ViewClicked(object ASender, EventArgs AArgs)
		{
			View();
		}
	}

	public abstract class EditableItemNode : ItemNode
	{
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add(0, new MenuItem(Strings.ObjectTree_EditMenuText, new EventHandler(EditClicked), Shortcut.F2));
			LMenu.MenuItems.Add(0, new MenuItem(Strings.ObjectTree_DeleteMenuText, new EventHandler(DeleteClicked), Shortcut.Del));
			return LMenu;
		}

		protected virtual string EditDocument()
		{
			return String.Empty;
		}
		
		protected virtual string DeleteDocument()
		{
			return String.Empty;
		}
		
		private string FNewText;
		
		protected virtual string KeyColumnName()
		{
			return null;
		}

		private void EditDataViewOnValidate(object ASender, EventArgs AArgs)
		{
			string AKey = KeyColumnName();
			if (AKey != null)
				FNewText = ((DAE.Client.DataView)ASender)[AKey].AsDisplayString;
			else
				FNewText = Text;
		}

		protected virtual void PrepareEditForm(Frontend.Client.Windows.IWindowsFormInterface AForm)
		{
			AForm.MainSource.Filter = GetFilter();
		}

		public virtual void Edit()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(EditDocument(), LForm);
				PrepareEditForm(LForm);
				LForm.HostNode.Open();
				LForm.MainSource.DataView.OnValidate += new EventHandler(EditDataViewOnValidate);
				if (LForm.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				if (FNewText != Text.Trim())
					NameChanged(FNewText);
			}
		}

		protected virtual void NameChanged(string ANewName)
		{
			ListNode LParent = ParentList;
			Parent.Nodes.Remove(this);
			UpdateText();
			LParent.AddNode(this);
			TreeView.SelectedNode = this;
		}

		private void EditClicked(object ASender, EventArgs AArgs)
		{
			Edit();
		}
		
		public virtual void Delete()
		{
			using (Frontend.Client.Windows.IWindowsFormInterface LForm = (Frontend.Client.Windows.IWindowsFormInterface)Dataphoria.FrontendSession.CreateForm())
			{
				Dataphoria.FrontendSession.CreateHost().Load(DeleteDocument(), LForm);
				LForm.MainSource.Filter = GetFilter();
				LForm.HostNode.Open();
				if (LForm.ShowModal(Frontend.Client.FormMode.Delete) != DialogResult.OK)
					throw new AbortException();
			}
			Remove();
		}

		private void DeleteClicked(object ASender, EventArgs AArgs)
		{
			Delete();
		}
	}

	public abstract class SchemaListNode : ListNode
	{
		protected const string CSchemaListFilter = 
			@"
				where (Library_Name = ALibraryName) 
				where (AShowGenerated or not(IsGenerated)) 
					and (AShowSystem or not(IsSystem))
			";

		public SchemaListNode(string ALibraryName)
		{
			FLibraryName = ALibraryName;
		}

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			MenuItem LMenuItem = new MenuItem(Strings.ObjectTree_ShowGeneratedObjects, new EventHandler(ShowGeneratedObjectsClicked));
			LMenuItem.Checked = false;
			LMenu.MenuItems.Add(0, LMenuItem);
			LMenuItem = new MenuItem(Strings.ObjectTree_ShowSystemObjects, new EventHandler(ShowSystemObjectsClicked));
			LMenuItem.Checked = true;
			LMenu.MenuItems.Add(1, LMenuItem);
			LMenu.MenuItems.Add(2, new MenuItem("-"));
			return LMenu;
		}

		private bool FShowGeneratedObjects;
		public bool ShowGeneratedObjects { get { return FShowGeneratedObjects; } }
		
		private bool FShowSystemObjects = true;
		public bool ShowSystemObjects { get { return FShowSystemObjects; } }

		protected override Alphora.Dataphor.DAE.Runtime.DataParams GetParams()
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "AShowGenerated", FShowGeneratedObjects));
			LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "AShowSystem", FShowSystemObjects));
			LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "ALibraryName", FLibraryName));
			return LParams;
		}
		
		private void ShowGeneratedObjectsClicked(object ASender, EventArgs AArgs)
		{
			MenuItem LMenuItem = (MenuItem)ASender;
			LMenuItem.Checked = !LMenuItem.Checked;
			FShowGeneratedObjects = LMenuItem.Checked;
			Refresh();
		}
		
		private void ShowSystemObjectsClicked(object ASender, EventArgs AArgs)
		{
			MenuItem LMenuItem = (MenuItem)ASender;
			LMenuItem.Checked = !LMenuItem.Checked;
			FShowSystemObjects = LMenuItem.Checked;
			Refresh();
		}
		
		private string FLibraryName;
		public string LibraryName
		{
			get { return FLibraryName; }
		}

		public string QualifyObjectName(string AName)
		{
			if (AName.StartsWith("."))
				return AName;
			else
				return FLibraryName + "." + AName;
		}

		public string UnqualifyObjectName(string AName)
		{
			if (AName.IndexOf(FLibraryName) == 0)
				return AName.Substring(FLibraryName.Length + 1);
			else
				return "." + AName;
		}
	}

	public abstract class SchemaItemNode : ItemNode
	{
		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_DropMenuText, new EventHandler(DropClicked), Shortcut.Del));
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewDependencies, new EventHandler(ViewDependenciesClicked)));
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_ViewDependents, new EventHandler(ViewDependentsClicked)));
			LMenu.MenuItems.Add(new MenuItem("-"));
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitCreateScriptMenuText, new EventHandler(EmitCreateScriptClicked)));
			LMenu.MenuItems.Add(new MenuItem(Strings.ObjectTree_EmitDropScriptMenuText, new EventHandler(EmitDropScriptClicked)));
			return LMenu;
		}

		private SchemaListNode FParentSchemaList;
		public SchemaListNode ParentSchemaList
		{
			get { return FParentSchemaList; }
			set { FParentSchemaList = value; }
		}

		private string FObjectName;
		public string ObjectName
		{
			get { return FObjectName; }
			set
			{
				FObjectName = value;
				UpdateText();
			}
		}

		public override bool IsEqual(DAE.Runtime.Data.Row ARow)
		{
			return ((string)ARow["Name"] == FObjectName);
		}

		public override string GetFilter()
		{
			return String.Format("Name = '{0}'", FObjectName);
		}

		protected override void UpdateText()
		{
			Text = ParentSchemaList.UnqualifyObjectName(FObjectName);
		}

		private void ViewDependentsClicked(object ASender, EventArgs AArgs)
		{
			ViewDependents();
		}
		
		private void ViewDependenciesClicked(object ASender, EventArgs AArgs)
		{
			ViewDependencies();
		}

		private void DropClicked(object ASender, EventArgs AArgs)
		{
			Drop();
		}

		private void EmitCreateScriptClicked(object ASender, EventArgs AArgs)
		{
			EmitCreate();
		}

		private void EmitDropScriptClicked(object ASender, EventArgs AArgs)
		{
			EmitDrop();
		}

		protected virtual void Drop()
		{
			// Confirm the deletion
			Frontend.Client.Windows.IWindowsFormInterface LForm = 
				Dataphoria.FrontendSession.LoadForm
				(
					null,
					".Frontend.Form('Frontend', 'DropDependents')"
				);
			try
			{
				Frontend.Client.ISource LSource = (Frontend.Client.ISource)LForm.FindNode("Dependents");
				LSource.Expression = 
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
				LSource.Enabled = true;
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				LForm.HostNode.Dispose();
			}

			// Emit and execute the drop script
			using (DAE.Runtime.Data.Scalar LScript = (DAE.Runtime.Data.Scalar)Dataphoria.EvaluateQuery(GetScriptDropExpression()))
				Dataphoria.ExecuteScript(LScript.AsString);

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
			Frontend.Client.Windows.IWindowsFormInterface LForm = 
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
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		protected virtual void ViewDependencies()
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = 
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
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		protected abstract string GetViewExpression();

		protected override string ViewDocument()
		{
			return String.Format(".Frontend.Derive('{0} adorn tags {{ Frontend.Title = ''{1}'' }}', 'View', false)", GetViewExpression(), Text);
		}
	}
}
