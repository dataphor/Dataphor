/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using DataSet = System.Data.DataSet;
using Image = System.Drawing.Image;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Client.Provider;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.Dataphoria.TextEditor.BlockActions;
using Alphora.Dataphor.Frontend.Client.Windows;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

using WeifenLuo.WinFormsUI.Docking;


namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
	public partial class D4Editor : TextEditor, IErrorSource
	{
		protected DockContent _dockContentResultPanel;
		private ResultPanel _resultPanel;

		public D4Editor() // dummy constructor for MDI menu merging
		{
			InitializeComponent();
			InitializeDocking();
            InitializeCodeCompletion();
		}

		public D4Editor(IDataphoria dataphoria, string designerID) : base(dataphoria, designerID)
		{
			InitializeComponent();
			InitializeDocking();
			InitializeDebugger();
		    InitializeCodeCompletion();
			
			_textEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemQuestion] = new ToggleBlockDelimiter();
			_textEdit.EditActions[Keys.Control | Keys.Oemcomma] = new PriorBlock();
			_textEdit.EditActions[Keys.Control | Keys.OemPeriod] = new NextBlock();
			_textEdit.EditActions[Keys.Shift | Keys.Control | Keys.Oemcomma] = new SelectPriorBlock();
			_textEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemPeriod] = new SelectNextBlock();

			_resultPanel.BeginningFind += BeginningFind;
			_resultPanel.ReplacementsPerformed += ReplacementsPerformed;
			_resultPanel.TextNotFound += TextNotFound;

			UpdateCurrentLocation();
		}

        private void InitializeCodeCompletion()
        {                        
            _textEdit.ActiveTextAreaControl.TextArea.KeyEventHandler +=
                (
					AKey =>
					{
						// Send the command to the existing code completion window if there is one
						if (_codeCompletionWindow != null)
						{
							if (_codeCompletionWindow.ProcessKeyEvent(AKey))
								return true;
						}
						
						// Handle the request to show code completion
						if (AKey == ' ' && ModifierKeys == Keys.Control)
						{
							var completionDataProvider = new D4CompletionDataProvider(this.Dataphoria);

							_codeCompletionWindow = 
								CodeCompletionWindow.ShowCompletionWindow
								(
									this,
									_textEdit,
									Text,
									completionDataProvider,
									AKey
								);
							if (_codeCompletionWindow != null)
							{
								_codeCompletionWindow.Closed +=
									(
										(ASender, AE) =>
										{
											if (_codeCompletionWindow != null)
											{
												_codeCompletionWindow.Dispose();
												_codeCompletionWindow = null;
											}
										}
									);
							}
							return true;
						}
						return false;
					}
				);
        }

	    


	    private void Deinitialize()
		{
			DeinitializeDebugger();
		}
		
		private void InitializeDocking()
		{
			_resultPanel = new ResultPanel();

			// 
			// FResultPanel
			// 
			_resultPanel.BackColor = SystemColors.Control;
			_resultPanel.CausesValidation = false;
			_resultPanel.EnableFolding = false;
			_resultPanel.IndentStyle = IndentStyle.Auto;
			_resultPanel.IsIconBarVisible = false;
			_resultPanel.Location = new Point(1, 21);
			_resultPanel.Name = "FResultPanel";
			_resultPanel.ShowInvalidLines = false;
			_resultPanel.ShowLineNumbers = false;
			_resultPanel.ShowVRuler = true;
			_resultPanel.Size = new Size(453, 212);
			_resultPanel.TabIndent = 3;
			_resultPanel.TabIndex = 1;
			_resultPanel.TabStop = false;
			_resultPanel.VRulerRow = 100;
			_resultPanel.Dock = DockStyle.Fill;

			_dockContentResultPanel = new DockContent();
			_dockContentResultPanel.HideOnClose = true;
			_dockContentResultPanel.Controls.Add(_resultPanel);
			_dockContentResultPanel.Text = "Results";
			_dockContentResultPanel.TabText = "Results";
			_dockContentResultPanel.ShowHint = DockState.DockBottom;
			_dockContentResultPanel.DockPanel = FDockPanel;
			_dockContentResultPanel.Name = "DockContentResultPanel";
		}

		protected override IHighlightingStrategy GetHighlightingStrategy()
		{
			switch (DesignerID)
			{
				case "SQL":
					return HighlightingStrategyFactory.CreateHighlightingStrategy("SQL");
				default:
					return HighlightingStrategyFactory.CreateHighlightingStrategy("D4");
			}
		}

		private void ShowResults()
		{
			_dockContentResultPanel.Show();
			_textEdit.Focus();
		}

		#region Execution

		private ScriptExecutor _executor;

		void IErrorSource.ErrorHighlighted(Exception exception)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception exception)
		{
			if (!Service.Dataphoria.LocateToError(exception))
			{
				var located = exception as ILocatedException;
				if (located != null)
					GotoPosition(located.Line, located.LinePos);

				Activate();	
				_textEdit.Focus();			
			}
		}

		private void AppendResultPanel(string results)
		{
			ShowResults();
			_resultPanel.AppendText(results);
		}

		private void HideResultPanel()
		{
			_resultPanel.Clear();
			_dockContentResultPanel.Hide();			
		}

		private string GetTextToExecute()
		{
			return
				(
					_textEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected
						? _textEdit.ActiveTextAreaControl.SelectionManager.SelectedText
						: _textEdit.Document.TextContent
				);
		}

		private QueryLanguage GetQueryLanguage()
		{
			return DesignerID == "SQL" ? QueryLanguage.RealSQL : QueryLanguage.D4;
		}

		private void SwitchToSQL()
		{
			Dataphoria.ExecuteScript("SetLanguage('RealSQL');");
		}

		private void SwitchFromSQL()
		{
			Dataphoria.ExecuteScript("SetLanguage('D4');");
		}

		private TextLocation GetSelectionPosition()
		{
			if (_textEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				return _textEdit.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition;
			return TextLocation.Empty;
		}

		private void ProcessErrors(ErrorList errors)
		{
			TextLocation offset = GetSelectionPosition();
			DebugLocator locator = Service.GetLocator();
			string locatorLocator = locator == null ? null : locator.Locator;
			
			for (int index = errors.Count - 1; index >= 0; index--)
			{
				Exception exception = errors[index];

				bool isWarning;
				var compilerException = exception as CompilerException;
				if (compilerException != null)
					isWarning = compilerException.ErrorLevel == CompilerErrorLevel.Warning;
				else
					isWarning = false;

				Dataphoria.Warnings.AppendError(this, exception, isWarning);
			}
		}

		private static void ShiftException(TextLocation offset, string locator, Exception exception)
		{
			var locatorException = exception as ILocatorException;
			if (locatorException != null)
			{
				if (String.IsNullOrEmpty(locatorException.Locator))
				{
					locatorException.Locator = locator;
					OffsetLocatedException(offset, locatorException);
				}
				else if (locatorException.Locator == locator)
					OffsetLocatedException(offset, locatorException);
			}
			else
			{
				var locatedException = exception as ILocatedException;
				if (locatedException != null)
					OffsetLocatedException(offset, locatedException);
			}
			if (exception.InnerException != null)
				ShiftException(offset, locator, exception.InnerException);
		}

		private static void OffsetLocatedException(TextLocation offset, ILocatedException locatedException)
		{
			if (locatedException.Line == 1)
				locatedException.LinePos += offset.X;
			if (locatedException.Line >= 0)
				locatedException.Line += offset.Y;
		}

		public void Prepare()
		{
			_resultPanel.Clear();
			PrepareForExecute();

			var result = new StringBuilder();

			var errors = new ErrorList();
			try
			{
				using (var statusForm = new StatusForm(Strings.ProcessingQuery))
				{
					bool attemptExecute = true;
					try
					{
						DateTime startTime = DateTime.Now;
						try
						{
							IServerScript script;
							IServerProcess process =
								DataSession.ServerSession.StartProcess(
									new ProcessInfo(DataSession.ServerSession.SessionInfo));
							try
							{
								script = process.PrepareScript(GetTextToExecute());
								try
								{
									if (ScriptExecutionUtility.ConvertParserErrors(script.Messages, errors))
									{
										foreach (IServerBatch batch in script.Batches)
										{
											if (batch.IsExpression())
											{
												IServerExpressionPlan plan = batch.PrepareExpression(null);
												try
												{
													attemptExecute &=
														ScriptExecutionUtility.ConvertCompilerErrors(plan.Messages,
																									 errors);
													if (attemptExecute)
													{
														result.AppendFormat
														(
															Strings.PrepareSuccessful,
															new object[]
															{
																plan.PlanStatistics.PrepareTime.ToString(),
																plan.PlanStatistics.CompileTime.ToString(),
																plan.PlanStatistics.OptimizeTime.ToString(),
																plan.PlanStatistics.BindingTime.ToString()
															}
														);
														result.Append("\r\n");
													}
												}
												finally
												{
													batch.UnprepareExpression(plan);
												}
											}
											else
											{
												IServerStatementPlan plan = batch.PrepareStatement(null);
												try
												{
													attemptExecute &=
														ScriptExecutionUtility.ConvertCompilerErrors(plan.Messages,
																									 errors);
													if (attemptExecute)
													{
														result.AppendFormat
														(
															Strings.PrepareSuccessful,
															new object[]
															{
																plan.PlanStatistics.PrepareTime.ToString(),
																plan.PlanStatistics.CompileTime.ToString(),
																plan.PlanStatistics.OptimizeTime.ToString(),
																plan.PlanStatistics.BindingTime.ToString()
															}
														);
														result.Append("\r\n");
													}
												}
												finally
												{
													batch.UnprepareStatement(plan);
												}
											}

											AppendResultPanel(result.ToString());
											result.Length = 0;
										}
									}
								}
								finally
								{
									process.UnprepareScript(script);
								}
							}
							finally
							{
								DataSession.ServerSession.StopProcess(process);
							}
						}
						finally
						{
							TimeSpan elapsed = DateTime.Now - startTime;
							_executionTimeStatus.Text = elapsed.ToString();
						}

						if (attemptExecute)
							SetStatus(Strings.ScriptPrepareSuccessful);
						else
							SetStatus(Strings.ScriptPrepareFailed);
					}
					catch (Exception exception)
					{
						SetStatus(Strings.ScriptFailed);
						errors.Add(exception);
					}
				}
			}
			finally
			{
				ProcessErrors(errors);
			}
		}

		public void PrepareLine()
		{
			if (SelectLine())
				Prepare();
		}

		public void Analyze()
		{
			PrepareForExecute();

			string plan;
			var errors = new ErrorList();
			try
			{
				using (var statusForm = new StatusForm(Strings.ProcessingQuery))
				{
					DateTime startTime = DateTime.Now;
					try
					{
						var paramsValue = new DataParams();
						paramsValue.Add(DataParam.Create(Dataphoria.UtilityProcess, "AQuery", GetTextToExecute()));
						plan = ((DAE.Runtime.Data.Scalar)Dataphoria.EvaluateQuery("ShowPlan(AQuery)", paramsValue)).AsString;
					}
					finally
					{
						TimeSpan elapsed = DateTime.Now - startTime;
						_executionTimeStatus.Text = elapsed.ToString();
					}
				}
			}
			catch (Exception exception)
			{
				errors.Add(exception);
				ProcessErrors(errors);
				SetStatus(Strings.ScriptAnalyzeFailed);
				return;
			}

			SetStatus(Strings.ScriptAnalyzeSuccessful);

			var analyzer = (Analyzer.Analyzer) Dataphoria.OpenDesigner(Dataphoria.GetDefaultDesigner("pla"), null);
			analyzer.LoadPlan(plan);
		}

		public void AnalyzeLine()
		{
			if (SelectLine())
				Analyze();
		}

		private void PrepareForExecute()
		{
			Dataphoria.EnsureServerConnection();
			Dataphoria.Warnings.ClearErrors(this);
			SetStatus(String.Empty);
		}

		public void Execute()
		{
			CancelExecute();

			PrepareForExecute();
			_resultPanel.Clear();

			FExecuteMenuItem.Enabled = false;
			FExecuteButton.Enabled = false;
			FExecuteLineMenuItem.Enabled = false;
			FCancelExecuteMenuItem.Enabled = true;
			FCancelExecuteButton.Enabled = true;
			_workingAnimation.Visible = true;

			_executor = 
				new ScriptExecutor
				(
					DataSession.ServerSession, 
					GetTextToExecute(),
					GetQueryLanguage(),
					ExecutorProgress,
					ExecutorFinished,
					GetExecuteLocator()
				);
			_executor.Start();
		}

		public bool SelectLine()
		{
			if (_textEdit.Document.TextLength > 0)
			{
				// Select the current line
				LineSegment line = _textEdit.Document.GetLineSegment(_textEdit.ActiveTextAreaControl.Caret.Line);
				_textEdit.ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new DefaultSelection
					(
						_textEdit.Document,
						new TextLocation(0, _textEdit.ActiveTextAreaControl.Caret.Line),
						new TextLocation(line.Length, _textEdit.ActiveTextAreaControl.Caret.Line)
					)
				);
				return true;
			}
			return false;
		}

		public void ExecuteLine()
		{
			if (SelectLine())
				Execute();
		}

		private void ExecutorProgress(PlanStatistics statistics, string results)
		{
			if (!IsDisposed)
				AppendResultPanel(results);
		}

		private void ExecutorFinished(ErrorList messages, TimeSpan elapsedTime)
		{
			// Stop animation & update controls
			_workingAnimation.Visible = false;
			FExecuteLineMenuItem.Enabled = true;
			FExecuteMenuItem.Enabled = true;
			FExecuteButton.Enabled = true;
			FCancelExecuteMenuItem.Enabled = false;
			FCancelExecuteButton.Enabled = false;

			// Handler Error/Warnings
			ProcessErrors(messages);
			if (ScriptExecutionUtility.ContainsError(messages))
				SetStatus(Strings.ScriptFailed);
			else
				SetStatus(Strings.ScriptSuccessful);

			// Handle execution time
			_executionTimeStatus.Text = elapsedTime.ToString();

			_executor = null;
		}

		private void CancelExecute()
		{
			if (_executor != null)
			{
				try
				{
					_executor.Stop();
					_executor = null;
				}
				finally
				{
					AppendResultPanel(Strings.UserAbort);
					ExecutorFinished(new ErrorList(), TimeSpan.Zero);
					SetStatus(Strings.UserAbort);
				}
			}
		}

		// IErrorSource

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			if (_executor != null)
			{
				if 
				(
					MessageBox.Show
					(
						Strings.AttemptingToCloseWhileExecuting,
						Strings.AttemptingToCloseWhileExecutingCaption, 
						MessageBoxButtons.OKCancel
					) == DialogResult.OK
				)
					CancelExecute();
				else
					args.Cancel = true;
			}
		}

		#endregion

		#region Exporting

		private void PromptAndExport(ExportType types)
		{
			using (var saveDialog = new SaveFileDialog())
			{
				saveDialog.Title = Strings.ExportSaveDialogTitle;
				saveDialog.Filter = "XML Schema (*.xsd)|*.xsd|XML File (*.xml)|*.xml|All Files (*.*)|*.*";
				if (types == ExportType.Schema) // schema only
				{
					saveDialog.DefaultExt = "xsd";
					saveDialog.FilterIndex = 1;
				}
				else
				{
					saveDialog.DefaultExt = "xml";
					saveDialog.FilterIndex = 2;
				}
				saveDialog.RestoreDirectory = false;
				saveDialog.InitialDirectory = ".";
				saveDialog.OverwritePrompt = true;
				saveDialog.CheckPathExists = true;
				saveDialog.AddExtension = true;

				if (saveDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				Cursor = Cursors.WaitCursor;
				try
				{
					Execute(types, saveDialog.FileName);
				}
				finally
				{
					Cursor = Cursors.Default;
				}
			}
		}

		private void Execute(ExportType exportType, string fileName)
		{
			// Make sure our server is connected...
			Dataphoria.EnsureServerConnection();

			using (var statusForm = new StatusForm(Strings.Exporting))
			{
				using (var connection = new DAEConnection())
				{
					if (DesignerID == "SQL")
						SwitchToSQL();
					try
					{
						using (var adapter = new DAEDataAdapter(GetTextToExecute(), connection))
						{
							using (var dataSet = new DataSet())
							{
								var process =
									DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.ServerSession.SessionInfo));
								try
								{
									connection.Open(DataSession.Server, DataSession.ServerSession, process);
									try
									{
										switch (exportType)
										{
											case ExportType.Data:
												adapter.Fill(dataSet);
												dataSet.WriteXml(fileName, XmlWriteMode.IgnoreSchema);
												break;
											case ExportType.Schema:
												adapter.FillSchema(dataSet, SchemaType.Source);
												dataSet.WriteXmlSchema(fileName);
												break;
											default:
												adapter.Fill(dataSet);
												dataSet.WriteXml(fileName, XmlWriteMode.WriteSchema);
												break;
										}
									}
									finally
									{
										connection.Close();
									}
								}
								finally
								{
									DataSession.ServerSession.StopProcess(process);
								}
							}
						}
					}
					finally
					{
						if (DesignerID == "SQL")
							SwitchFromSQL();
					}
				}
			}
		}

		#endregion

		#region Status and tool bar

		protected ToolStripStatusLabel _executionTimeStatus;
		private PictureBox _workingAnimation;
		protected ToolStripControlHost  _workingStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();

			this._executionTimeStatus = new ToolStripStatusLabel 
			{
				Name = "FExecutionTimeStatus",
				Alignment = ToolStripItemAlignment.Right,
				MergeIndex = 150,
				BorderSides = ToolStripStatusLabelBorderSides.All,
				Text = "0:00:00",
				DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
			};

			const string cResourceName = "Alphora.Dataphor.Dataphoria.Images.Rider.gif";
			Stream manifestResourceStream = GetType().Assembly.GetManifestResourceStream(cResourceName);
			Error.AssertFail(manifestResourceStream != null, "Resource must exist: " + cResourceName);
			_workingAnimation = 
				new PictureBox
				{
					Image = Image.FromStream(manifestResourceStream),
					SizeMode = PictureBoxSizeMode.AutoSize											
				};
			_workingStatus = new ToolStripControlHost(_workingAnimation, "FWorkingStatus")
			{
				Size = _workingAnimation.Size,
				Visible = false,
				Alignment = ToolStripItemAlignment.Right,
			};
			
			FStatusStrip.Items.Add(_executionTimeStatus);
			FStatusStrip.Items.Add(_workingStatus);
		}

		protected override void DisposeStatusBar()
		{
			base.DisposeStatusBar();

			_workingAnimation.Image.Dispose(); //otherwise the animation thread will still be hanging around
		}

		public override void MergeToolbarWith(ToolStrip parentToolStrip)
		{
			base.MergeToolbarWith(parentToolStrip);
		}

		#endregion

		#region Debugger

		private void InitializeDebugger()
		{
			Dataphoria.Debugger.PropertyChanged += Debugger_PropertyChanged;
			Dataphoria.Debugger.Breakpoints.Changed += Debugger_BreakpointsChanged;
			_textEdit.ActiveTextAreaControl.TextArea.IconBarMargin.MouseDown += IconBarMouseDown;
		}
		
		private void DeinitializeDebugger()
		{
			Dataphoria.Debugger.PropertyChanged -= Debugger_PropertyChanged;
			Dataphoria.Debugger.Breakpoints.Changed -= Debugger_BreakpointsChanged;
		}

        protected override void NameOrModifiedChanged(object sender, EventArgs args)
        {
			base.NameOrModifiedChanged(sender, args);
			ClearBreakpointBookmarks();
			LoadBreakpointBookmarks();
			UpdateCurrentLocation();
		}

		private void IconBarMouseDown(AbstractMargin iconBar, Point mousePos, MouseButtons mouseButtons)
		{
			if (mouseButtons == MouseButtons.Left)
			{
				TextLocation ALogicPos = 
					iconBar.TextArea.TextView.GetLogicalPosition
					(
						0, 
						mousePos.Y - iconBar.TextArea.TextView.DrawingPosition.Top
					);

				if (ALogicPos.Y >= 0 && ALogicPos.Y < iconBar.TextArea.Document.TotalNumberOfLines)
				{
					var locator = Service.GetLocator();
					if (locator != null)
						Dataphoria.Debugger.ToggleBreakpoint(new DebugLocator(locator.Locator, ALogicPos.Line + 1, -1));
				}
			}
		}

		private void Debugger_PropertyChanged(object sender, string[] propertyNames)
		{
			if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "IsPaused" || AItem == "CurrentLocation"; }))
				UpdateCurrentLocation();
		}

		private CurrentLineBookmark _currentBookmark;
	    private CodeCompletionWindow _codeCompletionWindow;

	    private void UpdateCurrentLocation()
		{
			var newCurrent = Dataphoria.Debugger.CurrentLocation;
			
			// Remove the existing current bookmark if it is no longer accurate
 			if (_currentBookmark != null && (newCurrent == null || !_currentBookmark.Locator.Equals(newCurrent)))
 			{
 				_textEdit.Document.BookmarkManager.RemoveMark(_currentBookmark);
 				_currentBookmark.RemoveMarker();
 				_currentBookmark = null;
 			}
 			
 			// Add a new current bookmark if needed and appropriate
			if 
			(
				_currentBookmark == null 
					&& newCurrent != null 
					&& Service.LocatorNameMatches(newCurrent.Locator) 
					&& newCurrent.Line >= 1 
					&& newCurrent.Line <= _textEdit.Document.TotalNumberOfLines
			)
 			{
 				var location = new TextLocation(newCurrent.LinePos < 1 ? 0 : newCurrent.LinePos - 1, newCurrent.Line - 1);
 				_currentBookmark = new CurrentLineBookmark(_textEdit.Document, location, newCurrent);
				AddBookmark(_currentBookmark);
 			}
		}

		private void AddBookmark(DebugBookmark bookmark)
		{
			_textEdit.Document.BookmarkManager.AddMark(bookmark);
			_textEdit.ActiveTextAreaControl.TextArea.Refresh(_textEdit.ActiveTextAreaControl.TextArea.IconBarMargin);
		}

		private void RemoveBookmark(BreakpointBookmark bookmark)
		{
			_textEdit.Document.BookmarkManager.RemoveMark(bookmark);
			bookmark.RemoveMarker();
			_textEdit.ActiveTextAreaControl.TextArea.Refresh(_textEdit.ActiveTextAreaControl.TextArea.IconBarMargin);
		}

		private void Debugger_BreakpointsChanged(NotifyingBaseList<DebugLocator> sender, bool isAdded, DebugLocator item, int index)
		{
			if (Service.LocatorNameMatches(item.Locator))
			{
				if (isAdded)
					AddBreakpointBookmark(item);
				else
				{
					var oldBookmark = (BreakpointBookmark)_textEdit.Document.BookmarkManager.GetFirstMark((Bookmark APredicate) => { return (APredicate is BreakpointBookmark) && ((BreakpointBookmark)APredicate).Locator == item; });
					if (oldBookmark != null)
						RemoveBookmark(oldBookmark);
				}
			}
		}
		
		private void LoadBreakpointBookmarks()
		{
			foreach (DebugLocator item in Dataphoria.Debugger.Breakpoints)
				if (Service.LocatorNameMatches(item.Locator))
					AddBreakpointBookmark(item);
		}

		private void ClearBreakpointBookmarks()
		{
			for (int i = _textEdit.Document.BookmarkManager.Marks.Count - 1; i >= 0; i--)
			{
				var bookmark = _textEdit.Document.BookmarkManager.Marks[i] as BreakpointBookmark;
				if (bookmark != null)
					RemoveBookmark(bookmark);
			}
		}

		private void AddBreakpointBookmark(DebugLocator item)
		{
			var location = new TextLocation((item.LinePos >= 0 ? item.LinePos - 1 : 0), item.Line - 1);
			var newBookmark = new BreakpointBookmark(_textEdit.Document, location, item);
			AddBookmark(newBookmark);
		}

		private DebugLocator GetExecuteLocator()
		{
			var locator = Service.GetLocator();
			if (locator != null)
			{
				if (_textEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				{
					TextLocation location = _textEdit.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition;
					return 
						new DebugLocator
						(
							locator.Locator, 
							location.Line + 1, 
							location.Column + 1
						);
				}
				else
					return locator;
			}
			else
				return null;
		}

		private void ToggleBreakpoint()
		{
			var locator = Service.GetLocator();
			if (locator != null)
				Dataphoria.Debugger.ToggleBreakpoint
				(
					new DebugLocator(locator.Locator, _textEdit.ActiveTextAreaControl.Caret.Line + 1, -1)
				);
		}

		#endregion

		#region Commands
		
		private void ExecuteClicked(object sender, EventArgs e)
		{
			Execute();
		}

		private void CancelExecuteClicked(object sender, EventArgs e)
		{
			CancelExecute();
		}

		private void PrepareClicked(object sender, EventArgs e)
		{
			Prepare();
		}

		private void AnalyzeClicked(object sender, EventArgs e)
		{
			Analyze();
		}

		private void ExecuteLineClicked(object sender, EventArgs e)
		{
			ExecuteLine();
		}

		private void PrepareLineClicked(object sender, EventArgs e)
		{
			PrepareLine();
		}

		private void AnalyzeLineClicked(object sender, EventArgs e)
		{
			AnalyzeLine();
		}

		private void SelectBlockClicked(object sender, EventArgs e)
		{
			new SelectBlock().Execute(_textEdit.ActiveTextAreaControl.TextArea);
		}

		private void PriorBlockClicked(object sender, EventArgs e)
		{
			new PriorBlock().Execute(_textEdit.ActiveTextAreaControl.TextArea);
		}

		private void NextBlockClicked(object sender, EventArgs e)
		{
			new NextBlock().Execute(_textEdit.ActiveTextAreaControl.TextArea);
		}

		private void ToggleBreakpointClicked(object sender, EventArgs e)
		{
			ToggleBreakpoint();
		}

		private void ExportSchemaClicked(object sender, EventArgs e)
		{
			PromptAndExport(ExportType.Schema);
		}

		private void ExportDataClicked(object sender, EventArgs e)
		{
			PromptAndExport(ExportType.Data);
		}

		private void ExportSchemaAndDataClicked(object sender, EventArgs e)
		{
			PromptAndExport(ExportType.Data | ExportType.Schema);
		}

		private void ViewResultsClicked(object sender, EventArgs e)
		{
			ShowResults();
		}
		
		#endregion
	}
}