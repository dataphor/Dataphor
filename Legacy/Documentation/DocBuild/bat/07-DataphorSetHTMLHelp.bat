cd ..\tmp\html
del *.html
c:\windows\system32\java.exe -Xmx500000000 -jar ../../java/saxon.jar ../DataphorSet.xml  ../../tohtmlhelp/htmlhelp/AlphoraHelp.xsl root.filename=DataphorSet  target.database.document=DataphorSetOlinkdb.xml current.docid=DataphorSet
c:\windows\system32\java.exe -Xmx500000000 -jar ../../java/saxon.jar ../DataphorSet.xml  ../../tohtmlhelp/htmlhelp/AlphoraTOC.xsl root.filename=DataphorSet
"C:\Program Files\HTML Help Workshop\hhc.exe" Dataphor.hhp
cd ..\..\bat
pause
