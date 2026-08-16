using UnityEngine;

public class MeshCombineUtility : MonoBehaviour
{
    [ContextMenu("Combine Children Meshes")]
    public void Combine()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Matrix4x4 matrix = transform.worldToLocalMatrix;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i].gameObject == gameObject) continue;
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = matrix * meshFilters[i].transform.localToWorldMatrix;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, true, true);

        #if UNITY_EDITOR
        string directory = $"Assets/Assets/Mesh/Tree/{gameObject.name}.asset";
        UnityEditor.AssetDatabase.CreateAsset(combinedMesh, directory);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"Combined mesh saved to {directory}");
        #endif
    }
}
