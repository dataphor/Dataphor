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

namespace Alphora.Dataphor.FHIR.Core
{
	public class JSONObjectConveyor : Conveyor
	{
		public JSONObjectConveyor() : base() {}
		
		public override bool IsStreaming { get { return base.IsStreaming; } }
		
		public override object Read(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				using (var jsonReader = new JsonTextReader(reader))
				{
					return JToken.ReadFrom(jsonReader);
				}
			}
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			using (var writer = new StreamWriter(stream))
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
