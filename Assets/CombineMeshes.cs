using UnityEngine;
using System.Collections;

public class CombineMeshes : MonoBehaviour {

    public void combineMeshes () {
/*
        MeshFilter[] meshFilters = Misc.FilterCarWays(GetComponentsInChildren<MeshFilter>());
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            i++;
        }

		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter> ();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);
        gameObject.SetActive(true);
*/
    }
}
