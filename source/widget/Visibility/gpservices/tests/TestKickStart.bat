@ECHO OFF
rem ------------------------------------------------------------------------------
rem  Copyright 2017 Esri
rem  Licensed under the Apache License, Version 2.0 (the "License");
rem  you may not use this file except in compliance with the License.
rem  You may obtain a copy of the License at
rem 
rem    http://www.apache.org/licenses/LICENSE-2.0
rem 
rem  Unless required by applicable law or agreed to in writing, software
rem  distributed under the License is distributed on an "AS IS" BASIS,
rem  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
rem  See the License for the specific language governing permissions and
rem  limitations under the License.
rem ------------------------------------------------------------------------------
rem  ArcMapTestKickStart.bat
rem ------------------------------------------------------------------------------
rem  requirements:
rem  * Python 2.7 
rem  author: ArcGIS Solutions
rem  company: Esri
rem ==================================================
rem  description:
rem  This file starts the test running for Desktop (Python 2.7+)
rem 
rem ==================================================

REM === LOG SETUP ====================================
REM usage: set LOG=<defaultLogFileName.log>
REM name is optional; if not specified, name will be specified for you
set LOG=
REM === LOG SETUP ====================================

ECHO Executing Tests ===============================
python TestRunner.py %LOG%
REM check if Desktop for ArcGIS/Python 2.7 tests failed
IF %ERRORLEVEL% NEQ 0 (
   ECHO 'One or more tests failed'
)
pause