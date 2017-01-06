using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Road : MonoBehaviour {
    [SerializeField] private List<RoadPoint> points = initList();

    public void Reset() {
        points = initList();
    }

    public RoadPoint this[int index] {
        get { return points[index]; }
        set { points[index] = value; }
    }

    public int size {
        get { return points.Count; }
    }

    public void AddPoint() {
        var last = points.Last();
        points.Add(new RoadPoint(last.position + last.direction1 * 3, last.direction1));
        points.Last().width = last.width;
    }

    public bool hasNext(int index) {
        return index < size - 1;
    }

    public bool hasPrevious(int index) {
        return index > 0;
    }

    private void Awake() {
        GenerateGeometry();
    }

    public void InsertPointAfter(int index, bool recalculateDirections = true) {
        if (!hasNext(index)) {
            AddPoint();
            return;
        }

        var prev = points[index];
        var next = points[index + 1];

        var position = 1f / 2 * (next.position + prev.position) + 3f / 8 * (prev.direction1 + next.direction0);
        var p = new RoadPoint(position, (next.position - prev.position) / 3);
        p.width = 0.5f * (prev.width + next.width);

        points.Insert(index + 1, p);
        RecalculateDirections(index);
        RecalculateDirections(index + 1);
        RecalculateDirections(index + 2);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;
        foreach (var point in points) {
            Gizmos.DrawSphere(transform.TransformPoint(point.rightCorner), 0.05f);
        }
        Gizmos.color = Color.black;
        foreach (var point in points) {
            Gizmos.DrawSphere(transform.TransformPoint(point.leftCorner), 0.05f);
        }
    }

    private static List<RoadPoint> initList() {
        var list = new List<RoadPoint>();

        list.Add(new RoadPoint(new Vector3(1, 0, 0), new Vector3(0.33f, 0, 0)));
        list.Add(new RoadPoint(new Vector3(2, 0, 0), new Vector3(0.33f, 0, 0)));

        return list;
    }

    private static RoadPoint[] initArray() {
        var arr = new RoadPoint[2];

        arr[0] = new RoadPoint(new Vector3(1, 0, 0), new Vector3(0.33f, 0, 0));
        arr[1] = new RoadPoint(new Vector3(2, 0, 0), new Vector3(0.33f, 0, 0));

        return arr;
    }

    public void RecalculateDirections(int index) {
        bool hasNext = index < points.Count - 1;
        bool hasPreivous = index > 0;

        if (!hasNext && !hasPreivous) return;

        if (!hasNext) {
            points[index].direction0 = (points[index - 1].position - points[index].position) / 3;
            points[index].normal = points[index - 1].normal;
            return;
        }

        if (!hasPreivous) {
            points[index].direction1 = (points[index + 1].position - points[index].position) / 3;
            points[index].normal = points[index + 1].normal;
            return;
        }

        var drNext = (points[index + 1].position - points[index].position);
        var drPrev = (points[index - 1].position - points[index].position);

        var direction = drNext / drNext.sqrMagnitude - drPrev / drPrev.sqrMagnitude;
        direction.Normalize();

        float length = Mathf.Sqrt(drNext.magnitude * drPrev.magnitude) / 3;
        direction *= length;

        points[index].direction1 = direction;
        points[index].direction0 = -direction;

        var summ = (drNext.magnitude + drPrev.magnitude);

        points[index].normal = points[index + 1].normal * drPrev.magnitude / summ +
                               points[index - 1].normal * drNext.magnitude / summ;
    }

    public void GenerateGeometry(int stepsCount = 4) {
        var vertices = new Vector3[2 * (stepsCount * size + 1 - stepsCount)];
        int vPos = 0;

        for (int i = 0; i < size - 1; i++) {
            RoadPoint prev = points[i];
            RoadPoint next = points[i + 1];

            var right = RoadPoint.getDirections(prev, next, true);
            var left = RoadPoint.getDirections(prev, next, false);

            Vector3 r0 = prev.rightCorner;
            Vector3 r1 = next.rightCorner;
            Vector3 l0 = prev.leftCorner;
            Vector3 l1 = next.leftCorner;

            vertices[vPos++] = prev.rightCorner;
            vertices[vPos++] = prev.leftCorner;

            for (int j = 1; j < stepsCount; ++j) {
                var t = ((float) j) / stepsCount;
                vertices[vPos++] = Bezier.GetPoint(r0, r0 + right.prev, r1 + right.next, r1, t);
                vertices[vPos++] = Bezier.GetPoint(l0, l0 + left.prev, l1 + left.next, l1, t);
            }
        }
        vertices[vPos++] = points.Last().rightCorner;
        vertices[vPos++] = points.Last().leftCorner;

        var triangles = new int[6 * (stepsCount * (size - 1))];
        var off = 0;
        for (int i = 0; i < triangles.Length;) {
            triangles[i++] = off;
            triangles[i++] = off + 1;
            triangles[i++] = off + 2;
            triangles[i++] = off + 1;
            triangles[i++] = off + 3;
            triangles[i++] = off + 2;

            off += 2;
        }

        var mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "procedural road";

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}