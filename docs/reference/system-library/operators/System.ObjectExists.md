# System.ObjectExists

**Declarations**

```d4
create operator .System.ObjectExists(const ASpecifier : System.String) : System.Boolean
 class "System.ObjectExistsNode" static tags { DAE.IsRemotable = "false" };
create operator .System.ObjectExists(const AName : System.Name) : System.Boolean
 class "System.ObjectExistsNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`ObjectExists` returns `true` if the name ASpecifier exists in the Catalog items, otherwise `false` is returned.

**Using System.ObjectExists**

```d4
select Objects where ID = ObjectID("Boolean") over { ID, Name };
select ObjectExists("Boolean");
```

```text
ID   Name
---- --------------
1003 System.Boolean

True
```

**See Also**

[Types](../../../developers-guide/d4-language-elements.md#types) \| [Catalog Elements](../../../developers-guide/d4-catalog.md) \| [System.ObjectDescription](System.ObjectDescription.md) \| [System.ObjectDisplayName](System.ObjectDisplayName.md) \| [System.ObjectID](System.ObjectID.md)
