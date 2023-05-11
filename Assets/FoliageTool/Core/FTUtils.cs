using System.Collections.Generic;
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

    /// <summary>
    /// Generate points in a grid x,y based on bounds.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="density"></param>
    /// <param name="disorder"></param>
    /// <param name="keepOutOfZone"></param>
    /// <returns></returns>
    public static Vector3[] GetGridPoints(Bounds bounds, float density, float disorder, bool keepOutOfZone = true)
    {
        Vector3 center = bounds.center;
        float width = bounds.size.x;
        float height = bounds.size.z;

        List<Vector3> points = new List<Vector3>();

        int numWidthPoints = Mathf.CeilToInt(width * density);
        float widthDistance = width / numWidthPoints;

        int numHeightPoints = Mathf.CeilToInt(height * density);
        float heightDistance = height / numHeightPoints;

        for (int i=0; i< numWidthPoints; i++)
        {
            for (int j=0; j<numHeightPoints; j++)
            {
                Vector3 disorderOffset = new Vector3(Random.Range(-disorder, disorder), 0f, Random.Range(-disorder, disorder));

                Vector3 pointOffset = new Vector3((i * widthDistance) - width/2, bounds.size.y / 2, (j * heightDistance) - height/2);

                Vector3 point = center + pointOffset + disorderOffset;

                if (!keepOutOfZone)
                {
                    if (point.x < center.x - width / 2 || 
                        point.x > center.x + width / 2 ||
                        point.z < center.z - height / 2 ||
                        point.z > center.z + height / 2) 
                        continue;
                }

                points.Add(point);
            }
        }

        return points.ToArray();
    }
}
