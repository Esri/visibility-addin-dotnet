using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppVisibilityModule.Models
{
    public class AddInPointObject
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private AddInPoint addInPoint;

        public AddInPoint AddInPoint
        {
            get { return addInPoint; }
            set { addInPoint = value; }
        }
        
    }
}
