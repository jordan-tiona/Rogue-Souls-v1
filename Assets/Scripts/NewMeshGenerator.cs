using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class NewMeshGenerator {
        public List<Mesh> GroundMeshes {
            get;
            private set;
        }

        public List<Mesh> WallMeshes {
            get;
            private set;
        }

        public List<MapRegion> MapRegions {
            get;
            private set;
        }

        public List<MapRegionMesh> MapRegionMeshes {
            get;
            private set;
        }

        enum NodeLocation { TOP_LEFT, CENTER_TOP, TOP_RIGHT, CENTER_RIGHT, BOTTOM_RIGHT, CENTER_BOTTOM, BOTTOM_LEFT, CENTER_LEFT }
        
        public float CellSize {
            get;
            private set;
        }

        public float WallHeight {
            get;
            private set;
        }

        public struct SimplexArguments {
            readonly public int octaves;
            readonly public int multiplier;
            readonly public float amplitude;
            readonly public float lacunarity;
            readonly public float persistence;
            readonly public string seed;

            public SimplexArguments(int _octaves, int _multiplier, float _amplitude, float _lacunarity, float _persistence, string _seed) {
                this.octaves = _octaves;
                this.multiplier = _multiplier;
                this.amplitude = _amplitude;
                this.lacunarity = _lacunarity;
                this.persistence = _persistence;
                this.seed = _seed;
            }
        }

        public SimplexArguments simplexArguments;
        public float[,] heightMap;

        public class Node {
            public Vector3 pos;
            public int vertIndex = -1;
            public Vector2 uv;

            public Node(Vector3 _pos) {
                this.pos = _pos;
            }
        }

        public class ControlNode : Node {
            public Node above;
            public Node right;
            public bool isActive;

            public ControlNode(Vector3 _pos, bool _isActive, float _meshSize) : base(_pos) {
                this.above = new Node(_pos + Vector3.forward * 0.5f * _meshSize);
                this.right = new Node(_pos + Vector3.right * 0.5f * _meshSize);
                this.isActive = _isActive;
            }
        }

        public class Square {
            public ControlNode topLeft, topRight, bottomRight, bottomLeft;
            public Node centerTop, centerRight, centerBottom, centerLeft;

            public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft, Node _centerTop, Node _centerRight, Node _centerBottom, Node _centerLeft) {
                this.topLeft = _topLeft;
                this.topRight = _topRight;
                this.bottomRight = _bottomRight;
                this.bottomLeft = _bottomLeft;

                this.centerTop = _centerTop;
                this.centerRight = _centerRight;
                this.centerBottom = _centerBottom;
                this.centerLeft = _centerLeft;
            }
        }

        public class Region {
            public Square[,] map;
            public int elevation;

            public Region(int _w, int _h, int _elevation) {
                this.map = new Square[_w, _h];
                this.elevation = _elevation;
            }
        }

        public class Edge {
            public int v1;
            public int v2;
        }

        private List<Region> regions;

        public int width;
        public int height;

        public NewMeshGenerator(int _width, int _height, List<MapRegion> _mapRegions, float _cellSize, float _wallHeight, SimplexArguments _simplexArguments) {
            this.CellSize = _cellSize;
            this.WallHeight = _wallHeight;

            this.width = _width;
            this.height = _height;

            this.simplexArguments = _simplexArguments;
            this.heightMap = new float[width, height];

            this.MapRegions = _mapRegions;
            this.MapRegionMeshes = new List<MapRegionMesh>();
            foreach (MapRegion region in MapRegions) {
                this.MapRegionMeshes.Add(new MapRegionMesh());
            }

            //Populate grid of nodes and squares from passed map regions
            this.regions = new List<Region>();
            GenerateHeightmap(this.simplexArguments.octaves, this.simplexArguments.multiplier, this.simplexArguments.amplitude, this.simplexArguments.lacunarity, this.simplexArguments.persistence, this.simplexArguments.seed);

            foreach (MapRegion mapRegion in MapRegions) {
                if (mapRegion.Elevation == 0) {
                    continue;
                }

                Region region = new Region(_width - 1, _height - 1, mapRegion.Elevation);
                
                ControlNode[,] nodes = new ControlNode[_width, _height];

                //Populate grid of nodes from region
               for (int x = 0; x < _width; x++) {
                    for (int y = 0; y < _height; y++) {
                        if (mapRegion.Region.Contains(new Vector2Int(x, y))) {

                            nodes[x, y] = new ControlNode(new Vector3(x * this.CellSize, (mapRegion.Elevation * this.WallHeight) + (heightMap[x, y] * this.WallHeight * 2.2f), y * this.CellSize), true, this.CellSize);
                        }
                        else {
                            nodes[x, y] = new ControlNode(new Vector3(x * this.CellSize, (mapRegion.Elevation * this.WallHeight) + (heightMap[x, y] * this.WallHeight * 2.2f), y * this.CellSize), false, this.CellSize);
                        }
                    }
                }

                //Populate grid of squares from region
                for (int x = 0; x < _width - 1; x++) {
                    for (int y = 0; y < _height - 1; y++) {
                        ControlNode topLeft = nodes[x, y + 1];
                        ControlNode topRight = nodes[x + 1, y + 1];
                        ControlNode bottomRight = nodes[x + 1, y];
                        ControlNode bottomLeft = nodes[x, y];

                        region.map[x, y] = new Square(topLeft, topRight, bottomRight, bottomLeft, topLeft.right, bottomRight.above, bottomLeft.right, bottomLeft.above);
                    }
                }

                this.regions.Add(region);
            }
        }

        public void GenerateMeshes() {
            this.GroundMeshes = new List<Mesh>();
            this.WallMeshes = new List<Mesh>();

            foreach (Region region in regions) {
                int _w = region.map.GetLength(0);
                int _h = region.map.GetLength(1);

                MapRegionMesh regionMesh = new MapRegionMesh();
                regionMesh.wallHeight = region.elevation;

                List<Edge> edgeList = new List<Edge>();
                Dictionary<int, List<Edge>> edgesContainingVertex = new Dictionary<int, List<Edge>>();

                //Marching squares
                for (int x = 0; x < _w; x++) {
                    for (int y = 0; y < _h; y++) {

                        int config = 0;

                        if (region.map[x, y].bottomLeft.isActive) {
                            config |= 1;
                        }
                        if (region.map[x, y].bottomRight.isActive) {
                            config |= 2;
                        }
                        if (region.map[x, y].topRight.isActive) {
                            config |= 4;
                        }
                        if (region.map[x, y].topLeft.isActive) {
                            config |= 8;
                        }

                        GenerateTriangles(region, regionMesh, edgeList, edgesContainingVertex, x, y, config);
                    }
                }

                regionMesh.outline = GenerateWalls(edgeList, edgesContainingVertex, regionMesh);

                //foreach (Vector3 vertex in regionMesh.vertices) {
                //    float percentX = Mathf.InverseLerp(0f, _w * CellSize, vertex.x);
                //    float percentY = Mathf.InverseLerp(0f, _h * CellSize, vertex.z);
                //    regionMesh.uvs.Add(new Vector2(percentX, percentY));
                //}

                //for (int i = regionMesh.uvs.Count; i < regionMesh.vertices.Count; i++) {
                //    regionMesh.uvs.Add(new Vector2(0.0f, 0.0f));
                //}

                Mesh groundMesh = new Mesh();
                groundMesh.vertices = regionMesh.vertices.ToArray();
                groundMesh.triangles = regionMesh.groundTriangles.ToArray();
                groundMesh.RecalculateNormals();
                groundMesh.uv = regionMesh.uvs.ToArray();

                this.GroundMeshes.Add(groundMesh);

                Mesh wallMesh = new Mesh();
                wallMesh.vertices = regionMesh.vertices.ToArray();
                wallMesh.triangles = regionMesh.wallTriangles.ToArray();
                wallMesh.RecalculateNormals();
                wallMesh.uv = regionMesh.uvs.ToArray();

                this.WallMeshes.Add(wallMesh);
            }
        }

        public void GenerateTriangles(Region region, MapRegionMesh _mesh, List<Edge> edgeList, Dictionary<int, List<Edge>> edgesContainingVertex, int x, int y, int config) {
            Square square = region.map[x, y];
            MapRegionMesh regionMesh = _mesh;

            switch (config) {
                //Zero points
                case 0:
                    break;

                //One point
                case 1:
                    MeshFromPoints(regionMesh, square.centerLeft, square.centerBottom, square.bottomLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerLeft, square.centerBottom);
                    break;

                case 2:
                    MeshFromPoints(regionMesh, square.centerBottom, square.centerRight, square.bottomRight);
                    AddEdge(edgeList, edgesContainingVertex, square.centerBottom, square.centerRight);
                    break;

                case 4:
                    MeshFromPoints(regionMesh, square.centerRight, square.centerTop, square.topRight);
                    AddEdge(edgeList, edgesContainingVertex, square.centerRight, square.centerTop);
                    break;

                case 8:
                    MeshFromPoints(regionMesh, square.centerTop, square.centerLeft, square.topLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerTop, square.centerLeft);
                    break;

                //Two points
                case 3:
                    MeshFromPoints(regionMesh, square.centerLeft, square.centerRight, square.bottomRight, square.bottomLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerLeft, square.centerRight);
                    break;

                case 6:
                    MeshFromPoints(regionMesh, square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                    AddEdge(edgeList, edgesContainingVertex, square.centerBottom, square.centerTop);
                    break;

                case 9:
                    MeshFromPoints(regionMesh, square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerTop, square.centerBottom);
                    break;

                case 12:
                    MeshFromPoints(regionMesh, square.centerLeft, square.topLeft, square.topRight, square.centerRight);
                    AddEdge(edgeList, edgesContainingVertex, square.centerRight, square.centerLeft);
                    break;

                case 5:
                    MeshFromPoints(regionMesh, square.bottomLeft, square.centerLeft, square.centerTop, square.topRight, square.centerRight, square.centerBottom);
                    AddEdge(edgeList, edgesContainingVertex, square.centerRight, square.centerBottom);
                    AddEdge(edgeList, edgesContainingVertex, square.centerLeft, square.centerTop);
                    break;

                case 10:
                    MeshFromPoints(regionMesh, square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerTop, square.centerRight);
                    AddEdge(edgeList, edgesContainingVertex, square.centerBottom, square.centerLeft);
                    break;

                //Three points
                case 7:
                    MeshFromPoints(regionMesh, square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerLeft, square.centerTop);
                    break;

                case 11:
                    MeshFromPoints(regionMesh, square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerTop, square.centerRight);
                    break;

                case 13:
                    MeshFromPoints(regionMesh, square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerBottom, square.centerRight);
                    break;

                case 14:
                    MeshFromPoints(regionMesh, square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                    AddEdge(edgeList, edgesContainingVertex, square.centerLeft, square.centerBottom);
                    break;

                //Four points
                case 15:
                    MeshFromPoints(regionMesh, square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                    break;
            }
        }

        public List<int> GenerateWalls(List<Edge> edgeList, Dictionary<int, List<Edge>> edgesContainingVertex, MapRegionMesh regionMesh) {
            Edge currentEdge = edgeList[0];

            int currentVertex = currentEdge.v1;
            int otherVertex = currentEdge.v2;

            List<Edge> edgesContainingOther;
            List<int> outline = new List<int>();

            while (true) {
                outline.Add(currentVertex);

                if (currentVertex == currentEdge.v1) {
                    otherVertex = currentEdge.v2;
                }
                else {
                    otherVertex = currentEdge.v1;
                }

                edgesContainingOther = edgesContainingVertex[otherVertex];

                if (edgesContainingOther.Count < 2)
                    break;

                if (currentEdge.Equals(edgesContainingOther[0])) {
                    Edge nextEdge = edgesContainingOther[1];
                    edgeList.Remove(currentEdge);
                    edgesContainingVertex[otherVertex].Remove(currentEdge);
                    currentEdge = nextEdge;
                }
                else {
                    Edge nextEdge = edgesContainingOther[0];
                    edgeList.Remove(currentEdge);
                    edgesContainingVertex[otherVertex].Remove(currentEdge);
                    currentEdge = nextEdge;
                }

                currentVertex = otherVertex;
            }

            List<int> topOutline = new List<int>();
            List<int> bottomOutline = new List<int>();
            
            for (int i = 0; i < outline.Count; i++) {
                Vector3 newVertex = regionMesh.vertices[outline[i]];

                topOutline.Add(regionMesh.vertices.Count);
                regionMesh.vertices.Add(newVertex);

                bottomOutline.Add(regionMesh.vertices.Count);
                regionMesh.vertices.Add(new Vector3(newVertex.x, -1.0f, newVertex.z));
                
                regionMesh.uvs.Add(new Vector2(i / 100.0f, 0.0f));
                regionMesh.uvs.Add(new Vector2(i / 100.0f, regionMesh.wallHeight / 3.0f));
            }

            for (int i = 0; i < outline.Count; i++) {

                int topLeft, topRight, bottomRight, bottomLeft;

                if (i == topOutline.Count - 1) {
                    topLeft = topOutline[i];
                    topRight = topOutline[0];
                    bottomRight = bottomOutline[0];
                    bottomLeft = bottomOutline[i];
                }
                else {
                    topLeft = topOutline[i];
                    topRight = topOutline[i + 1];
                    bottomRight = bottomOutline[i + 1];
                    bottomLeft = bottomOutline[i];
                }

                AddTriangle(regionMesh.wallTriangles, topLeft, bottomLeft, bottomRight);
                AddTriangle(regionMesh.wallTriangles, topLeft, bottomRight, topRight);
            }

            return outline;
        }

        public void MeshFromPoints(MapRegionMesh regionMesh, params Node[] points) {
            //If needed, add points to our list of vertices
            for (int i = 0; i < points.Length; i++) {
                if (points[i].vertIndex == -1) {
                    points[i].vertIndex = regionMesh.vertices.Count;
                    regionMesh.vertices.Add(points[i].pos);

                    float percentX = Mathf.InverseLerp(0f, width * CellSize, regionMesh.vertices[points[i].vertIndex].x);
                    float percentY = Mathf.InverseLerp(0f, height * CellSize, regionMesh.vertices[points[i].vertIndex].z);
                    regionMesh.uvs.Add(new Vector2(percentX, percentY));
                }
            }

            if (points.Length >= 3) {
                AddTriangle(regionMesh.groundTriangles, points[0].vertIndex, points[1].vertIndex, points[2].vertIndex);
            }
            if (points.Length >= 4) {
                AddTriangle(regionMesh.groundTriangles, points[0].vertIndex, points[2].vertIndex, points[3].vertIndex);
            }
            if (points.Length >= 5) {
                AddTriangle(regionMesh.groundTriangles, points[0].vertIndex, points[3].vertIndex, points[4].vertIndex);
            }
            if (points.Length >= 6) {
                AddTriangle(regionMesh.groundTriangles, points[0].vertIndex, points[4].vertIndex, points[5].vertIndex);
            }
        }

        public void AddEdge(List<Edge> edgeList, Dictionary<int, List<Edge>> edgesContainingVertex, Node _v1, Node _v2) {
            Edge edge = new Edge();
            edge.v1 = _v1.vertIndex;
            edge.v2 = _v2.vertIndex;
            edgeList.Add(edge);

            if (!edgesContainingVertex.ContainsKey(edge.v1)) {
                edgesContainingVertex.Add(edge.v1, new List<Edge>());
            }

            edgesContainingVertex[edge.v1].Add(edge);

            if (!edgesContainingVertex.ContainsKey(edge.v2)) {
                edgesContainingVertex.Add(edge.v2, new List<Edge>());
            }

            edgesContainingVertex[edge.v2].Add(edge);
        }

        public void AddTriangle(List<int> triangleList, int a, int b, int c) {
            triangleList.Add(a);
            triangleList.Add(b);
            triangleList.Add(c);
        }

        public void GenerateHeightmap(int octaves, int multiplier, float amplitude, float lacunarity, float persistence, string seed) {
            SimplexNoiseGenerator gen = new SimplexNoiseGenerator();

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    float val = gen.coherentNoise(x, y, 0.0f, octaves, multiplier, amplitude, lacunarity, persistence) * 0.5f + 0.5f;
                    this.heightMap[x, y] = val;
                }
            }
        }
    }
}