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
        private IDataphoria _dataphoria;
       
        public D4CompletionDataProvider(IDataphoria dataphoria)
        {
            InitializeComponent();
            _dataphoria = dataphoria;
        }

        public D4CompletionDataProvider(IContainer container, IDataphoria dataphoria)
        {            
            container.Add(this);
            InitializeComponent();
            _dataphoria = dataphoria;
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

        public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
        {
            textArea.Caret.Position = textArea.Document.OffsetToPosition(insertionOffset);
            return data.InsertAction(textArea, key);
        }

        public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            
            var completionList = new HashSet<ICompletionData>();
            if (charTyped == ' ')
            {
                var currentLibrary = this._dataphoria.GetCurrentLibraryName();
                int imageIndexTables = 14;                
                int imageIndexSystemTables = 32;                                
                int imageIndexGeneratedTables = 36;
                int imageIndexTablesCurrentLib = 41;
                
                int imageIndexViews = 30;
                int imageIndexSystemViews = 34;
                int imageIndexGeneratedViews = 38;
                int imageIndexViewsCurrentLib = 42;

                int imageIndexOperator = 12;
                int imageIndexOperatorSystem = 39;
                int imageIndexOperatorGenerated = 40;
                int imageIndexOperatorCurrentLib = 43;

                var queryTables = ".System.BaseTableVars";
                AddTableVarToCompletionList(queryTables, completionList, ARow=>
                                                                               {
                                                                                   if ((bool)ARow["IsSystem"])
                                                                                   {
                                                                                       if ((bool)ARow["IsGenerated"])
                                                                                       {
                                                                                           return imageIndexGeneratedTables;
                                                                                       }  
                                                                                       return imageIndexSystemTables;
                                                                                   }
                                                                                   if (currentLibrary == (string)ARow["Library_Name"])
                                                                                   {
                                                                                       return imageIndexTablesCurrentLib;
                                                                                   }
                                                                                   return imageIndexTables;                                                                                   
                                                                               });
                var queryViews = ".System.DerivedTableVars";
                AddTableVarToCompletionList(queryViews, completionList, ARow =>
                                                                              {
                                                                                  if ((bool)ARow["IsSystem"])
                                                                                  {
                                                                                      if ((bool)ARow["IsGenerated"])
                                                                                      {
                                                                                          return imageIndexGeneratedViews;
                                                                                      }  
                                                                                      return imageIndexSystemViews; 
                                                                                  }
                                                                                  if (currentLibrary == (string)ARow["Library_Name"])
                                                                                  {
                                                                                      return imageIndexViewsCurrentLib;
                                                                                  }
                                                                                  return imageIndexViews;
                                                                                      
                                                                                  
                                                                              });
                var queryOperators = ".System.Operators";
                AddToOperatorToCompletionList(queryOperators, completionList, ARow =>
                                                                              {
                                                                                  if ((bool)ARow["IsSystem"])
                                                                                  {
                                                                                      if ((bool)ARow["IsGenerated"])
                                                                                      {
                                                                                          return imageIndexOperatorGenerated;
                                                                                      }
                                                                                      return imageIndexOperatorSystem;
                                                                                  }
                                                                                  if (currentLibrary == (string)ARow["Library_Name"])
                                                                                  {
                                                                                      return imageIndexOperatorCurrentLib;
                                                                                  }                                                                                  
                                                                                  return imageIndexOperator;
                                                                              });                
                                                         
            }
            return completionList.ToArray();
        }

        private void AddTableVarToCompletionList(string query, HashSet<ICompletionData> completionList, Func<IRow,int> getImageIndex)
        {
            _dataphoria.Execute(query, null, ARow =>
            {
                var name = (string)ARow["Name"];
                var libraryAndTableName = name.Split('.');
                var libraryName = libraryAndTableName[0];
                name = libraryAndTableName[1];
                int imageIndex = getImageIndex(ARow);
                var completionData = new D4CompletionData(name, name + " at " + libraryName,
                                                      imageIndex);
                completionList.Add(completionData);
            });
        }

        private void AddToOperatorToCompletionList(string query, HashSet<ICompletionData> completionList, Func<IRow, int> getImageIndex)
        {
            _dataphoria.Execute(query, null, ARow =>
            {
                var name = (string)ARow["OperatorName"];
                var displayName = (string)ARow["OperatorName"] + ARow["Signature"];                
                var indexOfFirstDot = name.IndexOf('.');
                var libraryName = name.Substring(0, indexOfFirstDot);
                name = name.Substring(indexOfFirstDot+1);
                int imageIndex = getImageIndex(ARow);
                var completionData = new D4CompletionData(name, displayName + " at " + libraryName,
                                                      imageIndex);
                completionList.Add(completionData);
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
