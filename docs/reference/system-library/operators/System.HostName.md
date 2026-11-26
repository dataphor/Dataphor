# System.HostName

**Declarations**

```d4
create operator .System.HostName() : System.String
 class "System.SystemHostNameNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRemotable = "False"
    };
```

**Remarks**

`HostName` returns the name of the computer on which the Dataphor client is running.

**Using System.HostName**

```d4
select HostName();
```

```text
MyClient
```

**See Also**

[System.MachineName](System.MachineName.md) \| [Dataphoria, Aliases](../../../users-guide/aliases.md)
