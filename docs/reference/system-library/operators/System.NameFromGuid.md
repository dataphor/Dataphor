# System.NameFromGuid

**Declarations**

```d4
create operator .System.NameFromGuid(const AID : System.Guid) : System.Name
 class "System.SystemNameFromGuidNode";
```

**Remarks**

`NameFromGuid` takes a `Guid` value and returns a valid `Name` value. It is formed by taking the Guid, as a string, changing all the dashes to underscores, and prepending "Object\_".

**Using System.NameFromGuid**

```d4
select NameFromGuid(Guid("12345678-1234-1234-1234-123456789012"));
```

```text
Object_12345678_1234_1234_1234_123456789012
```

**See Also**

[System.Guid](../types/System.Guid.md) \| [System.ToGuid](System.ToGuid.md) \| [System.NewGuid](System.NewGuid.md) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Domains, Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Type Conversion](../../../developers-guide/d4-language-elements.md#type-conversions)
