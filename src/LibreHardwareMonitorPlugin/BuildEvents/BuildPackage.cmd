@echo off

set PROJECT_DIR=%~1
set OUTPUT_DIR=%~2

xcopy /s /y %PROJECT_DIR%metadata\ %OUTPUT_DIR%metadata\
xcopy /s /y %PROJECT_DIR%..\LibreHardwareMonitor\ %OUTPUT_DIR%LibreHardwareMonitor\

%PROJECT_DIR%LoupedeckPluginTool\LoupedeckPluginTool.exe pack -input=%OUTPUT_DIR% -output=%OUTPUT_DIR%..\LibreHardwareMonitor.lplug4
