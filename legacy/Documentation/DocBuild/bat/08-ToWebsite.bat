cd ..\tmp\web
attrib -r *.html
del *.html
..\..\ToWebsite.exe source=c:\DocBuild\tmp\html\,destination=c:\DocBuild\tmp\Web\,mask=*.html,Header=c:\DocBuild\xsl\NavHeader.sqlt,Footer=c:\DocBuild\xsl\NavFooter.sqlt,silent=yes
cd ..\..\bat
pause