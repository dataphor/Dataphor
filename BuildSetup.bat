call "%vs140comntools%..\..\VC\vcvarsall.bat"

msbuild Dataphor.proj /p:Configuration=Release /t:Rebuild

del Setup\Files.wxs

copy Setup\Clean-Files.wxs Setup\Files.wxs

msbuild Setup\Dataphor.Build\Build.sln

msbuild Setup\Setup.target

msbuild Setup\Setup.sln /p:Configuration=Release /t:Rebuild

