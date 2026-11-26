# System.SetDefaultMaxCallDepth

**Declarations**

```d4
create operator .System.SetDefaultMaxCallDepth(const ADefaultMaxCallDepth : System.Integer)
 class "System.SystemSetDefaultMaxCallDepthNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDefaultMaxCallDepth` sets the default maximum call depth to `ADefaultMaxCallDepth` for the session for any new processes that may be started. The initial default is 1024.

**Using System.SetDefaultMaxCallDepth**

```d4
SetDefaultMaxCallDepth(4096);
```

**See Also**

[System.SetMaxCallDepth](System.SetMaxCallDepth.md) \| [DefaultMaxCallDepth](../../../users-guide/process-defaults.md#defaultmaxcalldepth) \| [Session Information](../../../users-guide/aliases.md#session-information)
