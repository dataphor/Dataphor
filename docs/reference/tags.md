# Tags

## Summary

This chapter describes each Dataphor metadata tag.

**Further reference**

- [Tags in Data Definition Language (DDL) statements](../developers-guide/d4-catalog.md#metadata)
- [The Adorn Operator](../developers-guide/d4-table-operators.md#adorn)

## DAE Tags

This section lists the tags recognized by the Data Access Engine (DAE). The Data Access Engine is the query processor component of the Dataphor Server. DAE tags generally pertain to the runtime behavior of the Dataphor Server.

### Enforced

Determines whether the constraint on which it appears is enforced by the Dataphor Server. Setting the DAE.Enforced tag to false disables constraint validation in the Dataphor Server for the given constraint. The default determination for this setting is made by the compiler.

**Syntax**

```text
DAE.Enforced = "true | false"
```

**Applicable On**

- Scalar Type Constraints
- Column Constraints
- Keys
- Row Constraints
- Transition Constraints
- Reference Constraints
- Catalog Constraints

### IsClustered

Determines whether the key or order on which it appears is the clustered key for the table. This designation has several implications within the Dataphor Server, as well as for storage translation.

Within the Dataphor Server, the clustering key is used to determine an access path when an otherwise arbitrary selection would be made.

In translation, the designation indicates that the given key or order is to be used as the clustered index, or physical ordering, for the table variable.

By default, the compiler will select a clustering key based on physical considerations.

**Syntax**

```text
DAE.IsClustered = "true | false"
```

**Applicable On**

- Keys
- Orders

### IsComputed

Indicates whether a given column should recompute its value in response to proposable calls from the CLI. Only valid when introduced in an **add**, **redefine** or specify operator. By default, the value of the IsComputed tag is true, meaning that on a Change proposable call, the value of the column will be recomputed based on the expression in the **add** or **redefine** operator. To disable this recomputation, set the value of the IsComputed tag to false.

**Syntax**

```text
DAE.IsComputed = "true | false"
```

**Applicable On**

- Columns

### IsDeferred

Overrides the default determination for whether the constraint or event handler on which it appears should be deferred to transaction commit time. The default value for this setting is determined by the compiler based on the specific circumstances for each object. Refer to the documentation for each object type in the D4 Language Guide for more information.

**Syntax**

```text
DAE.IsDeferred = "true | false"
```

**Applicable On**

- Row Constraints
- Transition Constraints
- References

### IsOrderDependent

Specifies whether or not the aggregate operator on which it appears is an order-dependent aggregate operator. For more information on order-dependent aggregate operators, refer to the Aggregate Operators section of the Catalog Elements chapter of the D4 Language Guide.

**Syntax**

```text
DAE.IsOrderDependent = "true | false"
```

**Applicable On**

- Aggregate Operators

### IsOrderPreserving

Overrides the default determination for whether the operator on which it appears is an order-preserving operator. For more information on order-preserving operators, refer to the Operators section of the Language Elements chapter of the D4 Language Guide.

**Syntax**

```text
DAE.IsOrderPreserving = "true | false"
```

**Applicable On**

- Operators

### IsSparse

Determines whether the key on which it appears is a sparse key. If a key is defined as sparse, then the uniqueness constraint is enforced only for rows which have a value specified for all the columns of the key. By contrast, a dense key enforces that only one row within a table variable is allowed to contain no values for the columns of the key. The default value for this setting is false.

**Syntax**

```text
DAE.IsSparse = "true | false"
```

**Applicable On**

- Keys

### Message or SimpleMessage

Specifies a custom message to be used to display an error to the user when a constraint is violated. The Message tag must be a valid D4 expression. The same parameters available for writing the constraint expression are available within the message expression. Note that for transition constraints, a different message can be supplied for each transition by qualifying the message tag with the transition. For example, the tag DAE.Insert.Message can be used to specify the violation message for the insert transition of a given transition constraint.

**Syntax**

```bnf
DAE.[<transition>.]Message = "<expression>"
DAE.[<transition>.]SimpleMessage = "<string>"

<transition> ::= Insert | Update | Delete
```

**Applicable On**

- Scalar Type Constraints
- Column Constraints
- Row Constraints
- Keys
- References
- Transition Constraints
- Catalog Constraints

### Representation

Specifies a representation to be used to access values of the type on which it appears. Determines which representation should be used by controls in the Frontend Clients.

**Syntax**

```bnf
DAE.<native accessor name> = "<representation name>"

<native accessor name> ::=
    AsBoolean |
    AsByte |
    AsInt16 |
    AsInt32 |
    AsInt64 |
    AsDecimal |
    AsTimeSpan |
    AsDateTime |
    AsGuid |
    AsString |
    AsDisplayString |
    AsException |
    AsByteArray
```

**Applicable On**

- Scalar Types

### ShouldTranslate

Overrides the default determination for whether the object on which it appears should be translated into an application transaction. The default value for this setting is determined by the compiler based on the specific circumstances for each object. Refer to the documentation for each object type in the D4 Language Guide for more information.

**Syntax**

```text
DAE.ShouldTranslate = "true | false"
```

**Applicable On**

- Operators
- Table Variables
- Event Handlers

## Storage Tags

This section lists the tags recognized by the Storage Integration Architecture (SIA). These tags generally control how the Dataphor Server maps schema objects into underlying storage systems.

### Deferred

Specifies whether or not reads should be deferred for the domain or column on which it appears. This tag is used by the SQL devices to indicate that the data type used in the target system supports deferred read access. When this tag is used, columns that are deferred are not retrieved with the initial query. Instead, the device creates a deferred read stream that accesses the data of the column only when it is actually requested through the CLI.

**Syntax**

```text
Storage.Deferred = "true | false"
```

**Applicable On**

- Columns
- Scalar Type Maps
- Scalar Types

### Enforced

This tag has been deprecated. Use the DAE.Enforced tag to indicate whether a constraint should be enforced. For backwards compatibility, the Storage.Enforced tag will continue to function as described in this documentation, but support for this tag will be dropped in a future version. Note that the semantics for the DAE.Enforced tag are the opposite of the Storage.Enforced tag, in other words setting Storage.Enforced to true has the same effect as setting DAE.Enforced to false. Because the tag is deprecated, the compiler will automatically replace Storage.Enforced tags with the appropriate DAE.Enforced tag.

Determines whether the constraint on which it appears is enforced by the device. If the device is responsible for enforcing a given constraint, the Dataphor Server does not attempt to enforce it. In other words, setting the Enforced tag to true effectively disables constraint validation in the Dataphor Server for the given constraint. The default determination for this setting is made by the compiler.

**Syntax**

```text
Storage.Enforced = "true | false"
```

**Applicable On**

- Scalar Type Constraints
- Column Constraints
- Keys
- Row Constraints
- Transition Constraints
- Reference Constraints
- Catalog Constraints

### IsClustered

This tag has been deprecated. Use the DAE.IsClustered tag instead. For backwards compatibility, the Storage.IsClustered tag will continue to function as described in this documentation, but support for this tag will be dropped in a future version. Because the tag is deprecated, the compiler will automatically replace Storage.IsClustered tags with DAE.IsClustered tags.

Determines whether the key or order on which it appears is the clustered key for the table. This designation has several implications within the Dataphor Server, as well as for storage translation.

Within the Dataphor Server, the clustering key is used to determine an access path when an otherwise arbitrary selection would be made.

In translation, the designation indicates that the given key or order is to be used as the clustered index, or physical ordering, for the table variable.

By default, the compiler will select a clustering key based on physical considerations.

**Syntax**

```text
Storage.IsClustered = "true | false"
```

**Applicable On**

- Keys
- Orders

### IsSparse

> [!WARNING]
> This tag has been deprecated. Use the DAE.IsSparse tag instead. The semantics of the tag are the same. For backwards compatibility, the Storage.IsSparse will continue to function as described in this documentation, but support for this tag will be dropped in a future version. Because the tag is deprecated, the compiler will automatically replace Storage.IsSparse tags with DAE.IsSparse tags.

Determines whether the key on which it appears is a sparse key. If a key is defined as sparse, then the uniqueness constraint is enforced only for rows which have a value specified for all the columns of the key. By contrast, a dense key enforces that only one row within a table variable is allowed to contain no values for the columns of the key. The default value for this setting is false.

**Syntax**

```text
Storage.IsSparse = "true | false"
```

**Applicable On**

- Keys

### Length

Specifies the length of the domain definition to be used in the target system (i.e. VARCHAR(40)). This tag is used by the string-like scalar type maps introduced by the various SQL devices. Essentially, any type map that makes use of a CHAR or VARCHAR type on the target system will search for this tag to determine the length of the character data to be used in the target system. The tag is searched for in the following order:

1.  Column metadata
2.  Scalar type map metadata
3.  Scalar type metadata

**Syntax**

```text
Storage.Length = "<integer>"
```

**Applicable On**

- Columns
- Scalar Type Maps
- Scalar Types

### Name

Specifies the name of the object in the target system. For schema objects that were imported from existing schema in target systems, the Name tag will be set to the name of the corresponding schema object in the target system. For schema objects created within the Dataphor Server, the Name tag can be used to specify the name of the corresponding schema object created or referenced in the target system.

**Syntax**

```text
Storage.Name = "<string>"
```

**Applicable On**

- Scalar Types
- Scalar Type Maps
- Table Variables
- Columns
- Keys
- Orders

### Precision

Specifies the precision of the domain definition to be used in the target system (i.e. DECIMAL(12)). This tag is used by the decimal-like scalar type maps introduced by the various SQL devices. Essentially, any type map that makes use of a NUMERIC or DECIMAL type on the target system will search for this tag to determine the precision of the decimal data to be used in the target system. The tag is searched for in the following order:

1.  Column metadata
2.  Scalar type map metadata
3.  Scalar type metadata

**Syntax**

```text
Storage.Precision = "<integer>"
```

**Applicable On**

- Columns
- Scalar Type Maps
- Scalar Types

### Scale

Specifies the scale of the domain definition to be used in the target system (i.e. DECIMAL(12, 2)). This tag is used by the decimal-like scalar type maps introduced by the various SQL devices. Essentially, any type map that makes use of a NUMERIC or DECIMAL type on the target system will search for this tag to determine the scale of the decimal data to be used in the target system. The tag is searched for in the following order:

1.  Column metadata
2.  Scalar type map metadata
3.  Scalar type metadata

**Syntax**

```text
Storage.Scale = "<integer>"
```

**Applicable On**

- Columns
- Scalar Type Maps
- Scalar Types

### Schema

Specifies the schema name of the object in the target system. For schema objects that were imported from existing schema in target systems, the Schema tag will be set to the name of the schema for the corresponding schema object in the target system. For schema objects created within the Dataphor Server, the Schema tag can be used to specify the schema name of the corresponding schema object created in the target system. The value of the Schema tag will override the value of the Schema attribute set on an SQL device. Note that some target systems to not support the concept of database schemas, and will therefore ignore the value of this tag.

**Syntax**

```text
Storage.Schema = "<string>"
```

**Applicable On**

- Table Variables
- Keys
- Orders

### Scope

Specifies the storage scope for the table variable. This tag only applied to table variables stored in a memory device. The scope can be:

- Database
- Session
- Process

Regardless of the scope of the table variable, the definition of the type and structure of the table variable is the same for the all scopes. However, the contents of the table variable will vary based on the scope. For database-scoped table variables (the default), the same storage is provided for all sessions and processes. For session-scoped table variables, a different storage space is provided for each session, and similarly for process-scoped table variables, a different storage space is provided for each process.

**Syntax**

```text
Storage.Scope = "Database | Session | Process"
```

**Applicable On**

- Table Variables (only if stored in a Memory Device)

### ShouldReconcile

Determines whether or not the object on which it appears should be reconciled with the target system. By default, the value of the ShouldReconcile tag is true, meaning that the object should be reconciled with the target system. A value of false indicates that no reconciliation will be performed with the target system. This designation is used in addition to the reconciliation mode and master of each device.

**Syntax**

```text
Storage.ShouldReconcile = "true | false"
```

**Applicable On**

- Table Variables
- Columns
- Keys
- Orders

## Frontend Tags

This section lists the tags recognized by the Frontend. Note that these tags may appear in various contexts throughout the metadata, and that the Frontend Server will search for these tags in specific ways, based on the type of user interface being derived. For more information on how the Frontend uses these tags, refer to [Presentation Layer](../developers-guide/presentation-layer.md#summary). For more specific information on the use of tags to guide derivation of the user interface see [Guiding Derivation](../developers-guide/form-automation.md#guiding-derivation).

Frontend tags can be specific or general. For example:

```d4
ID : Integer tags { Frontend.Visible = "false" }
```

indicates that the ID is not to appear on any user interface (form) regardless of what mode the user interface is in.

```d4
ID : Integer tags { Frontend.Browse.Visible = "false" }
```

indicates ID will not be visible when the user-inteface (form) is in the browse mode. When the user interface is in any other mode, ID will be visible.

### Break

Determines that a break should be placed after the column or group on which it appears.

**Syntax**

```text
Frontend.Break = "true | false"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### Caption

Determines the caption of the user interface or control for the object on which it appears.

**Syntax**

```text
Frontend.Caption = "<string>"
```

**Applicable On**

- Tables
- Views
- Columns
- Scalar Types (indirectly)
- References

### DetailKeyNames

TagsDetailKeyNamesDetailKeyNames

Determines the value of the DetailKeyNames argument to derivations produced for the objects on which it appears.

**Syntax**

```text
Frontend.DetailKeyNames = "<column name semicolonlist>"
```

**Applicable On**

- References

### Document

Determines the document to present a user interface for the objects on which it appears. Can also be used prefixed with the Add, Edit, Delete, and View page types to specify documents for those commands within a plural page type.

**Syntax**

```text
Frontend.Document = "<document expression>"
```

**Applicable On**

- Tables
- Views
- References

### Elaborate

Determines the value of the Elaborate argument to derivations produced for the objects on which it appears.

**Syntax**

```text
Frontend.Elaborate = "true | false"
```

**Applicable On**

- Tables
- Views
- References

### ElementType

Determines the type of control to be used for the object on which it appears.

**Syntax**

```text
Frontend.ElementType = "<identifier>"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### Embedded

Determines whether the object on which it appears will be embedded in derived expressions or user interfaces.

**Syntax**

```text
Frontend.Embedded = "true | false"
```

**Applicable On**

- References

### Exposed

Determines whether the object on which it appears will be exposed (in the toolbar) in derived user interfaces.

**Syntax**

```text
Frontend.Exposed = "true | false"
```

**Applicable On**

- References

### Flow

Determines the flow to be used when performing layout for derived user interfaces.

**Syntax**

```text
Frontend.Flow = "default | vertical | horizontal"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### FlowBreak

Determines that a flowbreak should be placed after the column or group on which it appears.

**Syntax**

```text
Frontend.FlowBreak = "true | false"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### EliminateGroup

Determines whether the group on which it appears should be eliminated by the structuring process.

By default, the structuring process will eliminate groups containing only a single element. This is done to avoid unnecessary groupings of controls on forms. However, in cases where a single-element group is desired, this tag can be used to keep the group from being eliminated.

**Syntax**

```text
Frontend.EliminateGroup = "true | false"
```

**Applicable On**

- Tables (in a Group extraction context)
- References (in a Group extraction context)

### Group

Determines which group the column on which it appears should be a member of.

When specified on a reference, determines which group the reference should be a member of. Note that for references, specifying the group of a reference places all columns (and elaborated references for those columns, recursively) into the specified group.

**Syntax**

```text
Frontend.Group = "<qualified identifier>"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns
- References

### Hint

Determines the hint to display in derived user interfaces for the object on which it appears.

**Syntax**

```text
Frontend.Hint = "<string>"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### Include

Indicates whether the column on which it appears should be included in derived expressions.

On a reference, specifies whether the reference should be followed for the purpose of elaboration.

**Syntax**

```text
Frontend.Include = "true | false"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

### IncludeGroupTitle

Indicates whether the title of the inclusion reference should be included in the title of the column.

**Syntax**

```text
Frontend.IncludeGroupTitle = "true | false"
```

**Applicable On**

- References

### Frontend.IsDefault

Indicates that the key or order on which it appears is the default order.

**Syntax**

```text
Frontend.IsDefault = "true | false"
```

**Applicable On**

- Key
- Order

### MasterKeyNames

Determines the value of the MasterKeyNames argument to derivations produced for the objects on which it appears.

**Syntax**

```text
Frontend.MasterKeyNames = "<column name semicolonlist>"
```

**Applicable On**

- References

### Priority

Determines a priority for the object on which it appears.

**Syntax**

```text
Frontend.Priority = "<integer>"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns
- Tables
- Views
- References

### Query

Determines the value of the Query argument to derivations produced for the objects on which it appears.

**Syntax**

```text
Frontend.Query = "<table expression>"
```

**Applicable On**

- Tables
- Views
- References

### ReadOnly

Indicates that the object on which it appears should be presented with a read-only user interface. The default value for this tag depends on context, and is usually based on the inferred read-only characteristics for each column.

**Syntax**

```text
Frontend.ReadOnly = "true | false"
```

**Applicable On**

- Scalar Types [^1]
- Columns
- Tables
- Views

### Secure

Determines how the object on which it appears should behave in derived user interfaces if it is not accessible by the current user because of security. The default value for this tag is hidden, indicating that no user interface element will be visible for the object.

**Syntax**

```text
Frontend.Secure = "visible | disabled | hidden"
```

**Applicable On**

- Tables
- Views
- References

### ShouldEnlist

Indicates whether or not to enlist in an existing application transaction within the derived user interface for the table variable on which it appears. The default value for this tag is default, indicating that the determination should be made based on the cardinality of the result set for the derived user interface being displayed.

**Syntax**

```text
Frontend.ShouldEnlist = "default | true | false"
```

**Applicable On**

- Tables
- Views

### Title

Determines the title of the object on which it appears.

**Syntax**

```text
Frontend.Title = "<string>"
```

**Applicable On**

- Tables
- Views
- Columns
- Scalar Types (indirectly)
- References

### TreatAsLookup

This tag can be used on a parent reference to indicate whether or not the query elaboration process should treat the reference as a parent or lookup reference. If the reference is treated as a parent reference, the target table will be included in the result set using an inner join. If the reference is treated as a lookup reference (the default), the target table will be included in the result set using a left outer join, and the user interface will be constructed as though the reference were a lookup reference (i.e. using lookup controls to browse for the item to be selected, rather than entering the information directly on the derived form).

**Syntax**

```text
Frontend.TreatAsLookup = "true | false"
```

**Applicable On**

- References (parent references only)

### UseApplicationTransactions

Indicates whether or not application transactions should be used within the derived user interface for the table variable on which it appears.

**Syntax**

```text
Frontend.UseApplicationTransactions = "true | false"
```

**Applicable On**

- Tables
- Views

### UseBrowse

Indicates whether or not a browse clause should be used to specify the ordering within the derived user interface for the table variable on which it appears. The default value for this tag is true.

**Syntax**

```text
Frontend.UseBrowse = "true | false"
```

**Applicable On**

- Tables
- Views

### UseList

Indicates that the List page type should be used to derive forms for the objects on which it appears.

**Syntax**

```text
Frontend.UseList = "true | false"
```

**Applicable On**

- Tables
- Views
- References

### UseFullLookup

Determines that a full lookup control should be used to display the lookup for this reference in derived user interfaces.

**Syntax**

```text
Frontend.UseFullLookup = "true | false"
```

**Applicable On**

- References

### Visible

Determines whether the object on which it appears will be visible in derived user interfaces.

**Syntax**

```text
Frontend.Visible = "true | false"
```

**Applicable On**

- Tables
- Views
- Columns
- Scalar Types (indirectly)
- References

### Width

Determines the width of the object on which it appears.

**Syntax**

```text
Frontend.Width = "<integer>"
```

**Applicable On**

- Scalar Types (indirectly)
- Columns

[^1]: Indirectly. This tag is never looked for by the Frontend Server on a scalar type specifically. Specifying the tag on a scalar type "defaults" the tag for all columns of that type.
