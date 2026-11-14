# Lexical Elements

![Dataphor Logo](../images/dataphor-logo.png)

## Summary

The lexical elements of the D4 language allow special symbols (or tokens) in an input string to be recognized. These tokens help the parser determine the syntax of a given statement. A complete BNF reference for the D4 lexical analyzer can be found in [D4 BNF Lanquage Diagrams](../reference/d4-grammar.md#summary).

## Whitespace

The D4 language, like most computer languages, reserves certain characters as "whitespace". This means that the characters are only used to delimit tokens. The following D4 statements are syntactically equivalent:

```d4
X := 5 * 3;

X:= 5*      3       ;

X :=
5
*
3;

X:=5*3;
```

The following Unicode character values are considered whitespace in D4: 0x0009 (tab), 0x000a (line feed), 0x000b (vertical tab), 0x000c (form feed), 0x000d (carriage return), 0x0085, 0x2028, and 0x2029.

## Comments

Comments are sections of text within D4 code that are ignored by the compiler. Block comments allow for multi-line annotations, whereas line comments are terminated by the line break. Block comments in D4 can be nested, which allows for sections of code to be easily and temporarily "commented out". The following are some examples of D4 comments:

```d4
/* This is a multi
   line... /* nested */ ...comment */

X := 5; // Assigns 5 to variable X
```

## Keywords

A *keyword* is a special symbol used by the parser to delimit syntactic structure. For example, the keyword **begin** is used to delimit the beginning of a statement block. To avoid ambiguity while parsing, some keywords are also *reserved words*. Reserved words are keywords that cannot be used as identifiers because the parser would not be able to distinguish between an identifier and the keyword.

The following is a list of all keywords in D4. Keywords with an asterisk (\*) denote reserved words. The link provided will show one possible use of the keyword however there may be more. Use the help search to find all uses of a keyword.

**\*[add](../reference/system-library/operators/System.iExtend.md)**,

**\*[adorn](../reference/system-library/operators/System.iAdorn.md)**,

[after](d4-catalog.md#event-handlers)

**[aggregate](../reference/system-library/operators/System.iAggregate.md)**,

[aggregation](d4-catalog.md#aggregate-operators)

**[all](../reference/system-library/operators/System.All.md)**,

[alter](../reference/d4-grammar.md#d4-parser-bnf)

**\*[and](../reference/system-library/operators/System.iAnd.md)**,

[apply](d4-catalog.md#security-1)

[as](d4-language-elements.md#type-operator-clause)

[asc](d4-catalog.md#orders)

[attach](d4-catalog.md#event-handlers)

[attributes](d4-catalog.md#class-definitions)

[before](d4-catalog.md#event-handlers)

[begin](d4-imperative-statements.md#blocks-and-scoping)

[between](d4-language-elements.md#between-clause)

[break](d4-imperative-statements.md#break-and-continue)

**[browse](../reference/system-library/operators/System.iBrowse.md)**,

[by](../reference/d4-grammar.md#d4-parser-bnf)

[capabilities](../reference/d4-grammar.md#d4-parser-bnf)

[cascade](d4-catalog.md#references-1)

[case](d4-imperative-statements.md#case-statement)

[change](d4-catalog.md#event-handlers)

[class](d4-catalog.md#class-definitions)

[clear](d4-catalog.md#references-1)

[column](../reference/d4-grammar.md#d4-parser-bnf)

[commit](d4-imperative-statements.md#try-commit-statement)

[const](d4-catalog.md#creating-operators)

[constraint](d4-catalog.md#constraints)

[continue](d4-imperative-statements.md#break-and-continue)

[conversion](d4-catalog.md#conversions)

[create](../reference/d4-grammar.md#d4-parser-bnf)

[cursor](d4-language-elements.md#cursor-values)

[default](d4-catalog.md#defaults)

[delete](d4-catalog.md#references-1)

[desc](d4-catalog.md#orders)

[detach](d4-catalog.md#event-handlers)

[device](d4-catalog.md#device)

[distinct](d4-table-operators.md#aggregate)

[div](d4-language-elements.md#arithmetic-expression)

[divide](../reference/d4-grammar.md#d4-parser-bnf)

[do](d4-imperative-statements.md#do-while-statement)

[downto](d4-imperative-statements.md#for-statement)

[drop](d4-catalog.md#summary)

[dynamic](../reference/d4-grammar.md#d4-parser-bnf)

[else](d4-imperative-statements.md#if-statement)

[end](d4-language-elements.md#between-clause)

[except](d4-imperative-statements.md#try-except-statement)

[exclude](d4-catalog.md#orders)

**\*[exists](../reference/system-library/operators/System.iExists.md)**

[exit](d4-imperative-statements.md#exit-statement)

[explode](d4-table-operators.md#summary)

[false](#boolean)

[finalization](d4-catalog.md#aggregate-operators)

[finally](d4-imperative-statements.md#try-finally-statement)

[for](d4-imperative-statements.md#for-statement)

[from](d4-language-elements.md#indexer-expression)

[generic](d4-language-elements.md#types)

[grant](d4-catalog.md#security-1)

[group](d4-table-operators.md#aggregate)

[having](d4-table-operators.md#semijoin)

[if](d4-imperative-statements.md#if-statement)

**\*[in](../reference/system-library/operators/System.iIn.md)**

[include](../reference/d4-grammar.md#d4-parser-bnf)

[index](../reference/d4-grammar.md#d4-parser-bnf)

[indexes](../reference/d4-grammar.md#d4-parser-bnf)

[inherited](d4-catalog.md#security-1)

[initialization](d4-catalog.md#aggregate-operators)

[insert](d4-data-manipulation.md#insert-statement)

[intersect](d4-table-operators.md#summary)

[into](d4-data-manipulation.md#insert-statement)

[invoke](d4-catalog.md#event-handlers)

[is](d4-language-elements.md#type-operator-clause)

[isolation](../reference/d4-grammar.md#d4-parser-bnf)

[join](d4-table-operators.md#summary)

[keys](d4-catalog.md#keys)

[left](d4-table-operators.md#outer-join)

[level](d4-table-operators.md#explode)

[like](d4-catalog.md#like-types)

[list](d4-language-elements.md#list-types)

[lookup](d4-table-operators.md#join)

[master](d4-catalog.md#device)

**[matches](../reference/system-library/operators/System.iMatches.md)**

[minus](d4-table-operators.md#summary)

**[mod](../reference/system-library/operators/System.iMod.md)**

[mode](d4-catalog.md#device)

**modify**

[narrowing](d4-catalog.md#conversions)

[new](d4-catalog.md#row-constraints)

[nil](d4-language-elements.md#nil-semantics)

**[not](../reference/system-library/operators/System.iNot.md)**

[of](d4-language-elements.md#table-values)

[old](d4-catalog.md#row-constraints)

[on](d4-imperative-statements.md#try-except-statement)

[operator](d4-catalog.md#creating-operators)

**[or](../reference/system-library/operators/System.iOr.md)**

[order](d4-catalog.md#orders)

origin

[over](d4-table-operators.md#summary)

[parent](d4-language-elements.md#factor)

[raise](d4-imperative-statements.md#raise-statement)

[read](d4-catalog.md#properties)

[reconciliation](d4-catalog.md#device)

[recursively](d4-catalog.md#security-1)

[redefine](d4-table-operators.md#summary)

[reference](d4-catalog.md#references-1)

[references](d4-catalog.md#references-1)

[remove](d4-table-operators.md#summary)

[rename](d4-table-operators.md#summary)

[repeat](d4-imperative-statements.md#repeat-until-statement)

[representation](d4-catalog.md#logical-representations)

[require](d4-catalog.md#references-1)

[result](d4-catalog.md#creating-operators)

[return](d4-table-operators.md#quota)

[revert](d4-catalog.md#security-1)

[revoke](d4-catalog.md#security-1)

[right](d4-table-operators.md#outer-join)

[role](d4-catalog.md#security-1)

[row](d4-language-elements.md#indexer-expression)

[rowexists](d4-table-operators.md#outer-join)

[scalar](d4-language-elements.md#scalar-types)

[select](d4-data-manipulation.md#select-statement)

[selector](d4-catalog.md#logical-representations)

[sequence](d4-table-operators.md#explode)

[session](d4-catalog.md#creating-operators)

[set](d4-catalog.md#references-1)

[sort](d4-catalog.md#orders)

**\*source**

[special](d4-catalog.md#specials)

[static](../reference/d4-grammar.md#d4-parser-bnf)

[do](d4-imperative-statements.md#for-statement)

[store](../reference/d4-grammar.md#d4-parser-bnf)

[table](d4-table-operators.md#summary)

[tags](../reference/d4-grammar.md#d4-parser-bnf)

**\*target**

[then](d4-imperative-statements.md#case-statement)

[times](d4-table-operators.md#summary)

[to](d4-catalog.md#conversions)

[transition](d4-catalog.md#row-constraints)

[true](../reference/d4-grammar.md#d4-lexer-bnf)

[try](d4-imperative-statements.md#exception-handling)

[type](d4-catalog.md#scalar-types)

[typeof](d4-catalog.md#scalar-types)

[union](d4-table-operators.md#summary)

[until](d4-imperative-statements.md#repeat-until-statement)

[update](d4-data-manipulation.md#update-statement)

[usage](d4-catalog.md#security-1)

[user](d4-catalog.md#security-1)

[users](d4-catalog.md#security-1)

[using](d4-catalog.md#sorts)

[validate](d4-catalog.md#event-handlers)

**\*value**

[var](d4-imperative-statements.md#variable-declaration-statement)

[view](d4-catalog.md#views)

[when](d4-imperative-statements.md#case-statement)

[where](d4-table-operators.md#summary)

[while](d4-imperative-statements.md#looping-statements)

[widening](d4-catalog.md#conversions)

[with](d4-language-elements.md#language-modifiers)

[without](d4-table-operators.md#semiminus)

[write](d4-catalog.md#properties)

**\*[xor](../reference/system-library/operators/System.iXor.md)**

## Symbols

The D4 language also includes several special symbols that are used by the parser to delimit syntatic structure. These include parentheses, brackets, operator symbols, and other characters that have specific meaning within statements of D4. None of these symbols may be used in identifier names.

The following are parser-recognized symbols in D4.

[-](../reference/system-library/operators/System.iSubtraction.md)

[\$](#money)

[&](../reference/system-library/operators/System.iBitwiseAnd.md)

[(](d4-language-elements.md#operator-invocation)

[)](d4-language-elements.md#operator-invocation)

[\*](../reference/system-library/operators/System.iMultiplication.md)

[\*\*](../reference/system-library/operators/System.iPower.md)

[,](../reference/d4-grammar.md#d4-lexer-bnf)

[.](#qualified-identifiers)

[/](../reference/system-library/operators/System.iDivision.md)

[:](d4-language-elements.md#variables)

[:= ](d4-imperative-statements.md#assignment-statement)

[;](d4-scripts-and-execution.md#summary)

[?=](../reference/system-library/operators/System.iCompare.md)

[\[](../reference/system-library/operators/System.iIndexer.md)

[\]](../reference/system-library/operators/System.iIndexer.md)

[^](../reference/system-library/operators/System.iBitwiseXor.md)

[{](d4-language-elements.md#list-values)

[\|](../reference/system-library/operators/System.iBitwiseOr.md)

[}](d4-language-elements.md#list-values)

[~](../reference/system-library/operators/System.iBitwiseNot.md)

[+](../reference/system-library/operators/System.iAddition.md)

[\<](../reference/system-library/operators/System.iLess.md)

[\<\<](../reference/system-library/operators/System.iShiftLeft.md)

[\<=](../reference/system-library/operators/System.iInclusiveLess.md)

[\<\>](../reference/system-library/operators/System.iNotEqual.md)

[=](../reference/system-library/operators/System.iEqual.md)

[\>](../reference/system-library/operators/System.iGreater.md)

[\>=](../reference/system-library/operators/System.iInclusiveGreater.md)

[\>\>](../reference/system-library/operators/System.iShiftRight.md)

## Parser Literals

A *parser literal* is a value which is directly understood by the lexer as a token. For example the symbol 5 is a parser literal which represents the System.Integer value 5. The following types of parser literals are available in D4:

- Boolean
- Integer
- Decimal
- Money
- String

Here are some examples of parser literals within D4:

```d4
"Welcome to the ""community"" website."
'"Hello," she said.'
135
332.12d
31415926535897932e-16
$40.00
true
```

### Boolean

The boolean parser literal allows values of type [System.Boolean](../reference/system-library/types/System.Boolean.md) to be represented directly within D4.

The boolean parser literal in D4 has the following syntax:

```bnf
<boolean parser literal> ::=
    true | false
```

### Integer

The integer parser literal allows values of type [System.Integer](../reference/system-library/types/System.Integer.md) to be represented directly within D4. Integer values can be specified as a base 10 number using decimal digits, or as a base 16 number using hex digits. Base 16 representations must be prefixed with the symbol 0x.

The integer parser literal in D4 has the following syntax:

```bnf
<integer parser literal> ::=
    <digit>{<digit>} | 0x<hexdigit>{<hexdigit>}
```

### Decimal

The decimal parser literal allows values of type [System.Decimal](../reference/system-library/types/System.Decimal.md) to be represented directly within D4. Note that a sequence of digits alone will be interpreted as a value of type System.Integer so the trailing d must be used.

The decimal parser literal in D4 has the following syntax:

```bnf
<decimal parser literal> ::=
    <digit>{<digit>}[.{<digit>}][(e|E)[+|-]{<digit>}][d]
```

### Money

The money parser literal allows values of type [System.Money](../reference/system-library/types/System.Money.md) to be represented directly within D4.

The money parser literal in D4 has the following syntax:

```bnf
<money parser literal> ::=
    $<digit>{<digit>}[.{<digit>}]
```

The \$ symbol is just a symbol for the compiler to identify a data type of money. It does *not* specifiy the currency used, i.e. dollars.

### String

The string parser literal allows values of type [System.String](../reference/system-library/types/System.String.md) to be represented directly within D4. Note that the straight single quote character (') or the straight double quote character (") can be used to delimit a string. Do not use curved quote characters. Within the string, the delimiting quote character can be represented by double quoting.

The string parser literal in D4 has the following syntax:

```bnf
<string parser literal> ::=
    ""{<character>}"" | '{<character>}'
```

## Identifiers

Identifiers are user-defined names for catalog objects such as variables and types.

D4 identifiers have the following syntax:

```bnf
<identifier> ::=
    _ | <letter> {_ | <letter> | <digit>}
```

Here is an example of a valid D4 identifier:

```d4
Customers
```

### Qualified Identifiers

The D4 language uses the concept of namespaces to allow identifiers to be named more completely, yet accessed more concisely. An identifier that utilizes namespaces is called a qualified identifier because it is prefixed by one or more identifiers called qualifiers.

Qualified identifiers have the following syntax:

```bnf
<qualified identifier> ::=
    [.]{<identifier>.}<identifier>
```

Name resolution with qualified identifiers is based on the notion of *name equivalence*. A given name is equivalent to another name if and only if it is equal, case-sensitively, to some unqualified version of the name. Thus:

- A is equivalent to A
- A is equivalent to A.A and B.A, but not A.B
- A.A is equivalent to A.A, but not B.A

When attempting to resolve a name reference against a list of names such as the set of columns in a table, if the reference is equivalent to more than one name in the list, the reference is considered ambiguous.

The following is an example of a qualified identifier:

```d4
MyCompany.MyProduct.Customers
```

### Disambiguating Identifiers

Unless ambiguous, schema objects can be accessed using their unqualified names. Names must be qualified only to the point where they are no longer ambiguous, but may be qualified more completely if desired.

The following example illustrates the use of namespaces in D4:

```d4
var MyCompany.MyProduct.MyVariable : Integer;
var MyCompany.OtherProduct.MyVariable : Integer;
MyVariable := 5;  // Error, MyVariable must be disambiguated
OtherProduct.MyVariable := 5; // Valid
MyCompany.MyProduct.MyVariable := 6;  // Also valid
```

The root of the namespace can be accessed using a dot qualifier with no preceding identifier as follows:

```d4
var .I : Integer;
.I := 5;
```

## Case

D4 is a case-sensitive language, meaning that the symbols and identifiers read by the compiler will be compared case-sensitively. In other words, the symbol A is different than the symbol a. The following code sample illustrates this behavior.

```d4
begin
    var I : Integer;
    I := Length("Relational"); // valid reference
    i := Length("Relational"); // unknown identifier
end;
```

Because D4 is case-sensitive, Alphora recommends the use of Pascal-casing for all identifiers. In Pascal-casing, the first letter of each word in the identifier is capitalized, for example PascalCasing. This allows identifiers to have a completely open identifier space because all keywords are all lower case. For example, value is not a valid identifier because it conflicts with the reserved word **value**, but Value is a valid identifier.

> [!NOTE]
> Most SQL-based systems are case-insensitive, so be careful not to rely on casing for identifier resolution, as it could lead to problems when translating into the various dialects of SQL.
