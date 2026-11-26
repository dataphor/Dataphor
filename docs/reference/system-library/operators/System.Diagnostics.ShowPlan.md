# System.Diagnostics.ShowPlan

**Declarations**

```d4
create operator .System.Diagnostics.ShowPlan(const AStatement : System.String) : System.String
 class "System.SystemShowPlanNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ShowPlan` returns the execution plan of the specified statement `AStatement`.

**Using System.Diagnostics.ShowPlan**

```d4
select ShowPlan("select 'Operators'")
```

```text
2214: Alphora.Dataphor.DAE.Runtime.Instructions.FrameNode
    No device
    2213: Alphora.Dataphor.DAE.Runtime.Instructions.ValueNode
        Type: System.String
        No device
```

**See Also**

[Compiler](../../../developers-guide/dataphor-product-anatomy.md#compiler) \| [Query Processor](../../../developers-guide/dataphor-product-anatomy.md#query-processor) \| [Query Chunking](../../../developers-guide/storage-integration-architecture.md#query-chunking) \| [SQL Language](../../../developers-guide/devices.md#sql-language)
