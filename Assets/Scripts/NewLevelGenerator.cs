using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class NewLevelGenerator : MonoBehaviour {

        NewAutomataGenerator gen;

        public int width;
        public int height;
        public float cellSize;
        public float wallHeight;
        public int smoothIterations;
        public int minimumRegionSize;

        [Range(0, 100)]
        public int borderThickness;

        [Range(0, 100)]
        public int highThreshold;

        [Range(0, 100)]
        public int midThreshold;

        [Range(0, 100)]
        public int lowThreshold;
        public string Seed;

        private string seed;
        public bool useSeed;
        public bool regenerate;

        [Range(0, 100)]
        public int multiplier;

        [Range(0, 10)]
        public int octaves;

        [Range(0, 4)]
        public float amplitude;

        [Range(0, 3)]
        public float lacunarity;

        [Range(0, 2)]
        public float persistence;

        public Material groundMaterial;
        public Material wallMaterial;

        public GameObject treeSpawner;

        [Range(0, 10)]
        public float treeDensity;

        public GameObject grassPrefab;

        [Range(0, 10)]
        public float grassDensity;

        private NewMeshGenerator meshGenerator;

        void Start() {
            Generate();
        }

        void Update() {
            if (regenerate) {
                regenerate = false;
                Generate();
            }
        }

        void Generate() {
            if (useSeed)
                seed = Seed;
            else
                seed = System.DateTime.Now.Ticks.ToString();

            if (this.transform.childCount > 0) {
                foreach (Transform child in this.transform) {
                    GameObject.Destroy(child.gameObject);
                }
            }

            gen = new NewAutomataGenerator(seed.GetHashCode());

            gen.Generate(width, height, highThreshold, midThreshold, lowThreshold, smoothIterations, minimumRegionSize, borderThickness);

            NewMeshGenerator.SimplexArguments simplexArguments = new NewMeshGenerator.SimplexArguments(octaves, multiplier, amplitude, lacunarity, persistence, seed);

            meshGenerator = new NewMeshGenerator(width, height, gen.MapRegions, cellSize, wallHeight, simplexArguments);
            meshGenerator.GenerateMeshes();

            Texture2D splatMap = new Texture2D(meshGenerator.heightMap.GetLength(0), meshGenerator.heightMap.GetLength(1));
            for (int x = 0; x < splatMap.width; x++) {
                for (int y = 0; y < splatMap.height; y++) {
                    splatMap.SetPixel(x, y, new Color(0, 0, 1.0f, 0));

                    if (meshGenerator.heightMap[x, y] < 0.7)
                        splatMap.SetPixel(x, y, new Color(0, 1.0f, 0, 0));

                    if (meshGenerator.heightMap[x, y] < 0.55)
                        splatMap.SetPixel(x, y, new Color(1.0f, 0, 0, 0));
                }
            }

            splatMap.Apply();

            foreach (Mesh mesh in meshGenerator.GroundMeshes) {
                GameObject obj = new GameObject("Ground Mesh");
                obj.transform.parent = this.transform;
                groundMaterial.SetTexture("_Control", splatMap);
                obj.AddComponent<MeshRenderer>().material = groundMaterial;
                obj.AddComponent<MeshFilter>().mesh = mesh;

                obj.AddComponent<MeshCollider>();
            }

            foreach (Mesh mesh in meshGenerator.WallMeshes) {
                GameObject obj = new GameObject("Wall Mesh");
                obj.transform.parent = this.transform;
                obj.AddComponent<MeshRenderer>().material = wallMaterial;
                obj.AddComponent<MeshFilter>().mesh = mesh;

                obj.AddComponent<MeshCollider>();
            }

            FoliageSpawner foliageSpawner = new FoliageSpawner(meshGenerator, treeSpawner, treeDensity, grassPrefab, grassDensity, this.transform);

            foliageSpawner.Generate();
        }

        void OnDrawGizmos() {
            //if (gen != null) {
            //    for (int x = 0; x < gen.Map.GetLength(0); x++) {
            //        for (int y = 0; y < gen.Map.GetLength(1); y++) {
            //            if (gen.Map[x, y] == 0)
            //                Gizmos.color = Color.black;
            //            if (gen.Map[x, y] == 1)
            //                Gizmos.color = Color.blue;
            //            if (gen.Map[x, y] == 2)
            //                Gizmos.color = Color.cyan;
            //            if (gen.Map[x, y] == 3)
            //                Gizmos.color = Color.white;

            //            Gizmos.DrawCube(new Vector3(x, 0, y), Vector3.one);
            //        }
            //    }
            //}
        }
    }
}