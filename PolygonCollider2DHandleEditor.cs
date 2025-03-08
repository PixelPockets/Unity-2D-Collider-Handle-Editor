using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PolygonCollider2D))]
public class PolygonCollider2DHandleEditor : Editor
{
    private const string HandleSizeKey = "PolygonCollider2D_HandleSize";
    private const string HandleColorKey = "PolygonCollider2D_HandleColor";
    private const string EditColliderKey = "PolygonCollider2D_EditCollider";
    private const string HandleShapeKey = "PolygonCollider2D_HandleShape";

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
    private int _currentPathIndex = 0;

    private void OnEnable()
    {
        _handleSize = EditorPrefs.GetFloat(HandleSizeKey, 0.1f);
        _handleColor = GetColorFromPrefs(HandleColorKey, Color.green);
        _editCollider = EditorPrefs.GetBool(EditColliderKey, false);
        _handleShape = (HandleShape)EditorPrefs.GetInt(HandleShapeKey, (int)HandleShape.Cube);

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

        float newHandleSize = EditorGUILayout.FloatField("Handle Size", _handleSize);
        newHandleSize = Mathf.Max(0.01f, newHandleSize);
        Color newHandleColor = EditorGUILayout.ColorField("Handle Color", _handleColor);
        HandleShape newHandleShape = (HandleShape)EditorGUILayout.EnumPopup("Handle Shape", _handleShape);

        PolygonCollider2D collider = (PolygonCollider2D)target;
        if (collider.pathCount > 1)
        {
            _currentPathIndex = EditorGUILayout.IntSlider("Path Index", _currentPathIndex, 0, collider.pathCount - 1);
        }

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
            EditorGUILayout.HelpBox("Edit mode active: use the Scene view handles to adjust the collider points.\nNote: Scene tools are disabled. Selecting a scene tool disables handle editing.", MessageType.Info);
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
        PolygonCollider2D collider = (PolygonCollider2D)target;

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

        _currentPathIndex = Mathf.Clamp(_currentPathIndex, 0, collider.pathCount - 1);

        Vector2[] points = collider.GetPath(_currentPathIndex);
        Vector2[] newPoints = new Vector2[points.Length];
        bool pointsChanged = false;

        Handles.color = _handleColor;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = collider.transform.TransformPoint(points[i]);

            Vector3 newWorldPos = Handles.FreeMoveHandle(worldPos, _handleSize, Vector3.zero, GetHandleCap());

            Vector2 newLocalPos = collider.transform.InverseTransformPoint(newWorldPos);
            newPoints[i] = newLocalPos;

            if (newLocalPos != points[i])
            {
                pointsChanged = true;
            }

            if (i < points.Length - 1)
            {
                Vector3 nextWorldPos = collider.transform.TransformPoint(points[i + 1]);
                Handles.DrawLine(worldPos, nextWorldPos);
            }
            else if (points.Length > 2)
            {
                Vector3 firstWorldPos = collider.transform.TransformPoint(points[0]);
                Handles.DrawLine(worldPos, firstWorldPos);
            }
        }

        if (pointsChanged)
        {
            Undo.RecordObject(collider, "Modify PolygonCollider2D Points");
            collider.SetPath(_currentPathIndex, newPoints);
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

