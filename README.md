# visibility-addin-dotnet

An ArcGIS Desktop add-in providing visibility tools. These tools use elevation data
paired with observer and target information to produce Linear Line of Sight (LLOS) and Radial Line of Sight (RLOS) information.

![Image of Visibility Add-In](visibility.png) 

## Features

* Linear Lines of sight (LLOS)
* Radial Line of Sight (RLOS)
* Specifying observer and target locations by entering inputs manually or by clicking on the map 
* Use one or multiple observers and one or multiple targets to perform analysis 
* Add-in for ArcGIS Pro and ArcMap 

## Sections

* [Requirements](#requirements)
* [Instructions](#instructions)
	* [Build Instructions](#build-instructions)
	* [Running](#running)
* [Resources](#resources)
* [Issues](#issues)
* [Contributing](#contributing)
* [Licensing](#licensing)

## Requirements

### Build Requirements 

* Visual Studio 2015
* ArcGIS for Desktop 
	* ArcMap 10.3.1+
	* ArcGIS Pro 1.4+
* ArcGIS Desktop SDK for .NET 10.3.1+
	* [ArcGIS Desktop for .NET Requirements](https://desktop.arcgis.com/en/desktop/latest/get-started/system-requirements/arcobjects-sdk-system-requirements.htm)
* [ArcGIS Pro SDK](http://pro.arcgis.com/en/pro-app/sdk/) 1.4+

### Run Requirements

* ArcGIS for Desktop 
	* ArcMap 10.3.1+
	* ArcGIS Pro 1.4+
	* 3D Analyst and Spatial Analyst extensions installed and licensed

## Instructions

### Build Instructions

* To Build Using Visual Studio `*`
	* Open and build solution file
* To use MSBuild to build the solution
	* Open a Visual Studio Command Prompt: Start Menu | Visual Studio 2015 | Visual Studio Tools | Developer Command Prompt for VS2015
	* ` cd visibility-addin-dotnet\source`
	* ` msbuild Visibility.sln /property:Configuration=Release `
* To run Unit test from command prompt: Open a Visual Studio Command Prompt: Start Menu | Visual Studio 2015 | Visual Studio Tools | Developer Command Prompt for VS2015
	* ArcMap
		* ` cd visibility-addin-dotnet\source\Visibility\ArcMapAddinVisibility.Tests\bin\Release `
		* ` MSTest /testcontainer:ArcMapAddinVisibility.Tests.dll * `
	* ArcGIS Pro
		* ` cd visibility-addin-dotnet\source\Visibility\ProAppVisibilityModule.Tests\bin\Release `
		* ` MSTest /testcontainer:ProAppVisibilityModule.Tests.dll * `

`*` Note : Assembly references are based on a default install of the SDK, you may have to update the references if you chose an alternate install option

### Running

* To download and run the pre-built add-in, see the instructions at [solutions.arcgis.com](http://solutions.arcgis.com/defense/help/visibility)

## Resources

* [ArcGIS for Defense Visibility Component](http://solutions.arcgis.com/defense/help/visibility/)
* [Military Tools for ArcGIS](https://esri.github.io/military-tools-desktop-addins/)
* [Military Tools for ArcGIS Solutions Pages](http://solutions.arcgis.com/defense/help/military-tools/)
* [ArcGIS for Defense Solutions Website](http://solutions.arcgis.com/defense)
* [ArcGIS for Defense Downloads](http://appsforms.esri.com/products/download/#ArcGIS_for_Defense)
* [ArcGIS 10.X Help](http://resources.arcgis.com/en/help/)
* [ArcGIS Pro Help](http://pro.arcgis.com/en/pro-app/)
* [ArcGIS Blog](http://blogs.esri.com/esri/arcgis/)
* ![Twitter](https://g.twimg.com/twitter-bird-16x16.png)[@EsriDefense](http://twitter.com/EsriDefense)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an [issue](https://github.com/ArcGIS/visibility-addin-dotnet/issues).

## Contributing

Anyone and everyone is welcome to contribute. Please see our [guidelines for contributing](https://github.com/esri/contributing).

### Repository Points of Contact

#### Repository Owner: [Kevin](https://github.com/kgonzago)

* Merge Pull Requests
* Creates Releases and Tags
* Manages Milestones
* Manages and Assigns Issues

#### Secondary: [Patrick](https://github.com/pHill5136)

* Backup when the Owner is away

## Licensing

Copyright 2016-2017 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt](license.txt) file.

[](Esri Tags: Military Analyst Defense ArcGIS ArcObjects .NET WPF ArcGISSolutions ArcMap ArcPro Add-In Military-Tools-for-ArcGIS)
[](Esri Language: C#) 
