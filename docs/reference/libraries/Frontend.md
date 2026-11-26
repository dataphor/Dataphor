# Frontend

This chapter provides reference material for working with and understanding the Frontend Library.

## Definition

The following information describes the definition of the Frontend library.

**Version**

3.0.0.0

**Requisites**

| Name         | Version |
|--------------|---------|
| SimpleDevice | \*      |

**Frontend Files**

| Name                                 | Is Assembly |
|--------------------------------------|-------------|
| Alphora.Dataphor.Frontend.Server.dll | True        |

### Registered Assemblies

The following lists the assemblies registered in the Frontend Library.

Alphora.Dataphor.Frontend.Server

## Operators

The following sections describe the operators in the Frontend Library.

**See Also**

[System Operators](../system-library/README.md#operators)

### Application Operators

**See Also**

[Frontend Operators](Frontend.md#operators)

#### Frontend.AddApplication

**Declarations**

```d4
create operator .Frontend.AddApplication(const AID : System.Name, const ADescription : System.String, const AStartDocument : System.String, const ALibraryName : System.Name)
begin
    
    insert row { .AID ID, .ADescription Description, .AStartDocument StartDocument } into .Frontend.Applications;
    insert row { .AID Application_ID, .ALibraryName Library_Name } into .Frontend.ApplicationLibraries;;
end;
```

**Remarks**

`AddApplication` creates an application in the Dataphor Server with the given ID, name, starting document, and requisite library.

**Using Frontend.AddApplication**

```d4
AddApplication("Example", "Application", "Form", "Lib");
```

**See Also**

[Application Operators](Frontend.md#application-operators)

#### Frontend.PrepareApplication

**Declarations**

```d4
create operator .Frontend.PrepareApplication(const AApplicationID : System.Name) : System.String
begin
    
    .result := .Frontend.Applications[.AApplicationID by { ID }].StartDocument;
    var LCursor : cursor(table { Library_Name : System.Name }) := cursor
    ((
            (
                .Frontend.ApplicationLibraries
                    where (.Application_ID = .AApplicationID)
            )
                over { .Library_Name }
        )
        capabilities { navigable }
    );
    try
        while .System.Next(.LCursor) do
            .System.EnsureLibraryRegistered(.System.Select(.LCursor).Library_Name);;
    finally
        .System.Close(.LCursor);
    end;;
end;
```

**Remarks**

`PrepareApplication` returns the starting document expression for the application. It also ensures that all required libraries are registered and registers those that are not.

**Using Frontend.PrepareApplication**

```d4
PrepareApplication("Example");
```

**See Also**

[Application Operators](Frontend.md#application-operators)

### CRC Operators

**See Also**

[Document Operators](Frontend.md#document-operators) \| [Frontend Operators](Frontend.md#operators)

#### Frontend.GetCRC32

**Declarations**

```d4
create operator .Frontend.GetCRC32(AValue : generic) : System.Integer
 class "Frontend.GetCRC32Node" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`GetCRC32` returns the 32 bit Cyclic Redundancy Check (CRC) for any value specified in `AValue`. `AValue` can be any data type.

**Using Frontend.GetCRC32**

```d4
select GetCRC32("This is a string");
```

```text
-2078621776
```

**See Also**

[Frontend.LoadIfNecessary](Frontend.md#frontendloadifnecessary) \| [CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.LoadIfNecessary

**Declarations**

```d4
create operator .Frontend.LoadIfNecessary(const ADocument : System.String, const ACRC32 : System.Integer) : row { CRCMatches : System.Boolean, Value : System.Scalar, ActualCRC32 : System.Integer }
begin
    
    var LData : System.Scalar := ((.System.Evaluate(.ADocument) with { IsFunctional = "false", IsDeterministic = "false", IsRepeatable = "false" }) as System.Scalar);
    if not(.System.IsNil(.LData)) then
        begin
            
            var LCRC32 : System.Integer := .Frontend.GetCRC32(.LData);
            if (.LCRC32 = .ACRC32) then
                .result := row { true CRCMatches, nil Value, .LCRC32 ActualCRC32 }
            else
                .result := row { false CRCMatches, .LData Value, .LCRC32 ActualCRC32 };;
        end
    else
        .result := row { false CRCMatches, .LData Value, 0 ActualCRC32 };;
end;
```

**Remarks**

`LoadIfNecessary` evaluates an expression and creates a 32 bit CRC. The CRC is compared to the 32 bit CRC that is passed.

If both CRCs match it returns `true` in `CRCMatches`, `nil` in `Value,` and the same CRC in `ActualCRC32`.

If the CRCs don't match it returns `false` in `CRCMatches`, the value from the expression in `Value`, and the new CRC in `ActualCRC32`.

**Using Frontend.LoadIfNecessary**

```d4
select table { LoadIfNecessary("Script('Users')", 841367), key {} } ;
select table { LoadIfNecessary("Script('Users')", 172836843), key {} } ;
```

```d4
CRCMatches Value                     ActualCRC32
---------- ------------------------- -----------
False
create table .System.Users in System.Catalog
{
    ID : System.UserID,
    Name : System.String tags { Frontend.Preview.Include = "true" },
    Group_Name : System.GroupName,
    key{ID} static tags { Storage.Enforced = "true" }
    
} tags { Frontend.UseList = "true" }; 172836843

CRCMatches Value      ActualCRC32
---------- ---------- -----------
True       <No Value> 172836843
```

**See Also**

[Frontend.GetCRC32](Frontend.md#frontendgetcrc32) \| [CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

### Document Operators

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Frontend Operators](Frontend.md#operators)

#### Frontend.CopyDocument

**Declarations**

```d4
create operator .Frontend.CopyDocument(const ASourceLibrary : System.Name, const ASourceDocument : System.Name, const ATargetLibrary : System.Name, const ATargetDocument : System.Name)
 class "Frontend.CopyNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`CopyDocument` makes a copy of `ASourceDocument` in `ASourceLibrary`. The copy will be named `ATargetDocument` and will be placed in `ATargetLibrary`.

> [!NOTE]
> If the target document already exists it will be overwritten without warning.

**Using Frontend.CopyDocument**

```d4
CreateAndSave
(
 "General",
 "Doc",
 "txt",
 "This is the contents of the text document."
);
select LoadDocument("General", "Doc");
CopyDocument
(
 "General", "Doc",
  "General", "OtherDoc"
);
select DocumentExists("General", "OtherDoc");
DeleteDocument("General", "OtherDoc");
select DocumentExists("General", "OtherDoc");
DeleteDocument("General", "Doc");
```

```text
This is the contents of the text document.
True
False
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.CreateAndSave

**Declarations**

```d4
create operator .Frontend.CreateAndSave(const ALibraryName : System.Name, const AName : System.Name, ADocumentType : System.String, const ADocument : System.Scalar)
begin
    
    if (.ADocumentType = '') then
        .ADocumentType := .Frontend.GetDocumentType(.ALibraryName, .AName);
    if not(exists((
        .Frontend.DocumentTypes
            where (.ID = .ADocumentType)
    ))) then
        raise .System.Error((('Document type ''' + .ADocumentType) + ''' not found.'));
    if .Frontend.DocumentExists(.ALibraryName, .AName) then
        .Frontend.DeleteDocument(.ALibraryName, .AName);
    .Frontend.CreateDocument(.ALibraryName, .AName, .ADocumentType);
    .Frontend.SaveDocument(.ALibraryName, .AName, .ADocument);;
end;
```

**Remarks**

`CreateAndSave` creates a document `AName` in `ALibraryName` as a `ADocumentType` document and saves it with the contents `ADocument`.

`ADocument` can be of type `String` or `Binary` as needed to match `ADocumentType`.

If a document by the same name already exists in the library, that document will first be deleted.

**Using Frontend.CreateAndSave**

```d4
CreateAndSave
(
 "General", "Doc",
 "txt", "This is the contents of the text document."
);
select LoadDocument("General", "Doc");
DeleteDocument("General", "Doc");
```

```text
This is the contents of my text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.CreateDocument

**Declarations**

```d4
create operator .Frontend.CreateDocument(const ALibraryName : System.Name, const AName : System.Name, const ADocumentType : System.String)
 class "Frontend.CreateNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`CreateDocument` creates a document `AName` in `ALibraryName` as a `ADocumentType` document.

If a document by the same name already exists in the library a Runtime error will occur.

**Using Frontend.CreateDocument**

```d4
CreateDocument("General", "Doc", "txt");
select DocumentExists("General", "Doc");
DeleteDocument("General", "Doc");
select DocumentExists("General", "Doc");
```

```text
True
False
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.DeleteDocument

**Declarations**

```d4
create operator .Frontend.DeleteDocument(const ALibraryName : System.Name, const AName : System.Name)
 class "Frontend.DeleteNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`DeleteDocument` deletes `AName` from `ALibraryName`.

**Using Frontend.DeleteDocument**

```d4
CreateDocument("General", "Doc", "txt");
select DocumentExists("General", "Doc");
DeleteDocument("General", "Doc");
select DocumentExists("General", "Doc");
```

```text
True
False
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.DocumentExists

**Declarations**

```d4
create operator .Frontend.DocumentExists(const ALibraryName : System.Name, const AName : System.Name) : System.Boolean
 class "Frontend.ExistsNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`DocumentExists` returns `True` if `AName` exists in `ALibraryName`, otherwise it returns `False`.

**Using Frontend.DocumentExists**

```d4
CreateDocument("General", "Doc", "txt");
select DocumentExists("General", "Doc");
DeleteDocument("General", "Doc");
select DocumentExists("General", "Doc");
```

```text
True
False
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.GetDefaultDocumentType

**Declarations**

```d4
create operator .Frontend.GetDefaultDocumentType(const ADesignerID : System.String) : System.String
begin
    
    if not(exists((
        .Frontend.DocumentTypeDesigners
            where (.Designer_ID = .ADesignerID)
    ))) then
        raise .System.Error((('Designer (' + .ADesignerID) + ') is not associated with any document types.'));
    .result := (
        (
            .Frontend.DocumentTypeDesigners
                where (.Designer_ID = .ADesignerID)
        )
            return 1 by { .DocumentType_ID asc }
    )[].DocumentType_ID;;
end;
```

**Remarks**

`GetDefaultDocumentType` returns a document type that can be associated with the specified `ADesignerID`.

**Using Frontend.GetDefaultDocumentType**

```d4
select GetDefaultDocumentType("D4");
```

```text
d4
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.GetDocumentType

**Declarations**

```d4
create operator .Frontend.GetDocumentType(const ALibraryName : System.Name, const AName : System.Name) : System.String
 class "Frontend.GetDocumentTypeNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`GetDocumentType` returns the document type of the document `AName` in the library `ALibraryName`.

**Using Frontend.GetDocumentType**

```d4
select GetDocumentType("Security", "Register");
```

```text
d4
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.Load

**Declarations**

```d4
create operator .Frontend.Load(const ALibraryName : System.Name, const AName : System.Name) : System.String
 class "Frontend.LoadNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`Load` returns `AName` from the library `ALibraryName`. Note that dfd and dfdx documents will be loaded raw. No ancestors will be merged with the dfdx document.

The `LoadDocument` and `Load` operators are equivalent.

**Using Frontend.Load**

```d4
create table Table in Temp
{
 ID : Integer, Name : String, key { ID }
};
CreateAndSave("General", "Form", "dfd", Derive("General.Table"));
select Load("General", "Form");
drop table Table;
DeleteDocument("General", "Form");
```

```text
<interface xmlns:bop="www.alphora.com/schemas/bop" ...
```

Note: Only the first few characters of the results are shown.

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.Image

**Declarations**

```d4
create operator .Frontend.Image(const ALibraryName : System.Name, const AName : System.Name) : System.Graphic
 class "Frontend.ImageNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`Image` returns the contents of the document `AName` in `ALibraryName` as a `Binary`.

An error will occur if the document contents are not of type `Binary`.

The `LoadBinary` and `Image` operators are equivalent.

**Using Frontend.GetLibraryNodeTypes**

```d4
select Image("Frontend", "Image.Edit");
```

```text
<binary data>
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.LoadAndProcess

**Declarations**

```d4
create operator .Frontend.LoadAndProcess(const ALibraryName : System.Name, const AName : System.Name) : System.String
 class "Frontend.LoadAndProcessNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`LoadAndProcess` returns `AName`, a dfd or dfdx form from the library `ALibraryName`. In the case of a dfdx, all ancestors will be merged with the dfdx document.

The `LoadAndProcess` and `Form` operators are equivalent.

**Using Frontend.LoadAndProcess**

```d4
create table Table in Temp
{
 ID : Integer, Name : String, key { ID }
};
CreateAndSave("General", "Form", "dfd", Derive("General.Table"));
select LoadAndProcess("General", "Form");
drop table Table;
DeleteDocument("General", "Form");
```

```text
<interface xmlns:bop="www.alphora.com/schemas/bop" ...
```

Note: Only the first few characters of the results are shown.

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.LoadBinary

**Declarations**

```d4
create operator .Frontend.LoadBinary(const ALibraryName : System.Name, const AName : System.Name) : System.Binary
 class "Frontend.LoadBinaryNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`LoadBinary` returns the contents of the document `AName` in `ALibraryName` as a `Binary`.

An error will occur if the document contents are not of type `Binary`.

The `LoadBinary` and `Image` operators are equivalent.

**Using Frontend.LoadBinary**

```d4
select LoadBinary("Frontend", "Image.Edit");
```

```text
<binary data>
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.LoadCustomization

**Declarations**

```d4
create operator .Frontend.LoadCustomization(const ADilxDocument : System.String) : System.String
 class "Frontend.LoadCustomizationNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`LoadCustomization` returns a user interface form as a string. It requires as an argument a string representing a dfdx form, not a dfd, to which `LoadCustomization` will merge all ancestors.

**Using Frontend.LoadCustomization**

```d4
begin
 var LDilxString : String;
 LDilxString := Load("General", "DFDXForm");
 LoadCustomization(LDilxString);
end;
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.LoadDocument

**Declarations**

```d4
create operator .Frontend.LoadDocument(const ALibraryName : System.Name, const AName : System.Name) : System.String
 class "Frontend.LoadNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`LoadDocument` returns the contents of the document `AName` in `ALibraryName` as a `String`.

An error will occur if the document contents are not of type `String`.

The `LoadDocument` and `Load` operators are equivalent.

**Using Frontend.LoadDocument**

```d4
CreateAndSave
(
 "General",
 "Doc",
 "txt",
 "This is the contents of the text document."
);
select LoadDocument("General", "Doc");
DeleteDocument("General", "Doc");
```

```text
This is the contents of the text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.MoveDocument

**Declarations**

```d4
create operator .Frontend.MoveDocument(const ASourceLibrary : System.Name, const ASourceDocument : System.Name, const ATargetLibrary : System.Name, const ATargetDocument : System.Name)
 class "Frontend.MoveNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`MoveDocument` moves the document `ASourceDocument` in `ASourceLibrary` to `ATargetDocument` in `ATargetLibrary`.

If the target document already exists, it will be overwritten without warning. The source document will not exist after the move.

**Using Frontend.MoveDocument**

```d4
CreateAndSave
(
 "General",
 "Doc",
 "txt",
 "This is the contents of the text document."
);
MoveDocument("General", "Doc", "General", "OtherDoc");
select Documents where Library_Name = "General";
select LoadDocument("General", "OtherDoc");
DeleteDocument("General", "OtherDoc");
```

```text
Library_Name Name     Type_ID
------------ -------- -------
General      OtherDoc txt

This is the contents of the text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.RefreshDocuments

**Declarations**

```d4
create operator .Frontend.RefreshDocuments(const ALibraryName : System.Name)
 class "Frontend.RefreshNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`RefreshDocuments` clears and reloads the documents buffer.

**Using Frontend.RefreshDocuments**

```d4
RefreshDocuments("General");
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.RenameDocument

**Declarations**

```d4
create operator .Frontend.RenameDocument(const AOldLibraryName : System.Name, const AOldName : System.Name, const ANewLibraryName : System.Name, const ANewName : System.Name)
 class "Frontend.RenameNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`RenameDocument` moves the document `AOldName` in `AOldLibraryName` to `ANewName` in `ANewLibraryName`.

**Using Frontend.RenameDocument**

```d4
CreateAndSave
(
 "General",
 "Doc",
 "txt",
 "This is the contents of the text document."
);
RenameDocument("General", "Doc", "General", "OtherDoc");
select Documents where Library_Name = "General";
select LoadDocument("General", "OtherDoc");
DeleteDocument("General", "OtherDoc");
```

```text
Library_Name Name     Type_ID
------------ -------- -------
General      OtherDoc txt

This is the contents of the text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.Save

**Declarations**

```d4
create operator .Frontend.Save(const ALibraryName : System.Name, const AName : System.Name, const ADocument : System.Scalar)
 class "Frontend.SaveNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`Save` replaces the current contents of the document `AName` in `ALibraryName` with `ADocument`.

`ADocument` can be of type `String` or `Binary` as needed to match the type of `ADocument`.

The `SaveDocument` and `Save` operators are equivalent.

**Using Frontend.Save**

```d4
CreateDocument("General", "Doc", "txt");
Save
(
 "General",
 "Doc",
 "This is the contents of the text document."
);
select LoadDocument("General", "Doc");
DeleteDocument("General", "Doc");
```

```text
This is the contents of the text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.SaveDocument

**Declarations**

```d4
create operator .Frontend.SaveDocument(const ALibraryName : System.Name, const AName : System.Name, const ADocument : System.Scalar)
 class "Frontend.SaveNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`SaveDocument` replaces the current contents of the document `AName` in `ALibraryName` with `ADocument`.

`ADocument` can be of type `String` or `Binary` as needed to match the type of `ADocument`.

The `SaveDocument` and `Save` operators are equivalent.

**Using Frontend.SaveDocument**

```d4
CreateDocument("General", "Doc", "txt");
SaveDocument
(
 "General",
 "Doc",
 "This is the contents of the text document."
);
select LoadDocument("General", "Doc");
DeleteDocument("General", "Doc");
```

```text
This is the contents of the text document.
```

**See Also**

[CRC Operators](Frontend.md#crc-operators) \| [Document Operators](Frontend.md#document-operators)

### Dynamic-Execution Operators

**See Also**

[Document Operators](Frontend.md#document-operators) \| [Frontend Operators](Frontend.md#operators)

#### Frontend.ExecuteScript

**Declarations**

```d4
create operator .Frontend.ExecuteScript(const ADocumentName : System.Name)
 class "Frontend.ExecuteScriptNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.ExecuteScript(const ALibraryName : System.Name, const ADocumentName : System.Name)
 class "Frontend.ExecuteScriptNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`ExecuteScript` opens the specified file and executes the contents of the file as a D4 script.

**Using Frontend.ExecuteScript**

```d4
CreateAndSave
(
 "General",
 "SampleScript",
 "d4",
 "RenameDocument('General', 'SampleScript', 'General', 'ExampleScript');"
);
select Documents where Library_Name = "General";
ExecuteScript("SampleScript");
select Documents where Library_Name = "General";
DeleteDocument("General", "ExampleScript");
```

```text
Library_Name Name         Type_ID
------------ ------------ -------
General      SampleScript d4

Library_Name Name          Type_ID
------------ ------------- -------
General      ExampleScript d4
```

**See Also**

[System.Execute](../system-library/operators/System.Execute.md) \| [System.Evaluate](../system-library/operators/System.Evaluate.md)

### Form Operators

**See Also**

[Document Operators](Frontend.md#document-operators) \| [Frontend Operators](Frontend.md#operators)

#### Frontend.ClearDerivationCache

**Declarations**

```d4
create operator .Frontend.ClearDerivationCache()
 class "Frontend.ClearDerivationCacheNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

When a form is derived, the resulting form definition is cached. This provides performance improvements when the form is recalled because it does not have to be derived again. `ClearDerivationCache` forces the flushing of all forms from the derivation cache so that a subsequent request for a form will cause the derivation process to occur again.

Normally this operator is unnecessary. The cached copy of a derived form is automatically flushed any time a change is made to the schema.

**Using Frontend.ClearDerivationCache**

```d4
ClearDerivationCache();
```

**See Also**

[Frontend.Derive](Frontend.md#frontendderive) \| [Frontend.Load](Frontend.md#frontendload) \| [Frontend.LoadDocument](Frontend.md#frontendloaddocument) \| [Frontend.Form](Frontend.md#frontendform) \| [Frontend.LoadAndProcess](Frontend.md#frontendloadandprocess) \| [Form Operators](Frontend.md#form-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.Derive

**Declarations**

```d4
create operator .Frontend.Derive(const AQuery : System.String, const APageType : System.String, const AMasterKeyNames : System.String, const ADetailKeyNames : System.String, const AElaborate : System.Boolean) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.Derive(const AQuery : System.String, const APageType : System.String, const AMasterKeyNames : System.String, const ADetailKeyNames : System.String) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.Derive(const AQuery : System.String, const APageType : System.String, const AElaborate : System.Boolean) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.Derive(const AQuery : System.String, const APageType : System.String) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.Derive(const AQuery : System.String, const AElaborate : System.Boolean) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
create operator .Frontend.Derive(const AQuery : System.String) : System.String
 class "Frontend.DeriveNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`Derive` returns a user-interface definition, as a string, which is dynamicaly derived from the schema of `AQuery`. Optional arguments are `APageType, AMasterKeyNames, ADetailKeyNames, AElaborate`.

**Using Frontend.Derive**

```d4
create table Table in Temp
{
 ID : Integer, Name : String, key { ID }
};
CreateAndSave("General", "Form", "dfd", Derive("General.Table"));
select Form("General", "Form");
drop table Table;
DeleteDocument("General", "Form");
```

```text
<interface xmlns:bop="www.alphora.com/schemas/bop" .....
```

Note: The results are only the first few characters.

**See Also**

[Frontend.Form](Frontend.md#frontendform) \| [Frontend.Load](Frontend.md#frontendload) \| [Frontend.LoadAndProcess](Frontend.md#frontendloadandprocess) \| [Form Operators](Frontend.md#form-operators) \| [Document Operators](Frontend.md#document-operators)

#### Frontend.Form

**Declarations**

```d4
create operator .Frontend.Form(const ALibraryName : System.Name, const AName : System.Name) : System.String
 class "Frontend.LoadAndProcessNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`Form` returns `AName`, a dfd or dfdx form from the library `ALibraryName`. In the case of a dfdx, all ancestors will be merged with the dfdx document.

The `LoadAndProcess` and `Form` operators are equivalent.

**Using Frontend.Form**

```d4
create table Table in Temp
{
 ID : Integer, Name : String, key { ID }
};
CreateAndSave("General", "Form", "dfd", Derive("General.Table"));
select Form("General", "Form");
drop table Table;
DeleteDocument("General", "Form");
```

```text
<interface xmlns:bop="www.alphora.com/schemas/bop" .....
```

Note: The results are only the first few characters.

**See Also**

[Frontend.Load](Frontend.md#frontendload) \| [Frontend.LoadAndProcess](Frontend.md#frontendloadandprocess) \| [Form Operators](Frontend.md#form-operators) \| [Document Operators](Frontend.md#document-operators)

### Internal Operators

**See Also**

[Frontend Operators](Frontend.md#operators)

#### Frontend.GetApplicationNodeTypes

**Declarations**

```d4
create operator .Frontend.GetApplicationNodeTypes(const AClientTypeID : System.String, const AApplicationID : System.Name) : System.String
begin
    .result := .Frontend.GetNodeTypes(.AClientTypeID, (
        (
            .Frontend.ApplicationLibraries
                where (.Application_ID = .AApplicationID)
        )
            over { .Library_Name }
    ));
end;
```

**Remarks**

`GetApplicationNodeTypes` is used internally by a Dataphor client to determine the available component types. This operator will only be needed by someone developing their own client.

**See Also**

[Internal Operators](Frontend.md#internal-operators)

#### Frontend.GetLibraryNodeTypes

**Declarations**

```d4
create operator .Frontend.GetLibraryNodeTypes(const AClientTypeID : System.String, const ALibraryName : System.Name) : System.String
begin
    
    .result := '<?xml version="1.0" encoding="utf-8" ?> <nodetypes xmlns="http://www.alphora.com/schemas/nodetypes">';
    var LCursor : cursor(table { Library_Name : System.Name }) := cursor
    (((
            (
                (
                    .System.LibraryRequisites
                        explode
                            by (.Library_Name = parent.Required_Library_Name)
                            where (.Library_Name = .ALibraryName)
                             include sequence sequence
                )
                    over { .Required_Library_Name }
            )
                rename
                {
                    .Required_Library_Name Library_Name tags { Frontend.Width = "50", Frontend.Title = "Requisite Library Name" }
                }
        ) union table of { Library_Name : System.Name }
        {
            row { .ALibraryName Library_Name }
        })
        capabilities { navigable, backwardsnavigable }
    );
    try
        
        var LLibraryName : System.Name;
        var LDocumentName : System.Name := .System.Name((.AClientTypeID + '.NodeTypes'));
        while .System.Next(.LCursor) do
            begin
                
                .LLibraryName := .System.Select(.LCursor).Library_Name;
                if .Frontend.DocumentExists(.LLibraryName, .LDocumentName) then
                    .result := (.result + .Frontend.Load(.LLibraryName, .LDocumentName));;
            end;;;
    finally
        .System.Close(.LCursor);
    end;
    .result := (.result + '</nodetypes>');;
end;
```

**Remarks**

`GetLibraryNodeTypes` is used internally by a Dataphor client to determine the available component types. This operator will only be needed by someone developing their own client.

**See Also**

[Internal Operators](Frontend.md#internal-operators)

#### Frontend.GetNodeTypes

**Declarations**

```d4
create operator .Frontend.GetNodeTypes(const AClientTypeID : System.String, const ALibraries : table { Library_Name : System.Name }) : System.String
begin
    
    .result := '<?xml version="1.0" encoding="utf-8" ?> <nodetypes xmlns="http://www.alphora.com/schemas/nodetypes">';
    var LCursor : cursor(table { Library_Name : System.Name }) := cursor
    (((
            (
                (
                    .System.LibraryRequisites
                        explode
                            by (.Library_Name = parent.Required_Library_Name)
                            where (.Library_Name in .ALibraries)
                             include sequence sequence
                )
                    over { .Required_Library_Name }
            )
                rename
                {
                    .Required_Library_Name Library_Name tags { Frontend.Width = "50", Frontend.Title = "Requisite Library Name" }
                }
        ) union .ALibraries)
        capabilities { navigable, backwardsnavigable }
    );
    try
        
        var LLibraryName : System.Name;
        var LDocumentName : System.Name := .System.Name((.AClientTypeID + '.NodeTypes'));
        while .System.Next(.LCursor) do
            begin
                
                .LLibraryName := .System.Select(.LCursor).Library_Name;
                if .Frontend.DocumentExists(.LLibraryName, .LDocumentName) then
                    .result := (.result + .Frontend.Load(.LLibraryName, .LDocumentName));;
            end;;;
    finally
        .System.Close(.LCursor);
    end;
    .result := (.result + '</nodetypes>');;
end;
```

**Remarks**

`GetNodeTypes` is used internally by a Dataphor client to determine the available component types. This operator will only be needed by someone developing their own client.

**See Also**

[Internal Operators](Frontend.md#internal-operators)
