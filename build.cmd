@echo off
:: QuickDailyPaste build script
:: Uses the built-in .NET Framework 4 C# compiler

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

echo Building QuickDailyPaste.exe ...
%CSC% /nologo /target:winexe /out:QuickDailyPaste.exe /optimize ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll ^
  /r:Microsoft.CSharp.dll ^
  /win32icon:app.ico ^
  QuickPaste.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo   ✅ Build succeeded: QuickDailyPaste.exe
    echo   Double-click QuickDailyPaste.exe to run.
) else (
    echo.
    echo   ❌ Build failed. Check errors above.
)
pause
