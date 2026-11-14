/*
	Alphora Dataphor
	© Copyright 2000-2018 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.Dataphoria.Web.FHIR
{
	public class DataFhirServerResult
	{
		public DataParam[] Params { get; set; }
		public object Value { get; set; }
	}
}
