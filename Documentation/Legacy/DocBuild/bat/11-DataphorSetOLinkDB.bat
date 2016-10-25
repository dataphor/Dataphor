rem @ECHO OFF
cd ..\tmp\olink
del *.html

del DUGTargets.xml
del DDGTargets.xml
del RefTargets.xml

c:\windows\system32\java.exe -Xmx550000000 -jar ../../java/saxon.jar ../DataphorUserGuide.xml  ../../tohtmlhelp/html/docbook.xsl root.filename=DataphorUsersGuide collect.xref.targets=only targets.filename=DUGTargets.xml
c:\windows\system32\java.exe -Xmx550000000 -jar ../../java/saxon.jar ../DataphorDevGuide.xml  ../../tohtmlhelp/html/docbook.xsl root.filename=DataphorDevGuide collect.xref.targets=only targets.filename=DDGTargets.xml
c:\windows\system32\java.exe -Xmx550000000 -jar ../../java/saxon.jar ../DataphorReference.xml  ../../tohtmlhelp/html/docbook.xsl root.filename=DataphorReference collect.xref.targets=only targets.filename=RefTargets.xml


pause
cd ..\..\bat