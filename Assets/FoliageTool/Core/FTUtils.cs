using UnityEngine;

public static class FTUtils
{
    public static Vector3 RandomUniformVector3(float minimum, float maximum)
    {
        float randomValue = Random.Range(minimum, maximum);
        return new Vector3(randomValue, randomValue, randomValue);
    }

    public static Vector3 RandomNonUniformVector3(float minimum, float maximum)
    {
        float randomX = Random.Range(minimum, maximum);
        float randomY = Random.Range(minimum, maximum);
        float randomZ = Random.Range(minimum, maximum);
        return new Vector3(randomX, randomY, randomZ);
    }

    public static void Message(bool isDebug, object sender, string message)
    {
        if (isDebug)
        {
            Debug.Log(sender + " : " + message);
        }
    }

    // Transform a world location to grid location based on ComponentSize
    public static Vector3 TransformWorldToGrid(Vector3 worldPosition, float gridSize)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / gridSize) * gridSize,
            Mathf.Round(worldPosition.y / gridSize) * gridSize,
            Mathf.Round(worldPosition.z / gridSize) * gridSize
            );
    }
}
