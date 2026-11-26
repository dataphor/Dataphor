# System.ObjectMetaData

**Declarations**

```d4
create operator .System.ObjectMetaData(const AID : System.Integer, const ATagName : System.String, const ADefaultValue : System.String) : System.String
 class "System.ObjectMetaDataNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectMetaData(const AName : System.Name, const ATagName : System.String, const ADefaultValue : System.String) : System.String
 class "System.ObjectMetaDataNode" static tags { DAE.IsRemotable = "False" };
create operator .System.ObjectMetaData(const ASpecifier : System.String, const ATagName : System.String, const ADefaultValue : System.String) : System.String
 class "System.ObjectMetaDataNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`ObjectMetaData` allows retrieval of metadata for a given tag name from a given object. The object can be specified by ID, Name, or as a String. If the tag does not exist for the object, `ADefaultValue` is returned.

**Using System.ObjectMetaData**

```d4
create table .Table in Temp
{
 ID : Integer,
 Name : String tags { Frontend.Width = "20" },
 key { ID }
};
select ObjectMetaData
(
 Object_ID from row from
 (
  TableVarColumns
  where TableVar_Name = "Table" and Name = "ID"
 ),
 "Frontend.Width",
 "10"
);
select ObjectMetaData
(
 Object_ID from row from
 (
  TableVarColumns
  where TableVar_Name = "Table" and Name = "Name"
 ),
 "Frontend.Width",
 "10"
);
drop table Table;
```

```text
10
20
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [MetaData](../../../developers-guide/d4-catalog.md#metadata)
