using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FindDoorHintInstances
{
    [MenuItem("Tools/Find DoorHint Instances")]
    public static void Find()
    {
        var found = new List<string>();

        // Search prefabs (t:GameObject)
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            if (go.GetComponent<DoorHint>() != null)
            {
                Debug.Log($"Found DoorHint on prefab: {path}");
                found.Add(path);
            }
        }

        // Search scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var sg in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sg);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                var dh = root.GetComponentInChildren<DoorHint>(true);
                if (dh != null)
                {
                    string objPath = GetGameObjectPath(dh.gameObject);
                    Debug.Log($"Found DoorHint in scene {scenePath}: {objPath}");
                    found.Add(scenePath + ": " + objPath);
                }
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        if (found.Count == 0)
            Debug.Log("No DoorHint instances found in prefabs or scenes.");
        else
            Debug.Log($"FindDoorHintInstances: {found.Count} results (see console).");
    }

    static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
#endif
