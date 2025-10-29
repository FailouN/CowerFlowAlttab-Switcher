@echo off
set PROJECT=CoverflowAltTab
set RUNTIME=win-x64
set CONFIG=Release
set OUTDIR=%~dp0Build

echo 🔧 Building %PROJECT% for %RUNTIME% (%CONFIG%)...

if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"

dotnet publish %PROJECT%.csproj -c %CONFIG% -r %RUNTIME% --self-contained true ^
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTDIR%"

if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo ✅ Build complete!
echo 📂 Output: %OUTDIR%
pause
