using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcMapAddinVisibility.Models
{
    public class AddInPointObject
    {
        private int _id;

        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private AddInPoint _addInPoint;

        public AddInPoint AddInPoint
        {
            get { return _addInPoint; }
            set { _addInPoint = value; }
        }
        
    }
}
