using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Road))]
public class RoadInspector : Editor {
    private enum PointType {
        position,
        direction0,
        direction1,
        normal
    }

    private struct EditablePoint {
        public int index;
        public PointType type;

        public bool isValid() {
            return index >= 0;
        }

        public bool isValid(Road road) {
            return index >= 0 && index < road.size;
        }

        public EditablePoint(int index, PointType type) {
            this.index = index;
            this.type = type;
        }

        public static EditablePoint Empty() {
            return new EditablePoint(-1, PointType.position);
        }
    }

    private struct ViewSettings {
        public bool showNormals;
        public bool showDirections;
        public bool autoDirections;

        public static ViewSettings Default() {
            ViewSettings vs;
            vs.showNormals = true;
            vs.showDirections = true;
            vs.autoDirections = false;
            return vs;
        }
    }

    private Road road;
    private Transform transform;
    private Quaternion rotation;

    private EditablePoint selectedElement = EditablePoint.Empty();

    private ViewSettings viewSettings;

    private void OnSceneGUI() {
        road = target as Road;
        transform = road.transform;
        rotation = Tools.pivotRotation == PivotRotation.Local ? transform.rotation : Quaternion.identity;

        for (int i = 0; i < road.size - 1; ++i) {
            ShowPart(road[i], road[i + 1]);
        }

        for (int i = 0; i < road.size; ++i) {
            ShowWaypoint(i, i == selectedElement.index);
        }

        showSelectedElement(selectedElement);

        if (selectedElement.isValid(road)) {
            ShowNormal(selectedElement.index);
            ShowDirections(selectedElement.index);
        }
    }

    private void ShowPart(RoadPoint prev, RoadPoint next) {
        var right = RoadPoint.getDirections(prev, next, true);
        var left = RoadPoint.getDirections(prev, next, false);

        Vector3 r0 = prev.rightCorner;
        Vector3 r1 = next.rightCorner;
        Vector3 l0 = prev.leftCorner;
        Vector3 l1 = next.leftCorner;

        DrawBezier(r0, r1, r0 + right.prev, r1 + right.next, Color.red);
        DrawBezier(l0, l1, l0 + left.prev, l1 + left.next, Color.blue);
    }

    private void DrawBezier(Vector3 r0, Vector3 r1, Vector3 r2, Vector3 r3, Color color) {
        r0 = transform.TransformPoint(r0);
        r1 = transform.TransformPoint(r1);
        r2 = transform.TransformPoint(r2);
        r3 = transform.TransformPoint(r3);
        Handles.DrawBezier(r0, r1, r2, r3, color, null, 2f);
    }

    private void ShowWaypoint(int index, bool forceShow = false) {
        var r = road[index];
        float size = HandleUtility.GetHandleSize(transform.TransformPoint(r.position));

        MakeButton(r.position, size, index, PointType.position, Color.black);

        if (forceShow || viewSettings.showDirections) {
            MakeButton(r.position + r.direction0, size, index, PointType.direction0, Color.white);
            MakeButton(r.position + r.direction1, size, index, PointType.direction1, Color.white);
            ShowDirections(index);
        }

        if (forceShow || viewSettings.showNormals) {
            MakeButton(r.position + r.normal, size, index, PointType.normal, Color.gray);
            ShowNormal(index);
        }
    }

    private void MakeButton(Vector3 localPosition, float size, int index, PointType type, Color color) {
        Handles.color = color;
        var pos = transform.TransformPoint(localPosition);
        if (Handles.Button(pos, rotation, size * 0.04f, size * 0.06f, Handles.DotCap)) {
            selectedElement = new EditablePoint(index, type);
            Repaint();
        }
    }

    private void ShowDirections(int index) {
        DrawLine(road[index].position, road[index].position + road[index].direction0, Color.black);
        DrawLine(road[index].position, road[index].position + road[index].direction1, Color.black);
    }

    private void ShowNormal(int index) {
        DrawLine(road[index].position, road[index].position + road[index].normal, Color.black);
    }

    private void DrawLine(Vector3 localBegin, Vector3 localEnd, Color color) {
        Handles.color = color;
        Handles.DrawLine(transform.TransformPoint(localBegin), transform.TransformPoint(localEnd));
    }

    private void showSelectedElement(EditablePoint point) {
        if (!point.isValid(road)) return;

        switch (point.type) {
            case PointType.position: {
                Vector3 v = transform.TransformPoint(road[point.index].position);
                EditorGUI.BeginChangeCheck();
                v = Handles.DoPositionHandle(v, rotation);
                if (EditorGUI.EndChangeCheck()) {
                    recordObject("move point");
                    road[point.index].position = transform.InverseTransformPoint(v);
                    if (viewSettings.autoDirections) {
                        road.RecalculateDirections(point.index);
                    }
                }
                break;
            }
            case PointType.direction0: {
                var p = road[point.index];
                Vector3 dir0 = transform.TransformPoint(p.direction0 + p.position);
                EditorGUI.BeginChangeCheck();
                dir0 = Handles.DoPositionHandle(dir0, rotation);
                if (EditorGUI.EndChangeCheck()) {
                    recordObject("change direction0");
                    p.direction0 = transform.InverseTransformPoint(dir0) - p.position;
                }
                break;
            }
            case PointType.direction1: {
                var p = road[point.index];
                Vector3 dir1 = transform.TransformPoint(p.direction1 + p.position);

                EditorGUI.BeginChangeCheck();
                dir1 = Handles.DoPositionHandle(dir1, rotation);
                if (EditorGUI.EndChangeCheck()) {
                    recordObject("change direction0");
                    p.direction1 = transform.InverseTransformPoint(dir1) - p.position;
                }
                break;
            }
            case PointType.normal: {
                var p = road[point.index];
                Vector3 dir1 = transform.TransformPoint(p.normal + p.position);

                EditorGUI.BeginChangeCheck();
                dir1 = Handles.DoPositionHandle(dir1, rotation);
                if (EditorGUI.EndChangeCheck()) {
                    recordObject("change normal");
                    p.normal = transform.InverseTransformPoint(dir1) - p.position;
                }
                break;
            }
            default: {
                throw new ArgumentException(point.type.ToString());
            }
        }
    }

    private void recordObject(string reason) {
        Undo.RecordObject(road, reason);
        EditorUtility.SetDirty(road);
    }

    public override void OnInspectorGUI() {
        road = target as Road;

        if (selectedElement.isValid(road)) {
            var waypoing = road[selectedElement.index];

            EditorGUI.BeginChangeCheck();
            var mode = (RoadPoint.DirectionMode) EditorGUILayout.EnumPopup("direction mode", waypoing.mode);
            if (EditorGUI.EndChangeCheck()) {
                recordObject("change point mode");
                waypoing.mode = mode;
            }

            EditorGUI.BeginChangeCheck();
            float width = EditorGUILayout.Slider(waypoing.width, 0, 5);
            if (EditorGUI.EndChangeCheck()) {
                recordObject("change point mode");
                waypoing.width = width;
            }
        }

        viewSettings.showNormals = toggle("show normals", viewSettings.showNormals);
        viewSettings.showDirections = toggle("show directions", viewSettings.showDirections);
        viewSettings.autoDirections = toggle("auto direction", viewSettings.autoDirections);

        if (GUILayout.Button("add waypoint")) {
            recordObject("add waypoint");
            road.AddPoint();
        }

        if (selectedElement.isValid(road)) {
            if (GUILayout.Button("insert waypoint")) {
                recordObject("insert waypoint");
                road.InsertPointAfter(selectedElement.index);
            }
        }

        if (GUILayout.Button("recalculate all waypoints")) {
            recordObject("recalculate all waypoints");
            for (int i = 0; i < road.size; ++i) {
                road.RecalculateDirections(i);
            }
        }

        if (GUILayout.Button("generate geometry")) {
            Undo.RecordObject(road.gameObject, "generating geometry");
            EditorUtility.SetDirty(road.gameObject);
            road.GenerateGeometry();
        }
    }

    private bool toggle(string name, bool value) {
        EditorGUI.BeginChangeCheck();
        bool newValue = EditorGUILayout.Toggle(name, value);
        if (EditorGUI.EndChangeCheck()) {
            SceneView.RepaintAll();
        }
        return newValue;
    }
}