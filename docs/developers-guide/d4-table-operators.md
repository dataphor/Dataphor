# Table Operators

## Summary

This section covers the various table operators available in D4. The various inference mechanisms and updatability rules are covered for each of the operators.

The table operator clause in D4 has the following syntax:

```bnf
<table operator clause> ::=
    <where clause> |
    <over clause> |
    <remove clause> |
    <specify clause> |
    <add clause> |
    <rename clause> |
    <aggregate clause> |
    <quota clause> |
    <explode clause> |
    <adorn clause> |
    <redefine clause> |
    <union clause> |
    <intersect clause> |
    <minus clause> |
    <times clause> |
    <join clause> |
    <having clause> |
    <without clause> |
    <outer join clause>
```

ClosureAll D4 table operators are fully atomic and produce a new table value. This property is known as *closure* and allows the results of one table operator to be used as the argument to another. This allows operators within expressions to be *nested* as often as necessary, and in any order.

The following sections cover each table operator in detail. The sample database provided will be used throughout the examples in this section:

```d4
create table Employee
{
    ID : Integer,
    Name : String,
    key { ID }
};

insert
    table
    {
        row { 1 ID, "Joe" Name },
        row { 2, "John" },
        row { 3, "Josh" },
        row { 4, "Jeff" },
        row { 5, "Jake" },
        row { 6, "Jeff" }
    }
    into Employee;

create table Manager
{
    Manager_ID : Integer,
    Employee_ID : Integer,
    key { Manager_ID, Employee_ID },
    reference Manager_Employee { Manager_ID } references Employee { ID },
    reference Employee_Employee { Employee_ID } references Employee { ID }
};

insert
    table
    {
        row { 1 Manager_ID, 2 Employee_ID },
        row { 1 Manager_ID, 3 Employee_ID },
        row { 2 Manager_ID, 4 Employee_ID },
        row { 2 Manager_ID, 6 Employee_ID },
        row { 4 Manager_ID, 5 Employee_ID }
    }
    into Manager;

create table EmployeePhone
{
    ID : Integer,
    Phone : String,
    key { ID, Phone }
};

EmployeePhone :=
    table
    {
        row { 1 ID, "555-1234" Phone },
        row { 1 ID, "555-1000" Phone },
        row { 2 ID, "555-4321" Phone },
        row { 4 ID, "555-4444" Phone },
        row { 5 ID, "555-5678" Phone },
        row { 10 ID, "411" Phone   },
        row { 12 ID, "911" Phone }
    };
```

## Restriction

The **where** operator applies a condition to each row of the input. Only rows for which the condition evaluates to true appear in the result.

The where clause in D4 has the following syntax:

```bnf
<where clause> ::=
    where <expression term> [<language modifiers>]
```

The expression specified by [\<expression term](d4-language-elements.md#expressions)\> must be boolean-valued. Within the expression, access to the values of the input row are available by column name.

The result of a **where** has the same columns, orders, references, and metadata as the input table value. The restriction condition becomes a constraint of the result. If the restriction condition uses an equality test (=) against a key column, that column will be removed from the key if the equality comparison is part of a conjunction of comparisons (\<comparison\> **and** \<comparison\> **and**…) that constitute the entire restriction condition.

Data modifications against views defined using **where** are propagated directly to the input table. The language modifier EnforcePredicate can be used to control whether or not the new row must satisfy the restriction condition. The language modifier can be set to "true" or "false". The default is "false".

The following example illustrates the use of the **where** operator:

```d4
select Employee where ID >= 4;
```

```text
ID Name
-- ----
4  Jeff
5  Jake
6  Jeff
```

**See Also**

[System.iRestrict](../reference/system-library/operators/System.iRestrict.md)

## Projection

Projection allows a given set of columns to be removed from the result. There are two methods for specifying the projection list in D4, **over**, and **remove**. The **over** operator specifies the desired columns, while **remove** specifies the unwanted columns.

The over and remove clauses in D4 have the following syntax:

```bnf
<over clause> ::=
    over "{"<column name commalist>"}" [<language modifiers>]

<remove clause> ::=
    remove "{"<column name commalist>"}" [<language modifiers>]

<column name> ::=
    <qualified identifier>
```

The result of a projection has only the columns specified. Only keys of the input which are completely included in the specified column list are keys of the result. If all keys are excluded by the projection, the key becomes all columns of the result, eliminating duplicates as necessary. Only orders of the input which are completely included in the specified column list are orders of the result. References of the input which are completely included in the specified column list are references of the result.

Data modifications against views defined using **over** or **remove** are accomplished by performing the corresponding modifications on the input table. An insert will be rejected if the projection has excluded columns which do not have a default defined.

The following example illustrates the use of the project clause:

```d4
select Employee over { Name };
```

```text
Name
----
Jake
Jeff
Joe
John
Josh
```

The following query is equivalent to the above example but uses the remove clause instead:

```d4
select Employee remove { ID };
```

```text
Name
----
Jake
Jeff
Joe
John
Josh
```

The following examples illustrate key inference in a projection:

```d4
select Employee where Name = "Jeff";
```

```text
ID Name
-- ----
4  Jeff
6  Jeff
```

```d4
select Employee where Name = "Jeff" over { Name };
```

```text
Name
----
Jeff
```

**See Also**

[System.iProject](../reference/system-library/operators/System.iProject.md) \| [System.iRemove](../reference/system-library/operators/System.iRemove.md)

## Extension

The **add** operator allows a table value to be extended with new columns defined by expressions.

The add clause in D4 has the following syntax:

```bnf
<add clause> ::=
    add "{"<ne named expression term commalist>"}" [<language modifiers>]

<named expression term> ::=
    <expression term> <qualified identifier> <metadata>
```

Expressions within the \<add clause\> have access to the values of the current row by column name.

The result of an **add** has the same columns of the input, with the additional columns as defined by \<named expression term commalist\>. The result has the same keys, orders, references, and metadata as the input. In addition, introduced columns based on order-preserving expressions of columns that participate in keys in the input will result in keys in the output. For example, the expression:

```d4
select Employee add { ID ID1 }
```

will have keys `{ ID }` and `{ ID1 }`.

Modifications to views defined using **add** are propagated to the input by removing the extended columns.

The following example illustrates the use of the **add** operator:

```d4
select Employee
    add { "Employee Name = " + Name NewName }
    over { NewName };
```

```text
NewName
--------------------
Employee Name = Jake
Employee Name = Jeff
Employee Name = Joe
Employee Name = John
Employee Name = Josh
```

**See Also**

[System.iExtend](../reference/system-library/operators/System.iExtend.md)

## Rename

The **rename** operator is used to rename columns in the result. There are two variations of the **rename** operator. One renames a specified set of columns, and the other renames all the columns by qualifying each column name with a given identifier.

The rename clause in D4 has the following syntax:

```bnf
<rename clause> ::=
    rename ("{"<ne named column name commalist>"}" |
    (<qualified identifier> <metadata>))
    [<language modifiers>]

<named column name> ::=
    <column name> <column name>

<column name> ::=
    <qualified identifier>
```

The result of a **rename** operator has the same columns as the input, with the names changed as specified. The keys, orders, and references are included with the names of the columns involved updated appropriately. The result has the same metadata as the input.

Data modifications against views defined using **rename** are accomplished by transforming the modifications as appropriate for the name changes.

The following examples illustrate the use of the **rename** operator.

```d4
select Employee
    rename { ID EmployeeID, Name FirstName };
```

```text
EmployeeID FirstName
---------- ---------
1          Joe
2          John
3          Josh
4          Jeff
5          Jake
6          Jeff
```

```d4
select Employee rename X;
```

```text
X.ID X.Name
---- ------
1    Joe
2    John
3    Josh
4    Jeff
5    Jake
6    Jeff
```

**See Also**

[System.iRename](../reference/system-library/operators/System.iRename.md)

## Aggregate

The **group**, or *aggregate* table operator allows operations based on sets of rows to be computed and added to the result set. It should be noted that aggregation is not a primitive operator, as it can be expressed in terms of other operators. For example, the expression:

```d4
Employee group by { Name } add { Count() NameCount }
```

can also be expressed as:

```d4
Employee over { Name } rename X
    add { Count(Employee where Name = X.Name) NameCount }
```

The aggregate clause in D4 has the following syntax:

```bnf
<aggregate clause> ::=
    group [by "{"<ne column name commalist>"}"]
        add "{"<ne named aggregate expression commalist>"}"
        [<language modifiers>]

<named aggregate expression> ::=
    <aggregate expression> <column name>

<aggregate expression> ::=
    <operator name>
    "("
        [distinct]
        [<column name commalist>]
        [order by "{"<order column definition commalist>"}"]
    ")"

<column name> ::=
    <qualified identifier>

<operator name> ::=
    <qualified identifier>
```

The expression includes an optional \<by clause\> which specifies the grouping to be used for the aggregation. If no \<by clause\> is specified, the aggregation is performed for all the rows in the input. Otherwise, the input is partitioned into groups based on the columns in the \<by clause\>, and the aggregation is performed once for each group. The optional **distinct** specifier in the \<aggregate expression\> indicates that duplicates should be removed from the values for the source column prior to performing the aggregation.

The \<aggregate expression\> specifies the aggregate operator to be invoked. This can be a system-provided operator, or a user-defined operator, but it must be an aggregate operator. For a complete description of aggregate operators, refer to the [Aggregate Operators](d4-catalog.md#aggregate-operators) discussion in this guide.

With the exception of the Count, All, and Any operators, all the system-provided aggregate operators return nil when invoked on an empty set.

The result of the **group** operation is a table with the columns specified in the by clause and a column for each \<aggregate expression\>. The \<by clause\> functions as a projection so the keys of the result are determined the same as they would be for projection over the columns in the \<by clause\>. Orders, references, and metadata are also inferred as they are for projection.

Data modifications against views defined using **group** are accomplished by performing the modifications as though the expression were written longhand. In other words, the modifications are propagated through the equivalent projection and extension operators.

For complete descriptions of the aggregate operators available in D4, refer to [Aggregate Operators](../reference/system-library/operators/System.AggregateOperators.md) in the Dataphor System Library Reference.

**See Also**

[Aggregate Operators](d4-catalog.md#aggregate-operators)

## Quota

The **return**, or quota operator limits the result set to a given number of rows based on a specified order. Note that invocation of the **return** operator does not guarantee that the resulting set will have the given number of rows. There may be less, and there may be more, depending on the data involved, as explained below.

The quota clause in D4 has the following syntax:

```bnf
<quota clause> ::=
    return <expression term>
        [by "{"<ne order column definition commalist>"}"]
        [<language modifiers>]

<order column definition> ::=
    <column name>
        [sort <expression>]
        [asc | desc]
        [(include | exclude) nil]

<column name> ::=
    <qualified identifier>
```

The expression specified by \<expression term\> must be integer-valued, and specifies the number of rows to be returned in the result set. The actual number of rows returned may be lower if the input does not have enough rows to fulfill the request.

Note that if the columns specified in the by clause do not completely include some key of the input, then the actual cardinality of the output may be more than the number specified by the return expression. This is because the result will include rows that have the same values for the columns specified in the by clause. If the by clause is omitted, the compiler will select a key of the input to be used as the by specifier for the operation.

If the quota operator specifies that a single row should be returned (and the compiler can make this determination at compile time, i.e. the return expression is literal and evaluates to 1), and the quota operation is performed by some key of the input, the key of the output is the empty key. Otherwise, every key of the input is also a key of the output.

The result of a **return** operator has the same columns, orders, references, and metadata as the input.

Data modifications against views defined using **return** are propagated directly to the input. The language modifier EnforcePredicate can be used to control whether or not the new row must satisfy the quota condition. The language modifier can be set to "true" or "false". The default is "false".

The following examples illustrate the use of the quota operator:

```d4
select Employee return 3 by { Name };
```

```text
ID Name
-- ----
5  Jake
4  Jeff
6  Jeff
1  Joe
```

```d4
select Employee return 3 by { Name, ID };
```

```text
ID Name
-- ----
5  Jake
4  Jeff
6  Jeff
```

```d4
select Employee return 3 by { Name desc };
```

```text
ID Name
-- ----
3  Josh
2  John
1  Joe
```

**See Also**

[System.iQuota](../reference/system-library/operators/System.iQuota.md)

## Explode

The **explode** operator allows hierarchical queries to be expressed. Optional include specifications allow both the sequence within the hierarchy, and the level of the hierarchy to be included in the result set.

The explode clause in D4 has the following syntax:

```bnf
<explode clause> ::=
    explode by <expression term> where <expression term>
        [order by "{"<order column definition commalist>"}"]
        [include level [<column name>] <metadata>]
        [include sequence [<column name>] <metadata>]
        [<language modifiers>]
```

The expressions specified in the by clause and the [where](#restriction) clause must be boolean-valued. The by clause specifies the explode condition, and the where clause specifies the root condition. Within the explode condition, the values of the current parent row are accessible by the name of the column preceded by the **parent** keyword.

The optional **order by** specification provides a mechanism for describing the order in which rows will be processed in the explode.

The optional **include level** specification indicates that a column of type System.Integer and named **level**, or \<column name\> if supplied, be included in the result set. The value for this column is the nesting level for the row within the hierarchy.

The optional **include sequence** specification indicates that a column of type System.Integer and named **sequence**, or \<column name\> if supplied, be included in the result set. The value for this column is the sequence of the row within the hierarchy. The sequence column becomes a key of the result.

Note that if level or sequence are included, the input to the explode is required to be well-ordered (ordered by at least a key). This requirement ensures that the operation is well-defined when used on graphs that may have multiple parents for a single node (networks vs. hierarchies).

The result of an **explode** operator has all the columns of the input plus any included columns. All the keys are preserved, plus the key for the sequence, if included. The orders, references, and metadata of the input are preserved.

Modifications to views defined using **explode** are propagated directly to the input.

The following example illustrates the use of the **explode** operator:

```d4
select Manager
        join (Employee rename { ID Manager_ID, Name Manager_Name })
        join (Employee rename { ID Employee_ID, Name Employee_Name })
        explode
            by Manager_ID = parent Employee_ID
            where Manager_ID = 1
            include level
            include sequence;
```

```text
Manager_ID Employee_ID Manager_Name Employee_Name level sequence
---------- ----------- ------------ ------------- ----- --------
1          2           Joe          John          1     1
2          4           John         Jeff          2     2
4          5           Jeff         Jake          3     3
2          6           John         Jeff          2     4
1          3           Joe          Josh          1     5
```

**See Also**

[System.iExplode](../reference/system-library/operators/System.iExplode.md)

## Adorn

The **adorn** operator allows metadata and structural information to be added to the result set.

The adorn clause in D4 has the following syntax:

```bnf
<adorn clause> ::=
    adorn ["{"<ne adorn item definition commalist>"}"]
        <metadata>
        <alter metadata>
        [<language modifiers>]

<adorn item definition> ::=
    <adorn column> |
    <key definition> |
    (alter key "{"<column name commalist>"}" <alter metadata>) |
    (drop key "{"<column name commalist>"}") |
    <order definition> |
    (alter order "{"<order column definition commalist>"}" <alter metadata>) |
    (drop order "{"<order column definition commalist>"}") |
    <row constraint definition> |
    <reference definition> |
    (alter reference <reference name> <alter metadata>) |
    (drop reference <reference name>)

<adorn column> ::=
    <column name>
        <nilable definition>
        ["{"<ne column definition item commalist>"}"]
        <metadata>
        <alter metadata>

<column name> ::=
    <qualified identifier>
```

The **adorn** operator allows the definition of column defaults, column constraints, column metadata, keys, orders, and constraints, as well as the ability to alter the metadata for derived references, and exclude inferred keys, orders, and references. Each of these constructs is declared exactly as they are in the corresponding DDL statements. Note that keys and references introduced by the adorn operator are only used as structural information in the result set and are not enforced in the resulting expression, or within the database. Other types of constraints introduced by the **adorn** operator, such as row and transition constraints, are enforced.

The result of an **adorn** operator has the same columns, keys, orders, references, and metadata as the input, with the additional structural and metadata information specified by the operator.

Data modifications against views defined using **adorn** are propagated directly to the input.

The following example illustrates the use of the adorn operator:

```d4
select Employee
    adorn
    {
        ID { default 0 } tags { Frontend.Width = "5" },
        constraint IDValid ID > 5
        tags { DAE.Message = "ID must be greater than 5" }
    };
```

```text
ID Name
-- ----
6  Jeff
```

**See Also**

[System.iAdorn](../reference/system-library/operators/System.iAdorn.md)

## Redefine

The **redefine** operator is shorthand for an **add-remove-rename** operation. For example, the expression:

```d4
Employee redefine { ID := ID * 2 }
```

is equivalent to the following expression:

```d4
Employee add { ID * 2 X } remove { ID } rename { X ID }
```

The redefine clause in D4 has the following syntax:

```bnf
<redefine clause> ::=
    redefine "{"<redefine column commalist>"}" [<language modifiers>]

<redefine column> ::=
    <column name> := <expression term>

<column name> ::=
    <qualified identifier>
```

Each column is redefined in terms of an expression. Values of the current row are accessible by name within the expression.

The result of a **redefine** operation has the same column names as the input, with the specified columns redefined as specified. If any redefined column participates in a key, order, or reference, that structure is no longer part of the result. If this results in the elimination of all the keys, the key of the result is all the columns of the result.

Data modifications against views defined using **redefine** are propagated as though the equivalent **add-remove-rename** expression had been used.

The following example illustrates the use of the **redefine** operator:

```d4
select Employee redefine { ID := ID * 2 };
```

```text
ID Name
-- ----
2  Joe
4  John
6  Josh
8  Jeff
10 Jake
12 Jeff
```

## Specify

The specify operator (`{}`) is shorthand for an **add-project-rename** operation. It allows the desired column list to be specified in a single operation. The operation will only include extension, and rename if necessary, but will always include a projection over the specified column list.

The specify clause in D4 has the following syntax:

```bnf
<specify clause> ::=
    "{"<optionally named expression commalist>"}" [<language modifiers>]

<optionally named expression> ::=
    <expression> [<column name>]

<column name> ::=
    <qualified identifier>
```

Each column specifies either a column from the source table, or an expression that will be evaluated in terms of the source table, to be included in the result set. If the column specifies an expression, it must also specify a name for the column in the result set. Otherwise, the column may optionally specify a new name for the column in the result set.

The following examples illustrate the use of the specify operator:

```d4
// Specify as projection
select Employee { ID };
```

```text
ID
--
1
2
3
4
5
6
```

```d4
// Specify as rename
select Employee { ID Employee_ID, Name Employee_Name }
```

```text
Employee_ID Employee_Name
----------- -------------
1           Joe
2           John
3           Josh
4           Jeff
5           Jake
6           Jeff
```

```d4
// Specify as extension
select Employee { ID, Name, ID + 1 NewID }
```

```text
ID Name NewID
-- ---- -----
1  Joe  2
2  John 3
3  Josh 4
4  Jeff 5
5  Jake 6
6  Jeff 7
```

## Union

The **union** operator allows the rows of two table values to be included in a single result set. If a given row appears in both inputs, it will only appear once in the result. In other words, the **union** operation eliminates duplicates.

The union clause in D4 has the following syntax:

```bnf
<union clause> ::=
    union <expression term> [<language modifiers>]
```

The expression given by [\<expression term](d4-language-elements.md#expressions)\> must be table-valued. Both inputs to the **union** operation must be of the same table type.

The result of a **union** operation has the same type as the inputs. The key of the result is all columns of the table. The result has the orders, references, and metadata from both inputs.

Modifications to views defined using **union** are propagated to the inputs A and B as follows:

- Insert

  If the newly inserted row can be inserted into A or B, it is. Note that this implies that if the row can be inserted into both A and B it will appear in both. Only if the row cannot be inserted into either A or B will the insert be rejected.

- Update

  Update is treated as a delete of the original row, followed by an insert of the new row. If both operations can be performed against A or B, the operation is accepted. Note that this implies that if the row can be updated in both A and B, the update will be performed on both. Only if the modification fails on both A and B is the update rejected.

- Delete

  If the deleted row can be deleted from A or B, it is. Note that this implies that if the row can be deleted from both A and B it will be deleted from both. Only if the row cannot be delted from either A or B will the delete be rejected.

The following example illustrates the use of the union operator:

```d4
select (Employee where ID <= 4) union (Employee where ID >= 3);
```

```text
ID Name
-- ----
1  Joe
2  John
3  Josh
4  Jeff
5  Jake
6  Jeff
```

**See Also**

[System.iUnion](../reference/system-library/operators/System.iUnion.md)

## Intersect

The **intersect** operator computes the intersection of two table values. If a given row appears in both inputs, it will appear in the result.

The intersect clause in D4 has the following syntax:

```bnf
<intersect clause> ::=
    intersect <expression term> [<language modifiers>]
```

The expression given by \<expression term\> must be table-valued. Both inputs to the intersect operator must be of the same table type.

Because **intersect** is a special case of **join**, it has the same semantics for type inference and updatability. For this information, see [join](#join)

The following example illustrates the use of the **intersect** operator:

```d4
select (Employee where ID <= 4) intersect (Employee where ID >= 3);
```

```text
ID Name
-- ----
3  Josh
4  Jeff
```

**See Also**

[Join](#join) \| [Times](#times) \| [Sytem.iJoin](../reference/system-library/operators/System.iJoin.md)

## Minus

The **minus** operator computes the difference of two table values. Only rows appearing in the first input and not the second will appear in the result.

The minus clause in D4 has the following syntax:

```bnf
<minus clause> ::=
    minus <expression term> [<language modifiers>]
```

The expression given by \<expression term\> must be table-valued. Both inputs to the **minus** operator must be of the same table type.

The result of the **minus** operator has the same table type as both of the inputs. Keys, orders, references, and metadata are inferred from the first input.

Modifications to views defined using **minus** are propagated to the inputs A and B as follows:

- Insert

  If the newly inserted row can be inserted into B, it violates the predicate of the difference, and an error is returned. Otherwise, it is inserted into A.

- Update

  If the new version of the row can be inserted into B, it violates the predicate of the difference, and an error is returned. Otherwise, the update is performed against A.

- Delete

  The deleted row is deleted from A.

The following example illustrates the use of the minus operator:

```d4
select Employee minus (Employee where ID >= 3);
```

```text
ID Name
-- ----
1  Joe
2  John
```

**See Also**

[System.iDifference](../reference/system-library/operators/System.iDifference.md) \| [Table Operators](#summary)

## Times

The **times** operator computes the cartesian product (also called the cross join) of the inputs. For every row in the first input, a row appears in the result for every row in the second input that is the concatenation of both rows.

The times clause in D4 has the following syntax:

```bnf
<times clause> ::=
    times <expression term> [<language modifiers>]
```

The expression given by \<expression term\> must be table-valued. Inputs must have no column names in common.

Because **times** is a special case of **join**, it has the same semantics for type inference and updatability. For this information, see [join](#join).

The following example illustrates the use of the **times** operator:

```d4
select Employee times (Employee rename { Name FirstName } over { FirstName });
```

```text
ID Name FirstName
-- ---- ---------
1  Joe  Jake
1  Joe  Jeff
1  Joe  Joe
1  Joe  John
1  Joe  Josh
2  John Jake
2  John Jeff
2  John Joe
2  John John
2  John Josh
3  Josh Jake
3  Josh Jeff
3  Josh Joe
3  Josh John
3  Josh Josh
4  Jeff Jake
4  Jeff Jeff
4  Jeff Joe
4  Jeff John
4  Jeff Josh
5  Jake Jake
5  Jake Jeff
5  Jake Joe
5  Jake John
5  Jake Josh
6  Jeff Jake
6  Jeff Jeff
6  Jeff Joe
6  Jeff John
6  Jeff Josh
```

**See Also**

[System.iJoin](../reference/system-library/operators/System.iJoin.md) \| [Table Operators](#summary)

## Join

The **join** operator computes the combination of two table values based on the matching rows for a given set of columns. There are two types of joins in D4, the *natural join* and the *conditioned join*.

The natural join simply takes two table values as input and uses the commonly named columns to perform the join. The conditioned join includes a by clause which specifies the join condition.

Note that the two forms of the **join** operator are equivalent in terms of expressive power. Both forms are included in D4 to allow for different user preferences. The natural join lends itself to a database design in which column names are unique across the database, while the conditioned join lends itself to a design in which column names are only unique within tables.

The join clause in D4 has the following syntax:

```bnf
<join clause> ::=
    join <join specifier> [<language modifiers>]

<join specifier> ::=
    <expression term> [by <expression term>]
```

The result of a **join** operator on inputs A and B having column sets X, Y, and Z, where A has the columns `{ X, Y }` and B has the columns `{ Y, Z }` has the columns `{ X, Y, Z }`. Y represents the columns common to both inputs. Note that each of X, Y, and Z may be an empty set. For a conditioned join, Y is required to be an empty set.

The body of the result has a row for each row in A that matches the values for the columns given in Y for natural joins, or the condition for conditioned joins, for each row in B. The **join** operator is equivalent to a cartesian product of the inputs, followed by a restriction using the join condition.

Based on the join condition and the key information of the join operands, the *cardinality* of the join can be determined. The cardinality of a given join is the relationship between the number of rows in the left input and the number of potentially matching rows in the right input. There are four possibilities:

**One-to-one**  
If the join columns completely include some key in both inputs, the join is one-to-one. In other words, there will only ever be one matching row in the right input for a given row in the left input, and vice versa.

**One-to-many**  
If the join columns completely include some key of the left input, but not of the right input, the join is one-to-many. In other words, there may be any number of rows in the right input for any given row in the left input.

**Many-to-one**  
If the join columns completely include some key of the right input, but not of the left input, the join is many-to-one. In other words, there may be any number of rows in the left input for any given row in the right input.

**Many-to-many**  
If the join columns do not completely include any key of the left or right inputs, the join is many-to-many. In other words, there may be any number of rows in the left input that match any number of rows in the right input, and vice versa.

The cardinality of a join effectively determines the cardinality of the result. In addition to aiding the Dataphor Server in access path selection and optimization tasks, this information is used to determine how keys and orders are inferred through the join operation:

**One-to-one**  
For one-to-one joins, every key of the left and right inputs is also a key of the output. In the case of natural joins, duplicate keys are eliminated, resulting in a single key in the output.

For one-to-one joins, every order of the left and right inputs is also an order of the output, with duplicate orders eliminated in the case of natural joins.

**One-to-many**  
For one-to-many joins, every key of the right input is also a key of the output, plus a key for each key of the right input with join columns replaced by their correlated columns from the left input.

For one-to-many joins, every order of the right input is also an order of the output, with duplicate orders eliminated in the case of natural joins.

**Many-to-one**  
For many-to-one joins, every key of the left input is also a key of the output, plus a key for each key of the left input with join columns replaced by their correlated columns from the right input.

For many-to-one joins, every order of the left input is also an order of the output, with duplicate orders eliminated in the case of natural joins.

**Many-to-many**  
For many-to-many joins, every key of the left input is combined with every key of the right output to form a key in the output. In other words, the output has the cartesian product of all keys of both inputs, with duplicates eliminated in the case of natural joins. In addition, for conditioned joins only one column from each join condition will appear in any given key.

No orders are inferred for many-to-many joins.

For all types of joins, every source or target reference from both inputs that does not completely include the join columns is a source or target reference of the output, respectively. In other words, references that include the common columns of the join will not be inferred.

Data modifications against views defined using **join** are supported by projecting the modifications over the columns of each input.

The following sections describe the natural and conditioned joins.

### Natural Join

The natural join uses the commonly named columns, if any, in the inputs to perform the join. A natural join is by definition an *equi-join*. The commonly named columns are only included once in the result.

If the inputs have no column names in common, the natural join degrades to times. If the inputs have all column names in common, the natural join becomes an intersection. In both cases, type inference and updatability are unaffected. Note that if a natural join results in a times operation, the compiler will report a warning that a possibly incorrect times expression has been issued [^1].

The following example illustrates the use of the natural **join** operator:

```d4
select Employee join EmployeePhone;
```

```text
ID Name   Phone
--  ----  --------
1  Joe    555-1000
1  Joe    555-1234
2  John   555-4321
4  Jeff   555-4444
5  Jake   555-5678
```

### Conditioned Join

The conditioned join allows the join condition to be specified as part of the join. The input tables must have no column names in common.

The expression specified within the by clause by [\<expression term](d4-language-elements.md#expressions)\> must be boolean-valued and must specify an equi-join. These requirements are enforced by the compiler. Within the expression, the current values of both rows are accessible by column name.

The following example illustrates the use of the conditioned **join** operator:

```d4
select (Employee rename E)
    join (EmployeePhone rename EP)
        by E.ID = EP.ID
    remove { EP.ID };
```

```text
E.ID   E.Name    EP.Phone
------ --------  -----------
1      Joe       555-1000
1      Joe       555-1234
2      John      555-4321
4      Jeff      555-4444
5      Jake      555-5678
```

**See Also**

[System.iJoin](../reference/system-library/operators/System.iJoin.md) \| [Table Operators](#summary)

## Outer Join

The result of an outer **join** is the same as a **join**, except that rows in one side for which no matching row was found are still included in the result set, with the columns for the side with no matching row in the input set to nil.

In addition to specifying a natural or conditioned outer join, the outer join may be **left** or **right**. Left indicates that all rows in the left input should be preserved, while right indicates that all rows in the right input should be preserved.

The outer join clause in D4 has the following syntax:

```bnf
<outer join clause> ::=
    (left | right) (join | lookup) <join specifier>
        [include rowexists [<column name>] <metadata>]
        [<language modifiers>]

<join specifier> ::=
    <expression term> [by <expression term>]

<column name> ::=
    <qualified identifier>
```

The keys of the result are inferred from the keys of both inputs, and the cardinality of the join:

- One-to-one:

  The result has all the keys of the left and right input tables. If the join is left outer, the keys of the right table are inferred as *sparse* keys. If the join is right outer, the keys of the left table are inferred as sparse keys.

- One-to-many:

  The result has all the keys of the right input, plus a corresponding key for each key of the right input that has columns in common with the join columns. The join columns in the corresponding keys are replaced with the corresponding join columns in the left input. If the join is left outer, the corresponding keys are inferred as sparse keys.

- Many-to-one:

  The result has all the keys of the left input, plus a corresponding key for each key of the left input that has columns in common with the join columns. The join columns in the corresponding keys are replaced with the corresponding join columns in the right input. If the join is right outer, the corresponding keys are inferred as sparse keys.

- Many-to-many:

  For each key in the first input, for each key in the second input, the result has a key that is the union of the columns in both keys. In other words, the result has the cartesian product of the keys of both inputs. In addition, for conditioned joins only one column from each join condition will appear in any given key.

Order and reference inference for an outer **join** is the same as for a standard [join](#join).

The optional **rowexists** column is used to indicate whether a row is an actual join match, or is included in the result because the **join** is outer. This column is boolean-valued, and will be true whenever the row is a join match, and false otherwise. This column is updatable, and causes the insertion or deletion of a row in the outer input of the operation. Setting the **rowexists** column true causes a row to be inserted into the outer input, while setting the **rowexists** column false causes a row to be deleted from the outer input.

The **lookup** operator has the same semantics for retrieval, but modifications do not propagate through the **lookup**. For example, in a view defined by A **left lookup** B, an insert would be propagated to A, but not to B. The **lookup** operator is used primarily by client applications to control the update semantics involved in an expression.

The following examples illustrate the use of the outer **join** operators:

```d4
select Employee left join EmployeePhone;
```

```text
ID  Name Phone
-- ----  ----------
1  Joe   555-1000
1  Joe   555-1234
2  John  555-4321
3  Josh  <no value>
4  Jeff  555-4444
5  Jake  555-5678
6  Jeff  <no value>
```

```d4
select Employee right join EmployeePhone;
```

```text
ID  Name       Phone
--  ---------- --------
1   Joe        555-1000
1   Joe        555-1234
2   John       555-4321
4   Jeff       555-4444
5   Jake       555-5678
10 <no value>  411
12 <no value>  911
```

```d4
select Employee left lookup EmployeePhone include rowexists Extended;
```

```text
ID Name Extended Phone
-- ---- -------- ----------
1  Joe  True     555-1000
1  Joe  True     555-1234
2  John True     555-4321
3  Josh False    <no value>
4  Jeff True     555-4444
5  Jake True     555-5678
6  Jeff False    <no value>
```

```d4
create view EmpWithPhone
    Employee left lookup EmployeePhone;

select EmpWithPhone;
```

```text
ID Name Phone
-- ---- ----------
1  Joe  555-1000
1  Joe  555-1234
2  John 555-4321
3  Josh <no value>
4  Jeff 555-4444
5  Jake 555-5678
6  Jeff <no value>
```

```d4
// will be ignored since phone is across lookup
update EmpWithPhone set { Phone := "1701" };
select EmpWithPhone;
```

```text
ID Name  Phone
-- ----  ----------
1  Joe   555-1000
1  Joe   555-1234
2  John  555-4321
3  Josh  <no value>
4  Jeff  555-4444
5  Jake  555-5678
6  Jeff  <no value>
```

**See Also**

[System.iJoin](../reference/system-library/operators/System.iJoin.md) \| [System.iLeftJoin](../reference/system-library/operators/System.iLeftJoin.md) \| [System.iRightJoin](../reference/system-library/operators/System.iRightJoin.md) \| [Table Operators](#summary)

## Semijoin

In addition to the traditional join, the D4 language includes a semijoin operator called **having**. Loosely speaking, the operator computes the set of rows from the left input for which a matching row exists in the right input. It is equivalent to a join, followed by a projection over the columns of the right input. For example, the following query expressed using **having**:

```d4
select Employee
    having (EmployeeDepartment { Employee_ID ID })
```

is equivalent to the following **join-project**:

```d4
select Employee
    join (EmployeeDepartment { Employee_ID ID, Department_ID })
    over { ID, Name }
```

As with the **join** operator, there are two flavors of semijoin, the natural and conditioned semijoin. However, because the result set will always be projected over the columns of the left input, conditioned semijoins are not required to have unique column names. For example, the **having** above could be written:

```d4
select Employee
    having EmployeeDepartment
        by ID = Employee_ID
```

In addition, the semijoin condition can make use of the keywords **left** and **right** to distinguish potentially ambiguous column names, as in:

```d4
select Employee
    having EmployeeDepartment
        by left.ID = right.Employee_ID
```

The having clause in D4 has the following syntax:

```bnf
<having clause> ::=
    having <join specifier> [<language modifiers>]

<join specifier> ::=
    <expression term> [by <expression term>]
```

The result of a **having** operator on inputs A and B having column sets X, Y, and Z, where A has the columns `{ X, Y }` and B has the columns `{ Y, Z }` has the columns `{ X, Y }`. As with the join operator, Y represents the columns common to both inputs, and each of X, Y, and Z may be an empty set. Unlike the join, Y is not required to be empty for a conditioned semijoin.

The body of the result has a row for each row in A that matches the values for the columns given in Y for natural joins, or the condition for conditioned joins, for any row in B. Note carefully the difference in cardinality from a **join** operator. The cardinality of the result of a **having** will always be less than or equal to the cardinality of the left input.

As stated above, the semijoin is equivalent to a **join** followed by a projection over the columns of the left input.

Another way of expressing the semijoin operation is with the **exists** operator in a restriction condition. For example:

```d4
select Employee
    where exists (EmployeeDepartment where Employee_ID = ID)
```

In other words, the semijoin operator is a restriction based on the existence of a matching row in the right input. Because of this, the rules governing inference and updatability for the **having** operator most closely resemble that of the **where** operator. Namely, all columns, keys, orders, references, and metadata of the left input are inferred for the result.

Data modifications against views defined using **having** are propagated directly to the left input. The language modifier EnforcePredicate can be used to control whether or not the new row must satisfy the semijoin condition. The language modifier can be set to "true" or "false", with "false" being the default.

**See Also**

[System.iHaving](../reference/system-library/operators/System.iJoin.md) \| [Table Operators](#summary)

## Semiminus

The semiminus, or **without**, operator computes the set of rows from the left input for which a matching row does not exist in the right input. This operator is effectively the opposite of a semijoin, in that it is a restriction with a **not exists** in the condition. For example:

```d4
select Employee
    without (EmployeeDepartment { Employee_ID ID, Department_ID })
```

is equivalent to:

```d4
select Employee
    where not exists (EmployeeDepartment where Employee_ID = ID)
```

As with the **join** and **having** operators, there are two flavors of semiminus, the natural and conditioned semiminus. The result set will always be projected over the columns of the left input, so the conditioned semiminus is not required to have unique column names. For example, the **without** above could be written:

```d4
select Employee
    without EmployeeDepartment
        by ID = Employee_ID
```

In addition, the semiminus condition can make use of the keywords **left** and **right** to distinguish potentially ambiguous column names, as in:

```d4
select Employee
    without EmployeeDepartment
        by left.ID = right.Employee_ID
```

The without clause in D4 has the following syntax:

```bnf
<without clause> ::=
    without <join specifier> [<language modifiers>]

<join specifier> ::=
    <expression term> [by <expression term>]
```

The result of a **without** operator on inputs A and B having column sets X, Y, and Z, where A has the columns `{ X, Y }` and B has the columns `{ Y, Z }` has the columns `{ X, Y }`. As with the join operator, Y represents the columns common to both inputs, and each of X, Y, and Z may be an empty set. Unlike the join, Y is not required to be empty for a conditioned semiminus.

The body of the result has a row for each row in A that has no matching row in B based on the values for the columns given in Y for natural joins, or the condition for conditioned joins. The cardinality of the result of a **without** will always be less than or equal to the cardinality of the left input.

Another way of expressing the semijoin operation is with the **exists** operator in a restriction condition. For example:

```d4
select Employee
    where exists (EmployeeDepartment where Employee_ID = ID)
```

As with the semijoin, the semiminus operator is basically a restriction of the left input. Because of this, the rules governing inference and updatability for the **without** operator most closely resemble that of the **where** operator. Namely, all columns, keys, orders, references, and metadata of the left input are inferred for the result.

Data modifications against views defined using **without** are propagated directly to the left input. The language modifier EnforcePredicate can be used to control whether or not the new row must satisfy the semijoin condition. The language modifier can be set to "true" or "false", with "false" being the default.

**See Also**

[System.iWithout](../reference/system-library/operators/System.iWithout.md) \| [Table Operators](#summary)

## Comparison Operators

The following comparison operators are defined for tables:

- [iEqual](../reference/system-library/operators/System.iEqual.md)(=) and [iNotEqual](../reference/system-library/operators/System.iNotEqual.md)(\<\>)

  iEqual returns true if the given table values are equal. iNotEqual returns true if the two table values are not equal.

  Two table values are equal if they are of the same table type, and they have the same set of rows. For example:

  ```d4
  begin
      var LTable1 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };
      var LTable2 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };

      if not(LTable1 = LTable2) then
          raise Error("Table values are not equal");

      // equivalent formulation
      if not exists(LTable1 minus LTable2) and
          not exists (LTable2 minus LTable1) then
          raise Error("Table values are equal");
  end;
  ```

- iLess(\<)

  Returns true if the first table value is a proper subset of the second table value. A given table value A is a proper subset of some table value B if A and B are both of the same table type, and all the rows in A are in B, and there is at least one row in B that is not in A. For example:

  ```d4
  begin
      var LTable1 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID } };
      var LTable2 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };

      if not(LTable1 < LTable2) then
          raise Error("LTable1 is not a proper subset of LTable2");

      // equivalent formulation
      if not exists (LTable1 minus LTable2) and
          exists (LTable2 minus LTable1) then
          raise Error("LTable1 is a proper subset of LTable2");
  end;
  ```

- iInclusiveLess(\<=)

  Returns true if the first table value is a subset, not necessarily proper, of the second table value. A given table value A is a subset of some table value B if A and B are both of the same table type, and all the rows in A are in B. For example:

  ```d4
  begin
      var LTable1 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID } };
      var LTable2 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };

      if not(LTable1 <= LTable2) then
          raise Error("LTable1 is not a subset of LTable2");

      // equivalent formulation
      if not exists (LTable1 minus LTable2) then
          raise Error("LTable1 is a subset of LTable2");
  end;
  ```

- iGreater(\>)

  Returns true if the first table value is a proper superset of the second table value. A given table value A is a proper superset of some table value B if A and B are both of the same table type, and all the rows in B are in A, and there is at least one row in A that is not in B. For example:

  ```d4
  begin
      var LTable1 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };
      var LTable2 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID } };

      if not(LTable1 > LTable2) then
          raise Error("LTable1 is not a proper superset of LTable2");

      // equivalent formulation
      if not exists (LTable2 minus LTable1) and
          exists (LTable1 minus LTable2) then
          raise Error("LTable1 is a proper superset of LTable2");
  end;
  ```

- iInclusiveGreater(\>=)

  Returns true if the first table value is a superset, not necessarily proper, of the second table value. A given table value A is a superset of some table value B if A and B are both of the same table type, and all the rows in B are in A. For example:

  ```d4
  begin
      var LTable1 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID }, row { 7 ID } };
      var LTable2 : table { ID : Integer } :=
          table { row { 5 ID }, row { 6 ID } };

      if not (LTable1 >= LTable2) then
          raise Error("LTable1 is not a superset of LTable2");

      // equivalent formulation
      if not exists (LTable2 minus LTable1) then
          raise Error("LTable1 is a superset of LTable2");
  end;
  ```

- iCompare(?=)

  The iCompare operator is defined in terms of the other comparison operators:

  ```d4
  A ?= B = if A = B then 0 else if A < B then -1 else 1;
  ```

[^1]: This warning can be suppressed using the IsTimes modifier.
