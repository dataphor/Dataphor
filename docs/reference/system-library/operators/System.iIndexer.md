# System.iIndexer

**Declarations**

```d4
create operator .System.iIndexer(const AList : generic list, const AIndex : System.Integer) : generic
 class "System.IndexerNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iIndexer(const AValue : System.String, const AIndex : System.Integer) : System.String
 class "System.StringIndexerNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`[]` is the parser recognized symbol for `iIndexer`. `iIndexer` is an extractor operator. The `iIndexer` operator is overloaded for tables, lists, and strings, and may be overloaded for other types by using the operator name iIndexer.

> [!NOTE]
> List and string indexes are zero-based.

**Using System.iIndexer with lists**

For a list, the indexer returns the element of the list specified by the index.

```d4
select "The element at index position 2 in the list 'AA BB CC DD' is "
 + { "AA", "BB", "CC", "DD" }[2]
 + ".";
```

```text
The element at index position 2 in the list 'AA BB CC DD' is CC.
```

**Using System.iIndexer with strings**

For a string, the indexer returns the character in the position specified by the index.

```d4
select "The character at index position 2 in the string 'abcde' is "
 + "abcde"[2]
 + ".";
```

```text
The character at index position 2 in the string 'abcde' is c.
```

**Using System.iIndexer with tables**

For a table, the indexer returns a row.

See [Indexer Expression](../../../developers-guide/d4-language-elements.md#indexer-expression) for more details about using indexer expressions with tables.

In some of the following examples the [Qualifier Expession](../../../developers-guide/d4-language-elements.md#qualified-expression) (period) is used as a column extractor.

```d4
create table ExtractorExample in Temp
{
 Sign : String,
 Shape : String,
 Colors : String,
 key { Sign }
};
ExtractorExample := table
{
 row { "Stop" Sign, "Octagon" Shape, "Red and White" Colors },
 row { "Yield", "Triangle", "Red and White" },
 row { "School", "Pentagon", "Yellow and Black" }
};
select "Yield signs are "
 + ExtractorExample["Yield"].Colors
 + ".";
select "Octagon shaped signs are "
 + ExtractorExample["Octagon" by { Shape } ].Sign
 + "signs.";
select table { ExtractorExample["Yellow and Black" by { Colors } ] };

// The next statement returns nothing (nil) because
// 'Octagon' is not a value in the key column
// and the by clause was not used.

select ExtractorExample["Octagon"];

// The next statement (which is commented out)
// will cause an error to be thrown at run time
// because it results in more than on row.

// select ExtractorExample["Red and White" by { Colors } ].Sign;

// The next statement works because it returns only one row. 

select ExtractorExample["Triangle", "Red and White" by { Shape, Colors } ].Sign;

drop table ExtractorExample;
```

```d4
Yield signs are Red and White.

Octagon shaped signs are Stop signs.

Sign   Shape    Colors           
------ -------- ---------------- 
School Pentagon Yellow and Black 

<No Value>

Row extractor expression must reference a table expression with
 at most one row.
Use a restriction or quota query to limit the number of rows in
 the source table expression.

Yield
```

**See Also**

[Indexer Expression](../../../developers-guide/d4-language-elements.md#indexer-expression) \| [System.iIndexer](System.iLeftJoin.md) \| [System.String Scalar Type](../types/System.String.md) \| [Using Lists](../../../developers-guide/modeling-process-logic.md#using-lists) \| [Qualifier Expression](../../../developers-guide/d4-language-elements.md#qualified-expression)

## System.iIndexer (List)

**Remarks**

`[]` is the parser recognized symbol for `iIndexer`. `iIndexer` is an extractor operator.

**See**

[System.iIndexer](System.iIndexer.md)

## System.iIndexer (String)

**Remarks**

`[]` is the parser recognized symbol for `iIndexer`. `iIndexer` is an extractor operator.

**See**

[System.iIndexer](System.iIndexer.md)

## System.iIndexer (Table)

**Remarks**

`[]` is the parser recognized symbol for `iIndexer`. `iIndexer` is an extractor operator.

**See**

[System.iIndexer](System.iIndexer.md) \| [Indexer Expression](../../../developers-guide/d4-language-elements.md#indexer-expression)
