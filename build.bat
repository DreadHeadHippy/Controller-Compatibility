@echo off
echo Building Controller Compatibility Plugin (Test Version)...

REM Build the project
dotnet build ControllerCompatibility.csproj --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! This is a TEST version with mock Playnite SDK.
    echo.
    echo Test files are located in:
    echo %~dp0bin\Release\net462\
    echo.
    echo NOTE: This test version uses mock Playnite SDK classes.
    echo For production use, you would need the actual Playnite SDK.
    echo.
    echo The plugin demonstrates:
    echo - Controller detection and compatibility analysis
    echo - Steam-style visual overlays (UI components)
    echo - Game compatibility database
    echo - Advanced detection algorithms
    echo.
    echo To test with real Playnite:
    echo 1. Get the official Playnite SDK
    echo 2. Update project references to use real SDK
    echo 3. Remove MockPlayniteSDK.cs
    echo 4. Install to %%AppData%%\Playnite\Extensions\ControllerCompatibility\
) else (
    echo.
    echo Build failed. Please check the error messages above.
)

pause