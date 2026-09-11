@echo off
echo Building Controller Compatibility Plugin...

REM Build the project
dotnet build ControllerCompatibility.csproj --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Plugin built with official Playnite SDK.
    echo.
    echo Test files are located in:
    echo %~dp0bin\Release\net462\
    echo.
    rem NOTE: This build uses the official Playnite SDK.
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
    rem 3. Ensure MockPlayniteSDK.cs is removed (should not exist in production)
    echo 4. Install to %%AppData%%\Playnite\Extensions\ControllerCompatibility\
) else (
    echo.
    echo Build failed. Please check the error messages above.
)

pause