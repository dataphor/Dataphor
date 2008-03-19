/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.ComponentModel;
using System.Drawing.Design;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client
{
	public class LookupAction : DataAction, ILookupAction
	{
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				OnLookupAccepted = null;
				OnLookupRejected = null;
				OnLookupClose = null;
				BeforeLookupActivated = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}


		// MasterKeyNames

		private string FMasterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) which provide the values to filter the target set.  Used with DetailKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set { FMasterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string FDetailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) by which the target set will be filtered.  Used with LookupColumnNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set { FDetailKeyNames = value == null ? String.Empty : value; }
		}

		// LookupColumnNames

		private string FLookupColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be read from the lookups' source.")]
		public string LookupColumnNames
		{
			get { return FLookupColumnNames; }
			set
			{
				if (FLookupColumnNames != value)
					FLookupColumnNames = value;
			}
		}

		public string GetLookupColumnNames()
		{
			return FLookupColumnNames;
		}

		// ColumnNames

		private string FColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be set by the lookup.")]
		public string ColumnNames
		{
			get { return FColumnNames; }
			set
			{
				if (FColumnNames != value)
					FColumnNames = value;
			}
		}

		public string GetColumnNames()
		{
			return FColumnNames;
		}

		// Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		[Description("A form interface document which will be shown to perform the lookup.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return FDocument; }
			set { FDocument = value; }
		}
		
		// AutoPost

		private bool FAutoPost = true;
		[DefaultValue(true)]
		[Description("Indicates whether the source will be posted automatically after a lookup.")]
		public bool AutoPost
		{
			get { return FAutoPost; }
			set { FAutoPost = value; }
		}

		// OnLookupAccepted

		private IAction FOnLookupAccepted;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the lookup form has been accepted.")]
		public IAction OnLookupAccepted
		{
			get { return FOnLookupAccepted; }
			set 
			{
				if (FOnLookupAccepted != null)
					FOnLookupAccepted.Disposed -= new EventHandler(FormAcceptedActionDisposed);
				FOnLookupAccepted = value;	
				if (FOnLookupAccepted != null)
					FOnLookupAccepted.Disposed += new EventHandler(FormAcceptedActionDisposed);
			}
		}

		private void FormAcceptedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnLookupAccepted = null;
		}

		// OnLookupClose

		protected IAction FOnLookupClose;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the lookup form has been closed.")]
		public IAction OnLookupClose
		{
			get { return FOnLookupClose; }
			set	
			{ 
				if (FOnLookupClose != null)
					FOnLookupClose.Disposed -= new EventHandler(FormCloseActionDisposed);
				FOnLookupClose = value;	
				if (FOnLookupClose != null)
					FOnLookupClose.Disposed += new EventHandler(FormCloseActionDisposed);
			}
		}

		private void FormCloseActionDisposed(object ASender, EventArgs AArgs)
		{
			OnLookupClose = null;
		}

		// OnLookupRejected

		protected IAction FOnLookupRejected;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the lookup form has been rejected.")]
		public IAction OnLookupRejected
		{
			get { return FOnLookupRejected; }
			set	
			{ 
				if (FOnLookupRejected != null)
					FOnLookupRejected.Disposed -= new EventHandler(FormRejectedActionDisposed);
				FOnLookupRejected = value;	
				if (FOnLookupRejected != null)
					FOnLookupRejected.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void FormRejectedActionDisposed(object ASender, EventArgs AArgs)
		{
			OnLookupRejected = null;
		}

		// BeforeLookupActivated

		protected IAction FBeforeLookupActivated;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action to execute after the lookup form is created, but before it is activated.")]
		public IAction BeforeLookupActivated
		{
			get { return FBeforeLookupActivated; }
			set	
			{ 
				if (FBeforeLookupActivated != null)
					FBeforeLookupActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				FBeforeLookupActivated = value;	
				if (FBeforeLookupActivated != null)
					FBeforeLookupActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void BeforeLookupActivatedDisposed(object ASender, EventArgs AArgs)
		{
			BeforeLookupActivated = null;
		}

		public void LookupFormAccept(IFormInterface AForm) 
		{
			string[] LLookupColumns = LookupColumnNames.Split(DAE.Client.DataView.CColumnNameDelimiters);
			string[] LSourceColumns = ColumnNames.Split(DAE.Client.DataView.CColumnNameDelimiters);

			//Assign the field values
			for (int i = 0; i < LSourceColumns.Length; i++)
				Source.DataSource.DataSet.Fields[LSourceColumns[i]].Value = AForm.MainSource.DataView.Fields[LLookupColumns[i]].Value;
				
			if (FAutoPost)
				Source.DataView.Post();

			if (FOnLookupAccepted != null)
				FOnLookupAccepted.Execute(this, new EventParams("AForm", AForm));

			if (FOnLookupClose != null)
				FOnLookupClose.Execute(this, new EventParams("AForm", AForm));
		}

		public void LookupFormReject(IFormInterface AForm)
		{
			if (FOnLookupRejected != null)
				FOnLookupRejected.Execute(this, new EventParams("AForm", AForm));

			if (FOnLookupClose != null)
				FOnLookupClose.Execute(this, new EventParams("AForm", AForm));
		}

		public void LookupFormInitialize(IFormInterface AForm) 
		{
			if (FBeforeLookupActivated != null)
				FBeforeLookupActivated.Execute(this, new EventParams("AForm", AForm));
		}

		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			LookupUtility.DoLookup(this, new FormInterfaceHandler(LookupFormAccept), new FormInterfaceHandler(LookupFormReject), null);
		}
	}
}

