# System.GotoBookmark

**Declarations**

```d4
create operator .System.GotoBookmark(const ACursor : generic cursor, const ABookmark : generic row) : System.Boolean
 class "System.CursorGotoBookmarkNode";
```

**Remarks**

`GotoBookmark` takes the cursor to the specified bookmark. If the row no longer exists, then this operator returns false, and it will leave the cursor where it was before the operator call. If it could find the bookmark, then it returns true and moves the cursor to that row. This bookmark is one recieved from `Get``Bookmark`.

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.FindKey](System.FindKey.md) \| [System.GetBookmark](System.GetBookmark.md) \| [System.CompareBookmarks](System.CompareBookmarks.md)
