# System.UserID

**Type Declaration**

```d4
create type .System.UserID like System.String
    tags { Frontend.Title = "User ID", Frontend.Width = "10" }
    static tags { Catalog.Comment = "System String", Storage.Length = "255" }
```

**Declarations**

```d4
create operator .System.UserID(const Value : System.String) : System.UserID
 class "System.ScalarSelectorNode";
create operator .System.UserID() : System.UserID
 class "System.SystemUserIDNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`System.UserID` is the selector for the `UserID` Datatype.

**Using System.UserID**

```d4
select UserID("JD4");
```

```text
JD4
```

**See Also**

[System.UserID Operator](../operators/System.CreateUser.md) \| [Security](../../../users-guide/security.md) \| [System.CreateUser](../operators/System.CreateUser.md) \| [System.CreateUserWithEncryptedPassword](../operators/System.CreateUserWithEncryptedPassword.md) \| [System.DropUser](../operators/System.DropUser.md) \| [System.SetEncryptedPassword](../operators/System.SetEncryptedPassword.md) \| [System.SetPassword](../operators/System.SetPassword.md) \| [System.UserExists](../operators/System.UserExists.md) \| [System.UserName](../operators/System.UserName.md) \| [Session, Aliases](../../../users-guide/aliases.md#session-information) \| [User Credentials](../../../users-guide/aliases.md#user-credentials) \| Session, CLI \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Presentation Layer Representations](../../../developers-guide/application-development-concepts.md#representations) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions)
