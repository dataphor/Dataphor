# System.Boolean

**Type Declaration**

```d4
create type .System.Boolean
{
    representation Boolean
    {
        Value : System.Boolean
    } selector  class "System.ScalarSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.BooleanAsStringSelectorNode"
} class "System.BooleanConveyor"
    static tags { Catalog.Comment = "System Boolean", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Boolean(const Value : System.Boolean) : System.Boolean
 class "System.ScalarSelectorNode";
```

**Remarks**

This is the selector for the DataType `Boolean`

`System.Boolean` consists of the truth values true and false.

**Using System.Boolean**

```d4
create table Test in Temp
{
 TestValue : Boolean,
 key {}
};
insert table { row { 1 = 0 TestValue } } into Test;
select Test;
drop table Test;
```

```text
TestValue
---------
False
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Parser Literals](../../../developers-guide/d4-lexical-elements.md#parser-literals) \| [System.ToBoolean](../operators/System.ToBoolean.md)
