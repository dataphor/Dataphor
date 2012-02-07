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
	using System.Collections.Generic;
	using Alphora.Dataphor.Windows;
	
	public class FileStreamHeader : Disposable, ISponsor
	{
		public FileStreamHeader(StreamID streamID, string fileName)
		{
			StreamID = streamID;
			FileName = fileName;
			Stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, false);
		}
		
		protected override void Dispose(bool disposing)
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

			base.Dispose(disposing);
		}
		
		// ISponsor
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			return TimeSpan.FromMinutes(5);
		}
		
		public StreamID StreamID;
		public FileStream Stream;
		public string FileName;
	}
	
	public class FileStreamHeaders : Dictionary<StreamID, FileStreamHeader>, IDisposable
	{
		public FileStreamHeaders() : base(){}
		
		public new FileStreamHeader this[StreamID streamID] 
		{ 
			get { return (FileStreamHeader)base[streamID]; } 
		}
		
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
		
		protected void Dispose(bool disposing)
		{
			foreach (KeyValuePair<StreamID, FileStreamHeader> entry in this)
				entry.Value.Dispose();
			Clear();
		}

		public void Add(FileStreamHeader stream)
		{
			Add(stream.StreamID, stream);
		}
	}
	
	public class FileStreamProvider : StreamProvider
	{
		public FileStreamProvider() : base()
		{
			_path = PathUtility.CommonAppDataPath(Schema.Object.NameFromGuid(Guid.NewGuid()));
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);
		}
		
		public FileStreamProvider(string path) : base()
		{
			_path = path;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_headers != null)
			{
				_headers.Dispose();
				_headers = null;
			}
			
			base.Dispose(disposing);
		}

		private string _path;
		public string Path { get { return _path; } }
		
		private FileStreamHeaders _headers = new FileStreamHeaders();
		
		private string GetFileNameForStreamID(StreamID streamID)
		{
			return String.Format("{0}\\{1}", _path, streamID.ToString());
		}
				
		public void Create(StreamID iD)
		{
			lock (this)
			{
				_headers.Add(new FileStreamHeader(iD, GetFileNameForStreamID(iD)));
			}
		}
		
		private FileStreamHeader GetStreamHeader(StreamID streamID)
		{
			FileStreamHeader header;
			if (!_headers.TryGetValue(streamID, out header))
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, streamID.ToString());
			return header;
		}
		
		public override Stream Open(StreamID streamID)
		{
			lock (this)
			{
				FileStreamHeader header = GetStreamHeader(streamID);
				return header.Stream;
			}
		}
		
		public override void Close(StreamID streamID)
		{
			// no cleanup to perform here
		}
		
		public override void Destroy(StreamID streamID)
		{
			lock (this)
			{
				FileStreamHeader header = GetStreamHeader(streamID);
				_headers.Remove(streamID);
				header.Dispose();
			}
		}

		public override void Reassign(StreamID oldStreamID, StreamID newStreamID)
		{
			lock (this)
			{
				FileStreamHeader oldHeader = GetStreamHeader(oldStreamID);
				_headers.Remove(oldStreamID);
				oldHeader.StreamID = newStreamID;
				_headers.Add(oldHeader);
			}
		}
	}
}