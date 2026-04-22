@echo off
rem
rem	Perform a clean restore and build.
rem	Run all unit tests.
rem	Create a NuGet library.
rem	Publish a standalone application.
rem	Create an app bundle for the Microsoft Store.
rem

setlocal EnableExtensions EnableDelayedExpansion

if NOT EXIST "LicenseManagerX.slnx" (
  echo Run this script from the solution folder.
  goto :EOF
)

echo.
echo To avoid errors on locked files and folders, such as PackageLayout, etc.:
echo    - Pause Onedrive
echo    - Exit Visual Studio
echo.
echo Update the version in:
echo    - Directory.Build.props
echo    - Directory.Packages.props
echo    - LicenseManagerX.Package\Package.appxmanifest

::
:: SETUP
::

set PROJECT=LicenseManagerX
set ARCHIVE_NAME=%PROJECT%
set BUILD_OUTPUT_ROOT=C:\VSIntermediate\%PROJECT%
set ARTIFACTS_PATH=%BUILD_OUTPUT_ROOT%\artifacts
set PUBLISH_MSIX_PATH=%BUILD_OUTPUT_ROOT%\AppPackages
set PUBLISH_FILES_PATH=%BUILD_OUTPUT_ROOT%\publish
set DIRECTORY_BUILD_PROPS=.\Directory.Build.props
set DIRECTORY_PACKAGES_PROPS=.\Directory.Packages.props
set PROJECT_APP_PATH=.\%PROJECT%\%PROJECT%.csproj
set TARGET_EXE_PATH=%ARTIFACTS_PATH%\bin\%PROJECT%\release_win-x64\%PROJECT%.exe
set PROJECT_TESTS_PATH=.\%PROJECT%.UnitTests\%PROJECT%.UnitTests.csproj
set PROJECT_WAP_DIR=.\%PROJECT%.Package
set PROJECT_WAP_PATH=%PROJECT_WAP_DIR%\%PROJECT%.Package.wapproj
set PROJECT_MANIFEST_PATH=%PROJECT_WAP_DIR%\Package.appxmanifest

set PROJECT_NUGET_PATH=LicenseManager_12noon.Client\LicenseManager_12noon.Client.csproj
set PROJECT_EXAMPLE_PATH=.\%PROJECT%_Example\%PROJECT%_Example.csproj
set PROJECT_CONSOLE_PATH=.\%PROJECT%.Console\%PROJECT%.Console.csproj
set TARGET_EXAMPLE_PATH=%ARTIFACTS_PATH%\bin\%PROJECT%_Example\release_win-x64\%PROJECT%_Example.exe
set TARGET_CONSOLE_PATH=%ARTIFACTS_PATH%\bin\%PROJECT%.Console\release_win-x64\%PROJECT%.Console.exe

set LOCAL_NUGET_SOURCE_NAME=LocalPackages
set LOCAL_NUGET_CONFIG_DIR=%BUILD_OUTPUT_ROOT%\nuget-temp
set LOCAL_NUGET_CONFIG_PATH=%LOCAL_NUGET_CONFIG_DIR%\nuget.config

::
:: LOCATE MSBuild.exe
::

REM C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64
set VSWHERE_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set MSBUILD_EXE=
if exist "%VSWHERE_EXE%" (
	for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\amd64\MSBuild.exe`) do (
		if not defined MSBUILD_EXE set "MSBUILD_EXE=%%I"
	)
)

if not defined MSBUILD_EXE (
	for /f "usebackq delims=" %%I in (`where msbuild.exe 2^>nul`) do (
		if not defined MSBUILD_EXE set "MSBUILD_EXE=%%I"
	)
)

if not defined MSBUILD_EXE (
	echo ERROR: Unable to find MSBuild.exe using vswhere or PATH.
	exit /b 1
)

echo.
echo Using MSBuild: "%MSBUILD_EXE%"

::
:: PRE-BUILD CHECKS
::

echo.
echo === COMPARE VERSIONS IN Directory.Packages.props AND Directory.Build.props AND Package.appxmanifest ===
echo.

rem === 1. Locate the Directory.Build.props file ===
if not exist "%DIRECTORY_BUILD_PROPS%" (
	echo ERROR: Version file not found: %DIRECTORY_BUILD_PROPS%
	exit /b 1
)

rem === 2. Extract VersionPrefix from Directory.Build.props ===
for /f "usebackq delims=" %%V in (`
	powershell -NoLogo -NoProfile -Command ^
		"[xml]$x = Get-Content '%DIRECTORY_BUILD_PROPS%'; $x.Project.PropertyGroup.VersionPrefix"
`) do set "VERSION_PREFIX=%%V"

if "%VERSION_PREFIX%" == "" (
	echo ERROR: VersionPrefix not found in %DIRECTORY_BUILD_PROPS%
	exit /b 1
)

rem === 3. Locate the Directory.Packages.props file ===
if not exist "%DIRECTORY_PACKAGES_PROPS%" (
	echo ERROR: Version file not found: %DIRECTORY_PACKAGES_PROPS%
	exit /b 1
)

rem === 4. Extract Version from Directory.Package.props ===
for /f "usebackq delims=" %%V in (`
	powershell -NoLogo -NoProfile -Command ^
		"[xml]$x = Get-Content '%DIRECTORY_PACKAGES_PROPS%';" ^
		"$node = $x.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq 'LicenseManager_12noon.Client' };" ^
		"if ($node) { $node.Version }"
`) do set "NUGET_VERSION=%%V"

if "%NUGET_VERSION%" == "" (
	echo ERROR: Version not found in %DIRECTORY_PACKAGES_PROPS%
	exit /b 1
)

rem === 5. Locate the manifest ===
if not exist "%PROJECT_MANIFEST_PATH%" (
	echo ERROR: Manifest not found: %PROJECT_MANIFEST_PATH%
	exit /b 1
)

rem === 6. Extract Identity.Version from manifest ===
for /f "usebackq delims=" %%V in (`
	powershell -NoLogo -NoProfile -Command ^
		"[xml]$x = Get-Content '%PROJECT_MANIFEST_PATH%'; $x.Package.Identity.Version"
`) do set "MANIFEST_VERSION=%%V"

if "%MANIFEST_VERSION%" == "" (
	echo ERROR: Identity.Version not found in manifest
	exit /b 1
)

rem === 7. Compare all versions (in A.B.C.0 format) ===
set EXPECTED=%VERSION_PREFIX%.0
set EXPECTED_NUGET=%NUGET_VERSION%.0
set SAME_VERSIONS=1
if "%MANIFEST_VERSION%" neq "%EXPECTED%" set SAME_VERSIONS=0
if "%EXPECTED_NUGET%" neq "%EXPECTED%" set SAME_VERSIONS=0
if "%SAME_VERSIONS%" == "0" (
	echo ERROR: Version mismatch.
	echo   Directory.Build.props:    %VERSION_PREFIX%
	echo   Directory.Packages.props: %NUGET_VERSION%
	echo   Package.appxmanifest:     %MANIFEST_VERSION%
	exit /b 1
)

echo Version check successful: %MANIFEST_VERSION% matches %EXPECTED% matches %EXPECTED_NUGET%

set VERSION=%VERSION_PREFIX%

echo.
choice /c YN /n /m "Press N to quit, Y to continue: "
if errorlevel 2 (
	echo Quitting...
	exit /b 0
)

::
:: BUILD
::

echo.
echo === DOTNET CLEAN ===
dotnet clean "%PROJECT_NUGET_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet clean "%PROJECT_APP_PATH%"     --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet clean "%PROJECT_CONSOLE_PATH%" --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet clean "%PROJECT_TESTS_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet clean "%PROJECT_EXAMPLE_PATH%" --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET RESTORE ===
dotnet restore "%PROJECT_NUGET_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet restore "%PROJECT_APP_PATH%"     --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet restore "%PROJECT_CONSOLE_PATH%" --runtime win-x64
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet restore "%PROJECT_TESTS_PATH%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET BUILD RELEASE ===
dotnet build ^
	"%PROJECT_NUGET_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

dotnet build ^
	"%PROJECT_APP_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

dotnet build ^
	"%PROJECT_CONSOLE_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === VERIFY TARGET PROPERTIES ===
if exist "%TARGET_EXE_PATH%" (
	sigcheck.exe -nobanner "%TARGET_EXE_PATH%"
) else (
	echo File does not exist: "%TARGET_EXE_PATH%"
	exit /b
)
if exist "%TARGET_CONSOLE_PATH%" (
	sigcheck.exe -nobanner "%TARGET_CONSOLE_PATH%"
) else (
	echo File does not exist: "%TARGET_CONSOLE_PATH%"
	exit /b
)

::
:: TESTS
::

echo.
echo === DOTNET BUILD UNIT TESTS ===
dotnet build ^
	"%PROJECT_TESTS_PATH%" ^
	--configuration Release ^
	--no-restore

if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === DOTNET TEST ===
dotnet test ^
	--project "%PROJECT_TESTS_PATH%" ^
	--configuration Release ^
	--no-restore ^
	--no-build ^
	--no-ansi ^
	--no-progress ^
	--output detailed

if errorlevel 1 exit /b %ERRORLEVEL%

::
:: NUGET LIBRARY
::

echo.
echo === PACK (NuGet library) ===
dotnet pack ^
	"%PROJECT_NUGET_PATH%" ^
	--configuration Release ^
	--version %VERSION% ^
	--no-restore ^
	--no-build ^
	--output "%PUBLISH_FILES_PATH%"

if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo === CREATE TEMP LOCAL NUGET SOURCE ===
if exist "%LOCAL_NUGET_CONFIG_DIR%" rmdir /s /q "%LOCAL_NUGET_CONFIG_DIR%"
mkdir "%LOCAL_NUGET_CONFIG_DIR%"
if errorlevel 1 exit /b %ERRORLEVEL%

dotnet new nugetconfig --force --output "%LOCAL_NUGET_CONFIG_DIR%"
if errorlevel 1 (
	call :cleanup_local_nuget
	exit /b %ERRORLEVEL%
)

dotnet nuget add source ^
	"%PUBLISH_FILES_PATH%" ^
	--name "%LOCAL_NUGET_SOURCE_NAME%" ^
	--configfile "%LOCAL_NUGET_CONFIG_PATH%"
if errorlevel 1 (
	call :cleanup_local_nuget
	exit /b %ERRORLEVEL%
)

echo.
echo === RESTORE EXAMPLE (Local NuGet source) ===
dotnet restore ^
	"%PROJECT_EXAMPLE_PATH%" ^
	--runtime win-x64 ^
	--configfile "%LOCAL_NUGET_CONFIG_PATH%"
if errorlevel 1 (
	call :cleanup_local_nuget
	exit /b %ERRORLEVEL%
)

echo.
echo === BUILD EXAMPLE ===
dotnet build ^
	"%PROJECT_EXAMPLE_PATH%" ^
	--configuration Release ^
	--no-restore
set BUILD_EXAMPLE_EXIT_CODE=%ERRORLEVEL%
call :cleanup_local_nuget
if not "%BUILD_EXAMPLE_EXIT_CODE%" == "0" exit /b %BUILD_EXAMPLE_EXIT_CODE%

if exist "%TARGET_EXAMPLE_PATH%" (
	sigcheck.exe -nobanner "%TARGET_EXAMPLE_PATH%"
) else (
	echo File does not exist: "%TARGET_EXAMPLE_PATH%"
	exit /b 1
)

::
:: PUBLISH
::

echo.
echo === DOTNET PUBLISH (Standalone) ===
dotnet publish ^
	"%PROJECT_APP_PATH%" ^
	--configuration Release ^
	--no-restore ^
	--property:Platform=x64 ^
	--property:RuntimeIdentifier=win-x64 ^
	--property:PublishProtocol=FileSystem ^
	--property:SelfContained=false ^
	--property:PublishReadyToRun=false ^
	--property:PublishTrimmed=false ^
	--property:PublishSingleFile=true ^
	--property:PublishDir="%PUBLISH_FILES_PATH%"

if errorlevel 1 exit /b %ERRORLEVEL%

dotnet publish ^
	"%PROJECT_EXAMPLE_PATH%" ^
	--configuration Release ^
	--no-restore ^
	--property:Platform=x64 ^
	--property:RuntimeIdentifier=win-x64 ^
	--property:PublishProtocol=FileSystem ^
	--property:SelfContained=false ^
	--property:PublishReadyToRun=false ^
	--property:PublishTrimmed=false ^
	--property:PublishSingleFile=true ^
	--property:PublishDir="%PUBLISH_FILES_PATH%"

if errorlevel 1 exit /b %ERRORLEVEL%

dotnet publish ^
	"%PROJECT_CONSOLE_PATH%" ^
	--configuration Release ^
	--no-restore ^
	--property:Platform=x64 ^
	--property:RuntimeIdentifier=win-x64 ^
	--property:PublishProtocol=FileSystem ^
	--property:SelfContained=false ^
	--property:PublishReadyToRun=false ^
	--property:PublishTrimmed=false ^
	--property:PublishSingleFile=true ^
	--property:PublishDir="%PUBLISH_FILES_PATH%"

if errorlevel 1 exit /b %ERRORLEVEL%

pushd "%PUBLISH_FILES_PATH%"
nanazipc.exe u -tzip "%BUILD_OUTPUT_ROOT%\%ARCHIVE_NAME%_%VERSION%.zip" *.*
popd

echo.
echo === DOTNET PUBLISH (MS Store) ===
"%MSBUILD_EXE%" ^
	"%PROJECT_WAP_PATH%" ^
	-property:Configuration=Release ^
	-property:Platform=x64 ^
	-property:UapAppxPackageBuildMode=StoreUpload ^
	-property:AppxPackageDir="%PUBLISH_MSIX_PATH%" ^
	-verbosity:quiet

if errorlevel 1 exit /b %ERRORLEVEL%

echo Publish successful.

endlocal
exit /b 0

::
:: Delete the temporary local NuGet source and config.
::
:cleanup_local_nuget
if exist "%LOCAL_NUGET_CONFIG_PATH%" (
	dotnet nuget remove source "%LOCAL_NUGET_SOURCE_NAME%" --configfile "%LOCAL_NUGET_CONFIG_PATH%" >nul 2>nul
)
if exist "%LOCAL_NUGET_CONFIG_DIR%" rmdir /s /q "%LOCAL_NUGET_CONFIG_DIR%"
exit /b 0
