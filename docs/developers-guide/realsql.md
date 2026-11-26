# RealSQL Language

In addition to its native language of D4, the DAE can accept user commands in terms of RealSQL, a dialect of SQL designed specifically for use with the DAE. This chapter provides a detailed description of the RealSQL language.

## Introduction

RealSQL is only a data manipulation language. Data definition commands and imperative statements must be issued using D4. The RealSQL language is designed to allow users familiar with SQL to access data in the DAE without having to learn a new language. However, unlike traditional SQL, RealSQL conforms to the same data management principles as D4. For a complete discussion of these issues see the Data Management Differences section of the Comparison With SQL in this manual.

This language guide uses a variation of Extended Backus-Naur Form (EBNF) notation to describe the syntax of RealSQL. For a complete description of this notation refer to the Syntactic Conventions section of the D4 Language Guide.

Note that several of the non-terminals appearing in this grammar are not defined such as *qualified identifier* and *literal*. These non-terminals are borrowed from the D4 grammar, and have the same interpretation.

RealSQL also uses the same lexical analyzer as D4, so the lexical elements of the two languages are the same. For a complete description of these elements of RealSQL refer to the Lexical Elements section of the D4 Language Guide.

## Scripts and Execution

A RealSQL script is a sequence of statements to perform a given task. Statements in the script must be separated by a semi-colon. Note that the semi-colon in RealSQL is a statement separator, not a statement terminator. Scripts in RealSQL have the following syntax:

```bnf
<script> ::=
    <statement>{; <statement>}
```

All statements in a RealSQL script are compiled into the same batch for execution.

## Statements

The only statements available in RealSQL are data manipulation language statements such as select and insert, and operator invocations. RealSQL does not support any imperative constructs such as variable declaration or flow control. Statements of RealSQL have the following syntax:

```bnf
<statement> ::=
    <select statement> |
    <insert statement> |
    <update statement> |
    <delete statement> |
    <expression>
```

### Select Statement

Data retrieval in RealSQL is done using the select statement. This statement describes a result and, optionally, the order in which that result should be returned. The result to be returned is described using a *query expression*, which will be described in the next section. The order of the result is specified as a list of column identifiers which are allowed to be either ascending or descending.

The select statement in RealSQL has the following syntax:

```bnf
<select statement> ::=
    <query expression>
    [<order by clause>]

<order by clause> ::=
    order by <order column expression commalist>
    
<order column expression> ::=
    <column identifier> [asc | desc]
    
<column identifier> ::=
    <qualified identifier>
```

#### Query Expression

Query expressions in RealSQL are made up of one or more select expressions concatenated using one of the union-compatible table operators union, intersect, or minus.

The union operator includes rows from both inputs in the result. Note that the union operator has no optional `all` keyword because duplicates are always eliminated from results.

The intersect operator only includes a row in the result if it appears in both inputs.

The minus operator only includes a row in the result if it appears in the first input and not in the second.

The query expression in RealSQL has the following syntax:

```bnf
<query expression> ::=
    <select expression> [<binary table expression>]

<binary table expression> ::=
    [union | intersect | minus] <select expression>
```

#### Select Expression

Select expressions in RealSQL are made up of several different clauses, each describing a different group of actions to be performed on the result. These different clauses are:

- Select Clause
- From Clause
- Where Clause
- Group By Clause
- Having Clause

Each of these clauses will be described in detail in the following sections.

The select expression in RealSQL has the following syntax:

```bnf
<select expression> ::=
    <select clause>
        <from clause>
        [<where clause>]
        [<group by clause>]
        [<having clause>]
```

#### Select Clause

The select clause performs four distinct operations on the result:

- Projection
- Extension
- Renaming
- Aggregate Extension

The select clause in RealSQL has the following syntax:

```bnf
<select clause> ::=
    select (* | <column expression commalist>)

<column expression> ::=
    <expression> [as <identifier>]
```

Projection involves selecting the columns to be included in the result. Note that unlike SQL, the select list includes no optional `distinct` keyword to specify duplicate elimination should occur. This is because, like the union operator, duplicates are always eliminated from a result.

Extension involves adding new columns to the result based on computational expressions. For example, a new column Name could be added based on the concatenation of the existing columns LastName and FirstName. Values for the columns of the row being extended are accessible by name within the expressions defining the new columns.

Renaming involves changing the name of a column in the result. Renaming does not change the values in the column.

Aggregate extension involves adding new columns to the result based on computational expressions involving aggregate functions such as `Count` and `Sum`. If a given select clause includes both aggregate and non-aggregate expressions, the group by clause must be used to specify the non-aggregate expressions. The arguments to the aggregation are allowed to be expressions themselves. Values for the columns of the rows being aggregated are accessible by name within these expressions.

#### From Clause

The from clause in RealSQL consists of a table specifier followed by any number of join clauses. The table specifier describes the initial result set, and each join clause specifies a join operation to be used to join in additional results.

The from clause in D4 has the following syntax:

```bnf
<from clause> ::=
    from <table specifier> [<join clause list>]

<table specifier> ::=
    (<table identifier> | "("<query expression>")") [as <table identifier>]
    
<table identifier> ::=
    <identifier>

<join clause> ::=
    [cross | inner | ((left | right) [outer])] join <table specifier> [on <expression>]
```

The table specifier is allowed to be a simple table variable reference, or a nested query expression in its own right. The table specifier also includes an optional table alias which can be used to resolve ambiguous column names in the result.

The join clause specifies the type of join to be performed (cross, inner, or left or right outer), and which columns are to be used in the join. Note that unlike SQL, joins in RealSQL must be equi-joins. In other words, the expression specifying the columns to be joined must consist of one or more column equality comparisons between columns from opposite tables in the join, concatenated with the and operator.

The cross join computes the cartesian product of the inputs. In other words, for each row of the left table, for each row of the right table, the result will include a row that is the concatenation of both rows.

The inner join operator includes a row in the result for every row in the left table, for every row in the right table for which the specified join condition evaluates to true.

The left outer join operator includes a row in the result for every row in the left table, for every row in the right table for which the specified join condition evaluates to true, and includes a row for every row in the left table for which no matching row in the right table is found, with all the columns of the right table set to null.

The right outer join operator includes a row in the result for every row in the left table, for every row in the right table for which the specified join condition evaluates to true, and includes a row for every row in the right table for which no matching row in the left table is found, with all the columns of the left table set to null.

Note carefully that the outer join operators are not relational operators. Because the result of an outer join may include rows in which some of the columns have no values, the result is not a relation. For more discussion on this topic, see the discussion for outer join operators in the Table Expressions section of the D4 Language Guide.

#### Where Clause

The where clause specifies a filter to be applied to the result. Only rows for which the specified condition evaluates to true will be included in the result. Values for the columns of the row being evaluated are accessible by name within the filter expression.

The where clause in RealSQL has the following syntax:

```bnf
<where clause> ::=
    where <expression>
```

#### Group By Clause

The group by clause specifies a grouping to be applied to the result. The columns specified in the group clause must include all the non-aggregate columns specified in the select clause.

The group by clause in RealSQL has the following syntax:

```bnf
<group by clause> ::=
    group by <expression commalist>
```

#### Having Clause

The having clause specifies a filter to be applied to the result after aggregation has been computed. The specified condition is allowed to reference aggregate expressions. Only rows for which the specified condition evaluates to true will be included in the result. Values for the columns of the row being evaluated are accessible by name within the filter expression.

The having clause in RealSQL has the following syntax:

```bnf
<having clause> ::=
    having <expression>
```

### Insert Statement

The insert statement allows data to be inserted into a table. RealSQL supports two variations of the insert statement, one in which a single row is specified via the values clause, and one in which the data to be inserted is described by a query expression. Note that the values clause can be constructed from arbitrary expressions, unlike the values clause of typical SQL implementations.

The insert statement in RealSQL has the following syntax:

```bnf
<insert statement> ::=
    insert into <table identifier>"("<column identifier commalist>")"
        (<values clause> | <query expression>)
        
<values clause> ::=
    values"("<expression commalist>")"
```

### Update Statement

The update statement allows the data in existing rows of a table to be changed. The current values of the row being changed are accessible by name within the expressions defining the new values. The update statement includes an optional where clause which specifies the rows for which the update should occur. Values for the columns of the row being updated are accessible within the where clause by name. If no where clause is specified, the update applies to all rows in the table.

The update statement in RealSQL has the following syntax:

```bnf
<update statement> ::=
    update <table identifier> 
        set <update column expression commalist>
        [<where clause>]

<update column expression> ::=
    <identifier> = <expression>
```

### Delete Statement

The delete statement deletes the rows of a table. The delete statement includes an optional where clause which specifies which rows should be deleted. Values for the columns of the row being deleted are accessible within the where clause by name. If no where clause is specified, the delete applies to all rows in the table.

The delete statement in RealSQL has the following syntax:

```bnf
<delete statement> ::=
    delete <table identifier>
        [<where clause>]
```

### Expression

An expression of RealSQL is a combination of literals, identifiers, and operators which describe an operation to be performed. The result of evaluating any given expression is a value of some type. Expressions may be used anywhere the language requires a value to be specified.

Expressions in RealSQL have the following syntax:

```bnf
<expression> ::=
    <binary expression> <between clause>
```

#### Between Clause

The between operator is a ternary operator that returns true if the first operand is between the second and third operands, inclusively. In other words, the expression:

```text
A between B and C
```

is equivalent to the double comparison:

```text
A >= B and A <= C
```

The between clause in RealSQL has the following syntax:

```bnf
<between clause> ::=
    between <arithmetic expression> and <arithmetic expression>
```

#### Binary Expression

A binary expression consists of two operands separated by one of the built-in binary operators of the language. Operator precedence and associativity in RealSQL is the same as D4. For a description of operator precedence and associativity in D4, as well as a complete description of each built-in operator, refer to the D4 Language Guide.

Binary expressions in RealSQL have the following syntax:

```bnf
<binary expression> ::=
    <arithmetic expression> {<binary operator> <arithmetic expression>}

<binary operator> ::=
    and | or | xor | like | matches | in
    ^ | & | "|" | "<<" | ">>" |
    = | "<>" | "<" | ">" | "<=" | ">=" | ?=
```

#### Arithmetic Expression

An arithmetic expression consists of two operands separataed by one of the built-in arithmetic operators of the langauge. For a complete description of these operators, refer to the D4 Language Guide.

Arithmetic expressions in RealSQL have the following syntax:

```bnf
<arithmetic expression> ::=
    <unary expression> {<arithmetic operator> <unary expression>}

<arithmetic operator> ::=
    + | - | * | / | div | mod | **
```

#### Unary Expression

A unary expression consists of one of the built-in unary operators of the language, followed by an operand. For a complete description of these operators, refer to the D4 Language Guide.

Unary expressions in RealSQL have the following syntax:

```bnf
<unary expression> ::=
    {<unary operator>} <qualifier expression>
    
<unary operator> ::=
    + | - | ~ | not | exists
```

#### Qualifier Expression

A qualifier expression consists of an optional `.` followed by an expression qualified as many times as desired. Qualification in RealSQL serves the same purposes as it does in D4, with the addition that in RealSQL it also serves to qualify column names with the appropriate table alias name. For a complete description of the qualifier operator, refer to the D4 Language Guide.

Qualifier expressions in RealSQL have the following syntax:

```bnf
<qualifier expression> ::=
    [.]{<indexer expression>.}<indexer expression>
```

#### Indexer Expression

Indexer expressions consist of an expression followed by an indexing clause as many times as desired. Indexing in RealSQL serves the same purposes as it does in D4. For a complete description of the indexer operator, refer to the D4 Language Guide.

Indexer expressions in RealSQL have the following syntax:

```bnf
<indexer expression> ::=
    <factor>{"["<expression>"]"}
```

#### Factor

A factor in RealSQL is the basic building block of any expression. It may be a literal, an identifier, an operator invocation, a case expression, or another expression nested within parentheses. Each of these possibilities will be discussed in detail in the following sections.

Factors in RealSQL have the following syntax:

```bnf
<factor> ::= 
    "("<expression>")" |
    "("<query expression>")" |
    <literal> |
    <identifier> |
    <operator invocation> |
    <aggregate operator invocation> |
    <case expression>
```

#### Operator Invocation

An operator invocation in RealSQL consists of the name of the operator to be invoked followed by a comma separated list of arguments to the operator in parentheses. These arguments may be expressions in themselves. Note that unlike D4, operators which take variable arguments may not be invoked, because RealSQL has no imperative mechanism for declaring a variable. Besides this restriction, any operator of D4 which returns a value may be invoked within a RealSQL expression.

Operator invocations in RealSQL have the following syntax:

```bnf
<operator invocation> ::=
    <identifier>"("<expression commalist>")"
```

#### Aggregate Operator Invocation

An aggregate operator invocation in RealSQL consists of the name of the aggregate operator to be invoked followed by the argument to be aggregated in parentheses. The argument may include an optional `distinct` specification which indicates that duplicates should be eliminated from the column being aggregated prior to performing the aggregation. RealSQL only supports the following aggregate operators:

- Sum
- Min
- Max
- Avg
- Count

Aggregate operator invocations in RealSQL have the following syntax:

```bnf
<aggregate operator invocation> ::=
    <identifier>"("[distinct] <expression>")"
```

#### Case Expression

A case expression in RealSQL allows a particular value to be selected based on the result of evaluating a specified condition or set of conditions. There are two flavors of the case expression, one in which a single expression is used as the comparison target for each case item, and one in which no comparison target is given, rather each case item condition is a boolean comparison in itself. In both versions of the expression, the final unsatisfied condition is given by the else expression, and the results of all case items and the else expression must return values of compatible types.

Case expressions in RealSQL have the following syntax:

```bnf
<case expression> ::=
    case [<expression>]
        <ne case item expression commalist>
        else <expression>
    end
    
<case item expression> ::=
    when <expression> then <expression>
```
