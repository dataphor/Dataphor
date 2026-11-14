/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client
{
	public class LookupAction : DataAction, ILookupAction
	{
		protected override void Dispose(bool disposing)
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
				base.Dispose(disposing);
			}
		}


		// MasterKeyNames

		private string _masterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) which provide the values to filter the target set.  Used with DetailKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set { _masterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string _detailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) by which the target set will be filtered.  Used with LookupColumnNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set { _detailKeyNames = value == null ? String.Empty : value; }
		}

		// LookupColumnNames

		private string _lookupColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be read from the lookups' source.")]
		public string LookupColumnNames
		{
			get { return _lookupColumnNames; }
			set
			{
				if (_lookupColumnNames != value)
					_lookupColumnNames = value;
			}
		}

		public string GetLookupColumnNames()
		{
			return _lookupColumnNames;
		}

		// ColumnNames

		private string _columnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be set by the lookup.")]
		public string ColumnNames
		{
			get { return _columnNames; }
			set
			{
				if (_columnNames != value)
					_columnNames = value;
			}
		}

		public string GetColumnNames()
		{
			return _columnNames;
		}

		// Document

		private string _document = String.Empty;
		[DefaultValue("")]
		[Description("A form interface document which will be shown to perform the lookup.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DocumentExpressionOperator("Form")]
		public string Document
		{
			get { return _document; }
			set { _document = value; }
		}
		
		// AutoPost

		private bool _autoPost = true;
		[DefaultValue(true)]
		[Description("Indicates whether the source will be posted automatically after a lookup.")]
		public bool AutoPost
		{
			get { return _autoPost; }
			set { _autoPost = value; }
		}

		// OnLookupAccepted

		private IAction _onLookupAccepted;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the lookup form has been accepted.")]
		public IAction OnLookupAccepted
		{
			get { return _onLookupAccepted; }
			set 
			{
				if (_onLookupAccepted != null)
					_onLookupAccepted.Disposed -= new EventHandler(FormAcceptedActionDisposed);
				_onLookupAccepted = value;	
				if (_onLookupAccepted != null)
					_onLookupAccepted.Disposed += new EventHandler(FormAcceptedActionDisposed);
			}
		}

		private void FormAcceptedActionDisposed(object sender, EventArgs args)
		{
			OnLookupAccepted = null;
		}

		// OnLookupClose

		protected IAction _onLookupClose;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the lookup form has been closed.")]
		public IAction OnLookupClose
		{
			get { return _onLookupClose; }
			set	
			{ 
				if (_onLookupClose != null)
					_onLookupClose.Disposed -= new EventHandler(FormCloseActionDisposed);
				_onLookupClose = value;	
				if (_onLookupClose != null)
					_onLookupClose.Disposed += new EventHandler(FormCloseActionDisposed);
			}
		}

		private void FormCloseActionDisposed(object sender, EventArgs args)
		{
			OnLookupClose = null;
		}

		// OnLookupRejected

		protected IAction _onLookupRejected;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the lookup form has been rejected.")]
		public IAction OnLookupRejected
		{
			get { return _onLookupRejected; }
			set	
			{ 
				if (_onLookupRejected != null)
					_onLookupRejected.Disposed -= new EventHandler(FormRejectedActionDisposed);
				_onLookupRejected = value;	
				if (_onLookupRejected != null)
					_onLookupRejected.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void FormRejectedActionDisposed(object sender, EventArgs args)
		{
			OnLookupRejected = null;
		}

		// BeforeLookupActivated

		protected IAction _beforeLookupActivated;
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		[Description("An action to execute after the lookup form is created, but before it is activated.")]
		public IAction BeforeLookupActivated
		{
			get { return _beforeLookupActivated; }
			set	
			{ 
				if (_beforeLookupActivated != null)
					_beforeLookupActivated.Disposed -= new EventHandler(FormRejectedActionDisposed);
				_beforeLookupActivated = value;	
				if (_beforeLookupActivated != null)
					_beforeLookupActivated.Disposed += new EventHandler(FormRejectedActionDisposed);
			}
		}

		private void BeforeLookupActivatedDisposed(object sender, EventArgs args)
		{
			BeforeLookupActivated = null;
		}

		public void LookupFormAccept(IFormInterface form) 
		{
			string[] lookupColumns = LookupColumnNames.Split(DAE.Client.DataView.ColumnNameDelimiters);
			string[] sourceColumns = ColumnNames.Split(DAE.Client.DataView.ColumnNameDelimiters);

			//Assign the field values
			for (int i = 0; i < sourceColumns.Length; i++)
				Source.DataSource.DataSet.Fields[sourceColumns[i]].Value = form.MainSource.DataView.Fields[lookupColumns[i]].Value;
				
			if (_autoPost)
				Source.DataView.Post();

			if (_onLookupAccepted != null)
				_onLookupAccepted.Execute(this, new EventParams("AForm", form));

			if (_onLookupClose != null)
				_onLookupClose.Execute(this, new EventParams("AForm", form));
		}

		public void LookupFormReject(IFormInterface form)
		{
			if (_onLookupRejected != null)
				_onLookupRejected.Execute(this, new EventParams("AForm", form));

			if (_onLookupClose != null)
				_onLookupClose.Execute(this, new EventParams("AForm", form));
		}

		public void LookupFormInitialize(IFormInterface form) 
		{
			if (_beforeLookupActivated != null)
				_beforeLookupActivated.Execute(this, new EventParams("AForm", form));
		}

		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			LookupUtility.DoLookup(this, new FormInterfaceHandler(LookupFormAccept), new FormInterfaceHandler(LookupFormReject), null);
		}
	}
}

