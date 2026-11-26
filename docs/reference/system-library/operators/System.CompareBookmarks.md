# System.CompareBookmarks

**Declarations**

```d4
create operator .System.CompareBookmarks(const ACursor : generic cursor, const ABookmark1 : generic row, const ABookmark2 : generic row) : System.Integer
 class "System.CursorCompareBookmarksNode";
```

**Remarks**

`CompareBookmarks` return values are similar to the `compare` operator. `CompareBookmarks` compares two bookmarks. The values returned are interpreted as follows: -1 if the left bookmark is less than the right bookmark based on the ordering of the cursor, 0 if the bookmarks are the same, 1 if the left bookmark occurs is greater than the right bookmark. A bookmark is returned from the `GetBookmark` operator.

**See Also**

[Cursor Capabilities, Bookmarkable](../../../developers-guide/d4-data-manipulation.md#bookmarkable) \| [Select Statement, Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [System.iCompare](System.iCompare.md) \| [System.GetBookmark](System.GetBookmark.md)
