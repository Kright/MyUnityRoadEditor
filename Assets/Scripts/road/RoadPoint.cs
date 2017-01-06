using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Apple.ReplayKit;

[System.Serializable]
public class RoadPoint {
    public enum DirectionMode {
        Free,
        Aligned,
        Mirrored
    }

    [SerializeField] private Vector3 _direction0;
    [SerializeField] private Vector3 _direction1;
    [SerializeField] private DirectionMode _mode;
    [SerializeField] private Vector3 _normal;

    [SerializeField] public Vector3 position;
    [SerializeField] public float width;

    public RoadPoint(Vector3 position, Vector3 direction) {
        this.position = position;
        _direction1 = direction;
        _direction0 = -direction;
        _mode = DirectionMode.Mirrored;
        width = 1;
        _normal = Vector3.up;
    }

    public DirectionMode mode {
        get { return _mode; }
        set {
            _mode = value;
            updateDirection(1);
        }
    }

    public Vector3 direction0 {
        get { return _direction0; }
        set {
            _direction0 = value;
            updateDirection(1);
        }
    }

    public Vector3 direction1 {
        get { return _direction1; }
        set {
            _direction1 = value;
            updateDirection(0);
        }
    }

    public Vector3 normal {
        get { return _normal; }
        set { _normal = Vector3.Normalize(value); }
    }

    public Vector3 rightCorner {
        get { return position + rightShift; }
    }

    public Vector3 leftCorner {
        get { return position - rightShift; }
    }

    private Vector3 rightShift {
        get {
            var right = Vector3.Cross(_normal, _direction1 - _direction0);
            return right * (width / 2 / right.magnitude);
        }
    }

    private Vector3 getDirection(int n) {
        switch (n) {
            case 0:
                return _direction0;
            case 1:
                return _direction1;
        }
        throw new ArgumentException(n.ToString());
    }

    private void setDirection(int n, Vector3 value) {
        switch (n) {
            case 0:
                _direction0 = value;
                return;
            case 1:
                _direction1 = value;
                return;
        }
        throw new ArgumentException(n.ToString());
    }

    private void updateDirection(int n) {
        switch (_mode) {
            case DirectionMode.Free:
                break;
            case DirectionMode.Mirrored:
                setDirection(n, -getDirection(1 - n));
                break;
            case DirectionMode.Aligned: {
                var norm = Vector3.Normalize(getDirection(1 - n));
                var newDir = norm * Vector3.Dot(norm, getDirection(n));
                setDirection(n, newDir);
                break;
            }
        }
    }

    public struct DirectionsPair {
        public Vector3 prev;
        public Vector3 next;
    }

    public static DirectionsPair getDirections(RoadPoint prev, RoadPoint next, bool rightSize) {
        var dcenter = Vector3.Magnitude(next.position - prev.position);
        float multiplier;
        if (rightSize)
            multiplier = (next.rightCorner - prev.rightCorner).magnitude / dcenter;
        else
            multiplier = (next.leftCorner - prev.leftCorner).magnitude / dcenter;

        DirectionsPair result;
        result.next = next.direction0 * multiplier;
        result.prev = prev.direction1 * multiplier;
        return result;
    }
}