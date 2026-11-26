# System.Name

**Type Declaration**

```d4
create type .System.Name
{
    representation Name
    {
        Value : System.String
    } selector  class "System.SystemNameSelectorNode"
} class "System.StringConveyor" tags { Frontend.Width = "50" } static tags { Catalog.Comment = "System Catalog Name", Storage.Length = "200" }
```

**Declarations**

```d4
create operator .System.Name(const Value : System.String) : System.Name
 class "System.SystemNameSelectorNode";
```

**Remarks**

This is the selector for the DataType `Name`.

`Name` is the set of all possible names.

**Using System.Name**

```d4
select Name("User");
```

```text
User
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values)
