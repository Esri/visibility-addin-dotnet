///////////////////////////////////////////////////////////////////////////
// Copyright (c) 2017 Esri. All Rights Reserved.
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

define([
  'dojo/_base/declare', 
  'dojo/dom-construct',
  'jimu/BaseWidget',
  'dijit/_WidgetsInTemplateMixin',   
  'jimu/dijit/Message',
  'jimu/utils',
  './js/VisibilityControl'
	],
function(
  dojoDeclare,
  domConstruct,
  jimuBaseWidget,
  dijitWidgetsInTemplateMixin,   
  jimuMessage,
  utils,
  VisibilityControl
){
	var clazz = dojoDeclare([jimuBaseWidget, dijitWidgetsInTemplateMixin], {
      
    baseClass: 'jimu-widget-visiblity',      

    startup: function(){
      this.inherited(arguments);
      if (this.config) {
        if (this.config.taskUrl) {
          if (!this._isURL(this.config.taskUrl)) {
            new jimuMessage({message: this.nls.taskURLInvalid});
            return;
          }
        }
      }
      var visibilityCtrl = new VisibilityControl({
        nls: this.nls,
        appConfig: this.appConfig,
        pointSymbol: {
          'color': [255,0,0,64],
          'size': 12,
          'type': 'esriSMS',
          'style': 'esriSMSCircle',
          'outline': {
            'color': [0,0,0,255],
            'width': 1,
            'type': 'esriSLS',
            'style': 'esriSLSSolid'
          }
        },
        viewshedService: this.config.taskUrl,
        map: this.map
      }, domConstruct.create("div")).placeAt(this.visibilityContainer);
      visibilityCtrl.startup();
      this._setTheme();
    },

    _isURL: function(s) {
      var regexp = 
        /(ftp|http|https):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/;
      return regexp.test(s);
    },
    
    /**
    * Handle different theme styles
    **/
    //source:
    //https://stackoverflow.com/questions/9979415/dynamically-load-and-unload-stylesheets
    _removeStyleFile: function (filename, filetype) {
      //determine element type to create nodelist from
      var targetelement = null;
      if (filetype === "js") {
        targetelement = "script";
      } else if (filetype === "css") {
        targetelement = "link";
      } else {
        targetelement = "none";
      }
      //determine corresponding attribute to test for
      var targetattr = null;
      if (filetype === "js") {
        targetattr = "src";
      } else if (filetype === "css") {
        targetattr = "href";
      } else {
        targetattr = "none";
      }
      var allsuspects = document.getElementsByTagName(targetelement);
      //search backwards within nodelist for matching elements to remove
      for (var i = allsuspects.length; i >= 0; i--) {
        if (allsuspects[i] &&
          allsuspects[i].getAttribute(targetattr) !== null &&
          allsuspects[i].getAttribute(targetattr).indexOf(filename) !== -1) {
          //remove element by calling parentNode.removeChild()
          allsuspects[i].parentNode.removeChild(allsuspects[i]);
        }
      }
    },
    
    _setTheme: function () {
      //Check if DartTheme
      if (this.appConfig.theme.name === "DartTheme") {
        //Load appropriate CSS for dart theme
        utils.loadStyleLink('darkOverrideCSS', this.folderUrl + "css/dartTheme.css", null);
      } else {
        this._removeStyleFile(this.folderUrl + "css/dartTheme.css", 'css');
      }
      //Check if DashBoardTheme
      if (this.appConfig.theme.name === "DashboardTheme" && 
        this.appConfig.theme.styles[0] === "default"){
        //Load appropriate CSS for dashboard theme
        utils.loadStyleLink('darkDashboardOverrideCSS', this.folderUrl + 
          "css/dashboardTheme.css", null);
      } else {
        this._removeStyleFile(this.folderUrl + "css/dashboardTheme.css", 'css');
      }
    }
  });
  return clazz;
});  