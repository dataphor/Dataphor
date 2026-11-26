# System.CloseSession

**Declarations**

```d4
create operator .System.CloseSession(const ASessionID : System.Integer)
 class "System.SystemCloseSessionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`CloseSession` closes the session corresponding to the Session ID specified.

**Using System.CloseSession**

```d4
CloseSession(0);
```

**See Also**

Session
