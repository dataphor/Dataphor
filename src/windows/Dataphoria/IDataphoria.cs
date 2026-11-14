using System;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Session = Alphora.Dataphor.Frontend.Client.Windows.Session;

namespace Alphora.Dataphor.Dataphoria
{
    public interface IDataphoria : IErrorSource, IServiceProvider, IDisposable
    {
		event EventHandler Connected;
		event EventHandler Disconnected;
		
        Session FrontendSession { get; }
        IServerProcess UtilityProcess { get; }
        DataSession DataSession { get; }
        ErrorListView Warnings { get; }
        Settings Settings { get; }
        bool IsConnected { get; }
        Debugger Debugger { get; }
        
        FileDesignBuffer PromptForFileBuffer(IDesigner ADesigner, string AFileName);

        DocumentDesignBuffer PromptForDocumentBuffer(IDesigner ADesigner, string ALibraryName, string ADocumentName);

        void RefreshDocuments(string ALibraryName);

        void OpenFiles(string[] AArgs);

        IDesigner GetDesigner(DesignBuffer ABuffer);

        IDesigner OpenDesigner(DesignerInfo AInfo, DesignBuffer ABuffer);
        
        DesignBuffer DesignBufferFromLocator(out DesignerInfo AInfo, DebugLocator ALocator);
        
        void OpenLocator(DebugLocator ALocator);

        DesignerInfo GetDefaultDesigner(string ADocumentTypeID);

        IServerCursor OpenCursor(string AQuery);

        void Execute(string AQuery, DataParams AParams, Action<DAE.Runtime.Data.IRow> AAction);

        IServerCursor OpenCursor(String AQuery, DataParams AParams);

        void CloseCursor(IServerCursor ACursor);

        IDataValue EvaluateQuery(string AQuery);

        IDataValue EvaluateQuery(string AQuery, DataParams AParams);

        void ExecuteScript(string AScript);

        TextEditor.TextEditor EvaluateAndEdit(string AExpression, string ADocumentTypeID);

        Session GetLiveDesignableFrontendSession();

        void RefreshLibraries();

        void EnsureSecurityRegistered();

        void AttachForm(BaseForm AForm);

        void InvokeHelp(string p);

        bool DocumentExists(string ALibraryName, string ADocumentName);

        void CheckDocumentOverwrite(string LibraryName, string ADocumentName);

        void EnsureServerConnection();

        void Disconnect();

        IDesigner NewDesigner();

        void OpenFile();

        void OpenFileWith();

        void SaveCatalog();

        void BackupCatalog();

        void UpgradeLibraries();

        string GetCurrentLibraryName();

        event EventHandler OnFormDesignerLibrariesChanged;

        void AddDesignerForm(IFormInterface AInterface, IDesigner ADesigner);

        DesignerInfo ChooseDesigner(string ADocumentTypeID);

        void RegisterDesigner(DesignBuffer ABuffer, IDesigner ADesigner);

        void UnregisterDesigner(DesignBuffer ABuffer);

        void CheckNotRegistered(DesignBuffer ABuffer);

		object Invoke(Delegate method);
		object Invoke(Delegate method, params object[] args);       
		
		bool LocateToError(Exception AException); 
    }
}