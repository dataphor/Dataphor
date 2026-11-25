/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What a text parameter carries to a SQL Server, on the server named by the environment variable DATAPHOR_TEST_MSSQL; the
	/// whole fixture is ignored where that is not set.
	/// </summary>
	/// <remarks>
	/// <para>
	/// MSSQLCommand describes its parameters to the server in two places that must agree, and the cursor location decides which
	/// one a statement goes through: PrepareParameters configures the provider's own SqlParameter objects for the client-side
	/// path, and GetParamTypeDescriptor writes a T-SQL type into the @paramdef string that sp_cursoropen is given for the
	/// server-side path. Nothing else in the suite reaches the second one - the only other live SQL Server pass is the catalog
	/// store contract, and a store only ever executes statements, which is the client-side path. The server-side path is what
	/// the catalog device's own reads take (CatalogDeviceTable.InternalOpen sets CursorLocation to Server and carries the
	/// restriction's value as an SQLStringType(200) parameter) and what every device read whose statement begins with "select"
	/// takes, but both of those need a running server with a device or a catalog on it.
	/// </para>
	/// <para>
	/// So this fixture drives both paths directly, through the connection class the device and the store share, by setting the
	/// cursor location rather than letting DetectCursorLocation choose. The statement selects the parameter straight back out,
	/// so what the assertion reads is what the server received; the dummy derived table is there because a cursor wants
	/// something to range over, and it is the shape the device's own key-check select already has
	/// (tests/goldens/devices/MSSQL/DML.Insert.sql), so it is known to be one this API opens.
	/// </para>
	/// <para>
	/// The connection is opened with exactly what the variable holds, as the store contract fixture's housekeeping statements
	/// are: nothing here goes through the store or the device, so neither one's connection-string defaults apply, and a server
	/// whose certificate this machine does not trust needs Encrypt=False said in the variable.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class MSSQLTextParameterTest
	{
		/// <summary>Names a SQL Server; nothing here needs a database of its own, so whatever database the login defaults to serves.</summary>
		private const string ServerVariableName = "DATAPHOR_TEST_MSSQL";

		private static string _serverConnectionString;

		[OneTimeSetUp]
		public void RequireServer()
		{
			_serverConnectionString = Environment.GetEnvironmentVariable(ServerVariableName);
			if (String.IsNullOrEmpty(_serverConnectionString))
				Assert.Ignore(String.Format("Set {0} to a SQL Server connection string to run the text parameter cases against SQL Server; MSSQLStoreContractTests reads the same variable and its own message says what one looks like.", ServerVariableName));
		}

		/// <summary>
		/// A text parameter reaches the server with every character it was given, down either of the two paths and for either of
		/// the two SQL types that carry text. Reproduction of device-mssql-string-parameters: a string parameter was described as
		/// varchar and an SQLText parameter as text, and SQL Server converts a single-byte value to the database's collation code
		/// page on its way in, so every character that code page had no room for arrived as a question mark - silently, with
		/// nothing to read back afterwards but the question marks. In a write that is a value corrupted on the way into a column
		/// that could have held it; in the catalog device's reads and in a device restriction it is a comparison against a value
		/// that is not the one asked for, so the row is simply not found.
		/// </summary>
		/// <param name="location">Which of the two parameter descriptions the open goes through.</param>
		/// <param name="isText">True for an SQLTextType parameter (text/ntext), false for an SQLStringType one (varchar/nvarchar).</param>
		[TestCase(SQLCursorLocation.Client, false, TestName = "AStringParameterReachesTheServerIntact_ClientSideCursor")]
		[TestCase(SQLCursorLocation.Server, false, TestName = "AStringParameterReachesTheServerIntact_ServerSideCursor")]
		[TestCase(SQLCursorLocation.Client, true, TestName = "AnSQLTextParameterReachesTheServerIntact_ClientSideCursor")]
		[TestCase(SQLCursorLocation.Server, true, TestName = "AnSQLTextParameterReachesTheServerIntact_ServerSideCursor")]
		public void ANonASCIITextParameterReachesTheServerIntact(SQLCursorLocation location, bool isText)
		{
			// Chinese, Greek and Cyrillic beside a letter a Latin-1 code page does hold: a value that comes back with only the
			// accented letter intact was converted to a code page, rather than lost for some other reason.
			const string probe = "中文 αβγ Привет é";

			using (MSSQLConnection connection = new MSSQLConnection(_serverConnectionString))
			using (SQLCommand command = connection.CreateCommand(true))
			{
				command.CursorLocation = location;
				command.CommandType = SQLCommandType.Statement;
				command.LockType = SQLLockType.ReadOnly;
				command.Statement = "select @P1 as \"V\" from (select 0 as \"dummy1\") as \"dummy1\"";
				command.Parameters.Add(new SQLParameter("P1", isText ? (SQLType)new SQLTextType() : new SQLStringType(probe.Length), probe));

				using (SQLCursor cursor = command.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted))
				{
					Assert.That(cursor.Next(), Is.True, "the cursor returned no row at all, so the statement rather than the parameter is what went wrong.");
					Assert.That(cursor[0], Is.EqualTo(probe), "the text parameter did not reach the server intact.");
				}
			}
		}

		/// <summary>
		/// A string parameter declared wider than nvarchar can be is still describable, and still carries its value. nvarchar
		/// counts characters to 4,000 where varchar counted bytes to 8,000, so a width that was a legal varchar(n) is not always a
		/// legal nvarchar(n): a D4 string column with a Storage.Length tag between those two limits used to produce a parameter
		/// descriptor of varchar(5000) and would produce nvarchar(5000), which SQL Server rejects outright. This pins the max
		/// descriptor that MSSQLCommand.NVarCharDescriptor substitutes instead, on the path where the descriptor is literal T-SQL
		/// and on the path where the provider builds it.
		/// </summary>
		[TestCase(SQLCursorLocation.Client, TestName = "AStringParameterTooWideForNVarCharStillReachesTheServer_ClientSideCursor")]
		[TestCase(SQLCursorLocation.Server, TestName = "AStringParameterTooWideForNVarCharStillReachesTheServer_ServerSideCursor")]
		public void AStringParameterTooWideForNVarCharStillReachesTheServer(SQLCursorLocation location)
		{
			// Between nvarchar's 4,000 and varchar's 8,000: the band where the old descriptor was legal and a length-for-length
			// translation would not be. The value itself is short; it is the declared width that is being tested.
			const int declaredLength = 5000;
			const string probe = "中文 αβγ Привет é";

			using (MSSQLConnection connection = new MSSQLConnection(_serverConnectionString))
			using (SQLCommand command = connection.CreateCommand(true))
			{
				command.CursorLocation = location;
				command.CommandType = SQLCommandType.Statement;
				command.LockType = SQLLockType.ReadOnly;
				command.Statement = "select @P1 as \"V\" from (select 0 as \"dummy1\") as \"dummy1\"";
				command.Parameters.Add(new SQLParameter("P1", new SQLStringType(declaredLength), probe));

				using (SQLCursor cursor = command.Open(SQLCursorType.Dynamic, SQLIsolationLevel.ReadUncommitted))
				{
					Assert.That(cursor.Next(), Is.True, "the cursor returned no row at all; a width the server will not accept is the thing to look at.");
					Assert.That(cursor[0], Is.EqualTo(probe), "the string parameter did not reach the server intact.");
				}
			}
		}
	}
}
