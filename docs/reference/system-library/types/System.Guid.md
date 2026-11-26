# System.Guid

**Type Declaration**

```d4
create type .System.Guid
{
    representation Guid
    {
        Value : System.String
    } selector  class "System.SystemGuidSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.GuidAsStringSelectorNode"
} class "System.GuidConveyor" static tags { Catalog.Comment = "System Globally Unique Identifier", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.Guid(const Value : System.String) : System.Guid
 class "System.SystemGuidSelectorNode";
```

**Remarks**

This is an operator and the selector for the `Guid` datatype.

`System.Guid` is the set of all possible Globally Unique Identifiers (GUIDs). The physical representation for this type is a 128-bit unsigned integer. The operator `NewGuid()` generates a new globally unique identifier.

**Using System.Guid**

```d4
select Guid("12345678-1234-1234-1234-123456789012");
```

```text
12345678-1234-1234-1234-123456789012
```

**See Also**

[System.NewGuid](../operators/System.NewGuid.md) \| [System.NameFromGuid](../operators/System.NameFromGuid.md) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Type Conversion](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToGuid](../operators/System.ToGuid.md)
