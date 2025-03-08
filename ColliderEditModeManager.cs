using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ColliderEditModeManager
{
    static ColliderEditModeManager()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        GameObject selectedGameObject = Selection.activeGameObject;

        if (selectedGameObject == null || selectedGameObject.GetComponent<Collider2D>() == null)
        {
            DisableAllColliderEditing();
        }
    }

    private static void DisableAllColliderEditing()
    {
        EditorPrefs.SetBool("BoxCollider2D_EditCollider", false);
        EditorPrefs.SetBool("CircleCollider2D_EditCollider", false);
        EditorPrefs.SetBool("CapsuleCollider2D_EditCollider", false);
        EditorPrefs.SetBool("PolygonCollider2D_EditCollider", false);
        EditorPrefs.SetBool("CompositeCollider_EditHandles", false);

        Tools.hidden = false;
        SceneView.RepaintAll();
    }
}

