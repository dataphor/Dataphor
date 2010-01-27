@echo off

echo Dataphor Setup Creation Process

if "%1"=="" goto :ParamError
if "%2"=="" goto :ParamError

del output\*.* /q

echo .
echo Updating the Dataphor WXS source file
UpdateWix /B:"..\Deploy\Dataphor" Base.wxs %1 %2 %3 %4 %5 %6 %7
if errorlevel 1 goto ReportError

echo .
echo Compiling Dataphor WiX file...
wix\candle -nologo -sw -out output\Base.wixobj Base.wxs
if errorlevel 1 goto ReportError

echo .
echo Linking Dataphor WiX file...
wix\light -nologo -sw -b ..\Deploy\Dataphor -cc ".\output" -ext "DataphorWixBinderExtension.DataphorWixBinderExtension,DataphorWixBinderExtension" -out output\Setup.msi output\Base.wixobj wix\sca.wixlib
if errorlevel 1 goto ReportError


echo Deleting output temps
del output\base* /q

echo .
echo Completed successfully

echo Now running DEDKProcess.bat

call DEDKProcess.bat %1 %2 %3 %4 %5 %6 %7
if errorlevel 1 goto ReportError

goto End

:ParamError

echo Must pass /S:<shortversion> /L:<longversion>

:ReportError

echo .
echo An error occurred.
pause

:End