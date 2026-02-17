@echo off

if NOT EXIST "LicenseManagerX.slnx" (
  echo Run this script from the solution folder.
  goto :EOF
)

echo.
echo Be sure to update the version in the Package.appxmanifest file.
echo Exit Visual Studio to avoid directory locks, such as PackageLayout, etc.
pause

setlocal

:: Prompt for version number if not passed as an argument
if "%~1" == "" (
	echo.
	echo Usage: %~nx0 ^<version^>
	exit /b 1
)

set VERSION=%~1
set OUTPUT_PATH=C:\VSIntermediate\LicenseManagerX
set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64

:: 1. Restore packages for Windows x64
REM We do not specify /p:RuntimeIdentifiers=win-x64 because it is in the csproj file(s).

dotnet clean
dotnet restore

:: 2. Build everything once
REM Note that the wapproj project must be disabled for
REM Release because only Visual Studio can build it.

dotnet build ^
	LicenseManagerX.slnx ^
	--configuration Release

:: 3. Run unit tests using MS Testing Platform
REM Note that we need <UseMicrosoftTestingPlatform>true</UseMicrosoftTestingPlatform> in the csproj for MTP.

dotnet test ^
	--solution LicenseManagerX.slnx ^
	--configuration Release

:: 4. Pack the NuGet library

dotnet pack --nologo ^
	LicenseManager_12noon.Client\LicenseManager_12noon.Client.csproj ^
	-p:Version=%VERSION% ^
	--configuration Release ^
	--output "%OUTPUT_PATH%\publish"

:: 5. Publish each app

dotnet publish ^
	LicenseManagerX\LicenseManagerX.csproj ^
	--property:PublishProfile=FolderProfile

dotnet publish ^
	LicenseManagerX.Console\LicenseManagerX.Console.csproj ^
	--property:PublishProfile=FolderProfile

dotnet publish ^
	LicenseManagerX_Example\LicenseManagerX_Example.csproj ^
	--property:PublishProfile=FolderProfile

:: 6. Publish to the Microsoft Store
REM We do not specify /p:RuntimeIdentifiers=win-x64 because it is in the csproj file(s).

"%MSBUILD_PATH%\MSBuild.exe" LicenseManagerX.Package/LicenseManagerX.Package.wapproj ^
	/p:Configuration=Release ^
	/p:Platform=x64 ^
	/p:AppxPackageDir="%OUTPUT_PATH%\AppPackages" ^
	/p:UapAppxPackageBuildMode=StoreUpload

endlocal
