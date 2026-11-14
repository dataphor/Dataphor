cd ..\tmp
attrib -r *.xml
..\CopyTo.exe source=C:\DocBuild\Alphora\DocLibrary\,destination=c:\DocBuild\tmp\,mask=*.xml,StripDoctype=yes,silent=yes
cd ..\bat
