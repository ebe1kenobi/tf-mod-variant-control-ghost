@echo off

echo =================================
echo =       DEPLOY RELEASE          =
echo =================================

< NUL call config.bat

xcopy /E /S /Y "%RELEASE_PATH%*" %TOWERFALL_PATH%

cd %TOWERFALL_PATH%
%GAME_DRIVE%

taskkill /IM %TOWERFALL_EXE_NAME%
@REM timeout 5
del %TOWERFALL_EXE_NAME%
copy %ORIGINAL_EXE_NAME% %TOWERFALL_EXE_NAME%
rmdir /S /Q "Patcher"

@REM 
%PATCHER_EXE%

del %TOWERFALL_EXE_NAME%
copy %PATCHED_EXE_NAME% %TOWERFALL_EXE_NAME%
del %PATCHED_EXE_NAME%

pause
