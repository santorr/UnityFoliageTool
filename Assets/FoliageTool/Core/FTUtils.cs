using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
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
    public static Vector3[] GetGridPoints(Bounds bounds, Vector3 normal, float density, float disorder, Texture2D mask = null)
    {
        Vector3 center = bounds.center;
        float width = bounds.size.x;
        float depth = bounds.size.z;
        float height = bounds.size.y;

        // If mask is null, create a white texture as mask
        Texture2D texture = mask == null ? Texture2D.whiteTexture : mask;

        // Create a list to add matching points
        List<Vector3> points = new List<Vector3>();

        // Rotation to apply to create the grid on the right angle, e.g : wall, ground
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

        // Setup the number of iteration x,y and the distance between each iteration from density and bounds
        int numWidthPoints = Mathf.CeilToInt(width * density);
        float widthDistance = width / numWidthPoints;
        int numHeightPoints = Mathf.CeilToInt(depth * density);
        float heightDistance = depth / numHeightPoints;

        // Create the grid x,y
        for (int i=0; i< numWidthPoints; i++)
        {
            for (int j=0; j<numHeightPoints; j++)
            {
                // Create a random offset based on disorder
                Vector3 disorderOffset = new Vector3(Random.Range(-disorder, disorder), 0f, Random.Range(-disorder, disorder));

                Vector3 pointOffset = new Vector3((i * widthDistance), height/2, (j * heightDistance)) + disorderOffset;

                // Return if the point on the grid is outside of the bounds
                if (pointOffset.x < 0 || pointOffset.z < 0 || pointOffset.x > width || pointOffset.z > depth) continue;

                // Convert world position to texture pixel position
                int xPos = Mathf.CeilToInt((pointOffset.x / width) * texture.width);
                int yPos = Mathf.CeilToInt((pointOffset.z / depth) * texture.height);
                Color color = texture.GetPixel(x: xPos, y: yPos);

                // Return if black pixel
                if (color == Color.black) continue;

                Vector3 worldPosition = rotation * (pointOffset - new Vector3(width / 2, 0, depth / 2)) + center;

                points.Add(worldPosition);
            }
        }

        return points.ToArray();
    }
}
