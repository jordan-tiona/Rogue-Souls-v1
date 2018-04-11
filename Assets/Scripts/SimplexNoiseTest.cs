using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora {
    public class SimplexNoiseTest : MonoBehaviour {
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

        public bool renew = false;

        void Start() {
            Generate();
        }

        private void Update() {
            if (renew) {
                renew = false;
                Generate();
            }
        }

        public void Generate() {
            SimplexNoiseGenerator gen = new SimplexNoiseGenerator();

            Texture2D texture = new Texture2D(400, 200);

            GetComponent<MeshRenderer>().material.mainTexture = texture;

            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    float val = gen.coherentNoise(x, y, 0.0f, octaves, multiplier, amplitude, lacunarity, persistence);

                    texture.SetPixel(x, y, new Color(val, val, val));
                }
            }

            texture.Apply();
        }
    }
}