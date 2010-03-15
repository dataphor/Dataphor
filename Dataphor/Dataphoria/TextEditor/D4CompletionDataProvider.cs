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
            
            var LCompletionList = new HashSet<ICompletionData>();
            if (ACharTyped == ' ')
            {                

                int LImageIndexTables = 14;                
                int LImageIndexSystemTables = 32;                                
                int LImageIndexGeneratedTables = 36;
                
                int LImageIndexViews = 30;
                int LImageIndexSystemViews = 34;
                int LImageIndexGeneratedViews = 38;

                int LImageIndexOperator = 12;
                int LImageIndexOperatorSystem = 39;
                int LImageIndexOperatorGenerated = 40;

                var LQueryTables = ".System.BaseTableVars";
                AddTableVarToCompletionList(LQueryTables, LCompletionList, ARow=>
                                                                               {
                                                                                   if ((bool)ARow["IsSystem"])
                                                                                   {
                                                                                       if ((bool)ARow["IsGenerated"])
                                                                                       {
                                                                                           return LImageIndexGeneratedTables;
                                                                                       }  
                                                                                       return LImageIndexSystemTables;
                                                                                   }                                                                                   
                                                                                   return LImageIndexTables;                                                                                   
                                                                               });
                var LQueryViews = ".System.DerivedTableVars";
                AddTableVarToCompletionList(LQueryViews, LCompletionList, ARow =>
                                                                              {
                                                                                  if ((bool)ARow["IsSystem"])
                                                                                  {
                                                                                      if ((bool)ARow["IsGenerated"])
                                                                                      {
                                                                                          return LImageIndexGeneratedViews;
                                                                                      }  
                                                                                      return LImageIndexSystemViews; 
                                                                                  }
                                                                                  return LImageIndexOperator;
                                                                                      
                                                                                  
                                                                              });
                var LQueryOperators = ".System.Operators";
                AddToOperatorToCompletionList(LQueryOperators, LCompletionList, ARow =>
                                                                              {
                                                                                  if ((bool)ARow["IsSystem"])
                                                                                  {
                                                                                      if ((bool)ARow["IsGenerated"])
                                                                                      {
                                                                                          return LImageIndexOperatorGenerated;
                                                                                      }
                                                                                      return LImageIndexOperatorSystem;
                                                                                  }
                                                                                  return LImageIndexViews;
                                                                              });                
                                                         
            }
            return LCompletionList.ToArray();
        }

        private void AddTableVarToCompletionList(string AQuery, HashSet<ICompletionData> ACompletionList, Func<Row,int> AGetImageIndex)
        {
            FDataphoria.Execute(AQuery, null, ARow =>
            {
                var LName = (string)ARow["Name"];
                var LLibraryAndTableName = LName.Split('.');
                var LLibraryName = LLibraryAndTableName[0];
                LName = LLibraryAndTableName[1];
                int LImageIndex = AGetImageIndex(ARow);
                var LCompletionData = new D4CompletionData(LName, LName + " at " + LLibraryName,
                                                      LImageIndex);
                ACompletionList.Add(LCompletionData);
            });
        }

        private void AddToOperatorToCompletionList(string AQuery, HashSet<ICompletionData> ACompletionList, Func<Row, int> AGetImageIndex)
        {
            FDataphoria.Execute(AQuery, null, ARow =>
            {
                var LName = (string)ARow["OperatorName"];
                var LDisplayName = (string)ARow["OperatorName"] + ARow["Signature"];                
                var LIndexOfFirstDot = LName.IndexOf('.');
                var LLibraryName = LName.Substring(0, LIndexOfFirstDot);
                LName = LName.Substring(LIndexOfFirstDot+1);
                int LImageIndex = AGetImageIndex(ARow);
                var LCompletionData = new D4CompletionData(LName, LDisplayName + " at " + LLibraryName,
                                                      LImageIndex);
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
