# System.ProcessID

**Declarations**

```d4
create operator .System.ProcessID() : System.Integer
 class "System.SystemProcessIDNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`ProcessID` returns the ID of the current process. This operator is non-deterministic unless called within the same process.

**Using System.ProcessID**

```d4
select Processes where ID = ProcessID();
```

```text
ID   Session_ID UseDTC 
---- ---------- ------ 
1934 1          False
```

Note: This is just an example, results will vary.

**See Also**

[Session Information](../../../users-guide/aliases.md#session-information)
