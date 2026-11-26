# MSSQLDevice

This device enables communication with Microsoft SQL Server, versions 7.0 and 2000. The device is also capable of communicating with the Microsoft Desktop Engine (MSDE).

## Definition

The following information describes the definition of the MSSQLDevice library.

**Version**

3.0.0.0

**Requisites**

| Name      | Version |
|-----------|---------|
| System    | \*      |
| SQLDevice | \*      |

**MSSQLDevice Files**

| Name                   | Is Assembly |
|------------------------|-------------|
| AlphoraMSSQLDevice.dll | True        |

### Registered Assemblies

The following lists the assemblies registered in the MSSQLDevice Library.

AlphoraMSSQLDevice

## Documentation

### Settings

The following table lists the settings available only for the MSSQL device. Setting available to all SQL devices are listed in the next table. These settings are configured using the attributes of the class definition.

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
<td><code>ApplicationName</code></td>
<td><pre class="text"><code>&lt;application name&gt;</code></pre></td>
<td>Indicates the name of the application that will be specified in the connection to the target system. The default value for this attribute is 'Dataphor Server'.</td>
<td></td>
</tr>
<tr>
<td><code>DatabaseName</code></td>
<td><pre class="text"><code>&lt;database name&gt;</code></pre></td>
<td>Indicates the name of the database to be used within the specified server instance. This attribute has no default value.</td>
<td></td>
</tr>
<tr>
<td><code>IsMSSQL70</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Determines whether the target system is version 7.0. If this value is false, the system is considered version 2000. Version 2000 mappings will use user-defined functions in the target system to support many of the operators in the DAE. For a version 7.0 system, these operators must be translated as expressions inline. The default value for this attribute is false.</td>
<td></td>
</tr>
<tr>
<td><code>ServerName</code></td>
<td><pre class="text"><code>&lt;server name&gt;</code></pre></td>
<td>Indicates the name of the Microsoft SQL Server instance to use. This attribute has no default value.</td>
<td></td>
</tr>
<tr>
<td><code>ShouldDetermineVersion</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether the device should auto-determine the version of the target system. The default value for this attribute is true.</td>
<td></td>
</tr>
<tr>
<td><code>ShouldEnsureDatabase</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Determines whether the database name given by the <code>DatabaseName</code> attribute should be created on the target system if it does not exist. The default value for this attribute is true.</td>
<td></td>
</tr>
<tr>
<td><code>ShoudEnsureOperators</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Determines whether the device should ensure that the user-defined functions used to support operator mapping exist in the target system. This setting has no effect if the value of the <code>IsMSSQL70</code> attribute is true. The default value for this attribute is true. All the user-defined functions created by the MSSQL device are prefixed by <code>DAE_</code>.</td>
<td></td>
</tr>
<tr>
<td><code>UseIntegratedSecurity</code></td>
<td><pre class="text"><code>true | false</code></pre></td>
<td>Indicates whether integrated security should be used to connect to the Microsoft SQL Server Instance. If the value of this attribute is true, the device will connect to the specified server instance using windows authentication. For more information on security configuration in Microsoft SQL Server, see the Microsoft SQL Server Books Online.</td>
<td></td>
</tr>
</tbody>
</table>

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

The following sections describe the scalar types of the MSSQLDevice Library.

### MSSQLDevice.MSSQLBinary

**Type Declaration**

```d4
create type .MSSQLDevice.MSSQLBinary like System.Binary
 tags { Frontend.Grid.ElementType = "TextColumn" }
 static tags
 {
  Catalog.Comment = "System Binary",
  DAE.AsDisplayString = "AsDisplayString"
 };
```

**Declarations**

```d4
create operator .MSSQLDevice.MSSQLBinary(const Value : System.String) : MSSQLDevice.MSSQLBinary
 class "System.SystemBinarySelectorNode";
```

**Remarks**

This is the selector for the DataType `MSSQLBinary`. Binary data is data stored in its raw format.

`MSSQLDevice.MSSQLBinary` is the set of all byte streams of data with length less than or equal to 8192 (2<sup>13</sup>) bytes.

**Using MSSQLBinary**

```d4
// assuming the default device is a MSSQL device

create table TestBinary
{
 ID : Integer,
 BinaryData : MSSQLBinary tags { Frontend.ElementType = "TextBox" },
 key { ID }
};
// Image.Warning is a .png image file
insert table 
{ 
 row
 {
  1 ID, Binary('0123456789ABCDEF') BinaryData }
} into TestBinary;
select TestBinary;
// The image can be viewed by selecting the table
// in Dataphoria and selecting Browse.
drop table TestBinary;
```

```text
ID Image
-- -------------
1  <binary data>
```

**See Also**

[Domain, Conveyors](../../developers-guide/d4-language-elements.md#type-conversions) \| [System Data Types](../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../developers-guide/d4-language-elements.md#scalar-values)

## Operators

The following sections describe the operators in the MSSQLDevice Library.

**See Also**

[SQLDevice Operators](SQLDevice.md#operators) \| [System Operators](../system-library/README.md#operators) \| [Frontend Operators](Frontend.md#operators)

### MSSQLDevice.iCompare

**Declarations**

```d4
create operator .MSSQLDevice.iCompare(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Integer
 class "MSSQLDevice.MSSQLMSSQLBinaryCompareNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iCompare` does a byte by byte comparison of two `MSSQLBinary` type values and returns an integer value. If both values are equal 0 is returned. If `ALeftValue` is greater than `ARightValue` 1 is returned. If `ALeftValue` is less than `ARightValue` -1 is returned.

**Using MSSQLDevice.iCompare**

```d4
select MSSQLBinary("07a4").iCompare(MSSQLBinary("07a425e3"));
```

```text
-1
```

**See Also**

[MSSQLDevice.iEqual](MSSQLDevice.md#mssqldeviceiequal) \| [MSSQLDevice.iGreater](MSSQLDevice.md#mssqldeviceigreater) \| [MSSQLDevice.iInclusiveGreater](MSSQLDevice.md#mssqldeviceiinclusivegreater) \| [MSSQLDevice.iInclusiveLess](MSSQLDevice.md#mssqldeviceiinclusiveless) \| [MSSQLDevice.iLess](MSSQLDevice.md#mssqldeviceiless) \| [MSSQLDevice.iNotEqual](MSSQLDevice.md#mssqldeviceinotequal)

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iEqual

**Declarations**

```d4
create operator .MSSQLDevice.iEqual(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "System.BinaryEqualNode" tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`iEqual` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is equal to `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iEqual**

```d4
select MSSQLBinary("07a4").iEqual(MSSQLBinary("07a4"));
```

```text
True
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iGreater

**Declarations**

```d4
create operator .MSSQLDevice.iGreater(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "MSSQLDevice.MSSQLMSSQLBinaryGreaterNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iGreater` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is greater than `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iGreater**

```d4
select MSSQLBinary("07a4").iGreater(MSSQLBinary("07a4ea34"));
```

```text
False
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iInclusiveGreater

**Declarations**

```d4
create operator .MSSQLDevice.iInclusiveGreater(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "MSSQLDevice.MSSQLMSSQLBinaryInclusiveGreaterNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iInclusiveGreater` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is greater than or equal to `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iInclusiveGreater**

```d4
select MSSQLBinary("07a4").iInclusiveGreater(MSSQLBinary("07a4"));
select MSSQLBinary("07a4cf31").iInclusiveGreater(MSSQLBinary("07a4"));
```

```text
True
True
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iInclusiveLess

**Declarations**

```d4
create operator .MSSQLDevice.iInclusiveLess(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "MSSQLDevice.MSSQLMSSQLBinaryInclusiveLessNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iInclusiveLess` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is less than or equal to `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iInclusiveLess**

```d4
select MSSQLBinary("07a4").iInclusiveLess(MSSQLBinary("07a4"));
select MSSQLBinary("07a4cf31").iInclusiveLess(MSSQLBinary("07a4"));
```

```text
True
False
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iLess

**Declarations**

```d4
create operator .MSSQLDevice.iLess(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "MSSQLDevice.MSSQLMSSQLBinaryLessNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iLess` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is less than `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iLess**

```d4
select MSSQLBinary("07a3").iLess(MSSQLBinary("07a4"));
```

```text
True
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)

### MSSQLDevice.iNotEqual

**Declarations**

```d4
create operator .MSSQLDevice.iNotEqual(const ALeftValue : MSSQLDevice.MSSQLBinary, const ARightValue : MSSQLDevice.MSSQLBinary) : System.Boolean
 class "MSSQLDevice.MSSQLMSSQLBinaryNotEqualNode" static tags { DAE.IsBuiltin = "true" };
```

**Remarks**

`iNotEqual` does a byte by byte comparison of two `MSSQLBinary` type values and returns a boolean value. If `ALeftValue` is less than `ARightValue` `true` is returned, otherwise `false` is returned.

**Using MSSQLDevice.iNotEqual**

```d4
select MSSQLBinary("07a4ec12").iNotEqual(MSSQLBinary("07a4"));
```

```text
True
```

**See Also**

[MSSQLDevice Operators](MSSQLDevice.md#operators)
