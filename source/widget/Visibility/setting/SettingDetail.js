///////////////////////////////////////////////////////////////////////////
// Copyright © 2014 - 2016 Esri. All Rights Reserved.
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

define(['dojo/_base/declare',
  'dojo/_base/lang',
  'dojo/_base/html',
  'dojo/_base/array',
  'dojo/text!./SettingDetail.html',
  'dijit/_WidgetBase',
  'dijit/_TemplatedMixin',
  'jimu/dijit/LoadingShelter',
  'jimu/dijit/Message',
  './utils'
],
function(declare, lang, html, array, template, _WidgetBase, _TemplatedMixin, LoadingShelter, Message, gputils) {
  return declare([_WidgetBase, _TemplatedMixin], {
    baseClass: 'jimu-widget-setting-gp-detail',
    templateString: template,

    postCreate: function(){
      this.inherited(arguments);
      this.loadingCover = new LoadingShelter({hidden: true});
      this.loadingCover.placeAt(this.domNode);
      this.loadingCover.startup();
    },

    startup: function(){
      this.inherited(arguments);
    },

    setConfig: function(_config){
      var config = lang.clone(_config);
      if(this.config && this.config.taskUrl === config.taskUrl){
        this.config = config;
        if(!('serverInfo' in config)){
          this.loadingCover.show();
          //Load gp server info if it does not exist.
          gputils.getServiceDescription(this.config.taskUrl).then(lang.hitch(this,
            function(taskInfo){
              this.loadingCover.hide();
              this.config.serverInfo = taskInfo.serverInfo;
              this._initNavPane();
            }));
        }else{
          this._initNavPane();
        }
      }else{
        this.config = config;
        this.loadingCover.show();
        gputils.getServiceDescription(this.config.taskUrl).then(lang.hitch(this, function(taskInfo){
          this.loadingCover.hide();
          this._changeTaskInfoToConfig(taskInfo);
          this._initNavPane();
        }));
      }
    },

    getConfig: function(){
      //so, return it
      if(!this.config.taskUrl){
        new Message({
          message: this.nls.serviceURLError
        });
        return false;
      }
      return this.config;
    },

    _changeTaskInfoToConfig: function(taskInfo){
      var taskUrl = this.config.taskUrl;
      var isTaskValid = this._checkTaskParameters(taskInfo.parameters);
      if(isTaskValid){
        this.config.taskUrl = taskUrl;        
      } else {
        delete this.config.taskUrl;
      }
    },
    
    _checkTaskParameters: function(parameters){
      var validParameters = ["Input_Observer", 
                            "Maximum_Distance__RADIUS2_",
                            "Left_Azimuth__AZIMUTH1_",
                            "Right_Azimuth__AZIMUTH2_",
                            "Observer_Offset__OFFSETA_",
                            "Near_Distance__RADIUS1_",
                            "Output_Viewshed",
                            "Output_Wedge",
                            "Output_FullWedge"];
                            
      var taskParameters = [];
      array.forEach(parameters, function(param){
        taskParameters.push(param.name);
      });
      
      //convert both arrays to an ordered comma seperated list and compare
      if(validParameters.sort().join(',') === taskParameters.sort().join(',')){
          return true;
      } else {return false;}
    },

    _initNavPane: function(){
      html.empty(this.navPaneNode);
    }
    
  });
});
