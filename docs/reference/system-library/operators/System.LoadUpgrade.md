# System.LoadUpgrade

**Declarations**

```d4
create operator .System.LoadUpgrade(const ALibraryName : System.Name, const AVersion : System.VersionNumber) : System.String
 class "System.LoadUpgradeNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`LoadUpgrade` returns the upgrade script, as a string. `ALibraryName` and `AVersion` identify the script to return.

**Using System.LoadUpgrade**

See the [System.DeleteUpgrade](System.DeleteUpgrade.md) example.

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.DeleteUpgrade](System.DeleteUpgrade.md) \| [System.InjectUpgrade](System.InjectUpgrade.md) \| [System.SaveUpgrade](System.SaveUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeLibrary](System.UpgradeLibrary.md) \| [System.UpgradeVersions](System.UpgradeVersions.md)
