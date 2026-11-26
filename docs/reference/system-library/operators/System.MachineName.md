# System.MachineName

**Declarations**

```d4
create operator .System.MachineName() : System.String
 class "System.SystemMachineNameNode"
    static tags
    { 
        
        DAE.IsLiteral = "False", 
        DAE.IsDeterministic = "False", 
        DAE.IsRemotable = "False"
    };
```

**Remarks**

`MachineName` returns the name of the computer on which Dataphor server is running.

**Using System.MachineName**

```d4
select MachineName();
```

```text
MyServer
```

**See Also**

[System.HostName](System.HostName.md) \| [Aliases](../../../users-guide/aliases.md)
