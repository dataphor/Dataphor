# System.GroupName

**Type Declaration**

```d4
create type .System.GroupName
    like System.String
    tags { Frontend.Title = "Group Name", Frontend.Width = "30"}
    static tags { Catalog.Comment = "System String", Storage.Length = "255" }
```

**Remarks**

`GroupName` is the selector for the DataType and an operator.

`GroupName` is the set of all possible group names.

**Using System.GroupName**

```d4
select GroupName("Development");
```

```text
Development
```

**See Also**

[Security](../../../users-guide/security.md) \| System.CreateGroup \| System.DropGroup \| System.GroupExists
