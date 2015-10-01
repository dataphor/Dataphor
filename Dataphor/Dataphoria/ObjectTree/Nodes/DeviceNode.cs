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
		public DeviceListNode(string libraryName) : base (libraryName)
		{
			Text = "Devices";
			ImageIndex = 18;
			SelectedImageIndex = ImageIndex;
		}

		protected override string GetChildExpression()
		{
			return ".System.Devices " + SchemaListFilter + " over { Name, ReconciliationMode }";
		}
		
		protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
		{
			return new DeviceNode(this, (string)row["Name"], (DAE.Language.D4.ReconcileMode)Enum.Parse(typeof(DAE.Language.D4.ReconcileMode), (string)row["ReconciliationMode"], true));
		}
	}

	public class DeviceNode : SchemaItemNode
	{
		public DeviceNode(DeviceListNode node, string deviceName, DAE.Language.D4.ReconcileMode reconciliationMode) : base()
		{
			ParentSchemaList = node;
			ObjectName = deviceName;
			ImageIndex = 19;
			SelectedImageIndex = ImageIndex;
			_reconciliationMode = reconciliationMode;
		}
		
		private MenuItem LNone;
		private MenuItem LStartup;
		private MenuItem LCommand;
		private MenuItem LAutomatic;
		private DAE.Language.D4.ReconcileMode _reconciliationMode;

		protected override ContextMenu GetContextMenu()
		{
			ContextMenu menu = base.GetContextMenu();
			LNone = new MenuItem(Strings.ObjectTree_ReconciliationModeNoneMenuText, new EventHandler(NoneClicked));
			LNone.Checked = _reconciliationMode == DAE.Language.D4.ReconcileMode.None;
			LStartup = new MenuItem(Strings.ObjectTree_ReconciliationModeStartupMenuText, new EventHandler(StartupClicked));
			LStartup.Checked = (_reconciliationMode & DAE.Language.D4.ReconcileMode.Startup) != 0;
			LCommand = new MenuItem(Strings.ObjectTree_ReconciliationModeCommandMenuText, new EventHandler(CommandClicked));
			LCommand.Checked = (_reconciliationMode & DAE.Language.D4.ReconcileMode.Command) != 0;
			LAutomatic = new MenuItem(Strings.ObjectTree_ReconciliationModeAutomaticMenuText, new EventHandler(AutomaticClicked));
			LAutomatic.Checked = (_reconciliationMode & DAE.Language.D4.ReconcileMode.Automatic) != 0;
			menu.MenuItems.Add
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
			return menu;
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

		private void NoneClicked(object sender, EventArgs args)
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
		
		private void StartupClicked(object sender, EventArgs args)
		{
			LStartup.Checked = !LStartup.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}

		private void CommandClicked(object sender, EventArgs args)
		{
			LCommand.Checked = !LCommand.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}
		
		private void AutomaticClicked(object sender, EventArgs args)
		{
			LAutomatic.Checked = !LAutomatic.Checked;
			LNone.Checked = (!(LStartup.Checked || LCommand.Checked || LAutomatic.Checked));
			UpdateReconciliationMode();
		}
	}
}
