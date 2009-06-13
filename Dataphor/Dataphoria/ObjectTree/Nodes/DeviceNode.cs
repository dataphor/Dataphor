/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Specialized;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
	public class DeviceListNode : SchemaListNode
	{
		public DeviceListNode(string ALibraryName) : base (ALibraryName)
		{
			Text = "Devices";
			ImageIndex = 18;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.Devices " + CSchemaListFilter + " over { Name, ReconciliationMode }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
		{
			return new DeviceNode(this, ARow["Name"].AsString, (DAE.Language.D4.ReconcileMode)Enum.Parse(typeof(DAE.Language.D4.ReconcileMode), ARow["ReconciliationMode"].AsString, true));
		}
	}

	public class DeviceNode : SchemaItemNode
	{
		public DeviceNode(DeviceListNode ANode, string ADeviceName, DAE.Language.D4.ReconcileMode AReconciliationMode) : base()
		{
			ParentSchemaList = ANode;
			ObjectName = ADeviceName;
			ImageIndex = 19;
			SelectedImageIndex = ImageIndex;
			FReconciliationMode = AReconciliationMode;
		}
		
		private MenuItem LNone;
		private MenuItem LStartup;
		private MenuItem LCommand;
		private MenuItem LAutomatic;
		private DAE.Language.D4.ReconcileMode FReconciliationMode;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu LMenu = base.GetContextMenu();
			LNone = new MenuItem(Strings.ObjectTree_ReconciliationModeNoneMenuText, new EventHandler(NoneClicked));
			LNone.Checked = FReconciliationMode == DAE.Language.D4.ReconcileMode.None;
			LStartup = new MenuItem(Strings.ObjectTree_ReconciliationModeStartupMenuText, new EventHandler(StartupClicked));
			LStartup.Checked = (FReconciliationMode & DAE.Language.D4.ReconcileMode.Startup) != 0;
			LCommand = new MenuItem(Strings.ObjectTree_ReconciliationModeCommandMenuText, new EventHandler(CommandClicked));
			LCommand.Checked = (FReconciliationMode & DAE.Language.D4.ReconcileMode.Command) != 0;
			LAutomatic = new MenuItem(Strings.ObjectTree_ReconciliationModeAutomaticMenuText, new EventHandler(AutomaticClicked));
			LAutomatic.Checked = (FReconciliationMode & DAE.Language.D4.ReconcileMode.Automatic) != 0;
			LMenu.MenuItems.Add
			(
				0,
				new MenuItem
				(
					Strings.ObjectTree_ReconciliationModeMenuText, 
					new MenuItem[]
					{
						LNone,
						LStartup,
						LCommand,
						LAutomatic
					}
				)
			);
			return LMenu;
		}

		protected override string GetViewExpression()
		{
			return ".System.Devices";
		}
		
		private void UpdateReconciliationMode()
		{
			Dataphoria.ExecuteScript
			(	
				String.Format
				(	
					"alter device .{0} alter reconciliation {{ mode = {{ {1} }} }};", 
					ObjectName,
					DAE.Language.D4.ReconcileMode.None |
						(LStartup.Checked ? DAE.Language.D4.ReconcileMode.Startup : DAE.Language.D4.ReconcileMode.None) |
						(LCommand.Checked ? DAE.Language.D4.ReconcileMode.Command : DAE.Language.D4.ReconcileMode.None) |
						(LAutomatic.Checked ? DAE.Language.D4.ReconcileMode.Automatic : DAE.Language.D4.ReconcileMode.None)
				)
			);
		}

		private void NoneClicked(object ASender, EventArgs AArgs)
		{
			LNone.Checked = !LNone.Checked;
			if (LNone.Checked)
			{
				LStartup.Checked = false;
				LCommand.Checked = false;
				LAutomatic.Checked = false;
			}
			else
			{
				if (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked))
					LNone.Checked = true;
			}
			UpdateReconciliationMode();
		}
		
		private void StartupClicked(object ASender, EventArgs AArgs)
		{
			LStartup.Checked = !LStartup.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}

		private void CommandClicked(object ASender, EventArgs AArgs)
		{
			LCommand.Checked = !LCommand.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}
		
		private void AutomaticClicked(object ASender, EventArgs AArgs)
		{
			LAutomatic.Checked = !LAutomatic.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}
	}
}
