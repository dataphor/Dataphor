# System.SafeDrop

**Declarations**

```d4
create operator .System.SafeDrop(const AObjectSpecifier : System.String)
begin
    if .System.ObjectExists(.AObjectSpecifier) then
        .System.Execute(.System.ScriptDrop(.AObjectSpecifier));
end;
```

**Remarks**

`SafeDrop` drops the specified `AObjectSpecifier` catalog object and all dependents, if it exists.

> [!NOTE]
> An error is not raised if the catalog object does not exist.

**Using System.SafeDrop**

```d4
create table ExampleTable
{
 ID : Integer,
 key {}
};
select CatalogObjects where Name matches "Example" over { Name };
SafeDrop("ExampleTable");
select CatalogObjects where Name matches "Example" over { Name };
```

```text
Name
--------------------
General.ExampleTable

Name
----
```

**See Also**

[Catalog Elements](../../../developers-guide/d4-catalog.md)
