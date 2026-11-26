# System.Binary

**Type Declaration**

```d4
create type Binary
{
    representation Binary { Value : String },
    representation AsDisplayString { AsDisplayString : String }
}
    static tags 
    { 
        Catalog.Comment = "System Binary", 
        DAE.AsDisplayString = "AsDisplayString" 
};
```

**Declarations**

```d4
create operator .System.Binary(const Value : System.String) : System.Binary
 class "System.SystemBinarySelectorNode";
```

**Remarks**

This is the selector for the DataType `Binary`. Binary data is simply a stream of bit data. This data type is often used to store images or other uninterpreted data streams such as files.

`System.Binary` is the set of all byte streams of data with length less than or equal to 2,147,483,647 (2<sup>31</sup>-1) bytes.

**Using System.Binary**

```d4
create table TestBinary in Temp
{
 ID : Integer,
 Image : Binary,
 key { ID }
};
// Image.Warning is a .png image file
insert table 
{ 
 row
 {
  1 ID, Image('Frontend', 'Image.Warning') Image }
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

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values)
