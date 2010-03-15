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
                var LParams = new DataParams();
                string LLibraryName = FDataphoria.GetCurrentLibraryName();
                LParams.Add(DataParam.Create(FDataphoria.UtilityProcess, "ALibraryName", LLibraryName));
                

                var LQueryTables = ".System.BaseTableVars where (Library_Name = ALibraryName) and (not(IsGenerated)) and (not(IsSystem)) over { Name }";
                AddToCompletionList(LQueryTables, 14, LCompletionList, LParams);
                var LQueryViews = ".System.DerivedTableVars where (Library_Name = ALibraryName) and (not(IsGenerated)) and (not(IsSystem)) over { Name }";
                AddToCompletionList(LQueryViews, 30, LCompletionList, LParams);

                var LQuerySystemTables = ".System.BaseTableVars where (IsSystem = true) over { Name }";
                AddToCompletionList(LQuerySystemTables, 32, LCompletionList, null); 
                var LQuerySystemViews = ".System.DerivedTableVars where (IsSystem = true) over { Name }";
                AddToCompletionList(LQuerySystemViews, 34, LCompletionList, null);

                var LQueryGeneratedTables = ".System.BaseTableVars where  (IsGenerated = true) over { Name }";
                AddToCompletionList(LQueryGeneratedTables, 36, LCompletionList, null);
                var LQueryGeneratedViews = ".System.DerivedTableVars where (IsGenerated = true) over { Name }";
                AddToCompletionList(LQueryGeneratedViews, 38, LCompletionList, null);  
            }
            return LCompletionList.ToArray();
        }

        private void AddToCompletionList(string AQuery, int AImageIndex, List<ICompletionData> ACompletionList, DataParams AParams)
        {


            FDataphoria.Execute(AQuery, AParams, ARow =>
                                                     {
                                                         var LName = (string) ARow["Name"];
                                                         var LLibraryAndTableName = LName.Split('.');
                                                         var LLibraryName = LLibraryAndTableName[0];
                                                         LName = LLibraryAndTableName[1];                                                         
                                                         var LCompletionData = new D4CompletionData(LName,LName+" at "+LLibraryName,
                                                                                               AImageIndex);                                                                                                                
                                                         ACompletionList.Add(LCompletionData);
                                                     });
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
