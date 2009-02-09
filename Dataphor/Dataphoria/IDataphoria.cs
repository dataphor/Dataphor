using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.Frontend.Client.Windows;
using System.ComponentModel.Design;
using Alphora.Dataphor.Dataphoria.Designers;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Dataphoria
{
    public interface IDataphoria : IErrorSource, IServiceProvider, IDisposable
    {
        Alphora.Dataphor.Dataphoria.Designers.FileDesignBuffer PromptForFileBuffer(Alphora.Dataphor.Dataphoria.Designers.IDesigner ADesigner, string AFileName);

        Alphora.Dataphor.Dataphoria.Designers.DocumentDesignBuffer PromptForDocumentBuffer(Alphora.Dataphor.Dataphoria.Designers.IDesigner ADesigner, string ALibraryName, string ADocumentName);

        void RefreshDocuments(string ALibraryName);

        void OpenFiles(string[] AArgs);
        
        Frontend.Client.Windows.Session FrontendSession { get; }

        Alphora.Dataphor.Dataphoria.Designers.IDesigner GetDesigner(DesignBuffer ABuffer);

        Alphora.Dataphor.Dataphoria.Designers.IDesigner OpenDesigner(DesignerInfo AInfo, DesignBuffer ABuffer);

        DesignerInfo GetDefaultDesigner(string ADocumentTypeID);

        DAE.IServerCursor OpenCursor(string AQuery);

        Alphora.Dataphor.DAE.IServerCursor OpenCursor(String AQuery, DAE.Runtime.DataParams AParams);

        void CloseCursor(Alphora.Dataphor.DAE.IServerCursor ACursor);

        DAE.IServerProcess UtilityProcess { get; }
        
        DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery);

        DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery, DAE.Runtime.DataParams AParams);

        void ExecuteScript(string AScript);

        TextEditor.TextEditor EvaluateAndEdit(string AExpression, string ADocumentTypeID);

        Frontend.Client.Windows.Session GetLiveDesignableFrontendSession();

        void RefreshLibraries();

        void EnsureSecurityRegistered();

        Alphora.Dataphor.DAE.Client.DataSession DataSession { get; }

        void AttachForm(Form AForm);

        void InvokeHelp(string p);

        bool DocumentExists(string ALibraryName, string ADocumentName);

        void CheckDocumentOverwrite(string LibraryName, string ADocumentName);
        
        void EnsureServerConnection();

        void Disconnect();

        Alphora.Dataphor.Dataphoria.Designers.IDesigner NewDesigner();

        void OpenFile();

        void OpenFileWith();

        void SaveCatalog();

        void BackupCatalog();

        void UpgradeLibraries();

        string GetCurrentLibraryName();

        ErrorListView Warnings
        {
            get;
        }

        event EventHandler OnFormDesignerLibrariesChanged;

        void AddDesignerForm(IFormInterface AInterface, Alphora.Dataphor.Dataphoria.Designers.IDesigner ADesigner);

        DesignerInfo ChooseDesigner(string ADocumentTypeID);

        void RegisterDesigner(DesignBuffer ABuffer, Alphora.Dataphor.Dataphoria.Designers.IDesigner ADesigner);

        void UnregisterDesigner(DesignBuffer ABuffer);

        void CheckNotRegistered(DesignBuffer ABuffer);

        Settings Settings { get; }
       
    }
}
