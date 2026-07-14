#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AddEButtonToLancer : MonoBehaviour
{
    [MenuItem("Tools/Add E-Button to Lancer Prefab")]
    public static void AddEButton()
    {
        string turtlePath = "Assets/_Shared_Resources/Terrain/NPC/Turtle_Idle.prefab";
        string lancerPath = "Assets/_Shared_Resources/Terrain/NPC/Lancer_Idle.prefab";

        GameObject turtlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(turtlePath);
        GameObject lancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lancerPath);

        if (turtlePrefab == null || lancerPrefab == null)
        {
            Debug.LogError("Could not find Turtle or Lancer prefabs!");
            return;
        }

        GameObject lancerInstance = (GameObject)PrefabUtility.InstantiatePrefab(lancerPrefab);
        
        InjectEButtonSystem(turtlePrefab, lancerInstance);

        PrefabUtility.SaveAsPrefabAsset(lancerInstance, lancerPath);
        DestroyImmediate(lancerInstance);

        Debug.Log("✅ Successfully added E-Button and Dialogue Trigger to Lancer_Idle.prefab!");
    }

    [MenuItem("Tools/Add E-Button to Selected Object")]
    public static void AddEButtonToSelected()
    {
        string turtlePath = "Assets/_Shared_Resources/Terrain/NPC/Turtle_Idle.prefab";
        GameObject turtlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(turtlePath);

        if (turtlePrefab == null)
        {
            Debug.LogError("Could not find Turtle prefab!");
            return;
        }

        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogError("Please select an object in the Scene Hierarchy first!");
            return;
        }

        InjectEButtonSystem(turtlePrefab, selectedObj);
        Debug.Log($"✅ Successfully added E-Button and Dialogue Trigger to {selectedObj.name}!");
    }

    private static void InjectEButtonSystem(GameObject sourceTurtle, GameObject targetObj)
    {
        // 1. Copy the DialogueTrigger script
        DialogueTrigger turtleTrigger = sourceTurtle.GetComponent<DialogueTrigger>();
        if (turtleTrigger != null)
        {
            DialogueTrigger targetTrigger = targetObj.GetComponent<DialogueTrigger>();
            if (targetTrigger == null)
            {
                targetTrigger = targetObj.AddComponent<DialogueTrigger>();
            }
            
            // Copy basic properties
            targetTrigger.requireProximity = turtleTrigger.requireProximity;
            EditorUtility.CopySerialized(turtleTrigger, targetTrigger);
            
            // Clear the old UnityEvents
            targetTrigger.onPlayerEnterRange = new UnityEngine.Events.UnityEvent();
            targetTrigger.onPlayerExitRange = new UnityEngine.Events.UnityEvent();

            // 2. Copy the Canvas and EButton child objects
            Transform turtleCanvas = sourceTurtle.transform.Find("Canvas");
            Transform turtleEButton = sourceTurtle.transform.Find("EButton");

            GameObject newCanvas = null;

            if (turtleCanvas != null && targetObj.transform.Find("Canvas") == null)
            {
                newCanvas = Instantiate(turtleCanvas.gameObject, targetObj.transform);
                newCanvas.name = "Canvas";
            }
            else
            {
                var c = targetObj.transform.Find("Canvas");
                if (c != null) newCanvas = c.gameObject;
            }

            if (turtleEButton != null && targetObj.transform.Find("EButton") == null)
            {
                GameObject newEButton = Instantiate(turtleEButton.gameObject, targetObj.transform);
                newEButton.name = "EButton";
            }

            // 3. Wire up the UnityEvents to the NEW Canvas
            if (newCanvas != null)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(
                    targetTrigger.onPlayerEnterRange, 
                    new UnityEngine.Events.UnityAction<bool>(newCanvas.SetActive), 
                    true
                );

                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(
                    targetTrigger.onPlayerExitRange, 
                    new UnityEngine.Events.UnityAction<bool>(newCanvas.SetActive), 
                    false
                );
            }

            // 4. Ensure there is a BoxCollider2D (Is Trigger)
            BoxCollider2D col = targetObj.GetComponentInChildren<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                // If it doesn't have one, add it to the root
                col = targetObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(2f, 2f); // generous default size
            }
        }
    }
}
#endif
