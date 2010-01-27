@echo off

echo DEDK Setup creation

if "%1"=="" goto :ParamError
if "%2"=="" goto :ParamError

:DEDK
del output\dedk* /q
del output\disk1.cab /q

echo .
echo Updating the DEDK WXS source file
UpdateWix /B:"..\Deploy\DEDK" DEDK.wxs %1 %2 %3 %4 %5 %6 %7
if errorlevel 1 goto ReportError

echo .
echo Compiling DEDK WiX file...
wix\candle -nologo -sw -out output\DEDK.wixobj DEDK.wxs
if errorlevel 1 goto ReportError

echo .
echo Linking DEDK WiX file...
wix\light -nologo -sw -b ..\Deploy\DEDK -cc ".\output" -ext "DataphorWixBinderExtension.DataphorWixBinderExtension,DataphorWixBinderExtension" -out output\DEDK.msi output\DEDK.wixobj
if errorlevel 1 goto ReportError

echo .
echo Completed successfully

goto End

:ParamError

echo Must pass /S:<shortversion> /L:<longversion>

:ReportError

echo .
echo An error occurred.
pause

:End