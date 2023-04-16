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
}
