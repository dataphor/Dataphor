# System.SessionID

**Declarations**

```d4
create operator .System.SessionID() : System.Integer
 class "System.SystemSessionIDNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`SessionID` returns the ID for the current session.

**Using System.SessionID**

```d4
select Sessions where ID = SessionID() over {ID, User_ID};
```

```text
ID User_ID
-- -------
1  Admin
```

Note that session information may not be exactly the same in every case.

**See Also**

Session
