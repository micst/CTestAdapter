@echo off
setlocal enabledelayedexpansion

if not defined CTestAdapter_CMAKE (
  for %%X in (cmake.exe) do (
    set CTestAdapter_CMAKE=%%~$PATH:X
  )
)
if not exist "%CTestAdapter_CMAKE%" (
  echo =========================================================
  echo =               error finding cmake                     =
  echo =========================================================
  echo = No CMake executable found anywhere in PATH, and       =
  echo = CTestAdapter_CMAKE is not set to a CMake executable. =
  echo = Please make sure you have CMake 3.8 or later in your  =
  echo = PATH or set the CTestAdapter_CMAKE environment       =
  echo = variable to point to a valid CMake executable.        =
  echo =========================================================
  pause
  exit 1
)
echo ===============================================================
echo == using cmake from: 
echo == %CTestAdapter_CMAKE%
echo ===============================================================

if not defined VS_VERSION set VS_VERSION=15
if not defined ADDITIONAL_CMAKE_PARAMS set ADDITIONAL_CMAKE_PARAMS=

set CMAKE_SOURCEDIR=%~dp0
set CMAKE_BINARYDIR=%~dp0vs%VS_VERSION%

rem remove build directory, remove these lines
rem if you don't want to delete the build directory
rem everytime when bootstrapping
if exist "%CMAKE_BINARYDIR%" (
  rmdir /S /Q  "%CMAKE_BINARYDIR%"
)
rem setup build directory
if not exist "%CMAKE_BINARYDIR%" (
  mkdir "%CMAKE_BINARYDIR%"
)
if not exist "%CMAKE_BINARYDIR%" (
  echo could not create binary directory "%CMAKE_BINARYDIR%"
  timeout /T 10
  exit 1
)

cd "%CMAKE_BINARYDIR%"
if not %ERRORLEVEL%%==0 (
  echo could not cd to binary directory "%CMAKE_BINARYDIR%"
  timeout /T 10
  exit 1
)

"%CTestAdapter_CMAKE%"^
  -G "Visual Studio %VS_VERSION%"^
  %ADDITIONAL_CMAKE_PARAMS%^
  "%CMAKE_SOURCEDIR%"
if not %ERRORLEVEL%%==0 (
  echo error in cmake, skipping everything ...
  pause
  exit 1
)
"%CTestAdapter_CMAKE%" .

::"%CTestAdapter_CMAKE%" --build . --config Release
if not %ERRORLEVEL%%==0 (
  echo error when building ...
  pause
)
