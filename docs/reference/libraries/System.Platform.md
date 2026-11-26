# System.Platform

The file system and process library. D4 itself has no way to touch the machine the Dataphor Server runs on; this library adds it, as a scalar type for a path and a set of operators over files, directories, drives and child processes. Everything it defines runs **on the server**, against the server's file system and with the server process's rights, so an operator that writes a file writes it where the server is, not where the client is.

This library has no chapter in the 2007 documentation, so this page records only what the descriptor, the registered classes and the library's own D4 register script state.

## Definition

**Version**

3.0.0.0

**Requisites**

| Name | Version |
|---|---|
| System | \* |

**System.Platform Files**

| Name | Is Assembly |
|---|---|
| System.Platform.dll | True |

### Registered Assemblies

System.Platform

### Registered Classes

The library registers its classes in the `System.Platform` namespace, built by reflecting over the types in its assembly, so the full list is not reproducible from the source tree. Each one implements an operator below and is named in the `class "System.Platform.…Node"` clause of that operator's declaration; the few operators written in D4 rather than C# — `ReplaceFile` is one — name no class.

## What it defines

`libraries/System.Platform/Documents/Register.d4` is the authority on this: 45 declarations of 38 operators, plus one type and one role. The following summarises it; usage of every one of them is granted to `PlatformUser` and to nothing else.

- **The role `PlatformUser`.** Access to the server's file system is a right that is granted rather than one every user has.
- **The scalar type `FileName`**, a `String` constrained to 260 characters — the Windows maximum path length of the era — with the storage length and form presentation tags to match.
- **File operators**: `FileExists`, `CreateFile`, `CreateTextFile`, `CreateBinaryFile`, `DeleteFile`, `CopyFile` (with and without an overwrite flag), `MoveFile`, `ReplaceFile`, `LoadTextFile`, `LoadBinaryFile`, `SaveTextFile`, `SaveBinaryFile`, `AppendTextFile`, `AppendBinaryFile`, `ListFiles`.
- **Directory and drive operators**: `DirectoryExists`, `CreateDirectory`, `DeleteDirectory`, `MoveDirectory`, `ListDirectories`, `ListDrives`, `GetCurrentDirectory`, `SetCurrentDirectory`.
- **Path operators**: `CombinePath`, `GetFileName`, `GetDirectoryName`, `GetFileExtension`, `ChangeFileExtension`, `FileNameHasExtension`.
- **Attribute and timestamp operators**: `GetAttributes`, `SetAttributes`, `GetCreationTime`, `SetCreationTime`, `GetLastAccessTime`, `SetLastAccessTime`, `GetLastWriteTime`, `SetLastWriteTime`.
- **`PlatformExecute`**, which runs a program on the server.

**See Also**

[Libraries](README.md) | [Working With Libraries](../../developers-guide/working-with-libraries.md)
