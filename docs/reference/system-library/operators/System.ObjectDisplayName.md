# System.ObjectDisplayName

**Declarations**

```d4
create operator .System.ObjectDisplayName(const AName : System.Name) : System.String
 class "System.ObjectDisplayNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectDisplayName(const ASpecifier : System.String) : System.String
 class "System.ObjectDisplayNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectDisplayName(const AID : System.Integer) : System.String
 class "System.ObjectDisplayNameNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ObjectDisplayName` returns the terse name of an object. When the ID is specified the DisplayName is resolved from all objects. When the Name is spacified the DisplayName is resolved from the Catalog items.

**Using System.ObjectDisplayName**

```d4
select Objects where ID = ObjectID("Boolean") over { ID, Name };
select ObjectDisplayName(ObjectID("Boolean"));
select ObjectDisplayName("Boolean");
select ObjectDisplayName("Boolean");
select ObjectID("Boolean");
```

```text
ID   Name
---- --------------
1003 System.Boolean

System.Boolean
System.Boolean
System.Boolean
1003
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [Types](../../../developers-guide/d4-language-elements.md#types) \| [Catalog Elements](../../../developers-guide/d4-catalog.md) \| [System.ObjectDescription](System.ObjectDescription.md) \| [System.ObjectExists](System.ObjectExists.md) \| [System.ObjectID](System.ObjectID.md)
