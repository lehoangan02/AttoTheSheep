using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UpdateCursorHotspots : EditorWindow
{
    [MenuItem("Tools/Update Cursor Hotspots to 22,17")]
    public static void UpdateHotspots()
    {
        int updatedPrefabs = 0;
        int updatedSceneObjs = 0;

        // 1. Cập nhật trên tất cả các Prefab
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                HoverCursor[] cursors = prefab.GetComponentsInChildren<HoverCursor>(true);
                bool modified = false;
                foreach (var c in cursors)
                {
                    if (c.defaultHotSpot == Vector2.zero)
                    {
                        c.defaultHotSpot = new Vector2(22, 17);
                        modified = true;
                    }
                    if (c.hoverHotSpot == Vector2.zero)
                    {
                        c.hoverHotSpot = new Vector2(22, 17);
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    updatedPrefabs++;
                }
            }
        }

        // 2. Cập nhật trên Scene đang mở
        HoverCursor[] sceneCursors = Object.FindObjectsByType<HoverCursor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool sceneModified = false;
        foreach (var c in sceneCursors)
        {
            bool objModified = false;
            if (c.defaultHotSpot == Vector2.zero)
            {
                c.defaultHotSpot = new Vector2(22, 17);
                objModified = true;
                sceneModified = true;
            }
            if (c.hoverHotSpot == Vector2.zero)
            {
                c.hoverHotSpot = new Vector2(22, 17);
                objModified = true;
                sceneModified = true;
            }
            
            if (objModified)
            {
                EditorUtility.SetDirty(c);
                updatedSceneObjs++;
            }
        }
        
        if (sceneModified)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        Debug.Log($"[Auto Fix] Đã cập nhật tọa độ chuột cho {updatedPrefabs} Prefabs và {updatedSceneObjs} object trong Scene hiện tại!");
    }
}
