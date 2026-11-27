@echo off
setlocal
set "GAME_DIR=%~1"
if not defined GAME_DIR set "GAME_DIR=D:/SteamLibrary/steamapps/common/RimWorld"
set "HARMONY_DLL=%~2"
if not defined HARMONY_DLL set "HARMONY_DLL=%GAME_DIR%/../../workshop/content/294100/2009463077/1.7/Assemblies/0Harmony.dll"
dotnet build "%~dp0Source/WardrobePolicySync/WardrobePolicySync.csproj" -c Release -t:Rebuild -p:RimWorldVersion=1.7 "-p:RimWorldDir=%GAME_DIR%" "-p:HarmonyPath=%HARMONY_DLL%"
exit /b %errorlevel%
