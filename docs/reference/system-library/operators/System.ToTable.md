# System.ToTable

The `ToTable` operator is used to convert a list value to a table value.

**Declarations**

```d4
create operator .System.ToTable(const AValue : generic list) : generic table
 class "System.ListToTableNode";
create operator .System.ToTable(const AValue : generic list, const AColumnName : System.Name) : generic table
 class "System.ListToTableNode";
create operator .System.ToTable(const AValue : generic list, const AColumnName : System.Name, const ASequenceName : System.Name) : generic table
 class "System.ListToTableNode";
```

**Remarks**

Any list value can be converted to a table value using this operator. If the type of the elements in the list is a row type, the resulting table type will have the same header as the rows in the list with an additional Sequence column corresponding to the index of the entry in the list. Otherwise, the resulting table type will have a value column named `AColumnName` of the type of the elements in the list, and a sequence column corresponding to the index of the entry in the list. If `AColumnName` is not specified, the resulting column will be named *Value*. If `AColumnName` is specified, it must be literal (evaluable at compile-time) so that the type of the resulting table can be determined. ASequenceName must also be literal, and specifies the name of the sequence column. If no ASequenceColumn is given, then *Sequence* is used. If the sequence column name is not unique (because it conflicts with a row column name or with the value column name) a number starting from one is appended to the name to ensure uniqueness. If the list is of type row, the value column name is ignored. The value of the sequence column starts with zero, corresponding to the index within the list.

**Using System.ToTable**

```d4
select ToTable({1, 2, 3})
```

```text
Sequence Value 
-------- ----- 
0        1     
1        2     
2        3     
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToList](System.ToList.md)
