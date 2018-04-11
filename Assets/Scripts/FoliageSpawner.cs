using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class FoliageSpawner {

        public GameObject treeSpawner;
        public GameObject grassPrefab;

        private NewMeshGenerator meshGenerator;
        private float treeDensity;
        private float grassDensity;
        private Transform parent;

        public FoliageSpawner(NewMeshGenerator _meshGenerator, GameObject _treeSpawner, float _treeDensity, GameObject _grassPrefab, float _grassDensity, Transform _parent) {
            this.meshGenerator = _meshGenerator;
            this.treeSpawner = _treeSpawner;
            this.treeDensity = _treeDensity;
            this.grassPrefab = _grassPrefab;
            this.grassDensity = _grassDensity;
            this.parent = _parent;
        }

        public void Generate() {
            SpawnGrass();
            SpawnTrees();
        }

        void SpawnTrees() {
            List<Vector2> spawnPositions = new List<Vector2>();
            int height = meshGenerator.height;
            int width = meshGenerator.width;

            float maxNudge = 1 / (2 * treeDensity);
            
            //Evenly distribute area * density trees over map
            for (int x = 0; x < (int)(width * treeDensity); x++) {
                for (int y = 0; y < (int)(height * treeDensity); y++) {
                    //Nudge each tree by a random amount
                    float mapX = (x / treeDensity);
                    float mapY = (y / treeDensity);

                    float spawnX = (x / treeDensity) * meshGenerator.CellSize + Random.Range(-maxNudge, maxNudge);
                    float spawnY = (y / treeDensity) * meshGenerator.CellSize + Random.Range(-maxNudge, maxNudge);

                    //Probability of the tree surviving is based on the heightmap, 0 is guaranteed to live, 1 is guaranteed to die
                    if (meshGenerator.heightMap[(int)mapX, (int)mapY] < (float)Random.Range(0.0f, 0.75f)) {
                        //Tree survives, add it to list
                        spawnPositions.Add(new Vector2(spawnX, spawnY));
                    }
                }
            }

            foreach (Vector2 spawnPoint in spawnPositions) {
                RaycastHit[] hits = Physics.RaycastAll(new Ray(new Vector3(spawnPoint.x, 50.0f, spawnPoint.y), Vector3.down), 100.0f);
                if (hits.Length > 0) {
                    Vector3 location = hits[0].point;
                    Object.Instantiate(treeSpawner, location, Quaternion.identity, parent.transform);
                }
            }
        }

        void SpawnGrass() {
            List<Vector2> spawnPositions = new List<Vector2>();
            int height = meshGenerator.height;
            int width = meshGenerator.width;

            float maxNudge = 1 / grassDensity;

            //Evenly distribute area * density grass over map
            for (int x = 0; x < (int)(width * grassDensity); x++) {
                for (int y = 0; y < (int)(height * grassDensity); y++) {
                    //Nudge each grass by a random amount
                    float mapX = (x / grassDensity);
                    float mapY = (y / grassDensity);

                    float spawnX = (x / grassDensity) * meshGenerator.CellSize + Random.Range(-maxNudge, maxNudge);
                    float spawnY = (y / grassDensity) * meshGenerator.CellSize + Random.Range(-maxNudge, maxNudge);

                    //Probability of the grass surviving is based on the heightmap, <0.2 is guaranteed to live, >0.5 is guaranteed to die
                    if (meshGenerator.heightMap[(int)mapX, (int)mapY] < (float)Random.Range(0.2f, 0.5f)) {
                        //Grass survives, add it to list
                        spawnPositions.Add(new Vector2(spawnX, spawnY));
                    }
                }
            }

            foreach (Vector2 spawnPoint in spawnPositions) {
                RaycastHit[] hits = Physics.RaycastAll(new Ray(new Vector3(spawnPoint.x, 50.0f, spawnPoint.y), Vector3.down), 100.0f);
                if (hits.Length > 0) {
                    float rotation = Random.Range(-30.0f, 30.0f);
                    Vector3 location = hits[0].point;
                    Object.Instantiate(grassPrefab, location, Quaternion.Euler(90.0f, rotation, 0), parent.transform);
                }
            }
        }
    }
}
