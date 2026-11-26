# System.LibrarySetting

**Declarations**

```d4
create operator .System.LibrarySetting(const ALibraryName : System.Name, const ASettingName : System.String) : System.String
begin
    .result := .System.LibrarySettings[.ALibraryName, .ASettingName by { Library_Name, Name }].Value;
end;
create operator .System.LibrarySetting(const ASettingName : System.String) : System.String
 class "System.SystemLibrarySettingNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`LibrarySetting`, when invoked with the `ALibraryName` and `ASettingName` arguments, returns the value of the `ASettingName` setting defined in the library `ALibraryName`. If the setting is not defined for the library, [`nil`](../../../developers-guide/d4-language-elements.md#nil-semantics) is returned.

`LibrarySetting`, when invoked with only the `ASettingName` argument, returns the value of the `ASettingName` setting defined in the first library in a breath-first traversal of the library dependency graph beginning with the current library. If the setting is defined in more than one library at any level of the traversal, an ambiguous setting error is thrown. If the setting is not found [`nil`](../../../developers-guide/d4-language-elements.md#nil-semantics) is returned.

**Using System.LibrarySetting**

```d4
CreateLibrary
(
 LibraryDescriptor
 ("RequiredLib",
  VersionNumberUndefined(),
  "Temp"
 )
);
CreateLibrary
(
 LibraryDescriptor
 ("ExampleLib",
  VersionNumberUndefined(),
  "Temp",
  list(FileReference) {},
  list(LibraryReference) { LibraryReference("RequiredLib",VersionNumberUndefined()) }
 )
);
select LibrarySetting("SettingA");
select LibrarySetting("ExampleLib", "SettingB");
RegisterLibrary("ExampleLib");
insert
 table
 {
  row { "RequiredLib" Library_Name, "SettingA" Name, "ValueA" Value },
  row { "ExampleLib", "SettingB", "ValueB" }
 } into LibrarySettings;
select LibrarySetting("SettingA");
select LibrarySetting("ExampleLib", "SettingB");
UnregisterLibrary("ExampleLib");
UnregisterLibrary("RequiredLib");
DropLibrary("ExampleLib");
DropLibrary("RequiredLib");
```

```text
<No Value>
<No Value>
ValueA
ValueB
```

**See Also**

[Library Settings](../../../users-guide/dataphoria-libraries.md#library-settings) \| [Libraries](../../../developers-guide/working-with-libraries.md#status)
