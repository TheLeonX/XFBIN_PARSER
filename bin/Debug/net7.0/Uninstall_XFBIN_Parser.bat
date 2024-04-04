@echo off

reg delete "HKEY_CLASSES_ROOT\.xfbin" /f > nul 2>&1
reg delete "HKEY_CLASSES_ROOT\XFBIN" /f > nul 2>&1
reg delete "HKEY_CLASSES_ROOT\Folder\shell\XFBIN_PARSER" /f > nul 2>&1

