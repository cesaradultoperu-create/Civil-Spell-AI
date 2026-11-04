@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\..\scripts\Update-Drawing3D2024.ps1" -SourceDll "%~dp0CivilSpellAI.dll"
