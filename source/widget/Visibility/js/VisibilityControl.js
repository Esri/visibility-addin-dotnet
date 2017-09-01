///////////////////////////////////////////////////////////////////////////
// Copyright (c) 2016 Esri. All Rights Reserved.
//
// Licensed under the Apache License Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
///////////////////////////////////////////////////////////////////////////

/*global define*/
define([
    'dojo/_base/declare',
    'dojo/Deferred',
    'dojo/_base/lang',
    'dojo/_base/array',
    'dojo/on',
    'dojo/keys',
    'dojo/string',
    'dojo/topic',
    'dojo/dom',
    'dojo/dom-class',
    'dojo/dom-style',
    'dojo/mouse',
    'dojo/promise/all',
    'dijit/_WidgetBase',
    'dijit/_TemplatedMixin',
    'dijit/_WidgetsInTemplateMixin',
    'dijit/TooltipDialog',
    'dijit/popup',
    'dojo/text!../templates/VisibilityControl.html',
    './jquery.knob.min',
    'jimu/dijit/Message',
    './DrawFeedBack',
    'esri/map',
    'esri/dijit/util/busyIndicator',
    'esri/toolbars/draw',
    'esri/geometry/webMercatorUtils',
    'esri/graphic',
    'esri/layers/GraphicsLayer',
    'esri/tasks/Geoprocessor',
    'esri/tasks/FeatureSet',
    'esri/graphicsUtils',
    'esri/request',
    'esri/symbols/SimpleFillSymbol',
    'esri/symbols/SimpleLineSymbol',
    'esri/symbols/SimpleMarkerSymbol',
    'esri/Color',
    './CoordinateInput',
    './EditOutputCoordinate',
    'dijit/form/NumberTextBox',
    'jimu/dijit/CheckBox'    
], function (
    dojoDeclare,
    dojoDeferred,
    dojoLang,
    dojoArray,
    dojoOn,
    dojoKeys,
    dojoString,
    dojoTopic,
    dojoDom,
    dojoDomClass,
    dojoDomStyle,
    dojoMouse,
    dojoAll,
    dijitWidgetBase,
    dijitTemplatedMixin,    
    dijitWidgetsInTemplate,
    dijitTooltipDialog,
    dijitPopup,
    vistemplate,
    knob,
    Message,
    DrawFeedBack,
    Map,
    BusyIndicator,
    Draw,
    WebMercatorUtils,    
    Graphic,
    GraphicsLayer, 
    Geoprocessor, 
    FeatureSet, 
    graphicsUtils,
    Request,
    SimpleFillSymbol, 
    SimpleLineSymbol, 
    SimpleMarkerSymbol, 
    Color, 
    CoordInput,
    EditOutputCoordinate   
) {
    'use strict';
    return dojoDeclare([dijitWidgetBase, dijitTemplatedMixin, dijitWidgetsInTemplate], {
        templateString: vistemplate,
        baseClass: 'jimu-widget-visibility-control',
        FOV: 180,
        LA: 180,
        viewshedService: null,
        isSynchronous: false,
        map: null,
        gp: null,

        constructor: function(args) {
            dojoDeclare.safeMixin(this, args);
        },

        postCreate: function () {
            //check that the gpservice is valid for this widget
            var args = Request({
              url: this.viewshedService,
              content: {f: "json"},
              handleAs:"json",
              callbackParamName: "callback"
            });            
                     
            args.then(
              dojoLang.hitch(this, function(response) {
                var validParameters = ["Input_Observer", 
                            "Maximum_Distance__RADIUS2_",
                            "Left_Azimuth__AZIMUTH1_",
                            "Right_Azimuth__AZIMUTH2_",
                            "Observer_Offset__OFFSETA_",
                            "Near_Distance__RADIUS1_",
                            "Output_Viewshed",
                            "Output_Wedge",
                            "Output_FullWedge"];
                
                if(response.executionType === 'esriExecutionTypeSynchronous') {
                  this.isSynchronous = true;
                } else {
                  this.isSynchronous = false;
                }              
                var taskParameters = [];
                dojoArray.forEach(response.parameters, function(param){
                  taskParameters.push(param.name);
                });
                
                //convert both arrays to an ordered comma seperated list and compare
                if(validParameters.sort().join(',') === taskParameters.sort().join(',')){
                  //set up symbology for input
                  this._ptSym = new SimpleMarkerSymbol(this.pointSymbol);
                  
                  //set up symbology for output
                  this.visibleArea = new SimpleFillSymbol();
                  this.visibleArea.setOutline(new SimpleLineSymbol(SimpleLineSymbol.STYLE_SOLID, new Color([0, 0, 0, 0]), 1));
                  this.visibleArea.setColor(new Color([0, 255, 0, 0.5]));
                  this.notVisibleArea = new SimpleFillSymbol();
                  this.notVisibleArea.setOutline(new SimpleLineSymbol(SimpleLineSymbol.STYLE_SOLID, new Color([0, 0, 0, 0]), 1));
                  this.notVisibleArea.setColor(new Color([255, 0, 0, 0.5]));
                  this.fullWedge = new SimpleFillSymbol();
                  this.fullWedge.setOutline(new SimpleLineSymbol(SimpleLineSymbol.STYLE_DASH, new Color([0, 0, 0, 1]), 1));
                  this.fullWedge.setColor(new Color([0, 0, 0, 0]));
                  this.wedge = new SimpleFillSymbol();
                  this.wedge.setOutline(new SimpleLineSymbol(SimpleLineSymbol.STYLE_SOLID, new Color([255, 0, 0, 1]), 1));
                  this.wedge.setColor(new Color([0, 0, 0, 0]));

                  //set up observer input dijit
                  this.distanceUnit = this.distanceUnitDD.get('value');
                  this.observerHeightUnit = this.observerHeightDD.get('value');
                  this.coordTool = new CoordInput({appConfig: this.appConfig}, this.observerCoords);      
                  this.coordTool.inputCoordinate.formatType = 'DD';
                  this.coordinateFormat = new dijitTooltipDialog({
                    content: new EditOutputCoordinate(),
                    style: 'width: 400px'
                  });

                  if(this.appConfig.theme.name === 'DartTheme')
                  {
                    dojoDomClass.add(this.coordinateFormat.domNode, 'dartThemeClaroDijitTooltipContainerOverride');
                  }                  
                  
                  //initiate and add viewshed graphics layer
                  this._initGL();
                  
                  // add extended toolbar
                  this.dt = new DrawFeedBack(this.map,this.coordTool.inputCoordinate.util);
                  
                  //initiate synchronisation events
                  this._syncEvents();
                    
                } else {
                  this.gpTaskError(this.nls.taskURLInvalid);
                }
              }), dojoLang.hitch(this, function(error) {
                this.gpTaskError(this.nls.taskURLError);
              }));
        },      

        startup: function(){
            this.busyIndicator = BusyIndicator.create({target: this.domNode.parentNode.parentNode.parentNode, backgroundOpacity: 0});
            var updateValues = dojoLang.hitch(this,function(a,b,c) {
              this.angleUnits.checked?this.LA = a/17.777777777778:this.LA = a;
              this.FOV = Math.round(b);
              this.angleUnits.checked?this.tooltip.innerHTML  = c + " mils":this.tooltip.innerHTML  = c + " degrees";              
            });
              $("input.fov").knob({
                'min':0,
                'max':360,
                'cursor':360,
                'inputColor': '#ccc',
                'width': 160,
                'height': 160,
                'draw': function(){updateValues(this.v,this.o.cursor,this.cv)}
              });

            this.gp = new Geoprocessor(this.viewshedService);
            this.gp.setOutputSpatialReference({wkid: 102100});
        },
        
        /*
         * initiate and add viewshed graphics layer to map
         */        
        _initGL: function () {        
            this.graphicsLayer = new GraphicsLayer(),
            this.graphicsLayer.name = "Viewshed Layer";
            this.map.addLayer(this.graphicsLayer);
        },        

        /*
         * initiate synchronisation events
         */
        _syncEvents: function() {
            this.own(
              this.coordTool.inputCoordinate.watch('outputString', dojoLang.hitch(this, function (r, ov, nv) {
                if(!this.coordTool.manualInput){this.coordTool.set('value', nv);}
              })),
            
              this.dt.watch('startPoint' , dojoLang.hitch(this, function (r, ov, nv) {
                this.coordTool.inputCoordinate.set('coordinateEsriGeometry', nv);
                this.dt.addStartGraphic(nv, this._ptSym);
              })),            
            
              dojoOn(this.coordTool, 'keyup',dojoLang.hitch(this, this.coordToolKeyWasPressed)),
              
              this.dt.on('draw-complete',dojoLang.hitch(this, this.feedbackDidComplete)),
              
              dojoOn(this.coordinateFormatButton, 'click',dojoLang.hitch(this, this.coordinateFormatButtonWasClicked)),
              
              dojoOn(this.addPointBtn, 'click',dojoLang.hitch(this, this.pointButtonWasClicked)),
              
              dojoOn(this.btnCreate, 'click',dojoLang.hitch(this, this.createButtonWasClicked)),
              
              dojoOn(this.btnClear, "click", dojoLang.hitch(this, this.onClearBtnClicked)),
              
              dojoOn(this.minObsRange,'keyup', dojoLang.hitch(this, this.minObsRangeKeyWasPressed)),
              
              dojoOn(this.FOVInput,'mousemove', dojoLang.hitch(this, this.mouseMoveOverFOVInput)),
              
              dojoOn(this.FOVInput,dojoMouse.leave, dojoLang.hitch(this, this.mouseMoveOutFOVInput)),
              
              dojoOn(this.FOVGroup,dojoMouse.leave, dojoLang.hitch(this, function(){this.tooltip.hidden = true;})),
              
              dojoOn(this.FOVGroup,dojoMouse.enter, dojoLang.hitch(this, this.mouseMoveOverFOVGroup)),
              
              dojoOn(this.FOVInput,dojoMouse.enter, dojoLang.hitch(this, function(){this.tooltip.hidden = true;})),
              
              this.angleUnits.on('change',dojoLang.hitch(this, this.angleUnitsDidChange)),
              
              this.observerHeightDD.on('change',dojoLang.hitch(this, this.distanceUnitDDDidChange)),
              
              this.distanceUnitDD.on('change',dojoLang.hitch(this, this.distanceUnitDDDidChange)),
              
              dojoOn(this.coordinateFormat.content.applyButton, 'click', dojoLang.hitch(this, function () {
                var fs = this.coordinateFormat.content.formats[this.coordinateFormat.content.ct];
                var cfs = fs.defaultFormat;
                var fv = this.coordinateFormat.content.frmtSelect.get('value');
                if (fs.useCustom) {
                    cfs = fs.customFormat;
                }
                this.coordTool.inputCoordinate.set(
                  'formatPrefix',
                  this.coordinateFormat.content.addSignChkBox.checked
                );
                this.coordTool.inputCoordinate.set('formatString', cfs);
                this.coordTool.inputCoordinate.set('formatType', fv);
                this.setCoordLabel(fv);
                dijitPopup.close(this.coordinateFormat);                
              })),
              
              dojoOn(this.coordinateFormat.content.cancelButton, 'click', dojoLang.hitch(this, function () {
                dijitPopup.close(this.coordinateFormat);
              }))
            );
        },

        
        /*
         * 
         */
        viewshed: function (gpParams) { 
            this.map.setMapCursor("wait");

            if (!this.isNumeric(gpParams["Left_Azimuth__AZIMUTH1_"]) && !this.isNumeric(gpParams["Right_Azimuth__AZIMUTH2_"])) {
              var Azimuth1 = parseInt(this.LA - (this.FOV / 2));
              if(Azimuth1 < 0)
              {
                  Azimuth1 = Azimuth1 + 360;
              }
              var Azimuth2 = parseInt(this.LA + (this.FOV / 2));
              if(Azimuth2 > 360)
              {
                  Azimuth2 = Azimuth2 - 360;
              }
              if(this.FOV == 360)
              {
                  Azimuth1 = 0;
                  Azimuth2 = 360;
              }
              gpParams["Left_Azimuth__AZIMUTH1_"] = Azimuth1;
              gpParams["Right_Azimuth__AZIMUTH2_"] = Azimuth2;
            }
            
            if(this.isSynchronous){
                this.busyIndicator.show();
                this.gp.execute(gpParams, dojoLang.hitch(this, this.synchronousCompleteCallback), dojoLang.hitch(this, this.gpError));
            } else {
                this.busyIndicator.show();
                this.gp.submitJob(gpParams, dojoLang.hitch(this, this.aSynchronousCompleteCallback), dojoLang.hitch(this, this.callBack), dojoLang.hitch(this, this.gpError));              
            }
        },
        
        /*
         * 
         */
        callBack: function (jobInfo){
          console.log(jobInfo.jobStatus);
        },
        
        /*
         * 
         */        
        drawWedge: function (graphics,symbol){
          var deferred = new dojoDeferred();
          for (var w = 0, wl = graphics.length; w < wl; w++) {
            var feature = graphics[w];
            if (this.map.spatialReference.wkid === 4326) {
              feature.geometry = WebMercatorUtils.webMercatorToGeographic(feature.geometry);
            }
            feature.setSymbol(symbol);
            this.graphicsLayer.add(feature);                      
          }
          deferred.resolve("success");
          return deferred.promise;
        },
        
        /*
         * 
         */
        drawViewshed: function (graphics){
          var deferred = new dojoDeferred();
          for (var w = 0, wl = graphics.length; w < wl; w++) {
            var feature = graphics[w];
            if (this.map.spatialReference.wkid === 4326) {
              feature.geometry  = WebMercatorUtils.webMercatorToGeographic(feature.geometry);
            }
            if(feature.attributes.gridcode != 0)
            {
              feature.setSymbol(this.visibleArea);
              this.graphicsLayer.add(feature);
            }
            else
            {
              feature.setSymbol(this.notVisibleArea);
              this.graphicsLayer.add(feature);
            }            
          }
          deferred.resolve("success");
          return deferred.promise;          
        },
        
        /*
         * 
         */
        aSynchronousCompleteCallback: function (jobInfo) {
          if(jobInfo.jobStatus === 'esriJobSucceeded'){
            var processViewshed = this.gp.getResultData(jobInfo.jobId, "Output_Viewshed");
            var processFullWedge = this.gp.getResultData(jobInfo.jobId, "Output_FullWedge");
            var processWedge = this.gp.getResultData(jobInfo.jobId, "Output_Wedge");
            var promises = dojoAll([processViewshed, processFullWedge,processWedge]);
            promises.then(dojoLang.hitch(this,this.synchronousCompleteCallback));
          } else {
            var alertMessage = new Message({
              message: 'An error occured whilst creating visibility. Please ensure your observer location falls within the extent of your elevation surface.</p>'
            });
            this.map.setMapCursor("default");
            this.busyIndicator.hide();
          }
        },
        
        /*
         * 
         */
        synchronousCompleteCallback: function (results) { 
          this.drawViewshed(results[0].value.features);           
          this.drawWedge(results[2].value.features,this.fullWedge);
          this.drawWedge(results[1].value.features,this.wedge);
          this.map.setExtent(graphicsUtils.graphicsExtent(this.graphicsLayer.graphics), true);
          this.map.setMapCursor("default");
          this.busyIndicator.hide(); 
        },
        
        /*
         * catch key press in start point
         */
        coordToolKeyWasPressed: function (evt) {
          this.coordTool.manualInput = true;
          if (evt.keyCode === dojoKeys.ENTER) {
            this.coordTool.inputCoordinate.getInputType().then(dojoLang.hitch(this, function (r) {
              if(r.inputType == "UNKNOWN"){
                var alertMessage = new Message({
                  message: 'Unable to determine input coordinate type please check your input.'
                });
              } else {
                dojoTopic.publish(
                  'visibility-observer-point-input',
                  this.coordTool.inputCoordinate.coordinateEsriGeometry
                );
                this.setCoordLabel(r.inputType);
                var fs = this.coordinateFormat.content.formats[r.inputType];
                this.coordTool.inputCoordinate.set('formatString', fs.defaultFormat);
                this.coordTool.inputCoordinate.set('formatType', r.inputType);
                this.dt.addStartGraphic(r.coordinateEsriGeometry, this._ptSym);
                this.enableFOVDial();
              }
            }));
          }
        },
        
        /*
         * catch key press in min obs range, if valid, set max obs range min value accordingly
         */
        minObsRangeKeyWasPressed: function (evt) {
          if(this.minObsRange.isValid())
          {
            this.maxObsRange.constraints.min = Number(this.minObsRange.displayedValue) + 0.001;
            this.maxObsRange.set('value',Number(this.minObsRange.displayedValue) + 1);            
          }
        },        
        
        /*
         * 
         */
        mouseMoveOverFOVGroup: function (evt) {
          if(this.FOVInput.disabled == false) {             
            this.tooltip.hidden = false;
          }          
        },
        
        /*
         * 
         */
        mouseMoveOverFOVInput: function (evt) {
          if(this.FOVInput.disabled == false)
          {
            $(document).ready(function(){
                  $(document).mousemove(function(e){
                     var cpos = { top: e.pageY + 10, left: e.pageX + 10 };
                     $('#tooltip').offset(cpos);
                  });
                });
          }            
        },
        
        /*
         * 
         */
        mouseMoveOutFOVInput: function (evt) {
          this.tooltip.hidden = false;
          this.FOVInput.blur();
        },
        
        /*
         *
         */
        angleUnitsDidChange: function () {
          if(this.angleUnits.checked) {
            $("input.fov").trigger('configure',
              {
                  "max": 6400,
                  "units": 'mils',
                  "milsValue": 6400
              }
            );
            $("input.fov").val(6400).trigger('change');
          } else {
            $("input.fov").trigger('configure',
              {
                  "max": 360,
                  "units": 'degrees',
                  "milsValue": 6400
              }
            );
            $("input.fov").val(360).trigger('change');            
          }
        },
        
        /*
         *
         */
        distanceUnitDDDidChange: function () {
          this.distanceUnit = this.distanceUnitDD.get('value');
          this.observerHeightUnit = this.observerHeightDD.get('value'); 
        },
        
        /*
         *
         */
        setCoordLabel: function (toType) {
          this.coordInputLabel.innerHTML = dojoString.substitute(
            'Center Point (${crdType})', {
                crdType: toType
            });
        },
        
        /*
         *
         */
        feedbackDidComplete: function (results) {          
          dojoDomClass.remove(this.addPointBtn, 'jimu-state-active');
          this.dt.deactivate();
          this.map.enableMapNavigation();
          this.enableFOVDial();
        },
        
        /*
         *
         */
        enableFOVDial: function () { 
        if(this.FOVInput.disabled)
          {
          this.FOVInput.disabled = false;
            $("input.fov").trigger('configure',
                {
                    "fgColor":"#00ff66",
                    "bgColor":"#f37371",
                    "inputColor":"#ccc"                     
                }
            );
          }
        },
        
        /*
         *
         */
        coordinateFormatButtonWasClicked: function () {
          this.coordinateFormat.content.set('ct', this.coordTool.inputCoordinate.formatType);
          dijitPopup.open({
              popup: this.coordinateFormat,
              around: this.coordinateFormatButton
          });
        },
        
        /*
         * Button click event, activate feedback tool
         */
        pointButtonWasClicked: function () {
          this.coordTool.manualInput = false;
          dojoTopic.publish('clear-points');
          this.dt._setTooltipMessage(0);
          
          this.map.disableMapNavigation();          
          this.dt.activate('point');
          var tooltip = this.dt._tooltip;
          if (tooltip) {
            tooltip.innerHTML = 'Click to add observer location';
          }
          dojoDomClass.toggle(this.addPointBtn, 'jimu-state-active');
        },
        
        /*
         * Button click event, send viewshed request
         */
        createButtonWasClicked: function () {
          
          if(this.dt.startGraphic && this.minObsRange.isValid() && this.maxObsRange.isValid() && this.observerHeight.isValid() && this.FOVInput.value != 0)
          {
            var newObserver = new Graphic(this.coordTool.inputCoordinate.coordinateEsriGeometry);
            var featureSet = new FeatureSet();
            featureSet.features = [newObserver];

            var params = {
              "Input_Observer": featureSet,
              "Near_Distance__RADIUS1_": parseInt(this.coordTool.inputCoordinate.util.convertToMeters(this.minObsRange.value, this.distanceUnit)),
              "Maximum_Distance__RADIUS2_": parseInt(this.coordTool.inputCoordinate.util.convertToMeters(this.maxObsRange.value, this.distanceUnit)),
              "Observer_Offset__OFFSETA_": parseInt(this.coordTool.inputCoordinate.util.convertToMeters(this.observerHeight.value, this.observerHeightUnit))
            };
          
            this.viewshed(params);
          } else {
            var alertMessage = new Message({
              message: '<p>The visibility creation form has missing or invalid parameters, Please ensure:</p><ul><li>An observer location has been set.</li><li>The observer Field of View is not 0.</li><li>The observer height contains a valid value.</li><li>The min and max observable distances contain valid values.</li></ul>'
            });
          }
        },

        /*
         * 
         */
        gpError: function () {
            var alertMessage = new Message({
              message: 'An error occured whilst creating visibility. Please ensure your observer location falls within the extent of your elevation surface.</p>'
            });
            this.map.setMapCursor("default");
            this.busyIndicator.hide();
        },

        /*
         * 
         */
        gpTaskError: function (message) {
          dojoDomStyle.set(this.controls, 'display', 'none');
          dojoDomStyle.set(this.buttonContainer, 'display', 'none');
          this.errorText.innerHTML = message;
          dojoDomStyle.set(this.errorText, 'display', '');
        },
        
        /*
         * 
         */
        onClearBtnClicked: function () {
            this.graphicsLayer.clear();
            this.dt.removeStartGraphic();
            //reset dialog
            this.FOVInput.disabled = true;
            $("input.fov").val(360).trigger('change');
            $("input.fov").trigger('configure',
                {
                    "fgColor":"#ccc",
                    "bgColor":"#ccc",
                    "inputColor":"#ccc"                     
                }
            );
            this.tooltip.hidden = true;
        },

        /*
         * 
         */
        isNumeric: function(n) {
            return !isNaN(parseFloat(n)) && isFinite(n);
        }           
    });
});