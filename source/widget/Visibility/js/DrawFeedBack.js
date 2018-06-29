///////////////////////////////////////////////////////////////////////////
// Copyright © 2014 - 2018 Esri. All Rights Reserved.
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
//
///////////////////////////////////////////////////////////////////////////

define([
  'dojo/_base/declare',
  'dojo/_base/lang',
  'dojo/topic',
  './Feedback'
], function (
  dojoDeclare,
  dojoLang,
  dojoTopic,
  drawFeedback
) {
    var clz = dojoDeclare([drawFeedback], {
        /**
         *
         **/
        constructor: function () {
            this.syncEvents();
            this.inherited(arguments);
        },

        /*

        */
        syncEvents: function () {
            dojoTopic.subscribe(
                'visibility-observer-point-input',
                dojoLang.hitch(this, this.onCenterPointManualInputHandler)
            );

            dojoTopic.subscribe(
                'clear-points',
                dojoLang.hitch(this, this.clearPoints)
            );
        },

        /*
        Handler for clearing out points
        */
        clearPoints: function () {
            this._points = [];
            this.map.graphics.clear();
        },

        /**
         *
         **/
        clearGraphics: function () {
            this.map.graphics.clear();
        },

        /*
        Handler for the manual input of a center point
        */
        onCenterPointManualInputHandler: function (centerPoint) {
            this._points = [];
            this._points.push(centerPoint.offset(0, 0));
            this.set('startPoint', this._points[0]);
            this.map.centerAt(centerPoint);
        },

        /**
         *
         **/
        _onClickHandler: function (evt) {
            var snapPoint;
            if (this.map.snappingManager) {
                snapPoint = this.map.snappingManager._snappingPoint;
            }
            var start = snapPoint || evt.mapPoint;
            this._points.push(start.offset(0, 0));
            this.set('startPoint', this._points[0]);
            this._drawEnd(start);
        },

        /*
         *
         */
        cleanup: function () {
            //do nothing yet
        }

    });
    return clz;
});
