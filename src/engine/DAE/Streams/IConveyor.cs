/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

namespace Alphora.Dataphor.DAE.Streams
{
	/// <summary>
	/// The IConveyor interface provides serialization functionality for the physical representation of Dataphor values.
	/// </summary>
	public interface IConveyor
	{
		/// <summary>
		/// Indicates whether this conveyor uses the streaming read/write methods, or the byte[] read/write and GetSize methods.
		/// </summary>
		bool IsStreaming { get; }

		/// <summary>
		/// Returns the size in bytes required to store the given value.
		/// </summary>		
		int GetSize(object tempValue);

		/// <summary>
		/// Returns the physical representation of the value stored in the buffer given by buffer, beginning at the given offset.
		/// </summary>
		object Read(byte[] buffer, int offset);
		
		/// <summary>
		/// Writes the physical representation of tempValue into the buffer given by buffer beginning at the given offset.
		/// </summary>		
		void Write(object tempValue, byte[] buffer, int offset);

		/// <summary>
		/// Returns the physical representation of the value stored in the given stream.
		/// </summary>
		object Read(Stream stream);

		/// <summary>
		/// Writes the physical representation of the value given by tempValue into the given stream.
		/// </summary>
		void Write(object tempValue, Stream stream);
	}
}
