# System.SetMaxCallDepth

**Declarations**

```d4
create operator .System.SetMaxCallDepth(const AMaxCallDepth : System.Integer)
 class "System.SystemSetMaxCallDepthNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetMaxCallDepth` sets the maximum call depth at `AMaxCallDepth` for the current process. The initial default is 1024.

**Using System.SetMaxCallDepth**

```d4
SetMaxCallDepth(4096);
```

**See Also**

[System.DefaultSetMaxCallDepth](System.SetDefaultMaxCallDepth.md) \| [DefaultMaxCallDepth](../../../users-guide/process-defaults.md#defaultmaxcalldepth) \| [Session Information](../../../users-guide/aliases.md#session-information)
