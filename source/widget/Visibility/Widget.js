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
  './js/VisibilityControl'
	],
function(
  dojoDeclare,
  domConstruct,
  jimuBaseWidget,
  dijitWidgetsInTemplateMixin,   
  jimuMessage,
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
    },

    _isURL: function(s) {
      var regexp = 
        /(ftp|http|https):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/;
      return regexp.test(s);
    }
  });
  return clazz;
});  