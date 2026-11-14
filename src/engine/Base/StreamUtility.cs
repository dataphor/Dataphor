/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Alphora.Dataphor
{
	/// <summary> Sundry stream methods. </summary>
	public sealed class StreamUtility
	{
		public const int CopyBufferSize = 4096;

		/// <summary> Copies remainder of one stream to another stream. </summary>
		/// <param name="source"> Stream to copy from.  Copying begins at the current position. </param>
		/// <param name="target">
		///		The destination stream for copying.  The copied contents are placed beginning at 
		///		the current position.
		///	</param>
		public static void CopyStream(Stream source, Stream target)
		{
			MemoryStream sourceMemoryStream = source as MemoryStream;
			MemoryStream targetMemoryStream = target as MemoryStream;

			if (sourceMemoryStream != null)
			{
				if (targetMemoryStream != null)
					CopyStream(sourceMemoryStream, targetMemoryStream);
				else
					CopyStream(sourceMemoryStream, target);
			}
			else 
				if (targetMemoryStream != null)
					CopyStream(source, targetMemoryStream);
				else
					CopyStreamWithBufferSize(source, target, CopyBufferSize);
		}
		
		public static void CopyStreamWithBufferSize(Stream source, Stream target, int copyBufferSize)
		{
			byte[] buffer = new byte[copyBufferSize];
			int count;
			while ((count = source.Read(buffer, 0, copyBufferSize)) > 0)
				target.Write(buffer, 0, count);
		}
		
		/// <summary> Copies specified number of bytes from one stream to another stream. </summary>
		/// <param name="source"> Stream to copy from.  Copying begins at the current position. </param>
		/// <param name="target"> The destination stream for copying.  The copied contents are placed beginning at the current position. </param>
		/// <param name="count"> The number of bytes to copy. </param>
		public static void CopyStream(Stream source, Stream target, long count)
		{
			if (source is MemoryStream)
				CopyStream((MemoryStream)source, target, (int)count);
			else if (target is MemoryStream)
				CopyStream(source, (MemoryStream)target, (int)count);
			else
			{
				byte[] buffer = new byte[CopyBufferSize];
				while (count > 0)
				{
					target.Write(buffer, 0, source.Read(buffer, 0, (int)(count > CopyBufferSize ? CopyBufferSize : count)));
					count -= CopyBufferSize;
				}
			}
		}
		
		public static void CopyStreamWithBufferSize(Stream source, Stream target, long count, int copyBufferSize)
		{
			byte[] buffer = new byte[copyBufferSize];
			while (count > 0)
			{
				target.Write(buffer, 0, source.Read(buffer, 0, (int)(count > copyBufferSize ? copyBufferSize : count)));
				count -= copyBufferSize;
			}
		}

		/// <summary> CopyStream optimized for MemoryStream as source. </summary>
		public static void CopyStream(MemoryStream source, Stream target)
		{
			target.Write(source.GetBuffer(), (int)source.Position, (int)source.Length - (int)source.Position);
			source.Seek(0, SeekOrigin.End);
		}

		/// <summary> CopyStream optimized for MemoryStream as source. </summary>
		public static void CopyStream(MemoryStream source, Stream target, int count)
		{
			target.Write(source.GetBuffer(), (int)source.Position, count);
			source.Position += count;
		}

		/// <summary> CopyStream optimized for MemoryStream as target. </summary>
		public static void CopyStream(Stream source, MemoryStream target)
		{
			if (source.CanSeek)
				CopyStream(source, target, (int)source.Length - (int)source.Position);
			else
			{
				int requested = CopyBufferSize;
				int count;
				long oldLength = target.Length;
				target.SetLength(Math.Max(oldLength, target.Position + requested));
				while ((count = source.Read(target.GetBuffer(), (int)target.Position, requested)) > 0)
				{
					target.Seek(count, SeekOrigin.Current);
					requested = CopyBufferSize - count;
					if (requested == 0)
						requested = CopyBufferSize;
					target.SetLength(Math.Max(oldLength, target.Position + requested));
				}
				target.SetLength(Math.Max(oldLength, target.Position));
			}
		}

		/// <summary> CopyStream optimized for MemoryStream as target. </summary>
		public static void CopyStream(Stream source, MemoryStream target, int count)
		{
			target.SetLength(Math.Max(target.Length, count - (target.Length - target.Position) + target.Position)); 
			source.Read(target.GetBuffer(), (int)target.Position, count);
			target.Seek(count, SeekOrigin.Current);
		}

		/// <summary> CopyStream optimized for MemoryStream as source and target. </summary>
		public static void CopyStream(MemoryStream source, MemoryStream target)
		{
			long sourceRemainder = source.Length - source.Position;
			target.SetLength(Math.Max(target.Length, sourceRemainder - (target.Length - target.Position) + target.Position)); 
			source.Read(target.GetBuffer(), (int)target.Position, (int)sourceRemainder);
			target.Seek(sourceRemainder, SeekOrigin.Current);
		}

		/// <summary> Returns true if the two provided streams are identical in content. </summary>
		public static bool StreamsEqual(Stream sourceStream, Stream targetStream)
		{
			if ((sourceStream.Length - sourceStream.Position) != (targetStream.Length - targetStream.Position))
				return false;
			byte[] sourceBuffer = new byte[CopyBufferSize / 2];
			byte[] targetBuffer = new byte[CopyBufferSize / 2];
			int length = Math.Min((int)(sourceStream.Length - sourceStream.Position), sourceBuffer.Length);
			while (length > 0)
			{
				sourceStream.Read(sourceBuffer, 0, length);
				targetStream.Read(targetBuffer, 0, length);
				for (int i = 0; i < length; i++)
					if (sourceBuffer[i] != targetBuffer[i])
						return false;
				length = Math.Min((int)(sourceStream.Length - sourceStream.Position), sourceBuffer.Length);
			}
			return true;
		}
		
		public static void WriteInteger(Stream stream, int value)
		{
			stream.WriteByte((byte)(value >> 24));
			stream.WriteByte((byte)(value >> 16));
			stream.WriteByte((byte)(value >> 8));
			stream.WriteByte((byte)value);
		}
		
		public static int ReadInteger(Stream stream)
		{
			return
				(stream.ReadByte() << 24) +
				(stream.ReadByte() << 16) +
				(stream.ReadByte() << 8) +
				(stream.ReadByte());
		}

		// Do not localize
		public const string KeyAttribute = "key";
		public const string ValueAttribute = "value";

		/// <summary> Saves a dictionary containing both string key and value entries to a stream as XML. </summary>
		public static void SaveDictionary(Stream stream, IDictionary dictionary)
		{
			var writer = 
				XmlWriter.Create
				(
					stream, 
					new XmlWriterSettings() 
					{ 
						Encoding = Encoding.Unicode,
						Indent = true
					}
				);
			writer.WriteStartDocument();
			writer.WriteStartElement("stringdictionary");
			IDictionaryEnumerator entry = dictionary.GetEnumerator();
			while (entry.MoveNext())
			{
				writer.WriteStartElement("entry");
				writer.WriteAttributeString(KeyAttribute, ReflectionUtility.ValueToString(entry.Key, entry.Key.GetType()));
				writer.WriteAttributeString(ValueAttribute, ReflectionUtility.ValueToString(entry.Value, entry.Value.GetType()));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.Flush();
			// Don't close or dispose LWriter because it will close the stream
		}

		/// <summary> Loads a dictionary containing both string key and value entries from a stream containing XML. </summary>
		public static void LoadDictionary(Stream stream, IDictionary dictionary, Type keyType, Type valueType)
		{
			XDocument document;
			using (var reader = new StreamReader(stream))
				document = XDocument.Load(reader);
			foreach (XElement node in document.Root.Elements())
				dictionary.Add(ReflectionUtility.StringToValue(node.Attribute(KeyAttribute).Value, keyType), ReflectionUtility.StringToValue(node.Attribute(ValueAttribute).Value, valueType));
		}
	}

	/// <summary> Base class which simplifies the creation of a typical stream descendent. </summary>
	/// <remarks>
	///	The following members must be implemented in descendent classes:
	///		long Length { get; }
	///		void SetLength(long ALength);
	///		long Position { get; set; }
	///		int Read(byte[] ABuffer, int offset, int ACount);
	///		void Write(byte[] ABuffer, int offset, int ACount);
	/// </remarks>
	public abstract class StreamBase : Stream
	{
		protected StreamBase() : base(){}
		
		public override long Seek(long offset, SeekOrigin origin)
		{
			// This could be implemented in the base as well
			switch (origin)
			{
				case SeekOrigin.Begin: Position = offset; break; 
				case SeekOrigin.Current: Position = Position + offset; break; 
				case SeekOrigin.End: Position = Length - offset; break; 
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
		
		public virtual void CopyFrom(Stream stream, int count)
		{
			StreamUtility.CopyStream(stream, this, count);
		}
		
		public virtual void CopyFrom(Stream stream)
		{
			SetLength(0);
			StreamUtility.CopyStream(stream, this);
		}
		
		public new virtual void CopyTo(Stream stream, int count)
		{
			StreamUtility.CopyStream(this, stream, count);
		}
		
		public new virtual void CopyTo(Stream stream)
		{
			StreamUtility.CopyStream(this, stream);
		}
	}
}
