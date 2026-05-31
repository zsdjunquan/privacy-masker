@echo off
setlocal
set "DOTNET_EXE=%USERPROFILE%\.dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" (
  echo .NET SDK was not found at "%DOTNET_EXE%".
  echo Please install .NET 8 SDK first.
  exit /b 1
)
"%DOTNET_EXE%" run --project "%~dp0PrivacyMasker.csproj"
