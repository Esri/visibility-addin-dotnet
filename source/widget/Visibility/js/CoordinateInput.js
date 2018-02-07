///////////////////////////////////////////////////////////////////////////
// Copyright © 2017 Esri. All Rights Reserved.
//
// Licensed under the Apache License Version 2.0 (the 'License');
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an 'AS IS' BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
///////////////////////////////////////////////////////////////////////////

define([
  "dojo/_base/declare",
  "dijit/form/ValidationTextBox",
  "./Coordinate"
], function (
  dojoDeclare,
  dijitValidationTextBox,
  Coord
) {
  var mo = dojoDeclare("test", dijitValidationTextBox, {
    required: true,

    inputCoordinate: null,

    invalidMessage: "",

    validateOnInput: true,
    _validateOnInputSetter: function (value) {
        this.validateOnInput = (value === "true");
    },

    clear: function () {
      this.set("validateOnInput", true);
      this.set("value", "");
      this.inputCoordinate.coordinateEsriGeometry = null;
    },
    /**
     *
     **/
    constructor: function (args) {
        dojoDeclare.safeMixin(this, args);
        this.inherited(arguments);
        this.inputCoordinate = new Coord({nls: this.nls, appConfig: arguments[0].appConfig});
    },

    postMixinProperties: function () {
  
    },

    /**
     *
     **/
    validator: function (value) {

      if (!this.validateOnInput) {
        return true;
      }
      //if (this.get('value').length < 4) return false;

      this.inputCoordinate.set("inputString", value);

      //this.inputCoordinate.set('formatString', 'YN XE');

      this.set("invalidMessage", this.inputCoordinate.message);
      this.set("promptMessage", this.inputCoordinate.message);

      return true;
    }
  });

  return mo;
});
