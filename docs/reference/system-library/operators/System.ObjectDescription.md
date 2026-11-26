# System.ObjectDescription

**Declarations**

```d4
create operator .System.ObjectDescription(const AName : System.Name) : System.String
 class "System.ObjectDescriptionNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectDescription(const ASpecifier : System.String) : System.String
 class "System.ObjectDescriptionNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectDescription(const AID : System.Integer) : System.String
 class "System.ObjectDescriptionNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ObjectDescription` returns the verbose description of an object. When the ID is specified the description is resolved from all objects. When the Name is specified the description is resolved from the Catalog items.

**Using System.ObjectDescription**

```d4
select Objects where ID = ObjectID("Boolean") over { ID, Name };
select ObjectDescription(ObjectID("Boolean"));
select ObjectDescription("Boolean");
select ObjectDisplayName("Boolean");
select ObjectID("Boolean");
```

```text
ID   Name
---- --------------
1003 System.Boolean

Type "System.Boolean"
Type "System.Boolean"
System.Boolean
1003
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [Types](../../../developers-guide/d4-language-elements.md#types) \| [Catalog Elements](../../../developers-guide/d4-catalog.md) \| [System.ObjectDisplayName](System.ObjectDisplayName.md) \| [System.ObjectExists](System.ObjectExists.md) \| [System.ObjectID](System.ObjectID.md)
