@echo off

echo =================================
echo =       MAKE UNSEALED EXE       =
echo =================================

< NUL call config.bat

cd %BUILD_PATH%%
%REPO_DRIVE%
%
%PATCHER_EXE% makebaseimage -t %TOWERFALL_EXE_NAME%
pause