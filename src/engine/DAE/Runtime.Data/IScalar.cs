/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public interface IScalar : IDataValue
	{
		new Schema.IScalarType DataType { get; }
		StreamID StreamID { get; set; }
		bool AsBoolean { get; set; }
		bool GetAsBoolean(string representationName);
		void SetAsBoolean(string representationName, bool value);
		byte AsByte { get; set; }
		byte GetAsByte(string representationName);
		void SetAsByte(string representationName, byte value);
		short AsInt16 { get; set; }
		short GetAsInt16(string representationName);
		void SetAsInt16(string representationName, short value);
		int AsInt32 { get; set; }
		int GetAsInt32(string representationName);
		void SetAsInt32(string representationName, int value);
		long AsInt64 { get; set; }
		long GetAsInt64(string representationName);
		void SetAsInt64(string representationName, long value);
		decimal AsDecimal { get; set; }
		decimal GetAsDecimal(string representationName);
		void SetAsDecimal(string representationName, decimal value);
		TimeSpan AsTimeSpan { get; set; }
		TimeSpan GetAsTimeSpan(string representationName);
		void SetAsTimeSpan(string representationName, TimeSpan value);
		DateTime AsDateTime { get; set; }
		DateTime GetAsDateTime(string representationName);
		void SetAsDateTime(string representationName, DateTime value);
		Guid AsGuid { get; set; }
		Guid GetAsGuid(string representationName);
		void SetAsGuid(string representationName, Guid value);
		string AsString { get; set; }
		string GetAsString(string representationName);
		void SetAsString(string representationName, string value);
		string AsDisplayString { get; set; }
		Exception AsException { get; set; }
		Exception GetAsException(string representationName);
		void SetAsException(string representationName, Exception value);
		byte[] AsByteArray { get; set; }
		byte[] GetAsByteArray(string representationName);
		void SetAsByteArray(string representationName, byte[] value);
		string AsBase64String { get; set; }
	}
}
