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
 Viewshed.py
 --------------------------------------------------
 requirements: ArcGIS 10.3+, Python 2.7
 author: ArcGIS Solutions
 contact: support@esri.com
 company: Esri
 ==================================================
 description:
 Creates a viewshed based on input parameters
 ==================================================
'''

# Import arcpy module
import arcpy, math
import os

####
########Script Parameters##################
####
inputPoints = arcpy.GetParameterAsText(0)

Radius2_Input = arcpy.GetParameterAsText(1)
Azimuth1_Input = arcpy.GetParameterAsText(2)
Azimuth2_Input = arcpy.GetParameterAsText(3)
OffsetA_Input = arcpy.GetParameterAsText(4)
Radius1_Input = arcpy.GetParameterAsText(5)
viewshed = arcpy.GetParameterAsText(6)
wedge = arcpy.GetParameterAsText(7)
fullwedge = arcpy.GetParameterAsText(8)

# CHANGE PATH TO RASTER DATASET:
elevation = r'D:\Workspace\Data\n36prj.tif'


DEBUG = False

def drawWedge(cx,cy,r1,r2,start,end):
    point = arcpy.Point()
    array = arcpy.Array()

    #Calculate the end x,y for the wedge
    x_end = cx + r2*math.cos(start)
    y_end = cy + r2*math.sin(start)

    #Calculate the step value for the x,y coordiantes, use 50 points for each radius
    i = math.radians(0.1)

    #Calculate the outer edge of the wedge
    a = start

    #If r1 == 0 then create a wedge from the centre point
    if r1 == 0:
        #Add the start point to the array
        point.X = cx
        point.Y = cy
        array.add(point)
        #Calculate the rest of the wedge
        while a >= end:
            point.X = cx + r2*math.cos(a)
            point.Y = cy + r2*math.sin(a)
            array.add(point)
            a -= i
        #Close the polygon
        point.X = cx
        point.Y = cy
        array.add(point)

    else:
        while a >= end:
            point.X = cx + r2*math.cos(a)
            point.Y = cy + r2*math.sin(a)
            a -= i
            array.add(point)

        #Caluclate the inner edge of the wedge
        a = end

        while a <= start:
            a += i
            point.X = cx + r1*math.cos(a)
            point.Y = cy + r1*math.sin(a)
            array.add(point)

        #Close the polygon by adding the end point
        point.X = x_end
        point.Y = y_end
        array.add(point)

    #Create the polygon
    polygon = arcpy.Polygon(array)

    return polygon

def surfaceContainsPoint(pointFeature, surfRaster):
    '''
    Check if point extent falls within surface extent, return True or False
    '''
    surfDesc = arcpy.Describe(surfRaster)
    surfaceExtentPoly = extentToPoly(surfDesc.extent, surfDesc.spatialReference)

    pointDesc = arcpy.Describe(pointFeature)
    pointExtent = pointDesc.extent
    pointFeature = extentToPoly(pointExtent, pointDesc.spatialReference)
    #project poly to same sr as surface
    pointFeatProj = pointFeature.projectAs(surfDesc.spatialReference)

    isWithin = pointFeatProj.within(surfaceExtentPoly)

    if DEBUG: arcpy.AddMessage("Within: {0}".format(isWithin))
    return isWithin

def extentToPoly(extent, sr):
    '''
    Extent object returns arcpy.Polygon or arcpy.Point
    '''
    if extent.height == 0.0 and extent.width == 0.0:
        if DEBUG: arcpy.AddMessage("point: {0}\n sr: {1}".format(extent, sr.name))
        return arcpy.PointGeometry(extent.lowerLeft, sr)
    else:
        if DEBUG: arcpy.AddMessage("poly: {0}\n sr: {1}".format(extent, sr.name))
        return extent.polygon

def main():
    elevDesc = arcpy.Describe(elevation)
    Output_CS = elevDesc.spatialReference

    if not Output_CS.type == "Projected":
        msgErrorNonProjectedSurface = "Error: Input elevation raster must be in a projected coordinate system. Existing elevation raster is in {0}.".format(Output_CS.name)
        arcpy.AddError(msgErrorNonProjectedSurface)
        raise Exception(msgErrorNonProjectedSurface)

    arcpy.env.outputCoordinateSystem = Output_CS

    polylist = []
    wedges = []

    ####
    ########End of Script Parameters##################
    ####

    Point_Input = "in_memory\\tempPoints"
    arcpy.CopyFeatures_management(inputPoints, Point_Input)

    #Check if point extent falls within surface extent
    isWithin = surfaceContainsPoint(Point_Input, elevation)
    if not isWithin:
        msgErrorPointNotInSurface = "Error: Input Observer(s) does not fall within the extent of the input surface: {0}!".format(os.path.basename(elevation))
        arcpy.AddError(msgErrorPointNotInSurface)
        raise Exception(msgErrorPointNotInSurface)

    arcpy.CalculateField_management(Point_Input, "OFFSETB", "0", "PYTHON_9.3", "")
    arcpy.CalculateField_management(Point_Input, "RADIUS2", Radius2_Input, "PYTHON_9.3", "")
    arcpy.CalculateField_management(Point_Input, "AZIMUTH1", Azimuth1_Input, "PYTHON_9.3", "")
    arcpy.CalculateField_management(Point_Input, "AZIMUTH2", Azimuth2_Input, "PYTHON_9.3", "")
    arcpy.CalculateField_management(Point_Input, "OFFSETA", OffsetA_Input, "PYTHON_9.3", "")
    arcpy.CalculateField_management(Point_Input, "RADIUS1", Radius1_Input, "PYTHON_9.3", "")
    arcpy.AddMessage("Buffering observers...")
    arcpy.Buffer_analysis(Point_Input, "in_memory\OuterBuffer", "RADIUS2", "FULL", "ROUND", "NONE", "", "GEODESIC")

    desc = arcpy.Describe("in_memory\OuterBuffer")
    xMin = desc.Extent.XMin
    yMin = desc.Extent.YMin
    xMax = desc.Extent.XMax
    yMax = desc.Extent.YMax
    Extent = str(xMin) + " " + str(yMin) + " " + str(xMax) + " " + str(yMax)
    # Call image service a second time to get corrected extents
    arcpy.env.extent = Extent
    arcpy.env.mask = "in_memory\\OutBuffer"
    arcpy.AddMessage("Clipping image to observer buffer...")
    # arcpy.MakeImageServerLayer_management(elevation, "elevation", Extent, "#", "#", "#", "#", "#", elevDesc.meanCellWidth)
    arcpy.Clip_management(elevation, Extent, "in_memory\clip")
    arcpy.AddMessage("Calculating viewshed...")
    arcpy.Viewshed_3d("in_memory\clip", Point_Input, "in_memory\intervis", "1", "FLAT_EARTH", "0.13")
    arcpy.AddMessage("Creating features from raster...")
    arcpy.RasterToPolygon_conversion(in_raster="in_memory\intervis", out_polygon_features="in_memory\unclipped",simplify="NO_SIMPLIFY")

    fields = ["SHAPE@XY","RADIUS1","RADIUS2","AZIMUTH1","AZIMUTH2"]
    ## get the attributes from the input point
    with arcpy.da.SearchCursor(Point_Input,fields) as cursor:
        for row in cursor:
            cx = row[0][0]
            cy = row[0][1]
            r1 = row[1]
            r2 = row[2]
            start = math.radians(90 - row[3])
            if row[3] > row[4]:
                end = row[4] + 360
                end =  math.radians(90 - end)
            else:
                end = math.radians(90 - row[4])

            poly = drawWedge(cx,cy,r1,r2,start,end)
            polylist.append(poly)
            fullWedge = drawWedge(cx,cy,0,r2,start,end)
            wedges.append(fullWedge)


    arcpy.CopyFeatures_management(polylist,wedge)
    arcpy.CopyFeatures_management(wedges,fullwedge)
    arcpy.AddMessage("Finishing output features...")
    arcpy.Clip_analysis("in_memory\unclipped", wedge, "in_memory\\dissolve")
    arcpy.Dissolve_management("in_memory\\dissolve", viewshed, "gridcode", "", "MULTI_PART", "DISSOLVE_LINES")


# MAIN =============================================
if __name__ == "__main__":
    main()
