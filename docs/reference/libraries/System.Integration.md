# System.Integration

This chapter provides reference material for working with and understanding the Dataphor System.Integration Library.

## Definition

The following information describes the definition of the System.Integration library.

**Version**

3.0.0.0

**Requisites**

| Name   | Version |
|--------|---------|
| System | \*      |

**System.Integration Files**

| Name                   | Is Assembly |
|------------------------|-------------|
| System.Integration.dll | True        |

### Registered Assemblies

The following lists the assemblies registered in the System.Integration Library.

System.Integration

## Operators

The following sections describe the operators available in the System.Integration Library.

### System.Integration.Copy

**Declarations**

```d4
create operator .System.Integration.Copy(ASourceExpression : System.String, ATargetExpression : System.String, AKeyColumnNames : System.String, AInsertsOnly : System.Boolean) : System.String
 class "System.Integration.CopyNode" static tags { DAE.IsRemotable = "False" };
create operator .System.Integration.Copy(const ASourceExpression : System.String, const ATargetExpression : System.String, const AKeyColumnNames : System.String) : System.String
begin
    .result := .System.Integration.Copy(.ASourceExpression, .ATargetExpression, .AKeyColumnNames, false);
end;
```

**Remarks**

`System.Integration.Copy` Performs a row-atomic optimistic insert or update operation from the source expression to the target expression.

The `System.Integration.Copy` operator does the following:

- Inserts into target if the specified key does not exist in the target expression.
- If a key is not specified (blank), then a key is choosen from the target expression.
- Updates the target if not AInsertOnly and the specified key does exist but the comparable columns differ.
- Does nothing if the target row exists and the comparable columns match.
- Starts a browse isolated process for the source query, and starts a process for the target and performs a transaction for each target execution.
- Returns a string with a line for every row that raises an error during the copy.
- Raises an error if a compiler error occurs (runtime errors will be logged and returned).

**Using System.Integration.Copy**

```text
See example for System.Integration.CopyTables.
```

**See Also**

[System.Integration.CopyTables](System.Integration.md#systemintegrationcopytables)[System.Integration.Insert](System.Integration.md#systemintegrationinsert)

### System.Integration.CopyTables

**Declarations**

```d4
create operator .System.Integration.CopyTables(const ASourceLibrary : System.String, const ATargetLibrary : System.String, const ATableName : System.String) : System.String
begin
    
    while (.System.TransactionCount() > 0) do
        .System.RollbackTransaction();;
    var LCurrentLibrary : System.Name := .System.LibraryName();
    .System.SetLibrary(.System.Name(.ATargetLibrary));
    try
        
        .result := '';
        var LNames : cursor(table { Name : System.Name, TargetName : System.String, KeyName : System.String, Comma : System.String }) := cursor
        ((
                (
                    (
                        (
                            .System.BaseTableVars
                                over { .Name }
                        )
                            where ((.System.Name.ReadValue(.System.LibraryName(.Name)) = .ASourceLibrary) and (.System.SubString(.System.Name.ReadValue(.Name), (.System.IndexOf(.System.Name.ReadValue(.Name), '.') + 1)) = .System.IfNil(.ATableName, .System.SubString(.System.Name.ReadValue(.Name), (.System.IndexOf(.System.Name.ReadValue(.Name), '.') + 1)))))
                    )
                        over { .Name }
                )
                    add
                    {
                        .System.SubString(.System.Name.ReadValue(.Name), (.System.IndexOf(.System.Name.ReadValue(.Name), '.') + 1)) TargetName, 
                        (
                            (
                                .System.Objects
                                    where ((.Type = 'Key') and (.Catalog_Object_ID = .ID))
                            )
                                return 1 by { .ID asc include nil }
                        )[].Name KeyName, 
                        ',' Comma
                    }
            )
            capabilities { navigable }
        );
        try
            
            var LRow : row { Name : System.Name, TargetName : System.String, KeyName : System.String, Comma : System.String };
            var LKeyColumns : System.String;
            while .System.Next(.LNames) do
                begin
                    
                    .LRow := .System.Select(.LNames);
                    .result := (((((((.result + .System.String.Unicode(list(System.Integer){ 13, 10 })) + '---- ') + .System.Name.ReadValue(.LRow.Name)) + ' -> ') + .LRow.TargetName) + ' ----') + .System.String.Unicode(list(System.Integer){ 13, 10 }));
                    try
                        
                        begin
                            .System.BeginTransaction();
                            try
                                
                                if not(.System.ObjectExists(((.ATargetLibrary + '.') + .LRow.TargetName))) then
                                    .System.Execute((((('create table ' + .LRow.TargetName) + ' from .') + .System.Name.ReadValue(.LRow.Name)) + ' where false'));
                                .System.CommitTransaction();;
                            except
                                begin
                                    .System.RollbackTransaction();
                                    raise;
                                end;
                            end;
                        end;
                        .LKeyColumns := .System.SubString(.LRow.KeyName, (.System.IndexOf(.LRow.KeyName, '{') + 1));
                        .LKeyColumns := .System.Trim(.System.SubString(.LKeyColumns, 1, (.System.IndexOf(.LKeyColumns, '}') - 1)));
                        .result := (.result + .System.Integration.Copy(.System.Name.ReadValue(.LRow.Name), .LRow.TargetName, .LKeyColumns));
                        .System.String.Unicode(list(System.Integer){ 13, 10 });;
                    except
                        on LError : System.Error do 
                            begin
                                if (.System.Length(.result) < 100000) then
                                    .result := ((.result + .System.Error.ReadMessage(.LError)) + .System.String.Unicode(list(System.Integer){ 13, 10 }));
                            end;
                    end;;
                end;;;
        finally
            .System.Close(.LNames);
        end;;
    finally
        .System.SetLibrary(.LCurrentLibrary);
    end;;
end;
create operator .System.Integration.CopyTables(const ASourceLibrary : System.String, const ATargetLibrary : System.String) : System.String
begin
    .result := .System.Integration.CopyTables(.ASourceLibrary, .ATargetLibrary, (nil as System.String));
end;
```

**Remarks**

`System.Integration.CopyTables` duplicates the tables and data from on device to another. This operator can be used to import data and its structure, for example from Oracle to MS-SQL.

`System.Integration.CopyTables` creates (if they do not exists) all tables of the `ASourceLibrary` in the `ATargetLibrary` and copies the data into them by executing `System.Integration.Copy`.

**Using System.Integration.CopyTables**

```d4
RegisterLibrary("System.Integration"); CreateLibrary (
      LibraryDescriptor ("SourceLib", VersionNumberUndefined(),
      "SourceDevice", list(FileReference) {}, list(LibraryReference) {
      LibraryReference("System",VersionNumberUndefined()) } ) );
      RegisterLibrary("SourceLib"); create device SourceDevice class
      "System.MemoryDevice"; create table SampleTable { ID: Integer, Name :
      String, key{ID} }; SampleTable := table { row { 1 ID, "Jim" Name }, row
      { 2, "Jeff" } }; select TableVars over { Name } where Name matches
      "SampleTable"; select SampleTable; CreateLibrary (LibraryDescriptor
      ("TargetLib", VersionNumberUndefined(), "TargetDevice",
      list(FileReference) {}, list(LibraryReference) {
      LibraryReference("SimpleDevice",VersionNumberUndefined()) } ) );
      RegisterLibrary("TargetLib"); create device TargetDevice reconciliation
      { mode = { Command }, master = device } class "SimpleDevice" attributes
      { "DirectoryName" = "c:\Data", "AutoSave" = "true" };
      CopyTables("SourceLib","TargetLib"); select TableVars over { Name }
      where Name matches "SampleTable"; drop table SourceLib.SampleTable;
      UnregisterLibrary("SourceLib"); DropLibrary("SourceLib"); select
      TableVars over { Name } where Name matches "SampleTable"; select
      TargetLib.SampleTable; TruncateTable("TargetDevice",
      "TargetLib.SampleTable"); drop table TargetLib.SampleTable;
      UnregisterLibrary("TargetLib");
      DropLibrary("TargetLib");
```

```text
Name --------------------- SourceLib.SampleTable ID Name
      -- ---- 1 Jim 2 Jeff Name -------------------------------
      SourceLib.SampleTable TargetLib.TargetLib.SampleTable Name
      ------------------------------- TargetLib.TargetLib.SampleTable ID Name
      -- ---- 1 Jim 2 Jeff
```

**See Also**

[System.Integration.Copy](System.Integration.md#systemintegrationcopy)

### System.Integration.Insert

**Remarks**

`System.Integration.Insert` Performs a row-atomic optimistic insert operation from the source expression to the target expression.

The `System.Integration.Insert` operator does the following:

- Inserts into target if the specified key does not exist in the target expression.
- If a key is not specified (blank), then a key is choosen from the target expression.
- Logs an error and continues if the target row exists.
- Starts a browse isolated process for the source query, and starts a process for the target and performs a transaction for each target execution.
- Returns a string with an entry for every row that raises an error during the insert.
- Raises an error if a compiler error occurs (runtime errors will be logged and returned).

**Using System.Integration.Insert**

```d4
select
    Insert
    (
        table { row { 'General' Library_Name, 'Test' Name, 'txt' Type_ID } },
        Documents
    );
```

**See Also**

[System.Integration.Copy](System.Integration.md#systemintegrationcopy)
