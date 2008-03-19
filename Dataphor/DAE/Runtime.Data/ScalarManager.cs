/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;

#if USESCALARMANAGER
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class ScalarLink : System.Object
	{
		public ScalarLink() : base() {}

		public Scalar Scalar;		
		public ScalarLink NextLink;
	}
	
	public class ScalarLinkChain : System.Object
	{
		public ScalarLinkChain() : base() {}
		
		private ScalarLink FHead;

		public ScalarLink Add(ScalarLink ALink)
		{
			ALink.NextLink = FHead;
			FHead = ALink;
			return ALink;
		}
		
		public ScalarLink Remove()
		{
			ScalarLink LLink = FHead;
			FHead = FHead.NextLink;
			return LLink;
		}
		
		public bool IsEmpty()
		{
			return FHead == null;
		}
	}
	
	public class ScalarManager : System.Object
	{
		public ScalarManager(int AResourceManagerID) : base() 
		{
			FResourceManagerID = AResourceManagerID;
		}

		private ScalarLinkChain FAvailable = new ScalarLinkChain();
		private ScalarLinkChain FLinkBuffer = new ScalarLinkChain();		

		private int FMaxScalars = 1000;
		public int MaxScalars 
		{ 
			get { return FMaxScalars; } 
			set { FMaxScalars = value; } 
		}

		private int FScalarCount = 0;
		public int ScalarCount { get { return FScalarCount; } }

		private int FResourceManagerID;
		public int ResourceManagerID { get { return FResourceManagerID; } }
		
		private Scalar AllocateScalar()
		{
			// Ensure ScalarCount not exceeded
			if (FScalarCount >= FMaxScalars)
				throw new RuntimeException(RuntimeException.Codes.ScalarManagerOverflow);

			FScalarCount++;
			return new Scalar();
		}
		
		private Scalar GetScalar()
		{
			lock (this)
			{
				if (FAvailable.IsEmpty())
					return AllocateScalar();
				else
					return FLinkBuffer.Add(FAvailable.Remove()).Scalar;
			}
		}

		#if !NATIVEROW		
		public Scalar RequestScalar(IServerProcess AProcess, Schema.IScalarType AScalarType)
		{
			Scalar LScalar = GetScalar();
			LScalar.Open(AProcess, AScalarType);
			return LScalar;
		}
		#endif
		
		public Scalar RequestScalar(IServerProcess AProcess, Schema.IScalarType AScalarType, object AData)
		{
			Scalar LScalar = GetScalar();
			LScalar.Open(AProcess, AScalarType, AData);
			return LScalar;
		}
		
		#if NATIVEROW
		public Scalar RequestScalar(IServerProcess AProcess, Schema.IScalarType AScalarType, StreamID AStreamID)
		{
			Scalar LScalar = GetScalar();
			LScalar.Open(AProcess, AScalarType, AStreamID);
			return LScalar;
		}
		#else
		public Scalar RequestScalar(IServerProcess AProcess, Schema.IScalarType AScalarType, Stream AStream)
		{
			Scalar LScalar = GetScalar();
			LScalar.Open(AProcess, AScalarType, AStream);
			return LScalar;
		}
		#endif
		
		private ScalarLink GetScalarLink()
		{
			if (FLinkBuffer.IsEmpty())
				return new ScalarLink();
			else
				return FLinkBuffer.Remove();
		}
		
		public void ReleaseScalar(Scalar AScalar)
		{
			AScalar.Close();
			lock (this)
			{
				ScalarLink LLink = GetScalarLink();
				LLink.Scalar = AScalar;
				FAvailable.Add(LLink);
			}
		}
	}
}
#endif