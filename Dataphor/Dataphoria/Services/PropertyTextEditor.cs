/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.DAE.Client.Controls.Design;

namespace Alphora.Dataphor.Dataphoria.Services
{
	public class PropertyTextEditorService : IPropertyTextEditorService
	{
		/// <summary> Called by the multi-line property editor to edit a property. </summary>
		public void EditProperty(object AInstance, PropertyDescriptor ADescriptor)
		{
			// Determine the DocumentTypeID by looking for a DocumentType attribute or by looking for a ScriptAction specifically
			string LDocumentTypeID;
			DAE.Client.Design.EditorDocumentTypeAttribute LDocumentTypeAttribute = (DAE.Client.Design.EditorDocumentTypeAttribute)ADescriptor.Attributes[typeof(DAE.Client.Design.EditorDocumentTypeAttribute)];
			if (LDocumentTypeAttribute != null)
				LDocumentTypeID = LDocumentTypeAttribute.DocumentTypeID;
			else
			{
				Frontend.Client.IScriptAction LAction = AInstance as Frontend.Client.IScriptAction;
				if (LAction == null)
					LDocumentTypeID = "txt";
				else
					if (LAction.Language == Frontend.Client.ScriptLanguages.CSharp)
						LDocumentTypeID = "cs";
					else
						LDocumentTypeID = "vb";
			}

			// Create a buffer for this property
			PropertyDesignBuffer LBuffer = new PropertyDesignBuffer(Dataphoria.DataphoriaInstance, AInstance, ADescriptor);

			// Look for an existing buffer
			IDesigner LDesigner = Dataphoria.DataphoriaInstance.GetDesigner(LBuffer);
			if (LDesigner != null)
				LDesigner.Select();		// activate existing designer
			else
				// Construct a new designer to manage the events between these objects
				new PropertyDesignerManager(AInstance, ADescriptor, Dataphoria.DataphoriaInstance.OpenDesigner(Dataphoria.DataphoriaInstance.GetDefaultDesigner(LDocumentTypeID), LBuffer));
		}
	}

	public class PropertyDesignerManager
	{
		/// <param name="ADesigner"> The property designer. </param>
		public PropertyDesignerManager(object AInstance, PropertyDescriptor ADescriptor, IDesigner ADesigner)
		{
			FDesigner = ADesigner;
			FDesigner.Disposed += new EventHandler(DesignerDisposed);
			FDesigner.Service.OnRequestSave += new RequestHandler(DesignerRequestedSave);

			FDescriptor = ADescriptor;
			FDescriptor.AddValueChanged(AInstance, new EventHandler(PropertyChanged));

			FInstance = AInstance;
			IDisposableNotify LInstance = FInstance as IDisposableNotify;
			if (LInstance != null)
				LInstance.Disposed += new EventHandler(InstanceDisposed);

			ISite LSite = ((IComponent)FInstance).Site;
			if (LSite != null)
			{
				FService = (IDesignService)LSite.GetService(typeof(IDesignService));
				FService.Dependants.Add(FDesigner.Service);
			}
			else
				FService = null;
		}

		Alphora.Dataphor.Dataphoria.Designers.IDesigner FDesigner;
		private PropertyDescriptor FDescriptor;
		private object FInstance;
		private IDesignService FService;

		private void PropertyChanged(object ASender, EventArgs AArgs)
		{
			FDesigner.Service.SetModified(true);
		}

		private void InstanceDisposed(object sender, EventArgs e)
		{
			FDesigner.Service.SetModified(false);	// don't bother confirming changes... this instance has been disposed!
			FDesigner.Close();
			FDescriptor.RemoveValueChanged(FInstance, new EventHandler(PropertyChanged));
		}

		private void DesignerDisposed(object sender, EventArgs e)
		{
			IDisposableNotify LInstance = FInstance as IDisposableNotify;
			if (LInstance != null)
				LInstance.Disposed -= new EventHandler(InstanceDisposed);
			FDescriptor.RemoveValueChanged(FInstance, new EventHandler(PropertyChanged));
			if (FService != null)
				FService.Dependants.Remove(FDesigner.Service);
		}

		private void DesignerRequestedSave(DesignService AService, DesignBuffer ABuffer)
		{
			if (FService != null)
				FService.SetModified(true);
		}
	}

}
