using UnityEngine;

public static class CustomExtensions {
    public static bool HasComponent<T>(this GameObject obj) where T : Component {
        return obj.GetComponent<T>() != null;
    }

    public static bool HasComponent<T>(this GameObject obj, T component) where T : Component {
        return obj.GetComponent<T>() == component;
    }
}