# System.VersionNumber

**Type Declaration**

```d4
create type .System.VersionNumber
{
    representation VersionNumber
    {
        Major : System.Integer,
        Minor : System.Integer,
        Revision : System.Integer,
        Build : System.Integer
    } selector  class "System.VersionNumberSelectorNode", 
    representation AsString
    {
        AsString : System.String
    } selector  class "System.VersionNumberAsStringSelectorNode"
} class "System.VersionNumberConveyor" static tags { Storage.Length = "14", DAE.AsString = "AsString" }
```

**Declarations**

```d4
create operator .System.VersionNumber(const Major : System.Integer, const Minor : System.Integer, const Revision : System.Integer, const Build : System.Integer) : System.VersionNumber
 class "System.VersionNumberSelectorNode";
create operator .System.VersionNumber(const AMajor : System.Integer) : System.VersionNumber
 class "System.VersionNumberSelectorNode";
create operator .System.VersionNumber(const AMajor : System.Integer, const AMinor : System.Integer) : System.VersionNumber
 class "System.VersionNumberSelectorNode";
create operator .System.VersionNumber(const AMajor : System.Integer, const AMinor : System.Integer, const ARevision : System.Integer) : System.VersionNumber
 class "System.VersionNumberSelectorNode";
```

**Remarks**

`VersionNumber` is the selector for the `VersionNumber` Datatype.

One demonstrated use of `VersionNumber` is in the libraries of Dataphor. The version of a library is specified using the `VersionNumber` data type. By default, the version of a library is the special value `VersionNumberUndefined`. When a library is referenced as a dependency by another library, the version number may also be specified, allowing libraries to require not only other libraries, but specific versions of those libraries. The notion of version compatibility is used to allow library references to target any granularity of the version number desired. For example, a library could reference version 2.0.\* of the System library, meaning that the library is compatible with any System library that has major version 2, and minor version 0.

**Using System.VersionNumber**

```d4
select VersionNumberUndefined();
select VersionNumber(2,0,15,9);
```

```text
*
2.3.4.5
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System.ToVersionNumber](../operators/System.ToVersionNumber.md) \| [System.Compatible](../operators/System.Compatible.md) \| [System.IsUndefined](../operators/System.IsUndefined.md) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions)
