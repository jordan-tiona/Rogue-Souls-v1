using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour {

    public Transform[] treePrefabs;

	void Start () {
        Transform tree = treePrefabs[Random.Range(0, treePrefabs.Length)];

        float scale = Random.Range(0.9f, 1.1f);

        float rotationAmount = (float)(Random.Range(0, 360));
        Quaternion rotation = Quaternion.Euler(new Vector3(0.0f, rotationAmount, 0.0f));

        Instantiate(tree, this.transform.position, rotation, this.transform).localScale *= scale;
	}
}
