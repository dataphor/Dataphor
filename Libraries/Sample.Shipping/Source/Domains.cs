/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Shipping
{
	using System;
	
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Device.SQL;
	using Alphora.Dataphor.DAE.Connection;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	
	/// <summary>
	/// SQL type : varchar(25)
	/// D4 type : Coordinate
	/// </summary>
    public class SQLCoordinate : SQLScalarType
    {
		public SQLCoordinate(int AID, string AName) : base(AID, AName) {}

		public override Scalar ToScalar(IServerProcess AProcess, object AValue)
		{
			return new Scalar(AProcess, ScalarType, StringToCoordinate((string)AValue));
		}
		
		public override object FromScalar(Scalar AValue)
		{
			return CoordinateToString((Coordinate)AValue.AsNative);
		}

		public override SQLType GetSQLType(D4.MetaData AMetaData)
		{
			return new SQLStringType(25);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData AMetaData)
		{
			return "varchar(25)";
		}

		public string CoordinateToString(Coordinate ACoordinate)
		{
			return String.Format("{0:F6}/{1:F6}", ACoordinate.Latitude, ACoordinate.Longitude);
		}
		
		public Coordinate StringToCoordinate(string AValue)
		{
			int LIndex = AValue.IndexOf('/');
			return new Coordinate(Decimal.Parse(AValue.Substring(0, LIndex)), Decimal.Parse(AValue.Substring(LIndex + 1, AValue.Length - (LIndex + 1))));
		}
    }
}