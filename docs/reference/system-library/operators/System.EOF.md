# System.EOF

**Declarations**

```d4
create operator .System.EOF(const ACursor : generic cursor) : System.Boolean
 class "System.CursorEOFNode";
```

**Remarks**

Checks to see if the cursor specified is at the end of the current recordset. Returns true if the cursor is at the end, false if it is anywhere else.

**Using System.EOF**

```d4
begin
    var Output : String := "";
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    while not TempCursor.EOF() do
    begin
        Output := Output + String.Unicode({13})+ Name from TempCursor.Select();
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

[System.BOF](System.BOF.md) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors)
