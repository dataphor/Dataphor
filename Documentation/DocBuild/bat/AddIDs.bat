rem ..\bin\saxon -o test.xml   %1.xml ../AddIDs.xsl id-prefix=%2
c:\windows\system32\java.exe -Xmx550000000  -jar c:/docbuild/java/saxon.jar  -o %1-ID.xml %1.xml c:/docbuild/xsl/AddIDs.xsl id-prefix=%2
rem insert this for nbsp problem: <!DOCTYPE part [ <!ENTITY nbsp "&#x00A0;" > ]>