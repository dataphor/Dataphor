# System.Error

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

This operator is the selector for values of type `Error`. Errors are used in the structured exception handling constructs of the D4 language.

`ASeverity` is one of User, Application, System, or Environment. `ACode` is a six digit error code. The first three digits indicate the origin, the last three digits indicate the message code. Origin codes in the range 500000 to 500999 are reserved for `User` error messages. `AMessage` is the message to be displayed. `AInnerError` is an error that can be passed as context information when selecting an error value.

In addition to the main `Error` selector, several overloads are provided to simplify the usage of `Error` values. If `ASeverity` is unspecified, the resulting `Error` has a severity of User. If `ACode` is unspecified, the resulting `Error` has a code of 500000.

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

[System.Error Scalar Type](../types/System.Error.md) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Exception Handling](../../../developers-guide/d4-imperative-statements.md#exception-handling) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Error Message Prefix Codes](../../error-codes.md)
