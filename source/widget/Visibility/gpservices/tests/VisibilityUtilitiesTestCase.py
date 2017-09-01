# coding: utf-8
'''
------------------------------------------------------------------------------
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
------------------------------------------------------------------------------
 ==================================================
 VisibilityUtilitiesTestCase.py
 --------------------------------------------------
 requirements: ArcGIS 10.3+, Python 2.7
 author: ArcGIS Solutions
 contact: support@esri.com
 company: Esri
 ==================================================
 description:
 Unit tests for Visibility tools
 ==================================================
'''

# IMPORTS ==========================================
import os
import sys
import traceback
import arcpy
from arcpy import env
import unittest
import UnitTestUtilities
import Configuration

# import the viewshed package
import Viewshed
# import the viewshed module
from Viewshed import Viewshed

class VisibilityUtilitiesTestCase(unittest.TestCase):

    def test__surfaceContainsPoint(self):
        '''
        Check if elevation dataset contains the specified point
        '''
        runToolMessage = ".....VisibilityUtilityTestCase.test__surfaceContainsPoint"
        arcpy.AddMessage(runToolMessage)
        Configuration.Logger.info(runToolMessage)

        # List of coordinates
        coordinates = [[-117.196717216, 34.046944853]]

        # Create an in_memory feature class to initially contain the coordinate pairs
        feature_class = arcpy.CreateFeatureclass_management(
            "in_memory", "tempfc", "POINT")[0]

        # Open an insert cursor
        with arcpy.da.InsertCursor(feature_class, ["SHAPE@XY"]) as cursor:
            # Iterate through list of coordinates and add to cursor
            for (x, y) in coordinates:
                cursor.insertRow([(x, y)])

        # Create a FeatureSet object and load in_memory feature class
        feature_set = arcpy.FeatureSet()
        feature_set.load(feature_class)
        Point_Input = "in_memory\\tempPoints"
        arcpy.CopyFeatures_management(feature_set, Point_Input)

        self.assetEqual(True, Viewshed.surfaceContainsPoint(Point_Input, Viewshed.elevation))