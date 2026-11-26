# System.DeleteUpgrade

**Declarations**

```d4
create operator .System.DeleteUpgrade(const ALibraryName : System.Name, const AVersion : System.VersionNumber)
 class "System.DeleteUpgradeNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DeleteUpgrade` deletes the specified upgrade script from the library.

**Using System.DeleteUpgrade**

NOTE: This example is only intended to demonstrate the functionality of the upgrade operators and not a logical sequence of their use. To understand the Dataphor upgrade system see Upgrades.

```d4
CreateLibrary
(
 LibraryDescriptor("Lib", VersionNumber(0, 1, 0, 5), "Temp")
);
RegisterLibrary("Lib");
create table Table in Temp { ID : Integer, Name : String, key { ID } };
select InjectUpgrade
(
 "Lib",
 "alter table Table
 {
  create column Address : Boolean { nil }
 };"
);
select UpgradeVersions("Lib");
SaveUpgrade
(
 "Lib",
 ToVersionNumber("0.1.2.*"),
 LoadUpgrade("Lib", ToVersionNumber("0.1.1.*")).Replace("Boolean", "String")
);
DeleteUpgrade("Lib", ToVersionNumber("0.1.1.*"));
select UpgradeVersions("Lib");
select LoadUpgrade("Lib", ToVersionNumber("0.1.2.*"));
UnregisterLibrary("Lib", false);
update Libraries
 where Name = "Lib"
 set { Version := ToVersionNumber("0.1.2.5") };
RegisterLibrary("Lib", false);
UpgradeLibrary("Lib");
select Table;
drop table Table;
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```d4
0.1.1.*

Version
-------
0.1.1.*

Version
-------
0.1.2.*

alter table Table
 {
  create column Address : String { nil }
 };

ID Name Address 
-- ---- -------
```

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.InjectUpgrade](System.InjectUpgrade.md) \| [System.LoadUpgrade](System.LoadUpgrade.md) \| [System.SaveUpgrade](System.SaveUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeLibrary](System.UpgradeLibrary.md) \| [System.UpgradeVersions](System.UpgradeVersions.md)
