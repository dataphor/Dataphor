/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Streams;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;

namespace Alphora.Dataphor.FHIR.Core
{
	public class FHIRObjectConveyor : Conveyor
	{
		public FHIRObjectConveyor() : base() {}
		
		public override bool IsStreaming { get { return base.IsStreaming; } }
		
		public override object Read(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				using (var jsonReader = new JsonTextReader(reader))
				{
					return FhirParser.Parse(jsonReader);
				}
			}
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			using (var writer = new StreamWriter(stream))
			{
				// Might be more efficient to use SerializeResource, but as the name implies, that only works with resources...
				writer.Write(FhirSerializer.SerializeToJson((Hl7.Fhir.Model.Base)tempValue));
			}
		}
	}
}
