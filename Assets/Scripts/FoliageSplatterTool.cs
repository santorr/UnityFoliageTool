using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FoliageSplatterTool : MonoBehaviour
{
    [SerializeField] public List<FoliageType> _foliageTypes = new List<FoliageType>();

    private Vector3 _bounds;

    public void GenerateData()
    {
        _bounds = transform.localScale;

        GenerateClassicMethod();
    }

    private void GenerateClassicMethod()
    {
        // Get the scene manager
        FTSceneManager sceneManager  = FindObjectOfType<FTSceneManager>();

        // If scene manager or scene data are null, show warning log and return
        if (sceneManager == null || sceneManager.SceneData == null)
        {
            Debug.LogWarning("Add a 'Data container' before generating.");
            return;
        }
        
        for (int i = 0; i < _foliageTypes.Count; i++)
        {
            FoliageData foliageData = sceneManager.SceneData.GetFoliageDataFromId(_foliageTypes[i].GetID);

            if (foliageData == null)
            {
                // Create a new Foliage data
                foliageData = new FoliageData(
                    id: _foliageTypes[i].GetID,
                    mesh: _foliageTypes[i].Mesh,
                    material: _foliageTypes[i].Material,
                    renderShadows: _foliageTypes[i].RenderShadows,
                    receiveShadows: _foliageTypes[i].ReceiveShadows
                    );
                sceneManager.SceneData.FoliageData.Add(foliageData);
            }

            // Start creating from grid
            int numTraceX = (int)(_bounds.x / _foliageTypes[i].SplatterDistance);
            int numTraceZ = (int)(_bounds.z / _foliageTypes[i].SplatterDistance);

            for (int vertical = 0; vertical < numTraceX; vertical++)
            {
                for (int horizontal = 0; horizontal < numTraceZ; horizontal++)
                {

                    Vector3 splatterPoint = transform.position - new Vector3(_bounds.x / 2f, 0, _bounds.z / 2f) + new Vector3(vertical * _foliageTypes[i].SplatterDistance, 0, horizontal * _foliageTypes[i].SplatterDistance);
                    Vector3 randomPosition = RandomPositionInCircle(splatterPoint, _foliageTypes[i].RandomizeDistance);

                    // Generate scale based on minimum and maximum values
                    Vector3 randomScale = FTUtils.RandomUniformVector3(minimum: _foliageTypes[i].MinimumScale, maximum: _foliageTypes[i].MaximumScale);

                    RaycastHit hit;
                    if (Physics.Raycast(randomPosition + Vector3.up * 10f, Vector3.down, out hit, 1000f, _foliageTypes[i].LayerMask))
                    {
                        if (hit.transform.gameObject)
                        {
                            Vector3 finalPosition = hit.point;
                            Quaternion finalRotation = Quaternion.identity;
                            
                            if (_foliageTypes[i].AlignToNormal)
                            {
                                finalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            }

                            if (_foliageTypes[i].RandomRotation)
                            {
                                Quaternion yRotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
                                finalRotation *= yRotation;
                            }


                            // Generate the matrix (position, rotation, scale)
                            Matrix4x4 matrice = Matrix4x4.TRS(finalPosition, finalRotation, randomScale);
                            // Add this matrice to the foliage data
                            foliageData.Matrice.Add(matrice);
                        }
                    }   
                }
            }   
        }
        EditorUtility.SetDirty(sceneManager.SceneData);
    }

    private Vector3 RandomPositionInCircle(Vector3 position, float radius)
    {
        Vector3 randomCircle = new Vector3(Random.Range(-radius, radius), 0, Random.Range(-radius, radius));
        return position + randomCircle;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}