# System.Error

**Type Declaration**

```d4
create type .System.Error
{
    representation Error
    {
        Severity : System.String,
        Code : System.Integer,
        Message : System.String,
        InnerError : System.Error
    } selector  class "System.SystemErrorSelectorNode"
} class "System.ObjectConveyor" static tags { Catalog.Comment = "System Error" };
```

**Declarations**

```d4
create operator .System.Error(const Severity : System.String, const Code : System.Integer, const Message : System.String, const InnerError : System.Error) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const AMessage : System.String) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const AMessage : System.String, const AInnerError : System.Error) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const ACode : System.Integer, const AMessage : System.String) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const ACode : System.Integer, const AMessage : System.String, const AInnerError : System.Error) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const ASeverity : System.String, const AMessage : System.String) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const ASeverity : System.String, const AMessage : System.String, const AInnerError : System.Error) : System.Error
 class "System.SystemErrorSelectorNode";
create operator .System.Error(const ASeverity : System.String, const ACode : System.Integer, const AMessage : System.String) : System.Error
 class "System.SystemErrorSelectorNode";
```

**Remarks**

This is the selector for the `Error` data type.

`System.Error` is the set of all `error` values in the DAE. Values of this type are used to implement the exception-handling mechanisms of D4.

`ASeverity` is one of User, Application, System, or Environment. `ACode` is a six digit error code. The first three digit indicates the origin, the last three digits indicate the message code. Origin codes in the range 500000 to 500999 are reserved for `User` error messages. `AMessage` is the message to be displayed.

**Using System.Error**

```d4
raise Error
(
 "User",
 500000,
 "This isn't a real error, but it will generate an error anyway."
);
```

```text
Warnings
  Description
  (User:500000) This isn't a real error, but it will generate an error anyway.
```

**See Also**

[System.Error Operator](../operators/System.Error.md) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Exception Handling](../../../developers-guide/d4-imperative-statements.md#exception-handling) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors)
