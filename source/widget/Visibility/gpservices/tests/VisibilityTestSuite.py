# coding: utf-8
'''
-----------------------------------------------------------------------------
Copyright 2016 Esri
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
  http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
-----------------------------------------------------------------------------
==================================================
VisibilityTestSuite.py
--------------------------------------------------
requirments:
* ArcGIS Desktop 10.X+
* Python 2.7
author: ArcGIS Solutions
company: Esri
==================================================
description:
This test suite collects all of the tests in the Visibility toolset within the Military Tools toolbox:
==================================================
'''

import logging
import unittest
import Configuration

TestSuite = unittest.TestSuite()

def getVisibilityTestSuites():
    ''' Add all of the tests in ./visibility_tests to the test suite '''
    visibilityUtilitiesTests = ['test__surfaceContainsPoint']

    if Configuration.DEBUG == True:
        print("   VisibilityTestSuite.getVisibilityTestSuites")

    Configuration.Logger.info("Adding Visibility tests including: ")

    if Configuration.Platform == "DESKTOP":
        Configuration.Logger.info("Visibility Desktop tests")
        addVisibilityUtilitiesTests(visibilityUtilitiesTests)
    else:
        Configuration.Logger.info("Visibility Pro tests")
        addVisibilityUtilitiesTests(visibilityUtilitiesTests)

    return TestSuite

def addVisibilityUtilitiesTests(inputTestList):
    if Configuration.DEBUG == True: print(".....VisibilityTestSuite.addVisibilityUtilitiesTests")
    from . import VisibilityUtilitiesTestCase
    for test in inputTestList:
        print("adding test: {0}".format(str(test)))
        Configuration.Logger.info(test)
        TestSuite.addTest(VisibilityUtilitiesTestCase.VisibilityUtilitiesTestCase(test))