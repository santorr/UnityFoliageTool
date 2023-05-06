using UnityEngine;

public static class FTUtils
{
    /// <summary>
    /// Remap a value based on min/max input and min/max output
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fromMin"></param>
    /// <param name="fromMax"></param>
    /// <param name="toMin"></param>
    /// <param name="toMax"></param>
    /// <returns></returns>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    } 

    /// <summary>
    /// Generate a random uniform scale (same value on x,y,z axis) based on min/max
    /// </summary>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <returns></returns>
    public static Vector3 RandomUniformVector3(float minimum, float maximum)
    {
        float randomValue = Random.Range(minimum, maximum);
        return new Vector3(randomValue, randomValue, randomValue);
    }

    /// <summary>
    /// Generate a random scale (random value on x,y,z axis) based on min/max
    /// </summary>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <returns></returns>
    public static Vector3 RandomNonUniformVector3(float minimum, float maximum)
    {
        float randomX = Random.Range(minimum, maximum);
        float randomY = Random.Range(minimum, maximum);
        float randomZ = Random.Range(minimum, maximum);
        return new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// Draw a message in the output log.
    /// </summary>
    /// <param name="isDebug"></param>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void Message(bool isDebug, object sender, string message)
    {
        if (isDebug)
        {
            Debug.Log(sender + " : " + message);
        }
    }

    /// <summary>
    /// Transform a world location to grid location, can return grid coordinate or world coordinate.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="isGridCoordinate"></param>
    /// <param name="gridSize"></param>
    /// <returns></returns>
    public static Vector3Int TransformWorldToGrid(Vector3 worldPosition, bool isGridCoordinate = false , int gridSize = 25)
    {
        Vector3Int gridCoordinate = new Vector3Int(
            (int)Mathf.Round(worldPosition.x / gridSize), 
            (int)Mathf.Round(worldPosition.y / gridSize), 
            (int)Mathf.Round(worldPosition.z / gridSize)
            );

        if (isGridCoordinate) return gridCoordinate;

        return gridCoordinate * gridSize;
    }
}
