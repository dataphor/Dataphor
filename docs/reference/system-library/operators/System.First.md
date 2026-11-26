# System.First

**Declarations**

```d4
create operator .System.First(const ACursor : generic cursor)
 class "System.CursorFirstNode";
```

**Remarks**

`First` moves the specified cursor to the BOF crack just prior to the first row.

**Using System.First**

```d4
begin
    var Output : String := "";
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    while not TempCursor.EOF() do
    begin
        Output :=
   Output
   + String.Unicode({13})
   + Name from TempCursor.Select();
        TempCursor.Next();
    end;
    raise Error(Output + Unicode({13}));
end;
```

```text
Joe
John
Josh
Jeff
Jake
Jeff
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors)
