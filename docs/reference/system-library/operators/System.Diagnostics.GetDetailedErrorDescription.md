# System.Diagnostics.GetDetailedErrorDescription

**Declarations**

```d4
create operator .System.Diagnostics.GetDetailedErrorDescription(const AValue : System.Error) : System.String
 class "System.SystemGetDetailedErrorDescriptionNode";
```

**Remarks**

`GetDetailedErrorDescription` returns a detailed description of the specified error, `AValue`, and inner errors recursively, starting at the deepest error.

**Using System.Diagnostics.GetDetailedErrorDescription**

```d4
create operator Divide
(
 const ANumerator : Integer, const ADenominator : Integer
) : String
begin
 try
  result := ToString(ANumerator / ADenominator);
 except
  on LError : Error do
  begin
   try
    raise Error("User",500000,"This is my error",LError);
   except
    on L2Error : Error do
     result := GetDetailedErrorDescription(L2Error);
   end;
  end;
 end;
end;

select Divide(1,0);
drop operator Divide(Integer, Integer);
```

```text
DivideByZeroException: Attempted to divide by zero.
   at System.Decimal.Divide(Decimal d1, Decimal d2)
   at System.Decimal.op_Division(Decimal d1, Decimal d2)
   at Alphora.Dataphor.DAE.Runtime.Instructions.IntegerDivisionNode.InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\ArithmeticNodes.cs:line 946
   at Alphora.Dataphor.DAE.Runtime.Instructions.InstructionNode.InternalExecute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\InstructionNode.cs:line 207
   at Alphora.Dataphor.DAE.Runtime.Instructions.PlanNode.Execute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\PlanNode.cs:line 338
RuntimeException: Runtime error: Attempted to divide by zero.
    Exception occurred while executing the following code: (ANumerator / ADenominator)
   at Alphora.Dataphor.DAE.Runtime.Instructions.PlanNode.Execute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\PlanNode.cs:line 345
   at Alphora.Dataphor.DAE.Runtime.Instructions.TryExceptNode.InternalExecute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\ImperativeNodes.cs:line 371

DataphorException: This is my error
   at Alphora.Dataphor.DAE.Runtime.Instructions.PlanNode.Execute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\PlanNode.cs:line 358
   at Alphora.Dataphor.DAE.Runtime.Instructions.TryExceptNode.InternalExecute(ServerProcess AProcess)
     in C:\src\Alphora\Dataphor\DAE\Runtime.Instructions\ImperativeNodes.cs:line 371
```

**See Also**

[System.Diagnostics.GetErrorDecsription](System.Diagnostics.GetErrorDescription.md) \| [System.Diagnostics.LogError](System.Diagnostics.LogError.md) \| [System.Diagnostics.LogMessage](System.Diagnostics.LogMessage.md) \| [System.Error](System.Error.md)
