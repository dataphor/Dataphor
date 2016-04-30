/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.FHIR.Server
{
	using DAE.Connection;
	using DAE.Device.SQL;
	using DAE.Runtime;
	using Hl7.Fhir.Serialization;
	using Newtonsoft.Json;
	using System.IO;
	using System.Text;
	using D4 = DAE.Language.D4;

	/// <summary>
	/// SQL type : varchar(25)
	/// D4 type : Coordinate
	/// </summary>
	public class SQLResource : SQLScalarType
	{
		public SQLResource(int iD, string name) : base(iD, name) { }

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return StringToResource((string)tempValue);
		}

		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return ResourceToString((Hl7.Fhir.Model.Base)tempValue);
		}

		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLTextType();
		}

		protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "text";
		}

		public string ResourceToString(Hl7.Fhir.Model.Base resource)
		{
			return FhirSerializer.SerializeToJson(resource);
		}

		public Hl7.Fhir.Model.Base StringToResource(string tempValue)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(tempValue));
			using (var reader = new StreamReader(stream))
			{
				using (var jsonReader = new JsonTextReader(reader))
				{
					return FhirParser.Parse(jsonReader);
				}
			}
		}
	}
}