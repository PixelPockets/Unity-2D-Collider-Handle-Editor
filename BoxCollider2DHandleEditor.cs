using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoxCollider2D))]
public class BoxCollider2DHandleEditor : Editor
{
    private const string HandleSizeKey       = "BoxCollider2D_HandleSize";
    private const string HandleColorKey      = "BoxCollider2D_HandleColor";
    private const string EditColliderKey     = "BoxCollider2D_EditCollider";
    private const string HandleShapeKey      = "BoxCollider2D_HandleShape";
    private const string ConsistentSizeKey   = "BoxCollider2D_ConsistentSize";

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
    private bool _useConsistentSize = true;

    private void OnEnable()
    {
        _handleSize       = EditorPrefs.GetFloat(HandleSizeKey, 0.1f);
        _handleColor      = GetColorFromPrefs(HandleColorKey, Color.green);
        _editCollider     = EditorPrefs.GetBool(EditColliderKey, false);
        _handleShape      = (HandleShape)EditorPrefs.GetInt(HandleShapeKey, (int)HandleShape.Cube);
        _useConsistentSize = EditorPrefs.GetBool(ConsistentSizeKey, true);

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
        EditorGUILayout.LabelField("Collider Handle Customization", EditorStyles.boldLabel);
        
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
        
        if (_editCollider && Tools.current != Tool.None)
        {
            EditorGUILayout.HelpBox("A scene tool is active. To use it, editing mode will be disabled.", MessageType.Warning);
        }
        else if (_editCollider)
        {
            EditorGUILayout.HelpBox("Edit mode active: use the Scene view handles to edit the collider.\nNote: Scene tools are disabled; selecting a scene tool disables handle editing.", MessageType.Info);
        }

        EditorGUILayout.Space();
        float newHandleSize = EditorGUILayout.FloatField("Handle Size", _handleSize);
        newHandleSize = Mathf.Max(0.01f, newHandleSize);
        Color newHandleColor = EditorGUILayout.ColorField("Handle Color", _handleColor);
        HandleShape newHandleShape = (HandleShape)EditorGUILayout.EnumPopup("Handle Shape", _handleShape);
        
        bool newUseConsistentSize = EditorGUILayout.Toggle("Consistent Size When Zooming", _useConsistentSize);
        if (newUseConsistentSize != _useConsistentSize)
        {
            _useConsistentSize = newUseConsistentSize;
            EditorPrefs.SetBool(ConsistentSizeKey, _useConsistentSize);
            SceneView.RepaintAll();
        }

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
        
        BoxCollider2D collider = (BoxCollider2D)target;
        Vector2 size = collider.size;
        Vector3 offset = collider.offset;
        Vector3 colliderCenter = collider.transform.position + offset;

        Vector3 top    = colliderCenter + new Vector3(0, size.y / 2, 0);
        Vector3 bottom = colliderCenter - new Vector3(0, size.y / 2, 0);
        Vector3 left   = colliderCenter - new Vector3(size.x / 2, 0, 0);
        Vector3 right  = colliderCenter + new Vector3(size.x / 2, 0, 0);

        Handles.color = _handleColor;
        
        float effectiveHandleSize = _handleSize;
        if (_useConsistentSize)
        {
            effectiveHandleSize *= HandleUtility.GetHandleSize(colliderCenter);
        }
        
        Vector3 newTop    = Handles.FreeMoveHandle(top, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newBottom = Handles.FreeMoveHandle(bottom, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newLeft   = Handles.FreeMoveHandle(left, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newRight  = Handles.FreeMoveHandle(right, effectiveHandleSize, Vector3.zero, GetHandleCap());

        if (newTop != top || newBottom != bottom || newLeft != left || newRight != right)
        {
            Undo.RecordObject(collider, "Resize BoxCollider2D");
            size = new Vector2(Mathf.Abs(newRight.x - newLeft.x), Mathf.Abs(newTop.y - newBottom.y));
            offset = new Vector2((newLeft.x + newRight.x) / 2 - collider.transform.position.x,
                                 (newTop.y + newBottom.y) / 2 - collider.transform.position.y);
            collider.size = size;
            collider.offset = offset;
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
