@echo off

SET cwd=%CD%

cd ..\src\Tools\runtests\bin\Debug

runtests.exe %cwd%\..\tools\php-7\php.exe "%cwd%"

cd %cwd%
pause