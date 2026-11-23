/*
	Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alphora.Dataphor.Libraries.System.Internet
{
	public class JSONObjectConveyor : Conveyor
	{
		public JSONObjectConveyor() : base() {}
		
		public override bool IsStreaming { get { return true; } }
		
		// UTF-8 without a byte-order mark: what StreamReader and StreamWriter default to, named here because the
		// leaveOpen overloads below have no default encoding. Encoding.UTF8 would prepend a three-byte preamble
		// to every value written.
		private static readonly Encoding TextEncoding = new UTF8Encoding(false, true);
		
		public override object Read(Stream stream)
		{
			// leaveOpen: the engine owns the stream and closes it itself.
			using (var reader = new StreamReader(stream, TextEncoding, true, -1, true))
			{
				using (var jsonReader = new JsonTextReader(reader))
				{
					return JToken.ReadFrom(jsonReader);
				}
			}
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			// leaveOpen: the engine owns the stream and reads its length after this returns to size the row buffer.
			using (var writer = new StreamWriter(stream, TextEncoding, -1, true))
			{
				using (var jsonWriter = new JsonTextWriter(writer))
				{
					JToken instance = (JToken)tempValue;
					instance.WriteTo(jsonWriter);
				}
			}
		}
	}
}
