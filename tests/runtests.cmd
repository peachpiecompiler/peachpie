@echo off

SET "cwd=%CD%"

SET "TESTDIR=%cwd%"
IF NOT []==[%1] SET "TESTDIR=%1"

cd ..\src\Tools\runtests\bin\Debug

runtests.exe %cwd%\..\tools\php-7\php.exe "%TESTDIR%"

cd %cwd%
pause