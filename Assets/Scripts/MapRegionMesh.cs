using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class MapRegionMesh {

        public List<Vector3> vertices;
        public List<int> groundTriangles;
        public List<int> wallTriangles;
        public List<Vector2> uvs;

        public List<int> outline;
        public float wallHeight;

        public MapRegionMesh() {
            this.vertices = new List<Vector3>();
            this.groundTriangles = new List<int>();
            this.wallTriangles = new List<int>();
            this.uvs = new List<Vector2>();
            this.outline = new List<int>();
        }
    }
}