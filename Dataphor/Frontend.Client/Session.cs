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
using System.Web;

using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[ListInDesigner(false)]
	public abstract class Session : Disposable
	{
		public const string CApplicationNodeTableExpression = @".Frontend.GetApplicationNodeTypes(AClientType, AApplicationID)";
		public const string CGetLibraryFilesExpression = @".Frontend.GetLibraryFiles(ApplicationLibraries where Application_ID = AApplicationID over { Library_Name })";
		public const string CPrepareApplicationExpression = @".Frontend.PrepareApplication(AApplicationID)";

		public Session(DataSession ASession, bool AOwnsSession)
		{
			FDataSession = ASession;
			FOwnsDataSession = AOwnsSession;
			InitializePipe();
			FForms = new Forms();
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				UninitializePipe();
			}
			finally
			{
				try
				{
					if (FOwnsDataSession && (FDataSession != null))
					{
						FDataSession.Dispose();
						FDataSession = null;
					}
				}
				finally
				{
					base.Dispose(ADisposing);
				}
			}
		}

		protected virtual void InitializePipe()
		{
			FPipe = new Pipe(FDataSession.ServerSession);
		}

		protected virtual void UninitializePipe()
		{
			if (FPipe != null)
			{
				FPipe.Dispose();
				FPipe = null;
			}
		}

		// Pipe

		private Pipe FPipe;

		public Pipe Pipe
		{
			get { return FPipe; }
		}

		// AreImagesLoaded
		
		public virtual bool AreImagesLoaded()
		{
			return true;
		}

		// DataSession

		private DataSession FDataSession;
		private bool FOwnsDataSession;

		public DAE.Client.DataSession DataSession
		{
			get { return FDataSession; }
		}
		
		// Errors
		
		public virtual void ReportErrors(IHost AHost, ErrorList AErrors)
		{
			// Do nothing here, this is overridden by the Windows Session to report errors to Dataphoria when the session is hosted w/in the IDE.
		}

		// InvokeHelp

		public virtual void InvokeHelp(INode ASender, string AHelpKeyword, HelpKeywordBehavior AHelpKeywordBehavior, string AHelpString) {}

		// NodeTypeTable

		private NodeTypeTable FNodeTypeTable = new NodeTypeTable();

		public NodeTypeTable NodeTypeTable
		{
			get { return FNodeTypeTable; }
		}

		protected void ValidateNodeTypeTable()
		{
			if (NodeTypeTable["FormInterface"] == null)
				throw new ClientException(ClientException.Codes.EmptyOrIncompleteNodeTypesTable);
		}

		/// <summary> Prepares for opening a document in the specified application. </summary>
		/// <returns> The starting document configured for the application. </returns>
		public virtual string SetApplication(string AApplicationID, string AClientType)
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			LParams.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AApplicationID", AApplicationID));
			LParams.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AClientType", AClientType));
			
			// Get the node types table
			using (DAE.Runtime.Data.DataValue LNodeTable = DataSession.Evaluate(CApplicationNodeTableExpression, LParams))
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(LNodeTable.AsString);
			}
			ValidateNodeTypeTable();

			// Prepare the application and get name of the starting document
			string LDocumentString = null;
			using (DAE.Runtime.Data.DataValue LStartingDocument = DataSession.Evaluate(CPrepareApplicationExpression, LParams))
			{
				LDocumentString = LStartingDocument.AsString;
			}

			// Load the files required to register any nodes, if necessary				
			// TODO: This should be better when we actually have a story here.
			// The problem is that there is no way to distinguish between files required for different client types.
			// If a file is required for one client, it will be downloaded by all clients.
			// The library definition would have to be extended by the Frontend to be able to specify a client affinity (possibly multiple clients) for each file.
			// Then the CLI must somehow be able to expose the file caching mechanism maintained internally by the catalog repository services.
			// Right now we are doing this by simply casting and accessing the GetFile() method of the LocalServer.
			if (DataSession.Server is DAE.Server.LocalServer)
			{
				IServerCursor LCursor = DataSession.OpenCursor(CGetLibraryFilesExpression, LParams);
				try
				{
					using (DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow())
					{
						while (LCursor.Next())
						{
							LCursor.Select(LRow);
							((DAE.Server.LocalServer)DataSession.Server).GetFile((DAE.Server.LocalProcess)LCursor.Plan.Process, LRow["Library_Name"].AsString, LRow["Name"].AsString, LRow["TimeStamp"].AsDateTime);
						}
					}
				}
				finally
				{
					DataSession.CloseCursor(LCursor);
				}
			}
			
			return LDocumentString;
		}

		// Utility methods
		// NOTE: These methods are essentially the same overloads as are provided by the DataSession, except that they participate in the error reporting system of the Frontend session.
		
		public void ExecuteScript(string AScript)
		{
			ExecuteScript(null, AScript, null);
		}
		
		public void ExecuteScript(IServerProcess AProcess, string AScript)
		{
			ExecuteScript(AProcess, AScript, null);
		}

		public void ExecuteScript(string AScript, DAE.Runtime.DataParams AParams)
		{
			DataSession.ExecuteScript(null, AScript, AParams);
		}

		public void ExecuteScript(IServerProcess AProcess, string AScript, DAE.Runtime.DataParams AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;

			DAE.IServerScript LScript = AProcess.PrepareScript(AScript);
			try
			{
				foreach (DAE.IServerBatch LBatch in LScript.Batches)
					if (LBatch.IsExpression())
					{
						DAE.IServerExpressionPlan LPlan = LBatch.PrepareExpression(AParams);
						try
						{
							ErrorList LErrors = new ErrorList();
							LErrors.AddRange(LPlan.Messages);
							ReportErrors(null, LErrors);

							if (LPlan.DataType is DAE.Schema.TableType)
								LPlan.Close(LPlan.Open(AParams));
							else
								LPlan.Evaluate(AParams).Dispose();
						}
						finally
						{
							LBatch.UnprepareExpression(LPlan);
						}
					}
					else
					{
						DAE.IServerStatementPlan LPlan = LBatch.PrepareStatement(AParams);
						try
						{
							ErrorList LErrors = new ErrorList();
							LErrors.AddRange(LPlan.Messages);
							ReportErrors(null, LErrors);

							LPlan.Execute(AParams);
						}
						finally
						{
							LBatch.UnprepareStatement(LPlan);
						}
					}
			}
			finally
			{
				AProcess.UnprepareScript(LScript);
			}
		}
		
		public void Execute(string AStatement)
		{
			Execute(null, AStatement, (DAE.Runtime.DataParams)null);
		}
		
		public void Execute(IServerProcess AProcess, string AStatement)
		{
			Execute(AProcess, AStatement, (DAE.Runtime.DataParams)null);
		}
		
		public void Execute(string AStatement, DAE.Runtime.DataParams AParams)
		{
			Execute(null, AStatement, AParams);
		}
		
		public void Execute(IServerProcess AProcess, string AStatement, DAE.Runtime.DataParams AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;
				
			DAE.IServerStatementPlan LPlan = AProcess.PrepareStatement(AStatement, AParams);
			try
			{
				ErrorList LErrors = new ErrorList();
				LErrors.AddRange(LPlan.Messages);
				ReportErrors(null, LErrors);
				
				LPlan.Execute(AParams);
			}
			finally
			{
				AProcess.UnprepareStatement(LPlan);
			}
		}

		public void Execute(string AStatement, params object[] AParams)
		{
			Execute(null, AStatement, AParams);
		}
		
		public void Execute(IServerProcess AProcess, string AStatement, params object[] AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;
				
			Execute(AProcess, AStatement, DAE.Client.DataSessionBase.DataParamsFromNativeParams(AProcess, AParams));
		}

		public DAE.Runtime.Data.DataValue Evaluate(string AExpression)
		{
			return Evaluate(null, AExpression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.DataValue Evaluate(IServerProcess AProcess, string AExpression)
		{
			return Evaluate(AProcess, AExpression, (DAE.Runtime.DataParams)null);
		}

		public DAE.Runtime.Data.DataValue Evaluate(string AExpression, DAE.Runtime.DataParams AParams)
		{
			return Evaluate(null, AExpression, AParams);
		}

		public DAE.Runtime.Data.DataValue Evaluate(IServerProcess AProcess, string AExpression, DAE.Runtime.DataParams AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;
			
			DAE.IServerExpressionPlan LPlan = AProcess.PrepareExpression(AExpression, AParams);
			try
			{
				ErrorList LErrors = new ErrorList();
				LErrors.AddRange(LPlan.Messages);
				ReportErrors(null, LErrors);
	
				return LPlan.Evaluate(AParams);
			}
			finally
			{
				AProcess.UnprepareExpression(LPlan);
			}
		}

		/// <summary>Evaluates the given expression using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.DataValue Evaluate(string AExpression, params object[] AParams)
		{
			return Evaluate(null, AExpression, AParams);
		}										  

		/// <summary>Evaluates the given expression on the given process and using the given parameter values (auto numbered A0..An-1).</summary>
		public DAE.Runtime.Data.DataValue Evaluate(IServerProcess AProcess, string AExpression, params object[] AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;

			return Evaluate(AProcess, AExpression, DAE.Client.DataSessionBase.DataParamsFromNativeParams(AProcess, AParams));
		}

		/// <summary>Evaluates the given expression on the given process and using the given parameter names and values.</summary>
		public DAE.Runtime.Data.DataValue Evaluate(IServerProcess AProcess, string AExpression, string[] AParamNames, object[] AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;
			
			return Evaluate(AProcess, AExpression, DAE.Client.DataSessionBase.DataParamsFromNativeParams(AProcess, AParamNames, AParams));
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.DataValue EvaluateWith(Guid AID, string AExpression)
		{
			return EvaluateWith(null, AID, AExpression, (DAE.Runtime.DataParams)null);
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.DataValue EvaluateWith(Guid AID, string AExpression, DAE.Runtime.DataParams AParams)
		{
			return EvaluateWith(null, AID, AExpression, AParams);
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.DataValue EvaluateWith(IServerProcess AProcess, Guid AID, string AExpression, DAE.Runtime.DataParams AParams)
		{
			if (AProcess == null)
				AProcess = DataSession.UtilityProcess;
			AProcess.JoinApplicationTransaction(AID, false);
			try
			{
				return Evaluate(AProcess, AExpression, AParams);
			}
			finally
			{
				AProcess.LeaveApplicationTransaction();
			}
		}
		
		/// <summary>Evaluates the given expression enlisted within the specified application transaction.</summary>
		public DAE.Runtime.Data.DataValue EvaluateWith(Guid AID, string AExpression, params object[] AParams)
		{
			IServerProcess LProcess = DataSession.UtilityProcess;
			return EvaluateWith(LProcess, AID, AExpression, DAE.Client.DataSessionBase.DataParamsFromNativeParams(LProcess, AParams));
		}
		
		/// <summary>Evaluates the given expression enlisted within the given application transaction.</summary>
		public DAE.Runtime.Data.DataValue EvaluateWith(Guid AID, string AExpression, string[] AParamNames, object[] AParams)
		{
			IServerProcess LProcess = DataSession.UtilityProcess;
			return EvaluateWith(LProcess, AID, AExpression, DAE.Client.DataSessionBase.DataParamsFromNativeParams(LProcess, AParams));
		}

		// Forms

		private Forms FForms;
		public Forms Forms
		{
			get { return FForms; }
		}

		public virtual IFormInterface CreateForm()
		{
			return (IFormInterface)NodeTypeTable.CreateInstance("FormInterface");
		}

		public IFormInterface LoadForm(INode ANode, string ADocument)
		{
			return LoadForm(ANode, ADocument, null);
		}
		
		public IFormInterface LoadForm(INode ANode, string ADocument, FormInterfaceHandler ABeforeActivate)
		{
			// DO NOT use a using block with the interface.  Should return immediately.
			IHost LHost = CreateHost();
			try
			{
				IFormInterface LForm = CreateForm();
				try
				{
					LHost.Load
					(
						ADocument,
						LForm
					);
					if (ABeforeActivate != null)
						ABeforeActivate(LForm);
					LHost.Open();
					return LForm;
				}
				catch
				{
					LForm.Dispose();
					throw;
				}
			}
			catch
			{
				LHost.Dispose();
				throw;
			}
		}

		/// <remarks> Used for frames and popups. </remarks>
		public abstract IHost CreateHost();

		public virtual Deserializer CreateDeserializer()
		{
			return new Deserializer(FNodeTypeTable);
		}

		public virtual Serializer CreateSerializer()
		{
			return new Serializer();
		}
	}

	public delegate void FormsHandler(IFormInterface AForm);

	/// <summary> A linked list of form stacks. </summary>
	public class Forms : IEnumerable
	{
		private FormStack FFirst;
		public FormStack First { get { return FFirst; } }

		private FormStack FLast;
		public FormStack Last { get { return FLast; } }

		public event FormsHandler OnAdded;
		public event FormsHandler OnRemoved;

		public void Add(IFormInterface AForm)
		{
			FormStack LNewStack = new FormStack();
			LNewStack.Push(AForm);
			AddStackToTop(LNewStack);
			if (OnAdded != null)
				OnAdded(AForm);
		}

		/// <summary> Adds the form as modal (top of the stack) over some existing form. </summary>
		/// <param name="AParentForm"> A form that is at the top of some stack in the list. </param>
		public void AddModal(IFormInterface AForm, IFormInterface AParentForm)
		{
			FormStack LSearchStack = FFirst;
			IFormInterface LTopmost;
			while (LSearchStack != null)
			{
				LTopmost = LSearchStack.GetTopmostForm();
				if (AParentForm == LTopmost)
				{
					LTopmost.Disable(AForm);
					LSearchStack.Push(AForm);
					if (OnAdded != null)
						OnAdded(AForm);
					return;
				}
				LSearchStack = LSearchStack.FNext;
			}
			throw new ClientException(ClientException.Codes.InvalidParentForm, AParentForm.Text);
		}

		/// <summary> Removes a top-most form. </summary>
		/// <remarks> If the specified form is not a top-most form, nothing happens. </remarks>
		public void Remove(IFormInterface AForm)
		{
			FormStack LSearchStack = FFirst;
			while (LSearchStack != null)
			{
				if (AForm == LSearchStack.GetTopmostForm())
				{
					LSearchStack.Pop();
					if (LSearchStack.IsEmpty())
						RemoveStack(LSearchStack);
					else
						LSearchStack.GetTopmostForm().Enable();
					if (OnRemoved != null)
						OnRemoved(AForm);
					return;
				}
				LSearchStack = LSearchStack.FNext;
			}														
			Error.Warn(String.Format("Unable to find form '{0}' as a top-most form.", AForm.Text));
		}

		public void BringToFront(IFormInterface AForm)
		{
			FormStack LSearchStack = FFirst;
			while (LSearchStack != null)
			{
				if (AForm == LSearchStack.GetTopmostForm())
				{
					RemoveStack(LSearchStack);
					AddStackToTop(LSearchStack);
					return;
				}
				LSearchStack = LSearchStack.FNext;
			}														
			throw new ClientException(ClientException.Codes.UnableToFindTopmostForm, AForm.Text);
		}

		private void RemoveStack(FormStack AStack)
		{
			if (FFirst == AStack)
				FFirst = AStack.FNext;
			if (FLast == AStack)
				FLast = AStack.FPrior;
			if (AStack.FPrior != null)
				AStack.FPrior.FNext = AStack.FNext;
			if (AStack.FNext != null)
				AStack.FNext.FPrior = AStack.FPrior;
			AStack.FPrior = null;
			AStack.FNext = null;
		}

		private void AddStackToTop(FormStack AStack)
		{
			AStack.FNext = FFirst;
			AStack.FPrior = null;
			if (FFirst != null)
				FFirst.FPrior = AStack;
			FFirst = AStack;
			if (FLast == null)
				FLast = AStack;
		}

		/// <summary> Returns the topmost form on the topmost stack (or null if there are no forms). </summary>
		public IFormInterface GetTopmostForm()
		{
			if (FFirst != null)
				return FFirst.GetTopmostForm();
			else
				return null;
		}

		public bool IsEmpty()
		{
			return FFirst == null;
		}

		/// <summary> Gets an enumerator to enumerate the *enabled* forms. </summary>
		public IEnumerator GetEnumerator()
		{
			return new FormsEnumerator(this);
		}

		#region FormsEnumerator

		public class FormsEnumerator : IEnumerator
		{
			public FormsEnumerator(Forms AForms)
			{
				FForms = AForms;
			}

			private Forms FForms;
			private FormStack FCurrent;

			public void Reset()
			{
				FCurrent = null;
			}

			public object Current
			{
				get
				{
					return FCurrent.GetTopmostForm();
				}
			}

			public bool MoveNext()
			{
				if (FCurrent == null)
					FCurrent = FForms.FFirst;
				else
					FCurrent = FCurrent.FNext;
				return (FCurrent != null);
			}
		}

		#endregion

		#region FormStack

		public class FormStack
		{
			private ArrayList FForms = new ArrayList();
			public ArrayList Forms { get { return FForms; } }

			internal FormStack FNext;
			public FormStack Next { get { return FNext; } }

			internal FormStack FPrior;
			public FormStack Prior { get { return FPrior; } }

			public void Push(IFormInterface AForm)
			{
				FForms.Add(AForm);
			}

			public IFormInterface Pop()
			{
				IFormInterface LResult = (IFormInterface)FForms[FForms.Count - 1];
				FForms.RemoveAt(FForms.Count - 1);
				return LResult;
			}

			public bool IsEmpty()
			{
				return FForms.Count == 0;
			}

			public IFormInterface GetTopmostForm()
			{
				if (FForms.Count > 0)
					return (IFormInterface)FForms[FForms.Count - 1];
				else
					return null;
			}
		}

		#endregion
	}
}