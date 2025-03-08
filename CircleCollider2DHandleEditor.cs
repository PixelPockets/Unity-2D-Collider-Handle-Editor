using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CircleCollider2D))]
public class CircleCollider2DHandleEditor : Editor
{
    private const string HandleSizeKey   = "CircleCollider2D_HandleSize";
    private const string HandleColorKey  = "CircleCollider2D_HandleColor";
    private const string EditColliderKey = "CircleCollider2D_EditCollider";
    private const string HandleShapeKey  = "CircleCollider2D_HandleShape";

    private enum HandleShape 
    { 
        Cube,
        Sphere,
        Rectangle,
        Circle
    }
    private HandleShape _handleShape = HandleShape.Cube;

    private float _handleSize = 0.1f;
    private Color _handleColor = Color.green;
    private bool _editCollider;

    private void OnEnable()
    {
        _handleSize   = EditorPrefs.GetFloat(HandleSizeKey, 0.1f);
        _handleColor  = GetColorFromPrefs(HandleColorKey, Color.green);
        _editCollider = EditorPrefs.GetBool(EditColliderKey, false);
        _handleShape  = (HandleShape)EditorPrefs.GetInt(HandleShapeKey, (int)HandleShape.Cube);

        if (_editCollider)
        {
            Tools.current = Tool.None;
            Tools.hidden = true;
        }
        else
        {
            Tools.hidden = false;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        bool newEditCollider = GUILayout.Toggle(_editCollider, "Edit Collider Handles");
        if (newEditCollider != _editCollider)
        {
            _editCollider = newEditCollider;
            EditorPrefs.SetBool(EditColliderKey, _editCollider);

            if (_editCollider)
            {
                Tools.current = Tool.None;
                Tools.hidden = true;
            }
            else
            {
                Tools.hidden = false;
            }
            SceneView.RepaintAll();
        }
        
        if (_editCollider)
        {
            EditorGUILayout.HelpBox("Edit mode active: use the Scene view handles to adjust the collider.\nNote: Scene tools disabled; selecting a scene tool disables handle editing.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Handle Customization", EditorStyles.boldLabel);

        float newHandleSize = EditorGUILayout.FloatField("Handle Size", _handleSize);
        newHandleSize = Mathf.Max(0.01f, newHandleSize);
        Color newHandleColor = EditorGUILayout.ColorField("Handle Color", _handleColor);

        HandleShape newHandleShape = (HandleShape)EditorGUILayout.EnumPopup("Handle Shape", _handleShape);

        if (!Mathf.Approximately(newHandleSize, _handleSize) || newHandleColor != _handleColor || newHandleShape != _handleShape)
        {
            _handleSize = newHandleSize;
            _handleColor = newHandleColor;
            _handleShape = newHandleShape;

            EditorPrefs.SetFloat(HandleSizeKey, _handleSize);
            SaveColorToPrefs(HandleColorKey, _handleColor);
            EditorPrefs.SetInt(HandleShapeKey, (int)_handleShape);

            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        if (_editCollider && Tools.current != Tool.None)
        {
            _editCollider = false;
            EditorPrefs.SetBool(EditColliderKey, _editCollider);
            Tools.hidden = false;
            SceneView.RepaintAll();
            return;
        }

        if (!_editCollider)
            return;

        Tools.current = Tool.None;

        CircleCollider2D collider = (CircleCollider2D)target;
        Vector2 offset = collider.offset;
        float radius = collider.radius;
        Vector3 colliderCenter = collider.transform.position + (Vector3)offset;
        Vector3 handlePosition = colliderCenter + new Vector3(radius, 0, 0);

        Handles.color = _handleColor;
        Vector3 newHandlePosition = Handles.FreeMoveHandle(handlePosition, _handleSize, Vector3.zero, GetHandleCap());
        if (newHandlePosition != handlePosition)
        {
            Undo.RecordObject(collider, "Resize CircleCollider2D");
            radius = Vector3.Distance(colliderCenter, newHandlePosition);
            collider.radius = radius;
        }
    }

    private Handles.CapFunction GetHandleCap()
    {
        switch (_handleShape)
        {
            case HandleShape.Sphere:
                return Handles.SphereHandleCap;
            case HandleShape.Rectangle:
                return Handles.RectangleHandleCap;
            case HandleShape.Circle:
                return Handles.CircleHandleCap;
            case HandleShape.Cube:
            default:
                return Handles.CubeHandleCap;
        }
    }

    private void SaveColorToPrefs(string key, Color color)
    {
        EditorPrefs.SetFloat(key + "_R", color.r);
        EditorPrefs.SetFloat(key + "_G", color.g);
        EditorPrefs.SetFloat(key + "_B", color.b);
        EditorPrefs.SetFloat(key + "_A", color.a);
    }

    private Color GetColorFromPrefs(string key, Color defaultColor)
    {
        return new Color(
            EditorPrefs.GetFloat(key + "_R", defaultColor.r),
            EditorPrefs.GetFloat(key + "_G", defaultColor.g),
            EditorPrefs.GetFloat(key + "_B", defaultColor.b),
            EditorPrefs.GetFloat(key + "_A", defaultColor.a)
        );
    }
}
