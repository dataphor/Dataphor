# System.Diagnostics.GetErrorDescription

**Declarations**

```d4
create operator .System.Diagnostics.GetErrorDescription(const AValue : System.Error) : System.String
 class "System.SystemGetErrorDescriptionNode";
```

**Remarks**

`GetErrorDescription` returns a detailed description of the specified error, `AValue`, and inner errors recursively, starting at deepest error.

**Using System.Diagnostics.GetErrorDescription**

```d4
create operator Divide
(
 const ANumerator : Integer, const ADenominator : Integer
) : String
begin
 try  result := ToString(ANumerator / ADenominator);
 except
  on LError : Error do
  begin   try
    raise Error("User",500000,"This is my error",LError);
   except
    on L2Error : Error do
     result := GetErrorDescription(L2Error);
   end;
  end;
 end;
end;

select Divide(1,0);
drop operator Divide(Integer, Integer);
```

```text
Attempted to divide by zero.
Runtime error: Attempted to divide by zero.
    Exception occurred while executing the following code: (ANumerator / ADenominator)

This is my error
```

**See Also**

[System.Diagnostics.GetDetailedErrorDecsription](System.Diagnostics.GetDetailedErrorDescription.md) \| [System.Diagnostics.LogError](System.Diagnostics.LogError.md) \| [System.Diagnostics.LogMessage](System.Diagnostics.LogMessage.md) \| [System.Error](System.Error.md)
