@echo off
setlocal
set "DOTNET_EXE=%USERPROFILE%\.dotnet\dotnet.exe"
set "APP_DLL=%~dp0bin\Debug\net8.0-windows\PrivacyMasker.dll"

rem Use the per-user .NET install so the app works even when the system runtime is not installed globally.
if not exist "%DOTNET_EXE%" (
  echo .NET SDK was not found at "%DOTNET_EXE%".
  echo Please install .NET 8 SDK first.
  exit /b 1
)
"%DOTNET_EXE%" build "%~dp0PrivacyMasker.csproj" || exit /b 1
start "" "%DOTNET_EXE%" "%APP_DLL%"
