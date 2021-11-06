using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateColliderPostProcessor : AssetPostprocessor
{
    [MenuItem("Assets/Add Mesh")]
    static void AddMesh()
    {
        var assetsModelsNewmeshAsset = "Assets/Models/NewMesh.asset";
        AssetDatabase.CreateAsset(new Mesh() {name = "New Mesh"}, assetsModelsNewmeshAsset);
        AssetDatabase.AddObjectToAsset(new Mesh() {name = "Another New Mesh"}, assetsModelsNewmeshAsset);
        AssetDatabase.ImportAsset(assetsModelsNewmeshAsset);
    }

    [MenuItem("Assets/Add Mesh To Selection")]
    static void AddMeshToSelection()
    {
        var activeObject = Selection.activeObject;
        AssetDatabase.AddObjectToAsset(new Mesh() {name = "Another New Mesh"}, activeObject);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(activeObject));
    }

    void OnPostprocessModel(GameObject g)
    {
        List<Transform> transformsToDestroy = new List<Transform>();
        //Skip the root
        foreach (Transform child in g.transform)
        {
            GenerateCollider(child, transformsToDestroy);
        }

        for (int i = transformsToDestroy.Count - 1; i >= 0; --i)
        {
            GameObject.DestroyImmediate(transformsToDestroy[i].gameObject);
        }

        Debug.Log($"Path:{assetPath} Context:{context}");
        // AssetDatabase.AddObjectToAsset(new Mesh() {name = "New Mesh"}, assetPath);
    }

    // static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
    //     string[] movedFromAssetPaths)
    // {
    //     foreach (var importedAsset in importedAssets)
    //     {
    //         var asset = AssetDatabase.LoadAssetAtPath<GameObject>(importedAsset);
    //         Debug.Log($"Postprocess: {asset.name} at path {importedAsset}");
    //         Mesh newMesh = new Mesh();
    //         newMesh.name = "Test New Mesh";
    //         // AssetDatabase.AddObjectToAsset(newMesh, importedAsset);
    //         // AssetDatabase.AddObjectToAsset(newMesh, asset);
    //     }
    // }

    void GenerateCollider(Transform t, List<Transform> transformsToDestroy)
    {
        foreach (Transform child in t.transform)
        {
            GenerateCollider(child, transformsToDestroy);
        }

        // Debug.Log(t.name);

        if (t.name.ToLower().StartsWith("ubx_"))
        {
            AddCollider<BoxCollider>(t);
            transformsToDestroy.Add(t);
        }
        else if (t.name.ToLower().StartsWith("ucp_"))
        {
            AddCollider<CapsuleCollider>(t);
            transformsToDestroy.Add(t);
        }
        else if (t.name.ToLower().StartsWith("usp_"))
        {
            AddCollider<SphereCollider>(t);
            transformsToDestroy.Add(t);
        }
        else if (t.name.ToLower().StartsWith("ucx_"))
        {
            TranslateSharedMesh(t.GetComponent<MeshFilter>());
            var collider = AddCollider<MeshCollider>(t);
            collider.convex = true;
            transformsToDestroy.Add(t);
        }
        else if (t.name.ToLower().StartsWith("umc_"))
        {
            TranslateSharedMesh(t.GetComponent<MeshFilter>());
            AddCollider<MeshCollider>(t);
            transformsToDestroy.Add(t);
        }
    }

    void TranslateSharedMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null)
            return;

        var transform = meshFilter.transform;
        var mesh = meshFilter.sharedMesh;
        var vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i] = transform.TransformPoint(vertices[i]);
            vertices[i] = transform.parent.InverseTransformPoint(vertices[i]);
        }

        mesh.SetVertices(vertices);
    }

    T AddCollider<T>(Transform t) where T : Collider
    {
        T collider = t.gameObject.AddComponent<T>();
        T parentCollider = t.parent.gameObject.AddComponent<T>();
        parentCollider.name = t.name;

        EditorUtility.CopySerialized(collider, parentCollider);
        SerializedObject parentColliderSo = new SerializedObject(parentCollider);
        var parentCenterProperty = parentColliderSo.FindProperty("m_Center");
        if (parentCenterProperty != null)
        {
            SerializedObject colliderSo = new SerializedObject(collider);
            var colliderCenter = colliderSo.FindProperty("m_Center");
            var worldSpaceColliderCenter = t.TransformPoint(colliderCenter.vector3Value);

            parentCenterProperty.vector3Value = t.parent.InverseTransformPoint(worldSpaceColliderCenter);
            parentColliderSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return parentCollider;
    }
}