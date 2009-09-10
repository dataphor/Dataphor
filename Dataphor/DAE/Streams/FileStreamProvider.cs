/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Runtime.Remoting.Lifetime;
	using Alphora.Dataphor.Windows;
	
	public class FileStreamHeader : Disposable, ISponsor
	{
		public FileStreamHeader(StreamID AStreamID, string AFileName)
		{
			StreamID = AStreamID;
			FileName = AFileName;
			Stream = new FileStream(AFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, false);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}
			
			if (FileName != String.Empty)
			{
				File.Delete(FileName);
				FileName = String.Empty;
			}

			base.Dispose(ADisposing);
		}
		
		// ISponsor
		TimeSpan ISponsor.Renewal(ILease ALease)
		{
			return TimeSpan.FromMinutes(5);
		}
		
		public StreamID StreamID;
		public FileStream Stream;
		public string FileName;
	}
	
	public class FileStreamHeaders : Hashtable, IDisposable
	{
		public FileStreamHeaders() : base(){}
		
		public FileStreamHeader this[StreamID AStreamID] { get { return (FileStreamHeader)base[AStreamID]; } }
		
		#if USEFINALIZER
		~FileStreamHeaders()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
		
		public void Dispose()
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}
		
		protected void Dispose(bool ADisposing)
		{
			foreach (DictionaryEntry LEntry in this)
				((FileStreamHeader)LEntry.Value).Dispose();
			Clear();
		}

		public void Add(FileStreamHeader AStream)
		{
			Add(AStream.StreamID, AStream);
		}
	}
	
	public class FileStreamProvider : StreamProvider
	{
		public FileStreamProvider() : base()
		{
			FPath = PathUtility.CommonAppDataPath(Schema.Object.NameFromGuid(Guid.NewGuid()));
			if (!Directory.Exists(FPath))
				Directory.CreateDirectory(FPath);
		}
		
		public FileStreamProvider(string APath) : base()
		{
			FPath = APath;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FHeaders != null)
			{
				FHeaders.Dispose();
				FHeaders = null;
			}
			
			base.Dispose(ADisposing);
		}

		private string FPath;
		public string Path { get { return FPath; } }
		
		private FileStreamHeaders FHeaders = new FileStreamHeaders();
		
		private string GetFileNameForStreamID(StreamID AStreamID)
		{
			return String.Format("{0}\\{1}", FPath, AStreamID.ToString());
		}
				
		public void Create(StreamID AID)
		{
			lock (this)
			{
				FHeaders.Add(new FileStreamHeader(AID, GetFileNameForStreamID(AID)));
			}
		}
		
		private FileStreamHeader GetStreamHeader(StreamID AStreamID)
		{
			FileStreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, AStreamID.ToString());
			return LHeader;
		}
		
		public override Stream Open(StreamID AStreamID)
		{
			lock (this)
			{
				FileStreamHeader LHeader = GetStreamHeader(AStreamID);
				return LHeader.Stream;
			}
		}
		
		public override void Close(StreamID AStreamID)
		{
			// no cleanup to perform here
		}
		
		public override void Destroy(StreamID AStreamID)
		{
			lock (this)
			{
				FileStreamHeader LHeader = GetStreamHeader(AStreamID);
				FHeaders.Remove(AStreamID);
				LHeader.Dispose();
			}
		}

		public override void Reassign(StreamID AOldStreamID, StreamID ANewStreamID)
		{
			lock (this)
			{
				FileStreamHeader LOldHeader = GetStreamHeader(AOldStreamID);
				FHeaders.Remove(AOldStreamID);
				LOldHeader.StreamID = ANewStreamID;
				FHeaders.Add(LOldHeader);
			}
		}
	}
}