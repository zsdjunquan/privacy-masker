@echo off
setlocal
set "DOTNET_EXE=%USERPROFILE%\.dotnet\dotnet.exe"
set "APP_EXE=%~dp0bin\Release\net8.0-windows\win-x64\publish\PrivacyMasker.exe"

rem Publish a self-contained WinExe so launching the helper does not leave a console window open.
if not exist "%DOTNET_EXE%" (
  echo .NET SDK was not found at "%DOTNET_EXE%".
  echo Please install .NET 8 SDK first.
  exit /b 1
)
"%DOTNET_EXE%" publish "%~dp0PrivacyMasker.csproj" -c Release || exit /b 1
start "" "%APP_EXE%"
