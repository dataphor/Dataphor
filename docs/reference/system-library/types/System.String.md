# System.String

**Type Declaration**

```d4
create type .System.String
{
    representation String
    {
        Value : System.String
    } selector  class "System.ScalarSelectorNode", 
    representation Unicode
    {
        Unicode : list(System.Integer)
    } selector  class "System.SystemStringUnicodeNode", 
    representation UTF8
    {
        UTF8 : list(System.Byte)
    }selector  class "System.SystemStringUTF8Node", 
    representation ASCII
    {
        ASCII : list(System.Byte)
    } selector  class "System.SystemStringASCIINode"
} class "System.StringConveyor" static tags { Catalog.Comment = "System String", Storage.Length = "20" }
```

See System.String.Unicode \| See System.String.UTF8 \| See System.String.ASCII

**Declarations**

```d4
create operator .System.String(const Value : System.String) : System.String
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `String`.

`System.String` is the set of all case-sensitive character strings. The physical representation of this type is a Unicode string with a maximum length of 2<sup>31</sup>-1. The comparison operators for this domain are defined case-sensitively.

`ASCII` the selector for the `ASCII` representation of the `String` DataType. This can be used to return a native string given a list of Bytes. It can also be used to return a list of Bytes for a given String.

`Unicode` is the selector for the `Unicode` representation of the `String` DataType. This can be used to return a native string given a list of Integers. It can also be used to return a list of Integers for a given String.

`UTF8` is the selector for the `UTF8` representation of the `String` DataType. This can be used to return a native string given a list of Bytes. It can also be used to return a list of Bytes for a given String.

**Using System.String**

```d4
select String("123abc");

create session table Example { ASCIIString : String, key{} };
begin
    var LString : String;
    LString.ASCII := list(Byte) { 65, 108, 112, 104, 111, 114, 97 };
    insert table { row { LString ASCIIString } } into Example;
end;
select Example
    add { "Fun".ASCII[0] F, "Fun".ASCII[1] u, "Fun".ASCII[2] n };
drop table Example;
```

```text
123abc

ASCIIString F  u   n
----------- -- --- ---
Alphora     70 117 110
```

**See Also**

System.String.Unicode \| System.String.UTF8 \| System.String.ASCII \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToString](../operators/System.ToString.md)
