Notes:
-The component's key path is the binary image for the <ServiceInstall />
----------------------------------
http://weblogs.asp.net/sweinstein/archive/2004/08/31/223461.aspx

IntelliSense for WiX .wxs files 
...
The problem is that Visual Studio, by default, doesn't recognize .wxs files as XML. The fix for that is the following

1) add to your registry

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\7.1\Editors\{C76D83F8-A489-11D0-8195-00A0C91BBEE3}\Extensions]
"wxs"=dword:00000028

2) copy wix.xsd and wixloc.xsd from the WiX/doc directory to 

C:\Program Files\Microsoft Visual Studio .NET 2003\Common7\Packages\schemas\xml\


and provided you reference the WiX namespace at http://schemas.microsoft.com/wix/2003/01/wi you'll get IntelliSense too.
