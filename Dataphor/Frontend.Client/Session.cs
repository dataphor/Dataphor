/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Collections.Generic;
using System.Reflection;

using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[ListInDesigner(false)]
	public abstract class Session : Disposable
	{
		public const string ApplicationNodeTableExpression = @".Frontend.GetApplicationNodeTypes(AClientType, AApplicationID)";
		public const string GetLibraryFilesExpression = @".Frontend.GetLibraryFiles(.Frontend.ClientTypes[AClientType].Environment, ApplicationLibraries where Application_ID = AApplicationID over { Library_Name })";
		public const string PrepareApplicationExpression = @".Frontend.PrepareApplication(AApplicationID)";

		public Session(DataSession session, bool ownsSession)
		{
			_dataSession = session;
			_ownsDataSession = ownsSession;
			InitializePipe();
			_forms = new Forms();
			_forms.Added += new FormsHandler(FormAdded);
			_forms.Removed += new FormsHandler(FormRemoved);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				UninitializePipe();
			}
			finally
			{
				try
				{
					if (_ownsDataSession && (_dataSession != null))
					{
						_dataSession.Dispose();
						_dataSession = null;
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		#region Pipe

		private Pipe _pipe;

		public Pipe Pipe
		{
			get { return _pipe; }
		}

		protected virtual void InitializePipe()
		{
			_pipe = new Pipe(_dataSession.ServerSession);
		}

		protected virtual void UninitializePipe()
		{
			if (_pipe != null)
			{
				_pipe.Dispose();
				_pipe = null;
			}
		}
		
		#endregion

		// AreImagesLoaded
		
		public virtual bool AreImagesLoaded()
		{
			return true;
		}

		// DataSession

		private DataSession _dataSession;
		private bool _ownsDataSession;

		public DAE.Client.DataSession DataSession
		{
			get { return _dataSession; }
		}
		
		// Errors
		
		public virtual void ReportErrors(IHost host, ErrorList errors)
		{
			// Do nothing here, this is overridden by the Windows Session to report errors to Dataphoria when the session is hosted w/in the IDE.
		}

		// InvokeHelp

		public virtual void InvokeHelp(INode sender, string helpKeyword, HelpKeywordBehavior helpKeywordBehavior, string helpString) {}

		#region NodeTypeTable Setup

		private NodeTypeTable _nodeTypeTable = new NodeTypeTable();

		public NodeTypeTable NodeTypeTable
		{
			get { return _nodeTypeTable; }
		}

		protected void ValidateNodeTypeTable()
		{
			if (!NodeTypeTable.Contains("FormInterface"))
				throw new ClientException(ClientException.Codes.EmptyOrIncompleteNodeTypesTable);
		}

		/// <summary> Prepares for opening a document in the specified application. </summary>
		/// <returns> The starting document configured for the application. </returns>
		public virtual string SetApplication(string applicationID, string clientType)
		{
			DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
			paramsValue.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AApplicationID", applicationID));
			paramsValue.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AClientType", clientType));
			
			// Get the node types table
			using (DAE.Runtime.Data.Scalar nodeTable = DataSession.Evaluate(ApplicationNodeTableExpression, paramsValue))
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(nodeTable.AsString);
			}
			ValidateNodeTypeTable();

			// Prepare the application and get name of the starting document
			string documentString = null;
			using (DAE.Runtime.Data.Scalar startingDocument = DataSession.Evaluate(PrepareApplicationExpression, paramsValue))
			{
				documentString = startingDocument.AsString;
			}

			// Load the files required to register any nodes, if necessary				
			if (DataSession.Server is DAE.Server.LocalServer)
			{
				IServerCursor cursor = DataSession.OpenCursor(GetLibraryFilesExpression, paramsValue);
				try
				{
					using (DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow())
					{
						#if !SILVERLIGHT
						bool shouldLoad;
						List<string> filesToLoad = new List<string>();

						while (cursor.Next())
						{
							cursor.Select(row);
							string fullFileName = 
								((DAE.Server.LocalServer)DataSession.Server).GetFile
								(
									(DAE.Server.LocalProcess)cursor.Plan.Process, 
									(string)row["Library_Name"], 
									(string)row["Name"], 
									(DateTime)row["TimeStamp"], 
									(bool)row["IsDotNetAssembly"], 
									out shouldLoad
								);
							if (shouldLoad)
								filesToLoad.Add(fullFileName);
						}
						
						// Load each file to ensure they can be reached by the assembly resolver hack (see AssemblyUtility)
						foreach (string fullFileName in filesToLoad)
							Assembly.LoadFrom(fullFileName);
						#else
						while (cursor.Next())
						{
							cursor.Select(row);
							((DAE.Server.LocalServer)DataSession.Server).LoadAndRegister
							(
								(DAE.Server.LocalProcess)cursor.Plan.Process,
								cursor.Plan.Catalog.ClassLoader,
								(string)row["Library_Name"],
								(string)row["Name"],
								(bool)row["ShouldRegister"]
							);
						}
						#endif
					}
				}
				finally
				{
					DataSession.CloseCursor(cursor);
				}
			}
			
			return documentString;
		}
		
		#endregion

		#region Utility methods
		
		// NOTE: These methods are essentially the same overloads as are provided by the DataSession, except that they participate in the error reporting system of the Frontend session.
		
		public void ExecuteScript(string script)
		{
			ExecuteScript(null, script, null);
		}
		
		public void ExecuteScript(IServerProcess process, string script)
		{
			ExecuteScript(process, script, null);
		}

		public void ExecuteScript(string script, DAE.Runtime.DataParams paramsValue)
		{
			DataSession.ExecuteScript(null, script, paramsValue);
		}

		public void ExecuteScript(IServerProcess process, string script, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;

			DAE.IServerScript localScript = process.PrepareScript(script);
			try
			{
				foreach (DAE.IServerBatch batch in localScript.Batches)
					if (batch.IsExpression())
					{
						DAE.IServerExpressionPlan plan = batch.PrepareExpression(paramsValue);
						try
						{
							ErrorList errors = new ErrorList();
							errors.AddRange(plan.Messages);
							ReportErrors(null, errors);

							if (plan.DataType is DAE.Schema.TableType)
								plan.Close(plan.Open(paramsValue));
							else
								plan.Evaluate(paramsValue).Dispose();
						}
						finally
						{
							batch.UnprepareExpression(plan);
						}
					}
					else
					{
						DAE.IServerStatementPlan plan = batch.PrepareStatement(paramsValue);
						try
						{
							ErrorList errors = new ErrorList();
							errors.AddRange(plan.Messages);
							ReportErrors(null, errors);

							plan.Execute(paramsValue);
						}
						finally
						{
							batch.UnprepareStatement(plan);
						}
					}
			}
			finally
			{
				process.UnprepareScript(localScript);
			}
		}
		
		public void Execute(string statement)
		{
			Execute(null, statement, (DAE.Runtime.DataParams)null);
		}
		
		public void Execute(IServerProcess process, string statement)
		{
			Execute(process, statement, (DAE.Runtime.DataParams)null);
		}
		
		public void Execute(string statement, DAE.Runtime.DataParams paramsValue)
		{
			Execute(null, statement, paramsValue);
		}
		
		public void Execute(IServerProcess process, string statement, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
				
			DAE.IServerStatementPlan plan = process.PrepareStatement(statement, paramsValue);
			try
			{
				ErrorList errors = new ErrorList();
				errors.AddRange(plan.Messages);
				ReportErrors(null, errors);
				
				plan.Execute(paramsValue);
			}
			finally
			{
				process.UnprepareStatement(plan);
			}
		}

		public void Execute(string statement, params object[] paramsValue)
		{
			Execute(null, statement, paramsValue);
		}
		
		public void Execute(IServerProcess process, string statement, params object[] paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
				
			Execute(process, statement, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}

		public DAE.Runtime.Data.IDataValue Evaluate(string expression)
		{
			return Evaluate(null, expression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.IDataValue Evaluate(IServerProcess process, string expression)
		{
			return Evaluate(process, expression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.IDataValue Evaluate(string expression, DAE.Runtime.DataParams paramsValue)
		{
			return Evaluate(null, expression, paramsValue);
		}

		public DAE.Runtime.Data.IDataValue Evaluate(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
			
			DAE.IServerExpressionPlan plan = process.PrepareExpression(expression, paramsValue);
			try
			{
				ErrorList errors = new ErrorList();
				errors.AddRange(plan.Messages);
				ReportErrors(null, errors);
	
				return plan.Evaluate(paramsValue);
			}
			finally
			{
				process.UnprepareExpression(plan);
			}
		}

		/// <summary>Evaluates the given expression using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.IDataValue Evaluate(string expression, params object[] paramsValue)
		{
			return Evaluate(null, expression, paramsValue);
		}										  

		/// <summary>Evaluates the given expression on the given process and using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.IDataValue Evaluate(IServerProcess process, string expression, params object[] paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;

			return Evaluate(process, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}

		/// <summary>Evaluates the given expression on the given process and using the given parameter names and values.</summary>
		public DAE.Runtime.Data.IDataValue Evaluate(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
			
			return Evaluate(process, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramNames, paramsValue));
		}

		public DAE.Runtime.Data.Scalar EvaluateScalar(string expression)
		{
			return EvaluateScalar(null, expression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.Scalar EvaluateScalar(IServerProcess process, string expression)
		{
			return EvaluateScalar(process, expression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.Scalar EvaluateScalar(string expression, DAE.Runtime.DataParams paramsValue)
		{
			return EvaluateScalar(null, expression, paramsValue);
		}

		public DAE.Runtime.Data.Scalar EvaluateScalar(IServerProcess process, string expression, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;

			DAE.IServerExpressionPlan plan = process.PrepareExpression(expression, paramsValue);
			try
			{
				ErrorList errors = new ErrorList();
				errors.AddRange(plan.Messages);
				ReportErrors(null, errors);

				return (DAE.Runtime.Data.Scalar)plan.Evaluate(paramsValue);
			}
			finally
			{
				process.UnprepareExpression(plan);
			}
		}

		/// <summary>EvaluateScalars the given expression using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalar(string expression, params object[] paramsValue)
		{
			return EvaluateScalar(null, expression, paramsValue);
		}

		/// <summary>EvaluateScalars the given expression on the given process and using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalar(IServerProcess process, string expression, params object[] paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;

			return EvaluateScalar(process, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}

		/// <summary>EvaluateScalars the given expression on the given process and using the given parameter names and values.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalar(IServerProcess process, string expression, string[] paramNames, object[] paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;

			return EvaluateScalar(process, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramNames, paramsValue));
		}

		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateWith(Guid iD, string expression)
		{
			return EvaluateWith(null, iD, expression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateWith(Guid iD, string expression, DAE.Runtime.DataParams paramsValue)
		{
			return EvaluateWith(null, iD, expression, paramsValue);
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateWith(IServerProcess process, Guid iD, string expression, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
			process.JoinApplicationTransaction(iD, false);
			try
			{
				return Evaluate(process, expression, paramsValue);
			}
			finally
			{
				process.LeaveApplicationTransaction();
			}
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateWith(Guid iD, string expression, params object[] paramsValue)
		{
			IServerProcess process = DataSession.UtilityProcess;
			return EvaluateWith(process, iD, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}
		
		/// <summary>Evaluates the given expression enlisted within the given application transaction.</summary>
		public DAE.Runtime.Data.IDataValue EvaluateWith(Guid iD, string expression, string[] paramNames, object[] paramsValue)
		{
			IServerProcess process = DataSession.UtilityProcess;
			return EvaluateWith(process, iD, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}

		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalarWith(Guid iD, string expression)
		{
			return EvaluateScalarWith(null, iD, expression, (DAE.Runtime.DataParams)null);
		}

		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalarWith(Guid iD, string expression, DAE.Runtime.DataParams paramsValue)
		{
			return EvaluateScalarWith(null, iD, expression, paramsValue);
		}

		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalarWith(IServerProcess process, Guid iD, string expression, DAE.Runtime.DataParams paramsValue)
		{
			if (process == null)
				process = DataSession.UtilityProcess;
			process.JoinApplicationTransaction(iD, false);
			try
			{
				return EvaluateScalar(process, expression, paramsValue);
			}
			finally
			{
				process.LeaveApplicationTransaction();
			}
		}

		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalarWith(Guid iD, string expression, params object[] paramsValue)
		{
			IServerProcess process = DataSession.UtilityProcess;
			return EvaluateScalarWith(process, iD, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}

		/// <summary>Evaluates the given expression enlisted within the given application transaction.</summary>
		public DAE.Runtime.Data.Scalar EvaluateScalarWith(Guid iD, string expression, string[] paramNames, object[] paramsValue)
		{
			IServerProcess process = DataSession.UtilityProcess;
			return EvaluateScalarWith(process, iD, expression, DAE.Client.DataSession.DataParamsFromNativeParams(process, paramsValue));
		}
		
		#endregion

		#region Forms

		private Forms _forms;
		public Forms Forms
		{
			get { return _forms; }
		}

		protected virtual void FormAdded(IFormInterface form, bool stack)
		{
			// pure virtual
		}

		protected virtual void FormRemoved(IFormInterface form, bool stack)
		{
			// pure virtual
		}

		public virtual IFormInterface CreateForm()
		{
			return (IFormInterface)NodeTypeTable.CreateInstance("FormInterface");
		}

		public IFormInterface LoadForm(INode node, string document)
		{
			return LoadForm(node, document, null);
		}
		
		public IFormInterface LoadForm(INode node, string document, FormInterfaceHandler beforeActivate)
		{
			// DO NOT use a using block with the interface.  Should return immediately.
			IHost host = CreateHost();
			try
			{
				IFormInterface form = CreateForm();
				try
				{
					host.Load
					(
						document,
						form
					);
					if (beforeActivate != null)
						beforeActivate(form);
					host.Open();
					return form;
				}
				catch
				{
					form.Dispose();
					throw;
				}
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}

		/// <summary> Attempts to close the session's forms and returns true if they closed. </summary>
		/// <param name="exclude"> When true, the given root form is omitted. </param>
		public bool CloseAllForms(IHost exclude, CloseBehavior behavior)
		{
			Frontend.Client.Forms.FormStack formStack;
			Frontend.Client.Forms.FormStack next = this.Forms.First;
			while (next != null)
			{
				formStack = next;
				next = next.Next;	// remember the next item before it get's lost
				while 
				(
					!formStack.IsEmpty() && 
					(
						(exclude == null) || 
						(formStack.GetTopmostForm().HostNode != exclude)
					)
				)
					if (!formStack.GetTopmostForm().Close(behavior))
						return false;
			}
			return true;
		}

		/// <summary> Attempts to close all of a forms covering (child-modal) "children". </summary>
		/// <returns> True if any covering forms were closed. </returns>
		public bool UncoverForm(IFormInterface form, CloseBehavior behavior)
		{
			Frontend.Client.Forms.FormStack formStack = Forms.First;
			int i;
			while (formStack != null)
			{
				for (i = 0; i < formStack.Forms.Count; i++)
					if (formStack.Forms[i] == form)
					{
						for (int j = formStack.Forms.Count - 1; j > i; j--)
							if (!formStack.Forms[j].Close(behavior))
								return false;
						return true;
					}
				formStack = formStack.Next;
			}
			return true;
		}
		
		#endregion
		
		/// <remarks> Used for frames and popups. </remarks>
		public abstract IHost CreateHost();

		public virtual Deserializer CreateDeserializer()
		{
			return new Deserializer(_nodeTypeTable);
		}

		public virtual Serializer CreateSerializer()
		{
			return new Serializer();
		}
	}

	public delegate void FormsHandler(IFormInterface AForm, bool AStack);

	/// <summary> A linked list of form stacks. </summary>
	public class Forms : IEnumerable
	{
		private FormStack _first;
		public FormStack First { get { return _first; } }

		private FormStack _last;
		public FormStack Last { get { return _last; } }

		public event FormsHandler Added;
		public event FormsHandler Removed;

		public void Add(IFormInterface form)
		{
			FormStack newStack = new FormStack();
			newStack.Push(form);
			AddStackToTop(newStack);
			if (Added != null)
				Added(form, true);
		}

		/// <summary> Adds the form as modal (top of the stack) over some existing form. </summary>
		/// <param name="parentForm"> A form that is at the top of some stack in the list. </param>
		public void AddModal(IFormInterface form, IFormInterface parentForm)
		{
			FormStack searchStack = _first;
			IFormInterface topmost;
			while (searchStack != null)
			{
				topmost = searchStack.GetTopmostForm();
				if (parentForm == topmost)
				{
					topmost.Disable(form);
					searchStack.Push(form);
					if (Added != null)
						Added(form, false);
					return;
				}
				searchStack = searchStack._next;
			}
			throw new ClientException(ClientException.Codes.InvalidParentForm, parentForm.Text);
		}

		/// <summary> Removes a top-most form. </summary>
		/// <remarks> If the specified form is not a top-most form, nothing happens. </remarks>
		/// <returns> True if this form was the last of a stack. </returns>
		public bool Remove(IFormInterface form)
		{
			FormStack searchStack = _first;
			while (searchStack != null)
			{
				if (form == searchStack.GetTopmostForm())
				{
					searchStack.Pop();
					bool last = searchStack.IsEmpty();
					if (last)
						RemoveStack(searchStack);
					else
						searchStack.GetTopmostForm().Enable();
					if (Removed != null)
						Removed(form, last);
					return last;
				}
				searchStack = searchStack._next;
			}														
			Error.Warn(String.Format("Unable to find form '{0}' as a top-most form.", form.Text));
			return false;
		}

		public void BringToFront(IFormInterface form)
		{
			FormStack searchStack = _first;
			while (searchStack != null)
			{
				if (form == searchStack.GetTopmostForm())
				{
					RemoveStack(searchStack);
					AddStackToTop(searchStack);
					return;
				}
				searchStack = searchStack._next;
			}														
			throw new ClientException(ClientException.Codes.UnableToFindTopmostForm, form.Text);
		}

		private void RemoveStack(FormStack stack)
		{
			if (_first == stack)
				_first = stack._next;
			if (_last == stack)
				_last = stack._prior;
			if (stack._prior != null)
				stack._prior._next = stack._next;
			if (stack._next != null)
				stack._next._prior = stack._prior;
			stack._prior = null;
			stack._next = null;
		}

		private void AddStackToTop(FormStack stack)
		{
			stack._next = _first;
			stack._prior = null;
			if (_first != null)
				_first._prior = stack;
			_first = stack;
			if (_last == null)
				_last = stack;
		}

		/// <summary> Returns the topmost form on the topmost stack (or null if there are no forms). </summary>
		public IFormInterface GetTopmostForm()
		{
			if (_first != null)
				return _first.GetTopmostForm();
			else
				return null;
		}
		
		public bool IsTopMostOfSomeStack(IFormInterface form)
		{
			FormStack searchStack = _first;
			while (searchStack != null)
			{
				if (form == searchStack.GetTopmostForm())
					return true;
				searchStack = searchStack._next;
			}
			return false;
		}			

		public bool IsEmpty()
		{
			return _first == null;
		}

		/// <summary> Gets an enumerator to enumerate the *enabled* forms. </summary>
		public IEnumerator GetEnumerator()
		{
			return new FormsEnumerator(this);
		}

		#region FormsEnumerator

		public class FormsEnumerator : IEnumerator
		{
			public FormsEnumerator(Forms forms)
			{
				_forms = forms;
			}

			private Forms _forms;
			private FormStack _current;

			public void Reset()
			{
				_current = null;
			}

			public object Current
			{
				get
				{
					return _current.GetTopmostForm();
				}
			}

			public bool MoveNext()
			{
				if (_current == null)
					_current = _forms._first;
				else
					_current = _current._next;
				return (_current != null);
			}
		}

		#endregion

		#region FormStack

		public class FormStack
		{
			private List<IFormInterface> _forms = new List<IFormInterface>();
			public List<IFormInterface> Forms { get { return _forms; } }

			internal FormStack _next;
			public FormStack Next { get { return _next; } }

			internal FormStack _prior;
			public FormStack Prior { get { return _prior; } }

			public void Push(IFormInterface form)
			{
				_forms.Add(form);
			}

			public IFormInterface Pop()
			{
				IFormInterface result = _forms[_forms.Count - 1];
				_forms.RemoveAt(_forms.Count - 1);
				return result;
			}

			public bool IsEmpty()
			{
				return _forms.Count == 0;
			}

			public IFormInterface GetTopmostForm()
			{
				if (_forms.Count > 0)
					return _forms[_forms.Count - 1];
				else
					return null;
			}
		}

		#endregion
	}
}