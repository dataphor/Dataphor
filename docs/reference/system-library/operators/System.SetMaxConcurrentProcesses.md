# System.SetMaxConcurrentProcesses

**Declarations**

```d4
create operator .System.SetMaxConcurrentProcesses(const AMaxConcurrentProcesses : System.Integer)
 class "System.SystemSetMaxConcurrentProcessesNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetMaxConcurrentProcesses` sets the maximum number of concurrently executing processes. The default is 20. Any processes over the maximum will be placed on the queue to be executed when others complete.

**Using System.SetMaxConcurrentProcesses**

```d4
SetMaxConcurrentProcesses(35);
```

**See Also**

[System.SetProcessWaitTimeout](System.SetProcessWaitTimeout.md)
