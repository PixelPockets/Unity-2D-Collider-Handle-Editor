using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CompositeCollider2D))]
public class CompositeCollider2DHandleEditor : Editor
{
    private const string GlobalHandleSizeKey = "CompositeCollider_GlobalHandleSize";
    private const string HandleColorKey      = "CompositeCollider_HandleColor";
    private const string EditHandlesKey      = "CompositeCollider_EditHandles";
    private const string HandleShapeKey      = "CompositeCollider_HandleShape";
    private const string ConsistentSizeKey   = "CompositeCollider_ConsistentSize";

    private float _globalHandleSize = 0.1f;
    private Color _handleColor = Color.green;
    private bool _editHandles;
    private bool _useConsistentSize = true;

    private enum HandleShape 
    { 
        Cube, 
        Sphere, 
        Rectangle, 
        Circle
    }
    private HandleShape _handleShape = HandleShape.Cube;

    private void OnEnable()
    {
        _globalHandleSize = EditorPrefs.GetFloat(GlobalHandleSizeKey, 0.1f);
        _handleColor = GetColorFromPrefs(HandleColorKey, Color.green);
        _editHandles = EditorPrefs.GetBool(EditHandlesKey, false);
        _handleShape = (HandleShape)EditorPrefs.GetInt(HandleShapeKey, (int)HandleShape.Cube);
        _useConsistentSize = EditorPrefs.GetBool(ConsistentSizeKey, true);

        if (_editHandles)
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
        EditorGUILayout.LabelField("Global Collider Handle Customization", EditorStyles.boldLabel);

        bool newEditHandles = GUILayout.Toggle(_editHandles, "Edit Collider Handles");
        if (newEditHandles != _editHandles)
        {
            _editHandles = newEditHandles;
            EditorPrefs.SetBool(EditHandlesKey, _editHandles);
            if (_editHandles)
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

        if (_editHandles && Tools.current != Tool.None)
        {
            EditorGUILayout.HelpBox("A scene tool is active. To use it, editing mode will be disabled.", MessageType.Warning);
        }
        else if (_editHandles)
        {
            EditorGUILayout.HelpBox("Edit mode active: use the Scene view handles to adjust the composite collider.\nSelecting a scene tool disables handle editing.", MessageType.Info);
        }

        EditorGUILayout.Space();
        float newGlobalHandleSize = EditorGUILayout.FloatField("Global Handle Size", _globalHandleSize);
        newGlobalHandleSize = Mathf.Max(0.01f, newGlobalHandleSize);
        Color newHandleColor = EditorGUILayout.ColorField("Handle Color", _handleColor);
        HandleShape newHandleShape = (HandleShape)EditorGUILayout.EnumPopup("Handle Shape", _handleShape);
        
        bool newUseConsistentSize = EditorGUILayout.Toggle("Consistent Size When Zooming", _useConsistentSize);
        if (newUseConsistentSize != _useConsistentSize)
        {
            _useConsistentSize = newUseConsistentSize;
            EditorPrefs.SetBool(ConsistentSizeKey, _useConsistentSize);
            SceneView.RepaintAll();
        }

        if (!Mathf.Approximately(newGlobalHandleSize, _globalHandleSize) || newHandleColor != _handleColor || newHandleShape != _handleShape)
        {
            _globalHandleSize = newGlobalHandleSize;
            _handleColor = newHandleColor;
            _handleShape = newHandleShape;
            EditorPrefs.SetFloat(GlobalHandleSizeKey, _globalHandleSize);
            SaveColorToPrefs(HandleColorKey, _handleColor);
            EditorPrefs.SetInt(HandleShapeKey, (int)_handleShape);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        CompositeCollider2D compositeCollider = (CompositeCollider2D)target;

        if (_editHandles && Tools.current != Tool.None)
        {
            _editHandles = false;
            EditorPrefs.SetBool(EditHandlesKey, _editHandles);
            Tools.hidden = false;
            SceneView.RepaintAll();
            return;
        }
        if (!_editHandles)
            return;

        Tools.current = Tool.None;

        Collider2D[] childColliders = compositeCollider.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D childCollider in childColliders)
        {
            if (childCollider == compositeCollider)
                continue;
            if (!childCollider.enabled)
                continue;

            if (childCollider is BoxCollider2D boxCollider)
                DrawBoxColliderHandles(boxCollider);
            else if (childCollider is CircleCollider2D circleCollider)
                DrawCircleColliderHandles(circleCollider);
            else if (childCollider is PolygonCollider2D polygonCollider)
                DrawPolygonColliderHandles(polygonCollider);
            else if (childCollider is CapsuleCollider2D capsuleCollider)
                DrawCapsuleColliderHandles(capsuleCollider);
        }
    }

    private void DrawBoxColliderHandles(BoxCollider2D boxCollider)
    {
        Vector2 size = boxCollider.size;
        Vector2 offset = boxCollider.offset;
        Vector3 colliderCenter = boxCollider.transform.position + (Vector3)offset;
        Vector3 top = colliderCenter + new Vector3(0, size.y / 2, 0);
        Vector3 bottom = colliderCenter - new Vector3(0, size.y / 2, 0);
        Vector3 left = colliderCenter - new Vector3(size.x / 2, 0, 0);
        Vector3 right = colliderCenter + new Vector3(size.x / 2, 0, 0);

        Handles.color = _handleColor;
        
        float effectiveHandleSize = _globalHandleSize;
        if (_useConsistentSize)
        {
            effectiveHandleSize *= HandleUtility.GetHandleSize(colliderCenter);
        }
        
        Vector3 newTop = Handles.FreeMoveHandle(top, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newBottom = Handles.FreeMoveHandle(bottom, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newLeft = Handles.FreeMoveHandle(left, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newRight = Handles.FreeMoveHandle(right, effectiveHandleSize, Vector3.zero, GetHandleCap());

        if (newTop != top || newBottom != bottom || newLeft != left || newRight != right)
        {
            Undo.RecordObject(boxCollider, "Resize BoxCollider2D");
            size = new Vector2(Mathf.Abs(newRight.x - newLeft.x), Mathf.Abs(newTop.y - newBottom.y));
            offset = new Vector2((newLeft.x + newRight.x) / 2 - boxCollider.transform.position.x,
                                 (newTop.y + newBottom.y) / 2 - boxCollider.transform.position.y);
            boxCollider.size = size;
            boxCollider.offset = offset;
        }
    }

    private void DrawCircleColliderHandles(CircleCollider2D circleCollider)
    {
        Vector2 offset = circleCollider.offset;
        float radius = circleCollider.radius;
        Vector3 colliderCenter = circleCollider.transform.position + (Vector3)offset;
        Vector3 handlePosition = colliderCenter + new Vector3(radius, 0, 0);
        Handles.color = _handleColor;
        
        float effectiveHandleSize = _globalHandleSize;
        if (_useConsistentSize)
        {
            effectiveHandleSize *= HandleUtility.GetHandleSize(colliderCenter);
        }
        
        Vector3 newHandlePosition = Handles.FreeMoveHandle(handlePosition, effectiveHandleSize, Vector3.zero, GetHandleCap());
        if (newHandlePosition != handlePosition)
        {
            Undo.RecordObject(circleCollider, "Resize CircleCollider2D");
            radius = Vector3.Distance(colliderCenter, newHandlePosition);
            circleCollider.radius = radius;
        }
    }

    private void DrawPolygonColliderHandles(PolygonCollider2D polygonCollider)
    {
        Vector2[] points = polygonCollider.points;
        Handles.color = _handleColor;
        
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPoint = polygonCollider.transform.TransformPoint(points[i]);
            
            float effectiveHandleSize = _globalHandleSize;
            if (_useConsistentSize)
            {
                effectiveHandleSize *= HandleUtility.GetHandleSize(worldPoint);
            }
            
            Vector3 newWorldPoint = Handles.FreeMoveHandle(worldPoint, effectiveHandleSize, Vector3.zero, GetHandleCap());
            if (newWorldPoint != worldPoint)
            {
                Undo.RecordObject(polygonCollider, "Edit PolygonCollider2D Point");
                points[i] = polygonCollider.transform.InverseTransformPoint(newWorldPoint);
                polygonCollider.points = points;
            }
        }
    }

    private void DrawCapsuleColliderHandles(CapsuleCollider2D capsuleCollider)
    {
        Vector2 size = capsuleCollider.size;
        Vector2 offset = capsuleCollider.offset;
        Vector3 colliderCenter = capsuleCollider.transform.position + (Vector3)offset;
        Vector3 top = colliderCenter + new Vector3(0, size.y / 2, 0);
        Vector3 bottom = colliderCenter - new Vector3(0, size.y / 2, 0);
        Vector3 left = colliderCenter - new Vector3(size.x / 2, 0, 0);
        Vector3 right = colliderCenter + new Vector3(size.x / 2, 0, 0);
        Handles.color = _handleColor;
        
        float effectiveHandleSize = _globalHandleSize;
        if (_useConsistentSize)
        {
            effectiveHandleSize *= HandleUtility.GetHandleSize(colliderCenter);
        }
        
        Vector3 newTop = Handles.FreeMoveHandle(top, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newBottom = Handles.FreeMoveHandle(bottom, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newLeft = Handles.FreeMoveHandle(left, effectiveHandleSize, Vector3.zero, GetHandleCap());
        Vector3 newRight = Handles.FreeMoveHandle(right, effectiveHandleSize, Vector3.zero, GetHandleCap());
        if (newTop != top || newBottom != bottom || newLeft != left || newRight != right)
        {
            Undo.RecordObject(capsuleCollider, "Resize CapsuleCollider2D");
            size = new Vector2(Mathf.Abs(newRight.x - newLeft.x), Mathf.Abs(newTop.y - newBottom.y));
            offset = new Vector2((newLeft.x + newRight.x) / 2 - capsuleCollider.transform.position.x,
                                 (newTop.y + newBottom.y) / 2 - capsuleCollider.transform.position.y);
            capsuleCollider.size = size;
            capsuleCollider.offset = offset;
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
