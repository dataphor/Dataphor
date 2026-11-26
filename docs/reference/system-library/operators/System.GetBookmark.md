# System.GetBookmark

**Declarations**

```d4
create operator .System.GetBookmark(const ACursor : generic cursor) : generic row
 class "System.CursorGetBookmarkNode";
```

**Remarks**

`GetBookmark` returns a row that acts as a bookmark, that will be unique to the current row. This information can be saved in a row variable and used later in `GotoBookmark`.

**Using System.GetBookmark**

```d4
begin
    var EmpCursor : cursor := cursor (Employee capabilities {Updateable});
    var TempRow : row;
    TempRow = row {"Jed" Name,45 ID};
    EmpCursor.Insert(TempRow);
end;
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.GotoBookmark](System.GotoBookmark.md) \| [System.GetKey](System.GetKey.md)
