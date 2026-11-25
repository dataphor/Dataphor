description: When a Dataphor device talks to a database over ODBC, a money value sent as a query parameter is described to the driver with a nonsensical number of digits, so the query will fail or round wrongly.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/ODBCConnection/Source/ODBCConnection.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: ODBC is a deprioritized back end (decision D15), nothing in the repository reaches this line, and there is no way to confirm the fix without a machine with an ODBC driver and a data source - so a maintainer may reasonably leave it until someone actually uses an ODBC device.
----
## What is wrong

`ODBCCommand.PrepareParameters` in `libraries/ODBCConnection/Source/ODBCConnection.cs` describes a money parameter to the ODBC driver as a decimal with **precision 8 and scale 28** - that is, a number with 8 digits in total of which 28 are after the decimal point. No such number exists; scale can never exceed precision. Every other connection in the tree gets this right or avoids it: the Oracle connection parked under `legacy/libraries/MSOracleConnection` has the same two lines the other way round (`Precision = 28; Scale = 8`), which is what they were plainly meant to be here, and the SQL Server, PostgreSQL, OLE DB and generic ADO.NET connections use their provider's own money or currency type instead of spelling out digits.

The two values are transposed. The likely effect is that the driver rejects the parameter or silently truncates the value; which of the two depends on the driver, so it has to be seen rather than reasoned about.

This is original 3.1 code, not something the port introduced. It was never compiled before: the ODBC connection library's C# project was in Visual Studio 2003 format and built by nothing until `device-connection-cleanup` converted it, which is also when this was noticed. So the defect is as old as the file and has simply never been in a shipped build on this branch.

## Why it is not fixed yet

Swapping the two values is a one-line change, but it changes what a device sends to a database, and this repository has no way to run it: an ODBC connection needs an ODBC driver and a configured data source on the machine. Project policy is that behaviour is the specification and that a fix comes with its reproduction, and neither is available here. The reviewer of `device-connection-cleanup` therefore recorded the finding rather than changing the mapping blind.

## What would settle it

Someone with an ODBC data source (any back end - the point is the driver, not the server) runs a D4 query through an ODBC device with a money-typed parameter, sees what the driver does with precision 8 / scale 28, swaps the two values and sees the query work. That naturally belongs with whoever first gives ODBC a live test, next to the existing live SQL Server and PostgreSQL device tests; it is not worth standing up an ODBC environment for this line alone.

## Related, deliberately not folded in

`device-mssql-string-parameters` records a second mapping defect in the same method: a string parameter is sent as `OdbcType.VarChar`, which loses characters outside the server's code page. That ticket's text says to mention the ODBC case and file nothing for it, because it is a different driver and a different device. The two findings share a site and would be fixed in one visit, so whoever takes either should read both; they are listed separately because the string one is a known class across devices with its own ticket and this one is a local transposition.
