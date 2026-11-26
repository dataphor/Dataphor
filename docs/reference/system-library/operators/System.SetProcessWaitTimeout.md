# System.SetProcessWaitTimeout

**Declarations**

```d4
create operator .System.SetProcessWaitTimeout(const AProcessWaitTimeout : System.TimeSpan)
 class "System.SystemSetProcessWaitTimeoutNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetProcessWaitTimeout` sets the amount of time, in milliseconds, that the process will wait for a chance to execute. The default is one minute (60,000 milliseconds).

**Using System.SetProcessWaitTimeout**

```d4
SetProcessWaitTimeout(30000);
```

**See Also**

[System.SetMaxConcurrentProcesses](System.SetMaxConcurrentProcesses.md)
