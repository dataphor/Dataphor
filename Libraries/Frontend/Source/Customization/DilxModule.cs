/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Web;
using System.Xml;
using System.IO;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Server;

namespace Alphora.Dataphor.Frontend.Server.Customization
{
	public class DilxModule : IHttpModule
	{
		public const string CDilxExtension = ".dilx";	// Must be lower-case

		// IHttpModule

		public void Dispose() {}

		public void Init(HttpApplication AApplication)
		{
			AApplication.PreRequestHandlerExecute += new EventHandler(PreRequest);
		}

		private void PreRequest(object ASender, EventArgs AArgs)
		{
			HttpApplication LApplication = (HttpApplication)ASender;
			if (LApplication.Request.Url.AbsolutePath.ToLower().EndsWith(CDilxExtension))
				LApplication.Response.Filter = new DilxFilterStream(LApplication.Response.Filter, LApplication.Context);
		}
	}

	public class DilxFilterStream : Stream
	{
		public DilxFilterStream(Stream ATarget, HttpContext AContext) : base()
		{
			FTarget = ATarget;
			FContext = AContext;
			FBuffer = new MemoryStream();
		}

		private Stream FTarget;
		private HttpContext FContext;
		private MemoryStream FBuffer;

		public void Process(Stream ASource, Stream ATarget, HttpContext AContext)
		{
			// Read the document
			DilxDocument LDocument = new DilxDocument();
			LDocument.Read(ASource);

			// Process ancestors
			XmlDocument LCurrent = DilxUtility.MergeAncestors(null, LDocument.Ancestors, AContext);

			// Process content
			if (LCurrent == null)
				StreamUtility.CopyStream(LDocument.Content, ATarget);
			else
			{
				XmlDocument LMerge = new XmlDocument();
				LMerge.Load(LDocument.Content);
				LCurrent = Inheritance.Merge(LCurrent, LMerge);
				LCurrent.Save(ATarget);
			}
		}

		// Stream

		public override bool CanRead
		{
			get { return FBuffer.CanRead; }
		}
		
		public override bool CanSeek
		{
			get { return FBuffer.CanSeek; }
		}
		
		public override bool CanWrite
		{
			get { return FBuffer.CanWrite; }
		}
		
		public override void Flush()
		{
			FBuffer.Flush();
		}

		public override long Length
		{
			get { return FBuffer.Length; }
		}

		public override long Position
		{
			get { return FBuffer.Position; }
			set { FBuffer.Position = value; }
		}

		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			return FBuffer.Read(ABuffer, AOffset, ACount);
		}

		public override long Seek(long AOffset, SeekOrigin AOrigin)
		{
			return FBuffer.Seek(AOffset, AOrigin);
		}

		public override void SetLength(long AValue)
		{
			FBuffer.SetLength(AValue);
		}

		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			FBuffer.Write(ABuffer, AOffset, ACount);
		}

		public override void Close()
		{
			FBuffer.Seek(0, SeekOrigin.Begin);
			Process(FBuffer, FTarget, FContext);
			FBuffer.Close();
			FTarget.Close();
		}
	}
}
