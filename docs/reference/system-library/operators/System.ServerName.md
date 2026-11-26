# System.ServerName

**Declarations**

```d4
create operator .System.ServerName() : System.String
 class "System.SystemServerNameNode" static tags { DAE.IsRemotable = "False", DAE.IsDeterministic = "False" };
```

**Remarks**

`ServerName` returns the current server name. The server name is specified upon creation. The default server name for both In-Process servers and servers running as a service is "dataphor".

**Using System.ServerName**

```d4
select TableDee add {ServerName() server };
```

```text
server   
-------- 
dataphor 
```

**See Also**

[Connecting to a Server](../../../users-guide/starting-dataphor-applications.md#connecting-to-a-dataphor-server) \| [Server Architecture](../../../users-guide/architectural-overview.md)
