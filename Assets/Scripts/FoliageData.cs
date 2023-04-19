using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class FoliageData
{
    public Mesh Mesh;
    public Material Material;
    public ShadowCastingMode RenderShadows;
    public bool ReceiveShadows = true;
    public string ID;
    public List<Matrix4x4> Matrice = new List<Matrix4x4>();
    
    public FoliageData(string id, Mesh mesh, Material material, ShadowCastingMode renderShadows, bool receiveShadows) 
    {
        ID = id;
        Mesh = mesh;
        Material = material;
        RenderShadows = renderShadows;
        ReceiveShadows = receiveShadows;
    }

    public Vector3 Scale(int matriceIndex)
    {
        Matrix4x4 m = Matrice[matriceIndex];
        return m.lossyScale;
        // return new Vector3(desiredMatrice.m03, desiredMatrice.m13, desiredMatrice.m23);
    }

    public Vector3 Position(int matriceIndex)
    {
        Matrix4x4 m = Matrice[matriceIndex];
        return new Vector3(m.m03, m.m13, m.m23);
    }

    public Matrix4x4[] VisibleMatrices()
    {
        Matrix4x4[] test = Matrice.Where(matrice => Vector3.Distance(Camera.main.transform.position, new Vector3(matrice.m03, matrice.m13, matrice.m23)) <= 10f).ToArray();
        Debug.Log(test.Length);
        return Matrice.Where(matrice => Vector3.Distance(Camera.main.transform.position, new Vector3(matrice.m03, matrice.m13, matrice.m23)) <= 100f).ToArray();
    }


    public void Clear()
    {
        Mesh = null;
        Material = null;
        Matrice.Clear();
    }
}