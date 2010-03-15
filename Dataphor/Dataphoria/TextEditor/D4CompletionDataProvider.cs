using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
    public partial class D4CompletionDataProvider : Component,  ICompletionDataProvider
    {
        private IDataphoria FDataphoria;
       
        public D4CompletionDataProvider(IDataphoria ADataphoria)
        {
            InitializeComponent();
            FDataphoria = ADataphoria;
        }

        public D4CompletionDataProvider(IContainer AContainer, IDataphoria ADataphoria)
        {            
            AContainer.Add(this);
            InitializeComponent();
            FDataphoria = ADataphoria;
        }

        #region Implementation of ICompletionDataProvider

        public virtual CompletionDataProviderKeyResult ProcessKey(char key)
        {            
            if (key == ' ')
            {                
                return CompletionDataProviderKeyResult.BeforeStartKey;
            }
            if (char.IsLetterOrDigit(key) || key == '_')
            {                
                return CompletionDataProviderKeyResult.NormalKey;
            }                         
            return CompletionDataProviderKeyResult.InsertionKey;                        
        }

        public bool InsertAction(ICompletionData AData, TextArea ATextArea, int AInsertionOffset, char AKey)
        {
            ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(AInsertionOffset);
            return AData.InsertAction(ATextArea, AKey);
        }

        public ICompletionData[] GenerateCompletionData(string AFileName, TextArea ATextArea, char ACharTyped)
        {
            var LCompletionList = new List<ICompletionData>();
            if (ACharTyped == ' ')
            {

                string LQuery = ".System.BaseTableVars where (Library_Name = ALibraryName) and (not(IsGenerated)) and (not(IsSystem)) over { Name }";
                var LParams = new DataParams();
                string LLibraryName = FDataphoria.GetCurrentLibraryName();
                LParams.Add(DataParam.Create(FDataphoria.UtilityProcess, "ALibraryName", LLibraryName));
                FDataphoria.Execute(LQuery, LParams, ARow =>
                                                         {
                                                             var LTableName = (string) ARow["Name"];
                                                             var LLibrarYAndTableName = LTableName.Split('.');
                                                             if (LLibraryName == LLibrarYAndTableName[0])
                                                             {
                                                                 LTableName = LLibrarYAndTableName[1];
                                                             }
                                                             LCompletionList.Add(new DefaultCompletionData(LTableName,
                                                                                                           14));
                                                         });

            }
            return LCompletionList.ToArray();
        }       

        public ImageList ImageList
        {
            get
            {
                return FImageList;
            }
        }

        public string PreSelection
        {
            get { return null; }
        }

        public int DefaultIndex
        {
            get { return -1; }
        }

        #endregion
    }
}
