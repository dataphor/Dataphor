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
		public void EditProperty(object instance, PropertyDescriptor descriptor)
		{
			// Determine the DocumentTypeID by looking for a DocumentType attribute or by looking for a ScriptAction specifically
			string documentTypeID;
			DAE.Client.Design.EditorDocumentTypeAttribute documentTypeAttribute = (DAE.Client.Design.EditorDocumentTypeAttribute)descriptor.Attributes[typeof(DAE.Client.Design.EditorDocumentTypeAttribute)];
			if (documentTypeAttribute != null)
				documentTypeID = documentTypeAttribute.DocumentTypeID;
			else
			{
				Frontend.Client.IScriptAction action = instance as Frontend.Client.IScriptAction;
				if (action == null)
					documentTypeID = "txt";
				else
					if (action.Language == Frontend.Client.ScriptLanguages.CSharp)
						documentTypeID = "cs";
					else
						documentTypeID = "vb";
			}

			// Create a buffer for this property
            PropertyDesignBuffer buffer = new PropertyDesignBuffer(Program.DataphoriaInstance, instance, descriptor);

			// Look for an existing buffer
            IDesigner designer = Program.DataphoriaInstance.GetDesigner(buffer);
			if (designer != null)
				designer.Select();		// activate existing designer
			else
				// Construct a new designer to manage the events between these objects
                new PropertyDesignerManager(instance, descriptor, Program.DataphoriaInstance.OpenDesigner(Program.DataphoriaInstance.GetDefaultDesigner(documentTypeID), buffer));
		}
	}

	public class PropertyDesignerManager
	{
		/// <param name="designer"> The property designer. </param>
		public PropertyDesignerManager(object instance, PropertyDescriptor descriptor, IDesigner designer)
		{
			_designer = designer;
			_designer.Disposed += new EventHandler(DesignerDisposed);
			_designer.Service.OnRequestSave += new RequestHandler(DesignerRequestedSave);

			_descriptor = descriptor;
			_descriptor.AddValueChanged(instance, new EventHandler(PropertyChanged));

			_instance = instance;
			IDisposableNotify localInstance = _instance as IDisposableNotify;
			if (localInstance != null)
				localInstance.Disposed += new EventHandler(InstanceDisposed);

			ISite site = ((IComponent)_instance).Site;
			if (site != null)
			{
				_service = (IDesignService)site.GetService(typeof(IDesignService));
				_service.Dependants.Add(_designer.Service);
			}
			else
				_service = null;
		}

		Alphora.Dataphor.Dataphoria.Designers.IDesigner _designer;
		private PropertyDescriptor _descriptor;
		private object _instance;
		private IDesignService _service;

		private void PropertyChanged(object sender, EventArgs args)
		{
			_designer.Service.SetModified(true);
		}

		private void InstanceDisposed(object sender, EventArgs e)
		{
			_designer.Service.SetModified(false);	// don't bother confirming changes... this instance has been disposed!
			_designer.Close();
			_descriptor.RemoveValueChanged(_instance, new EventHandler(PropertyChanged));
		}

		private void DesignerDisposed(object sender, EventArgs e)
		{
			IDisposableNotify instance = _instance as IDisposableNotify;
			if (instance != null)
				instance.Disposed -= new EventHandler(InstanceDisposed);
			_descriptor.RemoveValueChanged(_instance, new EventHandler(PropertyChanged));
			if (_service != null)
				_service.Dependants.Remove(_designer.Service);
		}

		private void DesignerRequestedSave(DesignService service, DesignBuffer buffer)
		{
			if (_service != null)
				_service.SetModified(true);
		}
	}

}
