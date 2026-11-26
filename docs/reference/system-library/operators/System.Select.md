# System.Select

**Declarations**

```d4
create operator .System.Select(const ACursor : generic cursor) : generic row
 class "System.CursorSelectNode";
```

**Remarks**

`Select` returns the current row for the specified cursor. The individual elements can be accessed by extraction. It is an error to call `Select` when the cursor is positioned on the BOF or EOF cracks.

**Using System.Select**

```d4
begin
 var OutputString : String := "From the Beginning: ";
 var EmployeeCursor : cursor(table { ID : Integer, Name : String }) := cursor(Employee);
 try
  EmployeeCursor.First();
  while EmployeeCursor.Next() do
  begin
   OutputString := OutputString + EmployeeCursor.Select().Name + " ";
  end;
  EmployeeCursor.Last();
  OutputString := OutputString + " From the End: ";
  while EmployeeCursor.Prior() do
  begin
   OutputString := OutputString + EmployeeCursor.Select().Name + " ";
  end;
  raise Error(OutputString);
 finally
  EmployeeCursor.Close();
 end;
end;
```

```text
From the Beginning: Joe John Josh Jeff Jake Jeff  From the End: Jeff Jake Jeff Josh John Joe
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [Extraction](../../../developers-guide/representing-data.md#extracting-rows-and-columns)
