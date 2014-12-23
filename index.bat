@echo off
%~d0
cd %~dp0

start /wait "" "C:\sphinx-2.2.3-beta-win64\bin\SphinxDirectoryCrawler.exe"

start /wait "" "C:\sphinx-2.2.3-beta-win64\bin\indexer.exe" lib_eshia_ir_main --rotate --config "C:\sphinx-2.2.3-beta-win64\bin\sphinx.conf"

