cd ..\alphora\codedoc
rem breakout doc assemblies
c:\windows\system32\java.exe -Xmx550000000  -jar c:/docbuild/java/saxon.jar DataphorRefndoc.xml ../../xsl/AssemblyBreakout.xsl

rem break out library assemblies
c:\windows\system32\java.exe -Xmx550000000  -jar c:/docbuild/java/saxon.jar LibraryRefndoc.xml ../../xsl/AssemblyBreakout.xsl

cd ..\..\
rem DAE
c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar -o Alphora/docbook/DAE.xml   Alphora/codedoc/Alphora.Dataphor.DAE.xml todocbookxsl/ToDocBook.xslt namespacename=Alphora.Dataphor.DAE dofull=yes

rem DAEClient
c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar -o Alphora/docbook/DaeClient.xml  Alphora/codedoc/Alphora.Dataphor.DAE.Client.xml todocbookxsl/ToDocBook.xslt namespacename=Alphora.Dataphor.DAE.Client dofull=yes

rem DilRef total controls for 2.0
c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar -o Alphora/docbook/DILRef.xml Alphora/codedoc/DataphorRefndoc.xml todocbookxsl/DilrefDoc.xslt namespacename=Alphora.Dataphor.Frontend.Client dofull=yes
c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar  -o Alphora/docbook/DILReference.xml Alphora/docbook/DILRef.xml Alphora/docbook/dilref.xml xsl/SortSect2.xsl

rem DilRefAsControls (for 2.0)
rem c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar -o Alphora/docbook/DILReference.xml Alphora/codedoc/DataphorRefndoc.xml todocbookxsl/DILControlRefCodeDoc.xslt

rem DilRef (for 1.0)
rem c:\windows\system32\java.exe -Xmx550000000  -jar java/saxon.jar -o Alphora/docbook/DILReference.xml Alphora/codedoc/DataphorRefndoc.xml todocbookxsl/DILRefCodeDoc.xslt

pause
