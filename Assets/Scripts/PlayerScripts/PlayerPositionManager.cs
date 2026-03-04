using UnityEngine;

public static class PlayerPositionManager
{
    private static Vector3 savedPosition;
    public static bool HasSavedPosition = false;

    public static void SavePosition(Vector3 pos)
    {
        savedPosition = pos;
        HasSavedPosition = true;
    }

    public static Vector3 GetPosition()
    {
        return savedPosition;
    }
}
