using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppVisibilityModule.Models
{
    public class ProGraphic
    {
        public ProGraphic(IDisposable _disposable, Geometry _geometry, bool _isTemp = false)
        {
            Disposable = _disposable;
            Geometry = _geometry;
            IsTemp = _isTemp;
        }

        // properties   

        /// <summary>
        /// Property for the unique id of the graphic (guid)
        /// </summary>
        //public string UniqueId { get; set; }
        public IDisposable Disposable { get; set; }

        /// <summary>
        /// Property for the geometry of the graphic
        /// </summary>
        public Geometry Geometry { get; set; }

        /// <summary>
        /// Property to determine if graphic is temporary or not
        /// </summary>
        public bool IsTemp { get; set; }

    }
}
