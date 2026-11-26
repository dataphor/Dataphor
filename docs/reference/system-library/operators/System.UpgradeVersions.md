# System.UpgradeVersions

**Declarations**

```d4
create operator .System.UpgradeVersions(const ALibraryName : System.Name) : table { Version : System.VersionNumber }
 class "System.UpgradeVersionsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`UpgradeVersions` returns a table containing the list of version numbers available for `ALibraryName`

**Using System.UpgradeVersions**

See the [System.DeleteUpgrade](System.DeleteUpgrade.md) example.

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.DeleteUpgrade](System.DeleteUpgrade.md) \| [System.InjectUpgrade](System.InjectUpgrade.md) \| [System.LoadUpgrade](System.LoadUpgrade.md) \| [System.SaveUpgrade](System.SaveUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeLibrary](System.UpgradeLibrary.md)
