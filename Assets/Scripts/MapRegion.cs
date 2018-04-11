using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class MapRegion : System.IComparable {

        public int Elevation {
            get;
            private set;
        }

        public List<Vector2Int> Region {
            get;
            private set;
        }

        public MapRegion(List<Vector2Int> _region, int _elevation) {
            this.Elevation = _elevation;
            this.Region = _region;
        }

        public int CompareTo(object obj) {
            if (obj == null)
                return 1;

            MapRegion other = obj as MapRegion;
            if (other != null)
                return this.Elevation.CompareTo(other.Elevation);
            else
                throw new System.ArgumentException("Can't compare MapRegion to " + other.GetType());

        }
    }
}