using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Runtime;
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
                DAE.IServerCursor LCursor = FDataphoria.OpenCursor(LQuery, LParams);
                
                try
                {
                    DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
                    try
                    {
                        TreeNode LNode;
                        while (LCursor.Next())
                        {
                            LCursor.Select(LRow);
                            string LTableName=  (string) LRow["Name"];
                            LCompletionList.Add(new DefaultCompletionData(LTableName, 14));
                        }
                    }
                    finally
                    {
                        LCursor.Plan.ReleaseRow(LRow);
                    }
                }
                finally
                {
                    FDataphoria.CloseCursor(LCursor);
                }                
                //LCompletionList.Add(new DefaultCompletionData("view1", 30));
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
