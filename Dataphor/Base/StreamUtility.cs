/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Xml;
using System.Text;
using System.ComponentModel;
using System.Security.Cryptography;

namespace Alphora.Dataphor
{
	/// <summary> Sundry stream methods. </summary>
	public sealed class StreamUtility
	{
		public const int CCopyBufferSize = 4096;

		/// <summary> Copies remainder of one stream to another stream. </summary>
		/// <param name="ASource"> Stream to copy from.  Copying begins at the current position. </param>
		/// <param name="ATarget">
		///		The destination stream for copying.  The copied contents are placed beginning at 
		///		the current position.
		///	</param>
		public static void CopyStream(Stream ASource, Stream ATarget)
		{
			if (ASource is MemoryStream)
			{
				if (ATarget is MemoryStream)
					CopyStream((MemoryStream)ASource, (MemoryStream)ATarget);
				else
					CopyStream((MemoryStream)ASource, ATarget);
			}
			else if (ATarget is MemoryStream)
				CopyStream(ASource, (MemoryStream)ATarget);
			else
				CopyStreamWithBufferSize(ASource, ATarget, CCopyBufferSize);
		}
		
		public static void CopyStreamWithBufferSize(Stream ASource, Stream ATarget, int ACopyBufferSize)
		{
			byte[] LBuffer = new byte[ACopyBufferSize];
			int LCount;
			while ((LCount = ASource.Read(LBuffer, 0, ACopyBufferSize)) > 0)
				ATarget.Write(LBuffer, 0, LCount);
		}
		
		/// <summary> Copies specified number of bytes from one stream to another stream. </summary>
		/// <param name="ASource"> Stream to copy from.  Copying begins at the current position. </param>
		/// <param name="ATarget"> The destination stream for copying.  The copied contents are placed beginning at the current position. </param>
		/// <param name="ACount"> The number of bytes to copy. </param>
		public static void CopyStream(Stream ASource, Stream ATarget, long ACount)
		{
			if (ASource is MemoryStream)
				CopyStream((MemoryStream)ASource, ATarget, (int)ACount);
			else if (ATarget is MemoryStream)
				CopyStream(ASource, (MemoryStream)ATarget, (int)ACount);
			else
			{
				byte[] LBuffer = new byte[CCopyBufferSize];
				while (ACount > 0)
				{
					ATarget.Write(LBuffer, 0, ASource.Read(LBuffer, 0, (int)(ACount > CCopyBufferSize ? CCopyBufferSize : ACount)));
					ACount -= CCopyBufferSize;
				}
			}
		}
		
		public static void CopyStreamWithBufferSize(Stream ASource, Stream ATarget, long ACount, int ACopyBufferSize)
		{
			byte[] LBuffer = new byte[ACopyBufferSize];
			while (ACount > 0)
			{
				ATarget.Write(LBuffer, 0, ASource.Read(LBuffer, 0, (int)(ACount > ACopyBufferSize ? ACopyBufferSize : ACount)));
				ACount -= ACopyBufferSize;
			}
		}

		/// <summary> CopyStream optimized for MemoryStream as source. </summary>
		public static void CopyStream(MemoryStream ASource, Stream ATarget)
		{
			ATarget.Write(ASource.GetBuffer(), (int)ASource.Position, (int)ASource.Length - (int)ASource.Position);
			ASource.Seek(0, SeekOrigin.End);
		}

		/// <summary> CopyStream optimized for MemoryStream as source. </summary>
		public static void CopyStream(MemoryStream ASource, Stream ATarget, int ACount)
		{
			ATarget.Write(ASource.GetBuffer(), (int)ASource.Position, ACount);
			ASource.Position += ACount;
		}

		/// <summary> CopyStream optimized for MemoryStream as target. </summary>
		public static void CopyStream(Stream ASource, MemoryStream ATarget)
		{
			if (ASource.CanSeek)
				CopyStream(ASource, ATarget, (int)ASource.Length - (int)ASource.Position);
			else
			{
				int LRequested = CCopyBufferSize;
				int LCount;
				long LOldLength = ATarget.Length;
				ATarget.SetLength(Math.Max(LOldLength, ATarget.Position + LRequested));
				while ((LCount = ASource.Read(ATarget.GetBuffer(), (int)ATarget.Position, LRequested)) > 0)
				{
					ATarget.Seek(LCount, SeekOrigin.Current);
					LRequested = CCopyBufferSize - LCount;
					if (LRequested == 0)
						LRequested = CCopyBufferSize;
					ATarget.SetLength(Math.Max(LOldLength, ATarget.Position + LRequested));
				}
				ATarget.SetLength(Math.Max(LOldLength, ATarget.Position));
			}
		}

		/// <summary> CopyStream optimized for MemoryStream as target. </summary>
		public static void CopyStream(Stream ASource, MemoryStream ATarget, int ACount)
		{
			ATarget.SetLength(Math.Max(ATarget.Length, ACount - (ATarget.Length - ATarget.Position) + ATarget.Position)); 
			ASource.Read(ATarget.GetBuffer(), (int)ATarget.Position, ACount);
			ATarget.Seek(ACount, SeekOrigin.Current);
		}

		/// <summary> CopyStream optimized for MemoryStream as source and target. </summary>
		public static void CopyStream(MemoryStream ASource, MemoryStream ATarget)
		{
			long LSourceRemainder = ASource.Length - ASource.Position;
			ATarget.SetLength(Math.Max(ATarget.Length, LSourceRemainder - (ATarget.Length - ATarget.Position) + ATarget.Position)); 
			ASource.Read(ATarget.GetBuffer(), (int)ATarget.Position, (int)LSourceRemainder);
			ATarget.Seek(LSourceRemainder, SeekOrigin.Current);
		}

		/// <summary> Returns true if the two provided streams are identical in content. </summary>
		/// <remarks> This routine uses the MD5 hash provider to compute a hash for each segment of each stream, so 
		/// technically this function does not perform an exact comparison.  There appears to be no really fast
		/// way in .NET to do an exact comparison, so the astonomically small chance of a false positive makes this
		/// routine reasonable for most purposes. </remarks>
		public static bool StreamsEqual(Stream ASourceStream, Stream ATargetStream)
		{
			if ((ASourceStream.Length - ASourceStream.Position) != (ATargetStream.Length - ATargetStream.Position))
				return false;
			byte[] LSourceBuffer = new byte[CCopyBufferSize / 2];
			byte[] LTargetBuffer = new byte[CCopyBufferSize / 2];
			MD5CryptoServiceProvider LProvider = new MD5CryptoServiceProvider();
			int LLength = Math.Min((int)(ASourceStream.Length - ASourceStream.Position), LSourceBuffer.Length);
			while (LLength > 0)
			{
				ASourceStream.Read(LSourceBuffer, 0, LLength);
				ATargetStream.Read(LTargetBuffer, 0, LLength);
				byte[] LSourceHash = LProvider.ComputeHash(LSourceBuffer);
				byte[] LTargetHash = LProvider.ComputeHash(LTargetBuffer);
				if (LSourceHash.Length != LTargetHash.Length)
					return false;
				for (int i = 0; i < LSourceHash.Length; i++)
					if (LSourceHash[i] != LTargetHash[i])
						return false;
				LLength = Math.Min((int)(ASourceStream.Length - ASourceStream.Position), LSourceBuffer.Length);
			}
			return true;
		}
		
		public static void WriteInteger(Stream AStream, int AValue)
		{
			AStream.WriteByte((byte)(AValue >> 24));
			AStream.WriteByte((byte)(AValue >> 16));
			AStream.WriteByte((byte)(AValue >> 8));
			AStream.WriteByte((byte)AValue);
		}
		
		public static int ReadInteger(Stream AStream)
		{
			return
				(AStream.ReadByte() << 24) +
				(AStream.ReadByte() << 16) +
				(AStream.ReadByte() << 8) +
				(AStream.ReadByte());
		}

		// Do not localize
		public const string CKeyAttribute = "key";
		public const string CValueAttribute = "value";

		/// <summary> Saves a dictionary containing both string key and value entries to a stream as XML. </summary>
		public static void SaveDictionary(Stream AStream, IDictionary ADictionary)
		{
			XmlTextWriter LWriter = new XmlTextWriter(AStream, Encoding.Unicode);
			LWriter.Formatting = Formatting.Indented;
			LWriter.WriteStartDocument();
			LWriter.WriteStartElement("stringdictionary");
			IDictionaryEnumerator LEntry = ADictionary.GetEnumerator();
			while (LEntry.MoveNext())
			{
				LWriter.WriteStartElement("entry");
				LWriter.WriteAttributeString(CKeyAttribute, ReflectionUtility.ValueToString(LEntry.Key, LEntry.Key.GetType()));
				LWriter.WriteAttributeString(CValueAttribute, ReflectionUtility.ValueToString(LEntry.Value, LEntry.Value.GetType()));
				LWriter.WriteEndElement();
			}
			LWriter.WriteEndElement();
			LWriter.Flush();
			// Don't close or dispose LWriter because it will close the stream
		}

		/// <summary> Loads a dictionary containing both string key and value entries from a stream containing XML. </summary>
		public static void LoadDictionary(Stream AStream, IDictionary ADictionary, Type AKeyType, Type AValueType)
		{
			XmlDocument LDocument = new XmlDocument();
			LDocument.Load(AStream);
			TypeConverter LKeyConverter = TypeDescriptor.GetConverter(AKeyType);
			TypeConverter LValueConverter = TypeDescriptor.GetConverter(AValueType);
			foreach (XmlNode LNode in LDocument.DocumentElement.ChildNodes)
			{
				if (LNode is XmlElement)
				{
					ADictionary.Add(LKeyConverter.ConvertFromString(LNode.Attributes[CKeyAttribute].Value), LValueConverter.ConvertFromString(LNode.Attributes[CValueAttribute].Value));
				}
			}
		}
	}

	/// <summary> Base class which simplifies the creation of a typical stream descendent. </summary>
	/// <remarks>
	///	The following members must be implemented in descendent classes:
	///		long Length { get; }
	///		void SetLength(long ALength);
	///		long Position { get; set; }
	///		int Read(byte[] ABuffer, int AOffset, int ACount);
	///		void Write(byte[] ABuffer, int AOffset, int ACount);
	/// </remarks>
	public abstract class StreamBase : Stream
	{
		protected StreamBase() : base(){}
		
		public override long Seek(long AOffset, SeekOrigin AOrigin)
		{
			// This could be implemented in the base as well
			switch (AOrigin)
			{
				case SeekOrigin.Begin: Position = AOffset; break; 
				case SeekOrigin.Current: Position = Position + AOffset; break; 
				case SeekOrigin.End: Position = Length - AOffset; break; 
			}
			return Position;
		}
		
		public override bool CanRead
		{
			get { return true; }
		}
		
		public override bool CanSeek
		{
			get { return true; }
		}
		
		public override bool CanWrite
		{
			get { return true; }
		}
		
		public override void Flush()
		{
			// Do nothing by default
		}
		
		public virtual void CopyFrom(Stream AStream, int ACount)
		{
			StreamUtility.CopyStream(AStream, this, ACount);
		}
		
		public virtual void CopyFrom(Stream AStream)
		{
			SetLength(0);
			StreamUtility.CopyStream(AStream, this);
		}
		
		public virtual void CopyTo(Stream AStream, int ACount)
		{
			StreamUtility.CopyStream(this, AStream, ACount);
		}
		
		public virtual void CopyTo(Stream AStream)
		{
			StreamUtility.CopyStream(this, AStream);
		}
	}
}
