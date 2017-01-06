using UnityEngine;

public static class Bezier {
    public static Vector3 GetPoint(Vector3 r0, Vector3 r1, Vector3 r2, float t) {
        var p = 1 - t;

        return p * p * r0 + t * p * 2 * r1 + t * t * r2;
    }

    public static Vector3 GetPoint(Vector3 r0, Vector3 r1, Vector3 r2, Vector3 r3, float t) {
        var p = 1 - t;

        return p * p * p * r0 + t * p * p * 3 * r1 + t * t * p * 3 * r2 + t * t * t * r3;
    }
}