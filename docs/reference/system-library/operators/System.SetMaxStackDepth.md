# System.SetMaxStackDepth

**Declarations**

```d4
create operator .System.SetMaxStackDepth(const AMaxStackDepth : System.Integer)
 class "System.SystemSetMaxStackDepthNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetMaxStackDepth` sets the maximum stack depth at `AMaxStackDepth` for the current process. The initial default is 32767.

**Using System.SetMaxStackDepth**

```d4
SetMaxStackDepth(40959);
```

**See Also**

[System.DefaultSetMaxStackDepth](System.SetDefaultMaxStackDepth.md) \| [DefaultMaxStackDepth](../../../users-guide/process-defaults.md#defaultmaxstackdepth) \| [Session Information](../../../users-guide/aliases.md#session-information)
