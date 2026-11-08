rem This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
rem Copyright (c) 2026 RZeroF1.
rem If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
@echo off
setlocal
set "ARCH=%~1"
if "%ARCH%"=="" set "ARCH=x64"
set "OUT=%~2"
if "%OUT%"=="" set "OUT=%~dp0..\..\obj\native\%ARCH%"
if not exist "%OUT%" mkdir "%OUT%"
for %%I in ("%OUT%") do set "OUT=%%~fI"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSROOT=%%I"
if not defined VSROOT (
  echo Visual Studio C++ build tools are required to build the seed map.
  exit /b 1
)
call "%VSROOT%\Common7\Tools\VsDevCmd.bat" -no_logo -arch=%ARCH% -host_arch=x64
if errorlevel 1 exit /b %errorlevel%
pushd "%OUT%"
cl /nologo /O2 /MT /LD /Brepro /wd4996 /I"%~dp0upstream" "%~dp0seedmap.c" "%~dp0terrain.c" "%~dp0upstream\generator.c" "%~dp0upstream\layers.c" "%~dp0upstream\biomenoise.c" "%~dp0upstream\biomes.c" "%~dp0upstream\noise.c" "%~dp0upstream\finders.c" "%~dp0upstream\util.c" /link /Brepro /OUT:rzero_seedmap.dll
set "RESULT=%errorlevel%"
popd
exit /b %RESULT%
