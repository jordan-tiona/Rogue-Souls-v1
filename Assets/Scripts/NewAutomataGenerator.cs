using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class NewAutomataGenerator {

        private int seed;

        private float borderMin;
        private float borderMax;

        public int[,] Map {
            get;
            private set;
        }

	    public NewAutomataGenerator(int _seed = 0) {

            if (_seed == 0) {
                this.seed = System.DateTime.Now.Ticks.GetHashCode();
            }
            else {
                this.seed = _seed;
            }

        }

        public List<MapRegion> MapRegions {
            get;
            private set;
        }

        public void Generate(int _width, int _height, int _highThreshold, int _midThreshold, int _lowThreshold, int _smoothIterations, int _minimumRegionSize, float _borderThickness) {
            System.Random random = new System.Random(this.seed);
            this.Map = new int[_width, _height];

            this.borderMin = _borderThickness / 100.0f;
            this.borderMax = 1.0f - _borderThickness / 100.0f;

            //Randomly fill
            for (int x = 0; x < _width; x++) {
                for (int y = 0; y < _height; y++) {

                    if (x < _width * borderMin || x > _width * borderMax || y < _height * borderMin || y > _height * borderMax) {
                        if ( random.Next (0, 100) < 65)
                        Map[x, y] = 0;
                        continue;
                    }

                    int nextInt = random.Next(0, 100);

                    if (nextInt > _highThreshold)
                        Map[x, y] = 3;
                    else if (nextInt > _midThreshold)
                        Map[x, y] = 2;
                    else if (nextInt > _lowThreshold)
                        Map[x, y] = 1;
                    else
                        Map[x, y] = 0;

                    if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                        Map[x, y] = 0;
                }
            }

            //Smooth map
            for (int i = 0; i < _smoothIterations; i++) {
                Smooth();
            }

            //Remove regions that are too small
            CleanupRegions(_minimumRegionSize);

            //Get all regions
            this.MapRegions = new List<MapRegion>();

            foreach (List<Vector2Int> region in GetAllRegions(0)) {
                MapRegions.Add(new MapRegion(region, 0));
            }
            foreach (List<Vector2Int> region in GetAllRegions(1)) {
                MapRegions.Add(new MapRegion(region, 1));
            }
            foreach (List<Vector2Int> region in GetAllRegions(2)) {
                MapRegions.Add(new MapRegion(region, 2));
            }
            foreach (List<Vector2Int> region in GetAllRegions(3)) {
                MapRegions.Add(new MapRegion(region, 3));
            }
        }

        public void Smooth() {
            int width = Map.GetLength(0);
            int height = Map.GetLength(1);

            int[,] newMap = new int[width, height];

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        continue;

                    int numSame = NumNeighbours(x, y, Map[x, y]);
                    int numAbove = 0;
                    int numBelow = 0;

                    for (int i = Map[x, y] - 1; i >= 0; i--) {
                        numBelow += NumNeighbours(x, y, i);
                    }

                    for (int i = Map[x, y] + 1; i <= 3; i++) {
                        numAbove += NumNeighbours(x, y, i);
                    }

                    if (numSame >= 3 || numAbove == numBelow) {
                        newMap[x, y] = Map[x, y];
                    }
                    else {
                        if (numAbove > numBelow) {
                            newMap[x, y] = Map[x, y] + 1;
                        }
                        else {
                            newMap[x, y] = Map[x, y] - 1;
                        }
                    }
                }
            }

            Map = newMap;
        }

        public void CleanupRegions(int minimumRegionSize) {
            List<List<Vector2Int>> highRegions = GetAllRegions(3);

            foreach (List<Vector2Int> region in highRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 2;
                    }
                }
            }

            List<List<Vector2Int>> midRegions = GetAllRegions(2);

            foreach (List<Vector2Int> region in midRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 1;
                    }
                }
            }

            List<List<Vector2Int>> lowRegions = GetAllRegions(1);

            foreach (List<Vector2Int> region in lowRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 0;
                    }
                }
            }

            List<List<Vector2Int>> voidRegions = GetAllRegions(0);

            foreach (List<Vector2Int> region in voidRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 1;
                    }
                }
            }

            lowRegions = GetAllRegions(1);

            foreach (List<Vector2Int> region in lowRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 2;
                    }
                }
            }

            midRegions = GetAllRegions(2);

            foreach (List<Vector2Int> region in midRegions) {
                if (region.Count < minimumRegionSize) {
                    foreach (Vector2Int cell in region) {
                        Map[cell.x, cell.y] = 3;
                    }
                }
            }
        }

        public int NumNeighbours(int _x, int _y, int type) {

            int width = Map.GetLength(0);
            int height = Map.GetLength(1);

            int numNeighbours = 0;

            for (int x = _x - 1; x <= _x + 1; x++) {
                for (int y = _y - 1; y <= _y + 1; y++) {
                    if (x != _x || y != _y) {
                        if (x < 0 || x >= width || y < 0 || y >= height) {
                            //numNeighbours++;
                        }
                        else if (Map[x, y] == type) {
                            numNeighbours++;
                        }
                    }
                }
            }

            return numNeighbours;
        }

        List<List<Vector2Int>> GetAllRegions(int cellType) {
            int width = Map.GetLength(0);
            int height = Map.GetLength(1);

            List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
            bool[,] added = new bool[width, height];

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (!added[x, y] && Map[x, y] == cellType) {
                        List<Vector2Int> region = GetRegion(x, y);
                        regions.Add(region);

                        foreach (Vector2Int cell in region) {
                            added[cell.x, cell.y] = true;
                        }
                    }
                }
            }

            return regions;
        }

        List<Vector2Int> GetRegion(int startX, int startY) {
            int width = Map.GetLength(0);
            int height = Map.GetLength(1);

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            List<Vector2Int> region = new List<Vector2Int>();
            bool[,] added = new bool[width, height];

            int cellType = Map[startX, startY];
            queue.Enqueue(new Vector2Int(startX, startY));

            while (queue.Count > 0) {
                //Dequeue first cell in queue and add it to our region
                Vector2Int current = queue.Dequeue();
                region.Add(current);
                added[current.x, current.y] = true;

                Vector2Int above = current + Vector2Int.up;
                Vector2Int right = current + Vector2Int.right;
                Vector2Int below = current + Vector2Int.down;
                Vector2Int left = current + Vector2Int.left;

                if (IsInBounds(above.x, above.y) && (added[above.x, above.y] == false) && Map[above.x, above.y] == cellType) {
                    added[above.x, above.y] = true;
                    queue.Enqueue(above);
                }

                if (IsInBounds(below.x, below.y) && (added[below.x, below.y] == false) && Map[below.x, below.y] == cellType) {
                    added[below.x, below.y] = true;
                    queue.Enqueue(below);
                }

                if (IsInBounds(right.x, right.y) && (added[right.x, right.y] == false) && Map[right.x, right.y] == cellType) {
                    added[right.x, right.y] = true;
                    queue.Enqueue(right);
                }

                if (IsInBounds(left.x, left.y) && (added[left.x, left.y] == false) && Map[left.x, left.y] == cellType) {
                    added[left.x, left.y] = true;
                    queue.Enqueue(left);
                }
            }

            return region;
        }

        bool IsInBounds(int x, int y) {
            int width = Map.GetLength(0);
            int height = Map.GetLength(1);

            if (x >= 0 && x < width && y >= 0 && y < height)
                return true;
            else
                return false;
        }
    }
}