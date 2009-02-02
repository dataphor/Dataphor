cd ..\tmp
attrib -r *.xml
..\CopyTo.exe source=c:\src\alphora\docs\Docs2.0\,destination=c:\DocBuild\tmp\,mask=*.xml,StripDoctype=yes,silent=yes
copy c:\src\alphora\docs\Docs2.0\DataphorSet.xml
cd ..\bat
