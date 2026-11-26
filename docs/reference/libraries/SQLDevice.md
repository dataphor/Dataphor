# SQLDevice

The SQL Devices allow the Dataphor Server to connect to systems which use SQL as their primary interface language. Because these systems deal with data similarly, the Dataphor Server is able to hand off significant portions of the query processing load. This is accomplished by translating the expressions given in D4 into the equivalent SQL. Most of the work of translating into SQL is done by the generic SQL device, which handles the least common denominator SQL language. We refer to this de facto standard SQL as *Common SQL*. Each specific device is then responsible only for describing how the dialect it uses differs from this standard.

In order to make use of an SQL-based device from the Dataphor Server, two main factors must be considered: connectivity, and translation. Connectivity is concerned with basic data access, while translation is concerned with converting expressions of D4 into their equivalent representations in the various dialects of SQL.

## Definition

The following information describes the definition of the SQLDevice library.

**Version**

3.0.0.0

**Requisites**

| Name       | Version |
|------------|---------|
| System     | \*      |
| Connection | \*      |

**SQLDevice Files**

| Name                                | Is Assembly |
|-------------------------------------|-------------|
| Alphora.Dataphor.DAE.Device.SQL.dll | True        |

### Registered Assemblies

The following lists the assemblies registered in the SQLDevice Library.

Alphora.Dataphor.DAE.Device.SQL

## Documentation

### Settings

For a list of additional settings available for specific SQL devices see:

- [MSSQL device settings](MSSQLDevice.md#settings)
- Oracle device settings
- DB2 device settings

The following table lists the settings available for all SQL devices, e.g. MSSQL, Oracle, etc. These settings are configured using the attributes of the class definition: The format is "AttributeName"="Value".

**Examples**

"ConnectionBufferSize"="40"

"ConnectionParameters"="ConnectionTimeout=0;DataCompression=False"

"SupportsNestedFrom"="False"

<table style="width:99%;">
<colgroup>
<col style="width: 28%" />
<col style="width: 14%" />
<col style="width: 28%" />
<col style="width: 28%" />
</colgroup>
<thead>
<tr>
<th>Attribute Name</th>
<th>Possible Values</th>
<th>Meaning</th>
<th></th>
</tr>
</thead>
<tbody>
<tr>
<td><code>ConnectionBufferSize</code></td>
<td><pre class="text"><code>&lt;integer&gt;</code></pre></td>
<td>Specifies the number of rows to fetch at a time from the target system. The default value for this attribute is 20. A value of 0 for this attribute indicates that the entire result set should be read immediately, i.e., the result set should be <em>disconnected</em>.</td>
<td></td>
</tr>
<tr>
<td><code>ConnectionClass</code></td>
<td><pre class="text"><code>&lt;class name&gt;</code></pre></td>
<td>Determines the class which will be used to implement the connectivity layer for this device. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>ConnectionParameters</code></td>
<td><pre class="bnf"><code>&lt;parameter&gt;{;&lt;parameter&gt;}
            &#10;  &lt;parameter&gt; ::=
    &lt;parameter name&gt;=&lt;parameter value&gt;</code></pre></td>
<td>Specifies additional connection parameters to be used in building the connection string. For more information on how the SQL devices build a connection string, refer to the discussion on <a href="../../developers-guide/devices.md#basic-connectivity">Basic Connectivity</a> in Part IV of the Dataphor Developer's Guide. The default value for this attribute is an empty string.</td>
<td></td>
</tr>
<tr>
<td><code>ConnectionStringBuilderClass</code></td>
<td><pre class="text"><code>&lt;class name&gt;</code></pre></td>
<td>Determines the class which will be used to construct connection strings for this device. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>DeviceForeignKeysExpression</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>Specifies an SQL statement in the dialect of the target system to be used to retrieve the set of foreign keys defined in the target database. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>DeviceIndexesExpression</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>Specifies an SQL statement in the dialect of the target system to be used to retrieve the set of indexes and index keys defined on the tables in the target database. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>DeviceTablesExpression</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>Specifies an SQL statement in the dialect of the target system to be used to retrieve the set of tables and columns defined in the target database. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>IsOrderByInContext</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the order by clause is processed as part of the query context. If this is false the order must be specified in terms of the result set columns, rather than the range variable columns within the query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>OnBrowseConnectStatement</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>A D4 expression denoting an SQL statement in the target dialect to be executed on all new browse connections.</td>
<td></td>
</tr>
<tr>
<td><code>OnExecuteConnectStatement</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>A D4 expression denoting an SQL statement in the target dialect to be executed on all new execute connections.</td>
<td></td>
</tr>
<tr>
<td><code>Schema</code></td>
<td><pre class="text"><code>&lt;string&gt;</code></pre></td>
<td>Specifies the <code>Schema</code> setting for the device. Although the way that this is used is device-specific, it is typically used to specify the current schema in systems such as Oracle and DB2, or the current owner in systems such as SQL Server and Sybase.</td>
<td></td>
</tr>
<tr>
<td><code>ShouldNormalizeWhitespace</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Determines whether or not whitespace in the SQL statements emitted by this device should be normalized (i.e. whether or not carriage-returns, line-feeds, and other special characters should become spaces). The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsAlgebraicFromClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the SQL dialect for the device supports the use of an algebraic style from clause, e.g. <code>select * from A join B on A.ID = B.ID</code> rather than <code>select * from A, B where A.ID = B.ID</code></td>
<td></td>
</tr>
<tr>
<td><code>SupportsNestedCorrelation</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of correlation between queries nested within the from clause and the outer SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsNestedFrom</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of query expressions nested within the from clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsOrderByExpressions</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of expressions in the order by clause. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsSubSelectInGroupByClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of sub-selects within the group by clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsSubSelectInHavingClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of sub-selects within the having clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsSubSelectInOrderByClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the usse of sub-selects within the order by clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsSubSelectInSelectClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of sub-selects within the select clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>SupportsSubSelectInWhereClause</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device supports the use of sub-selects within the where clause of an SQL query. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>UseParametersForCursors</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device should use parameterization for cursors used to retrieve datasets from the target system. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>UseQualifiedNames</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether or not to use the fully qualified name of an object in D4 to produce the storage name for the object. Qualifiers will be replaced with underscores in the resulting storage name. The default is false.</td>
<td></td>
</tr>
<tr>
<td><code>UseQuotedIdentifiers</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device should use quoted identifiers when producing statements in the target dialect. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>UseStatementTerminator</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device should use statement terminators when producing statements in the target dialect. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>UseTransactions</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether or not to use transactions through the CLI of the target system. The default is set appropriately by each device.</td>
<td></td>
</tr>
<tr>
<td><code>UseValuesClauseInInsert</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>True if insert statements should be constructed using a values clause, false to use a select expression. The default is set appropriately by each device.</td>
<td></td>
</tr>
</tbody>
</table>

## Scalar Types

The following sections describe the sclar types declared in the SQLDevice Library.

### SQLDevice.SQLText

**Type Declaration**

```d4
create type .SQLDevice.SQLText
{
    representation SQLText
    {
        Value : System.String
    }
};
```

**Declarations**

```d4
create operator .SQLDevice.SQLText(const Value : System.String) : SQLDevice.SQLText
 class "System.ScalarSelectorNode";
```

**Remarks**

The `SQLText` data type provides a D4 representation for CLOB (Character Large Objects) data stored in an SQL DBMS. This type will be mapped to the various CLOB data types in each SQL system (i.e. text in Microsoft SQL Server, CLOB on Oracle and DB2, etc.,.).

Unlike SQL-based DBMSs, `SQLText` does support comparison operators (`=`, `<>`, `<`, `>`, `<=`, `>=`, and `?=`). However, because the target systems do not support these comparisons, they will be implemented within the Dataphor Server and will not be translated to SQL. NOTE: Depending on where the comparison appears in a particular query, this may impact performance.

**Using SQLDevice.SQLText**

```d4
// assuming the default device is a SQL device

create table TestText
{
 ID : Integer,
 TextData : SQLText,
 key { ID }
};
insert table 
{ 
 row { 1 ID, "Some text" TextData },
 row { 2 ID, "More text" TextData }
} into TestText;
select TestText;
select TestText where TextData like "Some%";
drop table TestText;
```

```text
ID TextData
-- ---------
1  Some text
2  More text

ID TextData
-- ---------
1  Some text
```

**See Also**

[Domain, Conveyors](../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../developers-guide/d4-language-elements.md#scalar-values)

## Operators

The following sections describe the operators of the SQLDevice.

**See Also**

[System Operators](../system-library/README.md#operators) \| [Frontend Operators](Frontend.md#operators) \| [MSSQLDevice Operators](MSSQLDevice.md#operators)

### SQLDevice.AvailableReferences

**Declarations**

```d4
create operator .SQLDevice.AvailableReferences() : generic table
 class "SQLDevice.AvailableReferencesNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.AvailableReferences(const ADeviceName : System.Name) : generic table
 class "SQLDevice.AvailableReferencesNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`AvailableReferences` returns a table listing the available references (foreign keys) from the specified or default SQL device. These references would have been created on the device using something other than Dataphor. Dataphor offloads tables and views to devices but not references. Dataphor stores references in the catalog.

**Using SQLDevice.AvailableReferences**

```d4
select AvailableReferences();
```

```text
Name                  StorageName
--------------------- ---------------------
Lib.DetailTable_Table DetailTable_Table
```

**See Also**

[SQLDevice Operators](SQLDevice.md#operators)

### SQLDevice.AvailableTables

**Declarations**

```d4
create operator .SQLDevice.AvailableTables() : generic table
 class "SQLDevice.AvailableTablesNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.AvailableTables(const ADeviceName : System.Name) : generic table
 class "SQLDevice.AvailableTablesNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`AvailableTables` returns a table listing the available tables from the specified or default SQL device.

**Using SQLDevice.AvailableTables**

```text
See example SQLDevice.D4ToSQL.
```

**See Also**

[SQLDevice Operators](SQLDevice.md#operators)

### SQLDevice.D4ToSQL

**Remarks**

`D4ToSQL` translates D4 code into the SQL dialect used by the specified device. This operator returns "Query is unsupported by the device" if the query is not supported. This operator returns "Device is not an SQL device" if the specified device is not an SQL device (e.g. a memory device).

**Using SQLDevice.D4ToSQL**

```d4
// assuming Admin is current user       
CreateLibrary
 (
  LibraryDescriptor
  (
   "Lib", VersionNumberUndefined(), "Device",
   list(FileReference) {},
   list(LibraryReference)
   {
    LibraryReference("MSSQLDevice",VersionNumberUndefined())
   }
  )
 );
RegisterLibrary("Lib");
create device Device
 reconciliation { mode = { Command }, master = device }
 class "MSSQLDevice"
        attributes { "ServerName" = ".", "DatabaseName" = "Docs" };
CreateDeviceUser("System", "Device", "sa", "");
CreateDeviceUser("Admin", "Device", "sa", "");
create table Table { ID: Integer, Name : String, key{ID} };
select D4ToSQL("Device", "select Table;");
drop table Table;
select AvailableTables();
drop device Device;
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```d4
select
 "T1"."ID" as "ID", "T1"."Name" as "Name"
 from "Table" as "T1" (fastfirstrow)
order by "ID" asc;
```

```d4
Name           StorageName
-------------- ------------
Lib.Table      Table
```

**See Also**

[SQLDevice Operators](SQLDevice.md#operators) \| [D4 Comparison with SQL](../../developers-guide/d4-comparison-with-sql.md)

### SQLDevice.SQLExecute

**Declarations**

```d4
create operator .SQLDevice.SQLExecute(const AStatement : System.String)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.SQLExecute(const AStatement : System.String, const AInValues : generic row)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.SQLExecute(const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.SQLExecute(const ADeviceName : System.Name, const AStatement : System.String)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.SQLExecute(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
create operator .SQLDevice.SQLExecute(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row)
 class "SQLDevice.SQLExecuteNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`SQLExecute` provides statement-level native SQL access to the target system for a given device, also known as pass-through query execution. The operator executes an SQL statement directly against the target system. No translation will be performed, so the statement must be issued in the dialect of the target system. Note that parameterization processing is still performed based on the connectivity implementation of the device being used. If `ADeviceName` is not specified, the default device is used. In either case the device must be a SQL device. `AInValues` is optional only if `AOutValues` is not specified. `AOutValues` is optional.

Note that this operator is only capable of executing a single statement of SQL in the target dialect. If multiple statements must be executed, multiple `SQLExecute` invocations are required.

**Using SQLDevice.SQLExecute**

```d4
// assuming the default device is a T-SQL compatible device
create table Example in Temp { TSum : Integer, key {} };
begin
 var LOutRow := row { 0 Arg2 };
 SQLExecute("set @Arg2 = 2 + @Arg1", row {4 Arg1}, var LOutRow);
 insert table { row { (Arg2 from LOutRow) TSum } } into Example;
end;
select Example;
drop table Example;
```

```text
TSum
----
6
```

**See Also**

[SQLDevice Operators](SQLDevice.md#operators)

### SQLDevice.SQLQuery

**Declarations**

```d4
create operator .SQLDevice.SQLQuery(const AStatement : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AInValues : generic row) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AInValues : generic row, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row, const ATableType : System.String, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
create operator .SQLDevice.SQLQuery(const ADeviceName : System.Name, const AStatement : System.String, const AInValues : generic row, var AOutValues : generic row, const ATableType : System.String, const AKeyInfo : System.String) : generic table
 class "SQLDevice.SQLQueryNode" tags { DAE.IsRemotable = "false", DAE.IsLiteral = "false" };
```

**Remarks**

`SQLQuery` provides query-level native SQL access to the target system for a given device, also known as pass-through query evaluation. The operator evaluates an SQL select statement directly against the target system and returns the result set for the query. No translation will be performed, so the statement must be issued in the dialect of the target system. Note that parameterization processing is still performed based on the connectivity implementation of the device being used. If `ADeviceName` is not specified, the default device is used. In either case the device must be a SQL device. `AInValues` is optional only if `AOutValues` is not specified. `AOutValues` is optional.

If `ATableType` and `AKeyInfo` are not specified, the compiler will attempt to determine the type and key information of the result set from the target system. The compiler will attempt to use a prepare-style API if it is available on the target system. If this approach fails to retrieve the schema, the compiler will issue a warning and attempt to evaluate the result set at compile-time in order to determine the type of the result set. If both of these approaches fail, a compile-time error is raised and the schema definition for the result set must be provided via `ATableType` and `AKeyInfo`.

Note that `ADeviceName`, `AStatement`, `AKeyInfo`, and `ATableType` must be literals (evaluable at compile-time) in order for the compiler to determine tye type of the result set. Use dynamic D4 execution if the select statement to be evaluated must be constructed at run-time.

Note that this operator is only capable of executing a single select statement of SQL in the target dialect. If multiple select statements must be evaluated, multiple `SQLQuery` invocations are required.

> [!NOTE]
> While an order by clause is allowed to be specified in the select statement to be evaluated, the result set will always be ordered by the inferred key of the result. This is because of the way that the result set is handled by the internal cursor engine of the Dataphor Server.

**Using SQLDevice.SQLExecute**

```d4
// assuming the default device is a Microsoft SQL Server device
select SQLQuery("select * from sysobjects where name = 'sysobjects'");
```

```text
name       id xtype ...
---------- -- ----- 
sysobjects 1  S     
```

**See Also**

[SQLDevice Operators](SQLDevice.md#operators)
