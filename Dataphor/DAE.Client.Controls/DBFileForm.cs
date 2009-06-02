/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;


namespace Alphora.Dataphor.DAE.Client.Controls
{
    /*
      There are four boolean variables to consider when determining how to process; Luckily not all the combinations are significant.
      The following table illustrates the process logic implemented in PollFile:
          note:  "X" indicates the variable is not significant for that case.
     
      IsReadOnly      ValidProcessHandle        TempFileLockedInitially     TempFileLockedNow                     Action
          T                   X                           X                       T                   Wait for process to release file
          T                   X                           X                       F                   Delete temp file, close form
          F                   T                           X                       X                   Attach to process; on exit: Save file, delete temp file, and close form
          F                   F                           T                       T                   Wait for process to release file
          F                   F                           T                       F                   Delete temp file, close form
          F                   F                           F                       X                   Wait for the user to close form; Save file, delete temp file, close form
      
      Because there is no reliable way to obtain a valid process handle, the design waits for a configurable interval to elapse and then makes the assumption 
      that if we still have a process handle it is valid, otherwise decisions are based on whether the associated temporary file was locked when the interval elapsed.
      
    */

    /// <summary>Manages temporary file clean-up and update file persistance.</summary>
	public class DBFileForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.PictureBox AnimationBox;
		private System.Windows.Forms.TextBox MessageTextBox;
        private System.Windows.Forms.Button InternalAcceptButton;
        private System.Windows.Forms.Button InternalCancelButton;

        private FieldDataLink FContentLink;
        private FieldDataLink FNameLink;
        private FieldDataLink FExtensionLink;
        private bool FSaveOnExit;

		public DBFileForm(FieldDataLink AContentLink, FieldDataLink ANameLink, FieldDataLink AExtensionLink) 
        {            
            FContentLink = AContentLink;
            FNameLink = ANameLink;
            FExtensionLink = AExtensionLink;
            FSaveOnExit = !AContentLink.ReadOnly;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
		}  

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
                if (FFileOpened && !FFileProcessed)
                    ProcessFile();
				//if (components != null)
				//{
				//    components.Dispose();
				//}
			}
			base.Dispose( disposing );
		}        

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBFileForm));
            this.AnimationBox = new System.Windows.Forms.PictureBox();
            this.MessageTextBox = new System.Windows.Forms.TextBox();
            this.InternalCancelButton = new System.Windows.Forms.Button();
            this.InternalAcceptButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.AnimationBox)).BeginInit();
            this.SuspendLayout();
            // 
            // AnimationBox
            // 
            this.AnimationBox.Image = ((System.Drawing.Image)(resources.GetObject("AnimationBox.Image")));
            this.AnimationBox.Location = new System.Drawing.Point(8, 8);
            this.AnimationBox.Name = "AnimationBox";
            this.AnimationBox.Size = new System.Drawing.Size(64, 64);
            this.AnimationBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.AnimationBox.TabIndex = 0;
            this.AnimationBox.TabStop = false;
            // 
            // MessageTextBox
            // 
            this.MessageTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.MessageTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageTextBox.Location = new System.Drawing.Point(80, 8);
            this.MessageTextBox.Multiline = true;
            this.MessageTextBox.Name = "MessageTextBox";
            this.MessageTextBox.ReadOnly = true;
            this.MessageTextBox.Size = new System.Drawing.Size(281, 88);
            this.MessageTextBox.TabIndex = 4;
            this.MessageTextBox.TabStop = false;
            this.MessageTextBox.Text = "Determining how to process the file.";
            // 
            // InternalCancelButton
            // 
            this.InternalCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.InternalCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.InternalCancelButton.Location = new System.Drawing.Point(251, 102);
            this.InternalCancelButton.Name = "InternalCancelButton";
            this.InternalCancelButton.Size = new System.Drawing.Size(112, 23);
            this.InternalCancelButton.TabIndex = 3;
            this.InternalCancelButton.Text = "&Cancel";
            this.InternalCancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // InternalAcceptButton
            // 
            this.InternalAcceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.InternalAcceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.InternalAcceptButton.Location = new System.Drawing.Point(131, 102);
            this.InternalAcceptButton.Name = "InternalAcceptButton";
            this.InternalAcceptButton.Size = new System.Drawing.Size(112, 23);
            this.InternalAcceptButton.TabIndex = 6;
            this.InternalAcceptButton.Text = "&OK";
            this.InternalAcceptButton.Visible = false;
            this.InternalAcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // DBFileForm
            // 
            this.AcceptButton = this.InternalAcceptButton;
            this.CancelButton = this.InternalCancelButton;
            this.ClientSize = new System.Drawing.Size(371, 131);
            this.ControlBox = false;
            this.Controls.Add(this.InternalAcceptButton);
            this.Controls.Add(this.MessageTextBox);
            this.Controls.Add(this.InternalCancelButton);
            this.Controls.Add(this.AnimationBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DBFileForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Analyzing File...";
            ((System.ComponentModel.ISupportInitialize)(this.AnimationBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion			       

        public const int CDefaultPollInterval = 500;
        private int FPollInterval = CDefaultPollInterval;
        public int PollInterval
        {
            get { return FPollInterval; }
            set { FPollInterval = value; }
        }

        public const int CDefaultWaitForProcessInterval = 4000;
        private int FWaitForProcessInterval = CDefaultWaitForProcessInterval;
        public int WaitForProcessInterval
        {
            get { return FWaitForProcessInterval; }
            set { FWaitForProcessInterval = value; }
        }

        private bool FAutoRenameOnOpen = true;
        public bool AutoRenameOnOpen
        {
            get { return FAutoRenameOnOpen; }
            set { FAutoRenameOnOpen = value; }
        }

        public void OpenFile()
        {
            if (FFileOpened)
                throw new InvalidOperationException("File already open.");            

            if ((FExtensionLink.DataField == null) || !FExtensionLink.DataField.HasValue() || (FExtensionLink.DataField.AsString == String.Empty))
                // Without an extension, we cannot auto-create the file, we must prompt for the name of the file
                FFileName = PromptForSaveName();
            else
                FFileName = GetTempFileName();

            try
            {
                InternalSaveToFile();
                FFileOpened = true;
            }
            catch (IOException)
            {
                bool LWriteable = false;
                string LText = Strings.Get("DBFileForm.CannotWriteText", FFileName);
                string LCaption = Strings.Get("DBFileForm.CannotWriteCaption");
                while (!LWriteable)
                {
                    if (MessageBox.Show(LText, LCaption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Retry)
                    {
                        try
                        {
                            InternalSaveToFile();
                            LWriteable = true;
                        }
                        catch (IOException) { } //LWriteable is already false, no need to do anything
                    }
                    else
                        return;
                }
            }

            
            if (ReadOnly)
                FileUtility.EnsureReadOnly(FFileName);
            else              
                Show(); //have to Show before the process or window will be on top of process          
            
            try
            {              
                ProcessStartInfo LInfo = new ProcessStartInfo(FFileName);
                FProcess = Process.Start(LInfo);               
                // Process.Start may or may not return a process.  ShellExecute does not return a process if it uses 
                //  DDE to an existing process (e.g. Word) to open the file.  As a result, we must use
                //  other stategies such as waiting for file availablility and prompting the user to continue.

                // TODO: Look into using AssocQueryString to get the associated file and forcing a create process, or look into DDE apis to find termination point
            }
            catch (Exception LException)
            {
                // If the process start fails, prompt the user whether to delete the file
                if (MessageBox.Show(Strings.Get("DBFileForm.ProcessStartFailedText", FFileName, LException.Message), Strings.Get("DBFileForm.ProcessStartFailedCaption"), MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    FileUtility.EnsureWriteable(FFileName);
                    File.Delete(FFileName);
                }
                throw new AbortException();
            }

            try
            {
                if (FProcess != null)
                    FProcess.EnableRaisingEvents = true;

                Thread LFilePoller = ReadOnly ? new Thread(new ThreadStart(PollFileReadOnly)) : new Thread(new ThreadStart(PollFile));                
                LFilePoller.IsBackground = true;
                LFilePoller.Start();       
            }
            catch
            {
                if (FProcess != null)
                    FProcess.Dispose();
                throw;
            }  
        }
       
        private bool FFileOpened = false;
        public bool FileOpened
        {
            get { return FFileOpened; }
        }

        private void PollFile()
        {
            Thread.Sleep(FWaitForProcessInterval);           
            bool LInitialPoll = true;
            while (true)
            {                    
                lock (FFilePollerLock)
                {
                    if (FFileProcessed)
                        return;                                   
                    //we waited the delay and the process stuck around: attach to the process; when the process exits save (if applicable) and delete the temp file
                    if ((FProcess != null) && (!FProcess.HasExited))
                    {
                        ProcessFileAttached();                      
                        return;
                    }
                    else if (!FileInUse)
                    {
                        //the temp file has been released (it was locked and now isn't): save the file and delete the temp file
                        if (!LInitialPoll)
                            ProcessFileUnlocked();
                        //we waited the delay, read-only is false, we don't have a valid process, and the temp file is not locked: the user will have to choose how to process 
                        else
                            UserInputRequired();                            
                        return;
                    }
                    else if (LInitialPoll)
                        ShowFileLockedInstructions();
                }

                LInitialPoll = false;
                Thread.Sleep(FPollInterval); 
            }
        }

        private void PollFileReadOnly()
        {
			try
			{
				Thread.Sleep(FWaitForProcessInterval);
				while (true)
				{
					lock (FFilePollerLock)
					{
						if (FFileProcessed)
							return;
						//we waited at least the delay; the temp file is not locked: delete it and end
						if (!FileInUse)
						{
							ProcessFile();
							return;
						}
					}
					//the temp file is locked: wait for the process to release it
					Thread.Sleep(FPollInterval);    
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
        }
                
        private object FFilePollerLock = new object();
        private bool FFileProcessed = false;
        public bool FileProcessed { get { return FFileProcessed; } }
        private string FFileName;
        public string FileName { get { return FFileName; } }

        public const long CDefaultMaximumContentLength = 30000000;	// about 30meg
        private long FMaximumContentLength = DBFileForm.CDefaultMaximumContentLength;
        public long MaximumContentLength
        {
            get { return FMaximumContentLength; }
            set { FMaximumContentLength = Math.Max(0, value); }
        }

        private Process FProcess;      

        private DataField DataField
        {
            get { return FContentLink == null ? null : FContentLink.DataField; }
        }

        private DataSet DataSet
        {
            get { return FContentLink == null ? null : FContentLink.DataSet; }
        }

        private bool ReadOnly
        {
            get { return FContentLink == null ? true : FContentLink.ReadOnly; }
        }

        private bool FileInUse
        {
            get
            {
                try
                {
                    using (FileStream LStream = File.Open(FFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        return false;
                }
                catch { return true; }
            }
        }

        private bool InternalFileWriteable
        {
            get { return FileWriteable(FFileName); }
        }
            
        private bool FileWriteable(string AFileName)
        {           
            try
            {
                using (FileStream LStream = new FileInfo(AFileName).OpenWrite())
                    return true;
            }
            catch { return false; }           
        }
        
        private bool FileReadable(string AFileName)
        {
            try
            {
                using (FileStream LStream = new FileInfo(AFileName).OpenRead())
                    return true;
            }
            catch { return false; }           
        }

        private void ProcessFileAttached()
        {
            ShowFileAttachedInstructions();
            FProcess.Exited += new EventHandler(ProcessLoadFile);
        }

        private void ProcessFileUnlocked()
        {
            if (Visible)
                Hide();
            ProcessLoadFile();
        }

        private void ProcessLoadFile()
        {            
            if (FSaveOnExit && !InternalContentIdenticalToFile)
                InternalLoadFromFile();
            ProcessFile();            
        }

        private void ProcessLoadFile(object ASender, EventArgs AArgs)
        {
            lock (FFilePollerLock)
            {
                if (Visible)
                    Hide();
                if (!FFileProcessed)
                    ProcessLoadFile();
            }
        }

        private void ProcessFile()
        {            
            try
            {
                FileUtility.EnsureWriteable(FFileName);
                File.Delete(FFileName);
            }
            catch { } //We could not delete temporary file, do nothing
            FFileProcessed = true;
        }

        private string GetTempFileName()
        {
            string LBaseFileName = String.Empty;           
            if ((FNameLink.DataField != null) && FNameLink.DataField.HasValue())
                LBaseFileName = FNameLink.DataField.AsString;

            string LExtension = FExtensionLink.DataField.AsString != String.Empty ? "." + FExtensionLink.DataField.AsString : "";

            string LTempDirectory = Path.Combine(Path.GetTempPath(), CTempSubDirectory);
            Directory.CreateDirectory(LTempDirectory);

            string LFileName = String.Empty;
            if (LBaseFileName == String.Empty)
            {
                // Create an auto-generated file name
                LFileName = Path.Combine(LTempDirectory, CBaseFileName) + LExtension;
                int i = 2;
                while (File.Exists(LFileName))
                {
                    LFileName = Path.Combine(LTempDirectory, CBaseFileName + i.ToString()) + LExtension;
                    i++;
                };
            }
            else
            {
                // Use the file name referenced by the NameColumnName of this control
                LFileName = Path.Combine(LTempDirectory, LBaseFileName) + LExtension;
                if (File.Exists(LFileName))
                {
                    if (FAutoRenameOnOpen)
                    {
                        int i = 2;
                        do 
                        {
                            LFileName = Path.Combine(LTempDirectory, LBaseFileName + "(" + i.ToString() + ")") + LExtension;
                            i++;
                        } while (File.Exists(LFileName));
                    }
                    else
                    {
                        if (!FileWriteable(LFileName))
                        {
                            DialogResult LDialogResult = DialogResult.Retry;
                            string LText = Strings.Get("DBFileForm.FileNameLockedText", LFileName);
                            string LCaption = Strings.Get("DBFileForm.FileNameLockedCaption");
                            do
                            {
                                LDialogResult = MessageBox.Show(LText, LCaption, MessageBoxButtons.AbortRetryIgnore);
                                switch (LDialogResult)
                                {
                                    case DialogResult.Ignore:
                                        LFileName = PromptForSaveName();
                                        break;
                                    case DialogResult.Abort:
                                        throw new AbortException();
                                    //default to Retry and do nothing
                                }
                            } while (File.Exists(LFileName) && !FileWriteable(LFileName));
                        }
                        else if (!ContentIdenticalToFile(LFileName))
                        {
                            DialogResult LResult = MessageBox.Show(Strings.Get("DBFileForm.OverwriteBoxText", LFileName), Strings.Get("DBFileForm.OverwriteBoxCaption"), MessageBoxButtons.YesNoCancel);
                            switch (LResult)
                            {
                                case DialogResult.No:
                                    LFileName = PromptForSaveName();
                                    break;
                                case DialogResult.Cancel:
                                    throw new AbortException();
                            }
                        }
                    }
                }
            }
            return LFileName;
        }

        public const string CTempSubDirectory = @"File";
        public const string CBaseFileName = @"File";
        
        private bool InternalContentIdenticalToFile
        {
            get { return ContentIdenticalToFile(FFileName); }
        }
        
        private bool ContentIdenticalToFile(string AFileName)
        {
            //if ContentIdenticalToFile can't read the file, consider true
            if (!FileReadable(AFileName))
                return false;
            using (FileStream LFileStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
            using (Stream LStream = DataField.Value.OpenStream())
                return StreamUtility.StreamsEqual(LStream, LFileStream);
        }

        private bool FUserInputRequired;
        private void UserInputRequired()
        {
            FUserInputRequired = true;
            ShowUserInputRequiredInstructions();
        }        
        
        private void ShowUserInputRequiredInstructions()
        {             
            string LCaption = Strings.Get("DBFileForm.UserInputRequiredInstructionsCaption");
            string LText = Strings.Get("DBFileForm.UserInputRequiredInstructionsText");
            ShowInstructions(LCaption, LText, true);                    
        }

        private void ShowFileAttachedInstructions()
        {
            string LCaption = Strings.Get("DBFileForm.FileAttachedInstructionsCaption", FProcess.MainWindowTitle);
            string LText = Strings.Get("DBFileForm.FileAttachedInstructionsText");
            ShowInstructions(LCaption, LText, false);            
        }

        private void ShowFileLockedInstructions()
        {
            string LCaption = Strings.Get("DBFileForm.FileLockedInstructionsCaption");
            string LText = Strings.Get("DBFileForm.FileLockedInstructionsText");
            ShowInstructions(LCaption, LText, false);
        }

        private delegate void InvokeInstructionsDelegate(string ACaption, string AText, bool AShowCancelButton);
        private void ShowInstructions(string ACaption, string AText, bool AShowCancelButton)
        {
            if (InvokeRequired)
            {
                Invoke(new InvokeInstructionsDelegate(ShowInstructions), new object[] { ACaption, AText, AShowCancelButton });
                return;
            }
            
            Text = ACaption;
            MessageTextBox.Text = AText;
            InternalCancelButton.Visible = AShowCancelButton;
            InternalAcceptButton.Visible = true;
            if (!Visible)
                Show();   
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            lock (FFilePollerLock)
            {
                Hide();
                if (!FFileProcessed && FUserInputRequired)
                    ProcessUserInputRequired();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            lock (FFilePollerLock)
            {
                Hide();
                ProcessFile();
            }
        }       

        private void ProcessUserInputRequired()
        {              
            if (FSaveOnExit && !InternalFileWriteable)
            {
                string LText = Strings.Get("DBFileForm.FileLockedText", FFileName);
                string LCaption = Strings.Get("DBFileForm.FileLockedCaption");
                do
                {
                    FSaveOnExit = MessageBox.Show(LText, LCaption, MessageBoxButtons.OKCancel) == DialogResult.OK;                    
                } while (FSaveOnExit && !InternalFileWriteable);
            }
            if (FSaveOnExit)
                ProcessLoadFile();
            else
                ProcessFile();           
        }

        /// <summary> Loads the associated field contents from the specified file. </summary>       
        public void LoadFromFile() 
        {
            FFileName = String.Empty;
            InternalLoadFromFile();
        }

        private void InternalLoadFromFile()
        {
            if (String.IsNullOrEmpty(FFileName))
                FFileName = PromptForLoadName();
            using (FileStream LFileStream = new FileStream(FFileName, FileMode.Open, FileAccess.Read))
            {
                if (LFileStream.Length > FMaximumContentLength)
                    throw new ControlsException(ControlsException.Codes.MaximumContentLengthExceeded, LFileStream.Length, FMaximumContentLength);
                using (DAE.Runtime.Data.Scalar LNewValue = new DAE.Runtime.Data.Scalar(DataSet.Process, DataSet.Process.DataTypes.SystemBinary))
                {
                    using (Stream LStream = LNewValue.OpenStream())
                        StreamUtility.CopyStream(LFileStream, LStream);
                    DataField.Value = LNewValue;
                }
            }
        }

        private string PromptForLoadName()
        {
            using (OpenFileDialog LDialog = new OpenFileDialog())
            {
                PrepareFileDialog(LDialog);

                LDialog.RestoreDirectory = false;
                LDialog.Title = Strings.Get("DBFileForm.LoadFileCaption");
                LDialog.Multiselect = false;
                LDialog.CheckFileExists = true;

                // TODO: Call back here so that InitialDirectory can be recalled

                DialogResult LResult = LDialog.ShowDialog();
                if (LResult != DialogResult.OK)
                    throw new AbortException();

                // TODO: Call back here so that InitialDirectory can be stored

                return LDialog.FileName;
            }
        }

        /// <summary> Saves the associated field contents to the specified file. </summary>
        /// <remarks> A null reference exception will occur if this method is invoked on a nil column. </remarks>
        public void SaveToFile()
        {
            FFileName = String.Empty;
            InternalSaveToFile();
        }

        internal void InternalSaveToFile()
        {
            if (String.IsNullOrEmpty(FFileName))
                FFileName = PromptForSaveName();
            // The file must be writable to overwrite
            if (File.Exists(FFileName))
                FileUtility.EnsureWriteable(FFileName);

            // Write/rewrite the file
            using (FileStream LFileStream = new FileStream(FFileName, FileMode.Create, FileAccess.Write))
                if (((Schema.ScalarType)DataField.DataType).NativeType == typeof(byte[]))
                    using (Stream LStream = DataField.Value.OpenStream())
                        StreamUtility.CopyStream(LStream, LFileStream);
                else
                    using (StreamWriter LStreamWriter = new StreamWriter(LFileStream))
                        LStreamWriter.Write(DataField.Value.AsString);  
        }

        private string PromptForSaveName()
        {
            using (SaveFileDialog LDialog = new SaveFileDialog())
            {
                PrepareFileDialog(LDialog);

                LDialog.RestoreDirectory = false;
                LDialog.Title = Strings.Get("DBFileForm.SaveFileCaption");
                LDialog.CheckPathExists = true;
                LDialog.OverwritePrompt = true;

                // TODO: Call back here so that InitialDirectory can be recalled

                DialogResult LResult = LDialog.ShowDialog();
                if (LResult != DialogResult.OK)
                    throw new AbortException();

                // TODO: Call back here so that InitialDirectory can be stored

                return LDialog.FileName;
            }
        }

        private void PrepareFileDialog(FileDialog ADialog)
        {
            string LExtension = String.Empty;
            if ((FExtensionLink.DataField != null) && FExtensionLink.DataField.HasValue() && !FExtensionLink.DataField.IsNil)
                LExtension = FExtensionLink.DataField.AsString;

            if (LExtension != String.Empty)
            {
                ADialog.DefaultExt = LExtension;
                ADialog.AddExtension = true;

                // Prepare filter
                ADialog.Filter = String.Format("{0} (*.{1})|*.{1}|{2}", Strings.Get("DBFileForm.UnknownFileDescription", LExtension), LExtension, Strings.Get("DBFileForm.AllFileFilter"));
                ADialog.FilterIndex = 0;

                // Set default filename
                if ((FNameLink.DataField != null) && FNameLink.DataField.HasValue() && !FNameLink.DataField.IsNil && (FNameLink.DataField.AsString != String.Empty))
                    ADialog.FileName = FNameLink.DataField.AsString + "." + LExtension;
            }
            else
            {
                // Prepare filter
                ADialog.Filter = Strings.Get("DBFileForm.AllFileFilter");
                ADialog.FilterIndex = 0;

                ADialog.AddExtension = false;
            }
        }       

        /// <summary> Stops the monitoring of any files initiated by a call to Open</summary>
        public void CloseFile()
        {
            lock (FFilePollerLock)
            {
                if (Visible)
                    Hide(); 
                if (!FFileProcessed)
                    ProcessFile();
            }
        }

        /// <summary> Allows the user to save a copy of the temp file, when the link to the control has been lost. </summary>	
        public void RecoverFile()
        {
            lock (FFilePollerLock)
            {
                if (Visible)
                    Hide();
                if (!FFileProcessed)
                {
                    if (FSaveOnExit)
                    {
                        string LText = Strings.Get("DBFileForm.ClosingText");
                        string LCaption = Strings.Get("DBFileForm.ClosingCaption");
                        if (MessageBox.Show(LText, LCaption, MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            string LTargetFileName;
                            using (SaveFileDialog LDialog = new SaveFileDialog())
                            {
                                string LExtension = Path.GetExtension(FFileName);
                                if (!String.IsNullOrEmpty(LExtension))
                                {
                                    LDialog.DefaultExt = LExtension;
                                    LDialog.AddExtension = true;
                                }
                                else
                                    LDialog.AddExtension = false;

                                LDialog.FileName = Path.GetFileName(FFileName);

                                LDialog.RestoreDirectory = false;
                                LDialog.Title = Strings.Get("DBFileForm.SaveFileTitle");
                                LDialog.CheckPathExists = true;
                                LDialog.OverwritePrompt = true;

                                if (LDialog.ShowDialog() != DialogResult.OK)
                                    throw new AbortException();

                                LTargetFileName = LDialog.FileName;

                            }
                            File.Copy(FFileName, LTargetFileName, true);
                        }
                    }
                    ProcessFile();
                }
            }
        }
	}
}
