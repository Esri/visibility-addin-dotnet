define([
    'dojo/_base/declare',
    'dojo/_base/lang',
    'dojo/topic',
    'dojo/_base/array',
    'dojo/Deferred', 
    'dojo/dom-construct',
    'jimu/BaseWidget',
    'dijit/_WidgetsInTemplateMixin',
    'dijit/registry',    
    'jimu/dijit/Message',
    './js/VisibilityControl'
	],

function(
    dojoDeclare,
    dojoLang,
    dojoTopic,
    dojoArray,
    Deferred,
    domConstruct,
    jimuBaseWidget,
    dijitWidgetsInTemplateMixin,
    dijitRegistry,    
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
              new jimuMessage({message: "Please supply a valid viewshed service."});
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
        // source: http://stackoverflow.com/questions/1701898/how-to-detect-whether-a-string-is-in-url-format-using-javascript
       var regexp = /(ftp|http|https):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/
       return regexp.test(s);
      },
    });
    return clazz;
});  