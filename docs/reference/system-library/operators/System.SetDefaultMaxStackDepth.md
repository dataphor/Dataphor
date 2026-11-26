# System.SetDefaultMaxStackDepth

**Declarations**

```d4
create operator .System.SetDefaultMaxStackDepth(const ADefaultMaxStackDepth : System.Integer)
 class "System.SystemSetDefaultMaxStackDepthNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDefaultMaxStackDepth` sets the default maximum stack depth at `ADefaultMaxStackDepth` for the session for any new processes that may be started. The initial default is 32767.

**Using System.SetDefaultMaxStackDepth**

```d4
SetDefaultMaxStackDepth(40959);
```

**See Also**

[System.SetMaxStackDepth](System.SetMaxStackDepth.md) \| [DefaultMaxStackDepth](../../../users-guide/process-defaults.md#defaultmaxstackdepth) \| [Session Information](../../../users-guide/aliases.md#session-information)
