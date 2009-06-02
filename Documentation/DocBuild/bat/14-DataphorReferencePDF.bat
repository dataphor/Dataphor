@ECHO OFF
cd ..\tmp
del DataphorReference.xml
c:\windows\system32\java.exe -Xmx1000000000  -jar c:/docbuild/java/saxon.jar  -o DataphorReference.xml DataphorSet.xml c:/docbuild/xsl/ExtractReference.xsl  target.database.document=/docbuild/tmp/DataphorSetOlinkdb.xml

del DataphorReference.fo
c:\windows\system32\java.exe -Xmx1000000000 -jar ../java/saxon.jar -o DataphorReference.fo DataphorReference.xml  ../tohtmlhelp/fo/alphoradocs.xsl root.filename=DataphorReference  target.database.document=/docbuild/tmp/DataphorSetOlinkdb.xml current.docid=DR

rem %~dp0 is the expanded pathname of the current script under NT
set LOCAL_FOP_HOME=c:\DocBuild\Java\
rem if "%OS%"=="Windows_NT" set LOCAL_FOP_HOME=c:\programs\fop-0.20.5\

set LIBDIR=%LOCAL_FOP_HOME%lib
set LOCALCLASSPATH=%LOCAL_FOP_HOME%fop.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\xml-apis.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\xercesImpl-2.2.1.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\xalan-2.4.1.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\batik.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\avalon-framework-cvs-20020806.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\jimi-1.0.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\jai_core.jar
set LOCALCLASSPATH=%LOCALCLASSPATH%;%LIBDIR%\jai_codec.jar
java -Xmx1000000000 -cp %LOCALCLASSPATH% org.apache.fop.apps.Fop -fo DataphorReference.fo -pdf DataphorReference.pdf

pause
cd ..\bat