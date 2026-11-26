# System.ToList

The `ToList` operator is used to convert a table value to a list of rows.

**Declarations**

```d4
create operator .System.ToList(const AValue : generic cursor) : generic list
 class "System.TableToListNode";
```

**Remarks**

The `ToList` operator results in a list value containing the rows of `AValue`, in the order specified by the cursor definition.

**Using System.ToList**

```d4
select ToTable(ToList(cursor(table { row { 1 Value }, row { 2 }, row { 3 } })))
```

```text
Value 
----- 
1     
2     
3     
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToTable](System.ToTable.md)
