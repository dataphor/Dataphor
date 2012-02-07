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
		public SQLCoordinate(int iD, string name) : base(iD, name) {}

		public override object ToScalar(IValueManager manager, object tempValue)
		{
			return StringToCoordinate((string)tempValue);
		}
		
		public override object FromScalar(IValueManager manager, object tempValue)
		{
			return CoordinateToString((Coordinate)tempValue);
		}

		public override SQLType GetSQLType(D4.MetaData metaData)
		{
			return new SQLStringType(25);
		}
		
		protected override string InternalNativeDomainName(D4.MetaData metaData)
		{
			return "varchar(25)";
		}

		public string CoordinateToString(Coordinate coordinate)
		{
			return String.Format("{0:F6}/{1:F6}", coordinate.Latitude, coordinate.Longitude);
		}
		
		public Coordinate StringToCoordinate(string tempValue)
		{
			int index = tempValue.IndexOf('/');
			return new Coordinate(Decimal.Parse(tempValue.Substring(0, index)), Decimal.Parse(tempValue.Substring(index + 1, tempValue.Length - (index + 1))));
		}
    }
}