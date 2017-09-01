define([
  'intern!object',
  'intern/chai!assert',
  'dojo/dom-construct',
  'dojo/_base/lang',
  'dojo/_base/window', 
  'esri/map',
  'esri/geometry/Point',
  'esri/graphic',
  'esri/geometry/Extent',
  'esri/tasks/Geoprocessor',  
  'esri/tasks/FeatureSet',  
  'Vis/VisibilityControl',
], function(
	registerSuite, 
    assert, 
	domConstruct, 
	lang, 	 
	win,
	Map, 
	esriPoint,
	esriGraphic,
	Extent, 
	Geoprocessor,
	FeatureSet,
	VisibilityControl
	){
	var vis, map;
  	registerSuite({
	    name: 'Visibility-Widget',
		//before the suite starts
		setup: function() {

			//load claro and esri css, create a map div in the body, and create the map object and print widget for our tests
			domConstruct.place('<link rel="stylesheet" type="text/css" href="//js.arcgis.com/3.19/esri/css/esri.css">', win.doc.getElementsByTagName("head")[0], 'last');
			domConstruct.place('<link rel="stylesheet" type="text/css" href="//js.arcgis.com/3.19/dijit/themes/claro/claro.css">', win.doc.getElementsByTagName("head")[0], 'last');
			domConstruct.place('<script src="http://js.arcgis.com/3.19/"></script>', win.doc.getElementsByTagName("head")[0], 'last');
			domConstruct.place('<div id="map" style="width:800px;height:600px;" class="claro"></div>', win.body(), 'only');
			domConstruct.place('<div id="visNode" style="width:300px;" class="claro"></div>', win.body(), 'last');	
			domConstruct.place('<div data-dojo-attach-point="drawBox" data-dojo-type="jimu/dijit/DrawBox"></div>', win.body(), 'last');	

			map = new Map("map", {
				basemap: "topo",
				center: [-13567710.133162694, 4382857.914599498],
				zoom: 5,
				sliderStyle: "small",
		        extent: new Extent({
					xmin:-13583666.362817202,
					ymin:4374488.060002282,
					xmax:-13546976.589240368,
					ymax:4391877.483937137,
					spatialReference:{
						wkid:102100
					}		        	
		        })				
			});
		  
		  	//unittests
		  	vis = new VisibilityControl({
				viewshedService: {
					url: 'https://nationalsecurity.esri.com:6443/arcgis/rest/services/Tasks/Viewshed/GPServer/Viewshed'
				},
				map: map
			}, domConstruct.create("div")).placeAt("visNode");	
			vis.startup();	
		},

		//before each test executes
		beforeEach: function() {
			//TODO
		},

		// after the suite is done (all tests)
		teardown: function() {
			if (map.loaded) {
				map.destroy();                    
			}            
		},
		
		'Test widget Loads': function(){
			assert.isNotNull(vis);
		},	

		'Test widget is instanceOf Visibility': function() {
			assert.instanceOf(vis, VisibilityControl);
		},
		
		'Test template loaded': function() {
			assert.isDefined(vis);
		},

		'Test execute viewshed': function() {
			var dfd = this.async(10000);

			var pt = new esriPoint(-13567710.133162694, 4382857.914599498, map.extent.spatialReference);		
			var graphic = new esriGraphic(pt, null, null, null);
			vis.graphicsLayer.add(graphic);

			var featureSet = new FeatureSet();
			featureSet.features = [graphic];

			var params = {
				"Input_Observer": featureSet,
				"Near_Distance__RADIUS1_": 3000,
				"Maximum_Distance__RADIUS2_": 5000,
				"Left_Azimuth__AZIMUTH1_": 45,
				"Right_Azimuth__AZIMUTH2_": 135,
				"Observer_Offset__OFFSETA_": 2
			}; 

			vis.gp.execute(params).then(dfd.callback(function(results, messages){
				assert.lengthOf(results, 3, "Results has 3 items");
				vis.drawViewshed(results, messages);
				assert.lengthOf(vis.graphicsLayer.graphics, 5, "Graphics layer has 5 graphics");
				dfd.resolve();
			}), dfd.callback(function(error) {
				dfd.reject(error);
			}));
		}
  	})
});