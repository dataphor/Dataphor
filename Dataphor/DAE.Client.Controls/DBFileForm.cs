/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Alphora.Dataphor.Windows;


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

        private FieldDataLink _contentLink;
        private FieldDataLink _nameLink;
        private FieldDataLink _extensionLink;
        private bool _saveOnExit;

		public DBFileForm(FieldDataLink contentLink, FieldDataLink nameLink, FieldDataLink extensionLink) 
        {            
            _contentLink = contentLink;
            _nameLink = nameLink;
            _extensionLink = extensionLink;
            _saveOnExit = !contentLink.ReadOnly;
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
                if (_fileOpened && !_fileProcessed)
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

        public const int DefaultPollInterval = 500;
        private int _pollInterval = DefaultPollInterval;
        public int PollInterval
        {
            get { return _pollInterval; }
            set { _pollInterval = value; }
        }

        public const int DefaultWaitForProcessInterval = 4000;
        private int _waitForProcessInterval = DefaultWaitForProcessInterval;
        public int WaitForProcessInterval
        {
            get { return _waitForProcessInterval; }
            set { _waitForProcessInterval = value; }
        }

        private bool _autoRenameOnOpen = true;
        public bool AutoRenameOnOpen
        {
            get { return _autoRenameOnOpen; }
            set { _autoRenameOnOpen = value; }
        }

        public void OpenFile()
        {
            if (_fileOpened)
                throw new InvalidOperationException("File already open.");            

            if ((_extensionLink.DataField == null) || !_extensionLink.DataField.HasValue() || (_extensionLink.DataField.AsString == String.Empty))
                // Without an extension, we cannot auto-create the file, we must prompt for the name of the file
                _fileName = PromptForSaveName();
            else
                _fileName = GetTempFileName();

            try
            {
                InternalSaveToFile();
                _fileOpened = true;
            }
            catch (IOException)
            {
                bool writeable = false;
                string text = Strings.Get("DBFileForm.CannotWriteText", _fileName);
                string caption = Strings.Get("DBFileForm.CannotWriteCaption");
                while (!writeable)
                {
                    if (MessageBox.Show(text, caption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Retry)
                    {
                        try
                        {
                            InternalSaveToFile();
                            writeable = true;
                        }
                        catch (IOException) { } //LWriteable is already false, no need to do anything
                    }
                    else
                        return;
                }
            }

            
            if (ReadOnly)
                FileUtility.EnsureReadOnly(_fileName);
            else              
                Show(); //have to Show before the process or window will be on top of process          
            
            try
            {              
                ProcessStartInfo info = new ProcessStartInfo(_fileName);
                _process = Process.Start(info);               
                // Process.Start may or may not return a process.  ShellExecute does not return a process if it uses 
                //  DDE to an existing process (e.g. Word) to open the file.  As a result, we must use
                //  other stategies such as waiting for file availablility and prompting the user to continue.

                // TODO: Look into using AssocQueryString to get the associated file and forcing a create process, or look into DDE apis to find termination point
            }
            catch (Exception exception)
            {
                // If the process start fails, prompt the user whether to delete the file
                if (MessageBox.Show(Strings.Get("DBFileForm.ProcessStartFailedText", _fileName, exception.Message), Strings.Get("DBFileForm.ProcessStartFailedCaption"), MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    FileUtility.EnsureWriteable(_fileName);
                    File.Delete(_fileName);
                }
                throw new AbortException();
            }

            try
            {
                if (_process != null)
                    _process.EnableRaisingEvents = true;

                Thread filePoller = ReadOnly ? new Thread(new ThreadStart(PollFileReadOnly)) : new Thread(new ThreadStart(PollFile));                
                filePoller.IsBackground = true;
                filePoller.Start();       
            }
            catch
            {
                if (_process != null)
                    _process.Dispose();
                throw;
            }  
        }
       
        private bool _fileOpened = false;
        public bool FileOpened
        {
            get { return _fileOpened; }
        }

        private void PollFile()
        {
            Thread.Sleep(_waitForProcessInterval);           
            bool initialPoll = true;
            while (true)
            {                    
                lock (_filePollerLock)
                {
                    if (_fileProcessed)
                        return;                                   
                    //we waited the delay and the process stuck around: attach to the process; when the process exits save (if applicable) and delete the temp file
                    if ((_process != null) && (!_process.HasExited))
                    {
                        ProcessFileAttached();                      
                        return;
                    }
                    else if (!FileInUse)
                    {
                        //the temp file has been released (it was locked and now isn't): save the file and delete the temp file
                        if (!initialPoll)
                            ProcessFileUnlocked();
                        //we waited the delay, read-only is false, we don't have a valid process, and the temp file is not locked: the user will have to choose how to process 
                        else
                            UserInputRequired();                            
                        return;
                    }
                    else if (initialPoll)
                        ShowFileLockedInstructions();
                }

                initialPoll = false;
                Thread.Sleep(_pollInterval); 
            }
        }

        private void PollFileReadOnly()
        {
			try
			{
				Thread.Sleep(_waitForProcessInterval);
				while (true)
				{
					lock (_filePollerLock)
					{
						if (_fileProcessed)
							return;
						//we waited at least the delay; the temp file is not locked: delete it and end
						if (!FileInUse)
						{
							ProcessFile();
							return;
						}
					}
					//the temp file is locked: wait for the process to release it
					Thread.Sleep(_pollInterval);    
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
        }
                
        private object _filePollerLock = new object();
        private bool _fileProcessed = false;
        public bool FileProcessed { get { return _fileProcessed; } }
        private string _fileName;
        public string FileName { get { return _fileName; } }

        public const long DefaultMaximumContentLength = 30000000;	// about 30meg
        private long _maximumContentLength = DBFileForm.DefaultMaximumContentLength;
        public long MaximumContentLength
        {
            get { return _maximumContentLength; }
            set { _maximumContentLength = Math.Max(0, value); }
        }

        private Process _process;      

        private DataField DataField
        {
            get { return _contentLink == null ? null : _contentLink.DataField; }
        }

        private DataSet DataSet
        {
            get { return _contentLink == null ? null : _contentLink.DataSet; }
        }

        private bool ReadOnly
        {
            get { return _contentLink == null ? true : _contentLink.ReadOnly; }
        }

        private bool FileInUse
        {
            get
            {
                try
                {
                    using (FileStream stream = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        return false;
                }
                catch { return true; }
            }
        }

        private bool InternalFileWriteable
        {
            get { return FileWriteable(_fileName); }
        }
            
        private bool FileWriteable(string fileName)
        {           
            try
            {
                using (FileStream stream = new FileInfo(fileName).OpenWrite())
                    return true;
            }
            catch { return false; }           
        }
        
        private bool FileReadable(string fileName)
        {
            try
            {
                using (FileStream stream = new FileInfo(fileName).OpenRead())
                    return true;
            }
            catch { return false; }           
        }

        private void ProcessFileAttached()
        {
            ShowFileAttachedInstructions();
            _process.Exited += new EventHandler(ProcessLoadFile);
        }

        private void ProcessFileUnlocked()
        {
            if (Visible)
                Hide();
            ProcessLoadFile();
        }

        private void ProcessLoadFile()
        {            
            if (_saveOnExit && !InternalContentIdenticalToFile)
                InternalLoadFromFile();
            ProcessFile();            
        }

        private void ProcessLoadFile(object sender, EventArgs args)
        {
            lock (_filePollerLock)
            {
                if (Visible)
                    Hide();
                if (!_fileProcessed)
                    ProcessLoadFile();
            }
        }

        private void ProcessFile()
        {            
            try
            {
                FileUtility.EnsureWriteable(_fileName);
                File.Delete(_fileName);
            }
            catch { } //We could not delete temporary file, do nothing
            _fileProcessed = true;
        }

        private string GetTempFileName()
        {
            string baseFileName = String.Empty;           
            if ((_nameLink.DataField != null) && _nameLink.DataField.HasValue())
                baseFileName = _nameLink.DataField.AsString;

            string extension = _extensionLink.DataField.AsString != String.Empty ? "." + _extensionLink.DataField.AsString : "";

            string tempDirectory = Path.Combine(Path.GetTempPath(), TempSubDirectory);
            Directory.CreateDirectory(tempDirectory);

            string fileName = String.Empty;
            if (baseFileName == String.Empty)
            {
                // Create an auto-generated file name
                fileName = Path.Combine(tempDirectory, BaseFileName) + extension;
                int i = 2;
                while (File.Exists(fileName))
                {
                    fileName = Path.Combine(tempDirectory, BaseFileName + i.ToString()) + extension;
                    i++;
                };
            }
            else
            {
                // Use the file name referenced by the NameColumnName of this control
                fileName = Path.Combine(tempDirectory, baseFileName) + extension;
                if (File.Exists(fileName))
                {
                    if (_autoRenameOnOpen)
                    {
                        int i = 2;
                        do 
                        {
                            fileName = Path.Combine(tempDirectory, baseFileName + "(" + i.ToString() + ")") + extension;
                            i++;
                        } while (File.Exists(fileName));
                    }
                    else
                    {
                        if (!FileWriteable(fileName))
                        {
                            DialogResult dialogResult = DialogResult.Retry;
                            string text = Strings.Get("DBFileForm.FileNameLockedText", fileName);
                            string caption = Strings.Get("DBFileForm.FileNameLockedCaption");
                            do
                            {
                                dialogResult = MessageBox.Show(text, caption, MessageBoxButtons.AbortRetryIgnore);
                                switch (dialogResult)
                                {
                                    case DialogResult.Ignore:
                                        fileName = PromptForSaveName();
                                        break;
                                    case DialogResult.Abort:
                                        throw new AbortException();
                                    //default to Retry and do nothing
                                }
                            } while (File.Exists(fileName) && !FileWriteable(fileName));
                        }
                        else if (!ContentIdenticalToFile(fileName))
                        {
                            DialogResult result = MessageBox.Show(Strings.Get("DBFileForm.OverwriteBoxText", fileName), Strings.Get("DBFileForm.OverwriteBoxCaption"), MessageBoxButtons.YesNoCancel);
                            switch (result)
                            {
                                case DialogResult.No:
                                    fileName = PromptForSaveName();
                                    break;
                                case DialogResult.Cancel:
                                    throw new AbortException();
                            }
                        }
                    }
                }
            }
            return fileName;
        }

        public const string TempSubDirectory = @"File";
        public const string BaseFileName = @"File";
        
        private bool InternalContentIdenticalToFile
        {
            get { return ContentIdenticalToFile(_fileName); }
        }
        
        private bool ContentIdenticalToFile(string fileName)
        {
            //if ContentIdenticalToFile can't read the file, consider true
            if (!FileReadable(fileName))
                return false;
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (Stream stream = DataField.Value.OpenStream())
                return StreamUtility.StreamsEqual(stream, fileStream);
        }

        private bool _userInputRequired;
        private void UserInputRequired()
        {
            _userInputRequired = true;
            ShowUserInputRequiredInstructions();
        }        
        
        private void ShowUserInputRequiredInstructions()
        {             
            string caption = Strings.Get("DBFileForm.UserInputRequiredInstructionsCaption");
            string text = Strings.Get("DBFileForm.UserInputRequiredInstructionsText");
            ShowInstructions(caption, text, true);                    
        }

        private void ShowFileAttachedInstructions()
        {
            string caption = Strings.Get("DBFileForm.FileAttachedInstructionsCaption", _process.MainWindowTitle);
            string text = Strings.Get("DBFileForm.FileAttachedInstructionsText");
            ShowInstructions(caption, text, false);            
        }

        private void ShowFileLockedInstructions()
        {
            string caption = Strings.Get("DBFileForm.FileLockedInstructionsCaption");
            string text = Strings.Get("DBFileForm.FileLockedInstructionsText");
            ShowInstructions(caption, text, false);
        }

        private delegate void InvokeInstructionsDelegate(string ACaption, string AText, bool AShowCancelButton);
        private void ShowInstructions(string caption, string text, bool showCancelButton)
        {
            if (InvokeRequired)
            {
                Invoke(new InvokeInstructionsDelegate(ShowInstructions), new object[] { caption, text, showCancelButton });
                return;
            }
            
            Text = caption;
            MessageTextBox.Text = text;
            InternalCancelButton.Visible = showCancelButton;
            InternalAcceptButton.Visible = true;
            if (!Visible)
                Show();   
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            lock (_filePollerLock)
            {
                Hide();
                if (!_fileProcessed && _userInputRequired)
                    ProcessUserInputRequired();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            lock (_filePollerLock)
            {
                Hide();
                ProcessFile();
            }
        }       

        private void ProcessUserInputRequired()
        {              
            if (_saveOnExit && !InternalFileWriteable)
            {
                string text = Strings.Get("DBFileForm.FileLockedText", _fileName);
                string caption = Strings.Get("DBFileForm.FileLockedCaption");
                do
                {
                    _saveOnExit = MessageBox.Show(text, caption, MessageBoxButtons.OKCancel) == DialogResult.OK;                    
                } while (_saveOnExit && !InternalFileWriteable);
            }
            if (_saveOnExit)
                ProcessLoadFile();
            else
                ProcessFile();           
        }

        /// <summary> Loads the associated field contents from the specified file. </summary>       
        public void LoadFromFile() 
        {
            _fileName = String.Empty;
            InternalLoadFromFile();
        }

        private void InternalLoadFromFile()
        {
            if (String.IsNullOrEmpty(_fileName))
                _fileName = PromptForLoadName();
            using (FileStream fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length > _maximumContentLength)
                    throw new ControlsException(ControlsException.Codes.MaximumContentLengthExceeded, fileStream.Length, _maximumContentLength);
                using (DAE.Runtime.Data.Scalar newValue = new DAE.Runtime.Data.Scalar(DataSet.Process.ValueManager, DataSet.Process.DataTypes.SystemBinary))
                {
                    using (Stream stream = newValue.OpenStream())
                        StreamUtility.CopyStream(fileStream, stream);
                    DataField.Value = newValue;
                }
            }
        }

        private string PromptForLoadName()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                PrepareFileDialog(dialog);

                dialog.RestoreDirectory = false;
                dialog.Title = Strings.Get("DBFileForm.LoadFileCaption");
                dialog.Multiselect = false;
                dialog.CheckFileExists = true;

                // TODO: Call back here so that InitialDirectory can be recalled

                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                    throw new AbortException();

                // TODO: Call back here so that InitialDirectory can be stored

                return dialog.FileName;
            }
        }

        /// <summary> Saves the associated field contents to the specified file. </summary>
        /// <remarks> A null reference exception will occur if this method is invoked on a nil column. </remarks>
        public void SaveToFile()
        {
            _fileName = String.Empty;
            InternalSaveToFile();
        }

        internal void InternalSaveToFile()
        {
            if (String.IsNullOrEmpty(_fileName))
                _fileName = PromptForSaveName();
            // The file must be writable to overwrite
            if (File.Exists(_fileName))
                FileUtility.EnsureWriteable(_fileName);

            // Write/rewrite the file
            using (FileStream fileStream = new FileStream(_fileName, FileMode.Create, FileAccess.Write))
                if (((Schema.ScalarType)DataField.DataType).NativeType == typeof(byte[]))
                    using (Stream stream = DataField.Value.OpenStream())
                        StreamUtility.CopyStream(stream, fileStream);
                else
                    using (StreamWriter streamWriter = new StreamWriter(fileStream))
                        streamWriter.Write(DataField.Value.AsString);  
        }

        private string PromptForSaveName()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                PrepareFileDialog(dialog);

                dialog.RestoreDirectory = false;
                dialog.Title = Strings.Get("DBFileForm.SaveFileCaption");
                dialog.CheckPathExists = true;
                dialog.OverwritePrompt = true;

                // TODO: Call back here so that InitialDirectory can be recalled

                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                    throw new AbortException();

                // TODO: Call back here so that InitialDirectory can be stored

                return dialog.FileName;
            }
        }

        private void PrepareFileDialog(FileDialog dialog)
        {
            string extension = String.Empty;
            if ((_extensionLink.DataField != null) && _extensionLink.DataField.HasValue() && !_extensionLink.DataField.IsNil)
                extension = _extensionLink.DataField.AsString;

            if (extension != String.Empty)
            {
                dialog.DefaultExt = extension;
                dialog.AddExtension = true;

                // Prepare filter
                dialog.Filter = String.Format("{0} (*.{1})|*.{1}|{2}", Strings.Get("DBFileForm.UnknownFileDescription", extension), extension, Strings.Get("DBFileForm.AllFileFilter"));
                dialog.FilterIndex = 0;

                // Set default filename
                if ((_nameLink.DataField != null) && _nameLink.DataField.HasValue() && !_nameLink.DataField.IsNil && (_nameLink.DataField.AsString != String.Empty))
                    dialog.FileName = _nameLink.DataField.AsString + "." + extension;
            }
            else
            {
                // Prepare filter
                dialog.Filter = Strings.Get("DBFileForm.AllFileFilter");
                dialog.FilterIndex = 0;

                dialog.AddExtension = false;
            }
        }       

        /// <summary> Stops the monitoring of any files initiated by a call to Open</summary>
        public void CloseFile()
        {
            lock (_filePollerLock)
            {
                if (Visible)
                    Hide(); 
                if (!_fileProcessed)
                    ProcessFile();
            }
        }

        /// <summary> Allows the user to save a copy of the temp file, when the link to the control has been lost. </summary>	
        public void RecoverFile()
        {
            lock (_filePollerLock)
            {
                if (Visible)
                    Hide();
                if (!_fileProcessed)
                {
                    if (_saveOnExit)
                    {
                        string text = Strings.Get("DBFileForm.ClosingText");
                        string caption = Strings.Get("DBFileForm.ClosingCaption");
                        if (MessageBox.Show(text, caption, MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            string targetFileName;
                            using (SaveFileDialog dialog = new SaveFileDialog())
                            {
                                string extension = Path.GetExtension(_fileName);
                                if (!String.IsNullOrEmpty(extension))
                                {
                                    dialog.DefaultExt = extension;
                                    dialog.AddExtension = true;
                                }
                                else
                                    dialog.AddExtension = false;

                                dialog.FileName = Path.GetFileName(_fileName);

                                dialog.RestoreDirectory = false;
                                dialog.Title = Strings.Get("DBFileForm.SaveFileTitle");
                                dialog.CheckPathExists = true;
                                dialog.OverwritePrompt = true;

                                if (dialog.ShowDialog() != DialogResult.OK)
                                    throw new AbortException();

                                targetFileName = dialog.FileName;

                            }
                            File.Copy(_fileName, targetFileName, true);
                        }
                    }
                    ProcessFile();
                }
            }
        }
	}
}
