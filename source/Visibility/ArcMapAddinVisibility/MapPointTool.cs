using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using VisibilityLibrary.Helpers;
using VisibilityLibrary;

namespace ArcMapAddinVisibility
{
    public class MapPointTool : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        ISnappingEnvironment m_SnappingEnv;
        IPointSnapper m_Snapper;
        ISnappingFeedback m_SnappingFeedback;

        public MapPointTool()
        {
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }

        protected override void OnActivate()
        {
			//Get the snap environment and initialize the feedback
			UID snapUID = new UID();

			snapUID.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";
			m_SnappingEnv = ArcMap.Application.FindExtensionByCLSID(snapUID) as ISnappingEnvironment;
			m_Snapper = m_SnappingEnv.PointSnapper;
			m_SnappingFeedback = new SnappingFeedbackClass();
			m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnv, true);
        }

        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            if (arg.Button != System.Windows.Forms.MouseButtons.Left)
                return;

            try
            {
                //Get the active view from the ArcMap static class.
                IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

                var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y) as IPoint;
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(null, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                Mediator.NotifyColleagues(Constants.NEW_MAP_POINT, point);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        protected override void OnMouseMove(MouseEventArgs arg)
        {
            IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

            var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y) as IPoint;
            ISnappingResult snapResult = null;
            //Try to snap the current position
            snapResult = m_Snapper.Snap(point);
            m_SnappingFeedback.Update(snapResult, 0);
            if (snapResult != null && snapResult.Location != null)
                point = snapResult.Location;

            Mediator.NotifyColleagues(Constants.MOUSE_MOVE_POINT, point);
        }
    }

}
