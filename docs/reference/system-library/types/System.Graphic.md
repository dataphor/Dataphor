# System.Graphic

**Type Declaration**

```d4
create type .System.Graphic
{
 representation Graphic
 {
  Value : System.String
 }selector  class "System.SystemBinarySelectorNode", 
 representation AsDisplayString
 {
  AsDisplayString : System.String
    }selector  class "System.GraphicAsDisplayStringSelectorNode"
} class "System.ObjectConveyor" tags { Frontend.ElementType = "Image", Frontend.Grid.ElementType = "ImageColumn" } static tags { Catalog.Comment = "System Graphic", DAE.AsDisplayString = "AsDisplayString" };
```

**Declarations**

```d4
create operator .System.Graphic(const Value : System.String) : System.Graphic
 class "System.SystemBinarySelectorNode";
```

**Remarks**

This is the selector for the DataType `Graphic`. Graphic date is data stored in its raw format. This data type is used to store images.

`System.Graphic` is the set of all byte streams of data with length less than or equal to 2,147,483,647 (2<sup>31</sup>-1) bytes.

**Using System.Graphic**

```d4
create table TestGraphic in Temp
{
 ID : Integer,
 Image : Graphic,
 key { ID }
};
// Image.Warning is a .png image file
insert table 
{ 
 row
 {
  1 ID, Image('Frontend', 'Image.Warning') Image },
  key { ID }
 } into TestGraphic;
select TestGraphic;
// The image can be viewed by selecting the table
// in Dataphoria and selecting Browse.
drop table TestGraphic;
```

```text
ID Image
-- -------------
1  <binary data>
```

**See Also**

[Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values)
