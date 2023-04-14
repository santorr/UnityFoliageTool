using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public enum EMethod
{
    Classic,
}

public class FoliageSplatterTool : MonoBehaviour
{
    [SerializeField] private EMethod _method;
    [SerializeField] public List<FoliageType> _foliageTypes = new List<FoliageType>();

    private Vector3 _bounds;
    private Vector3 _center;

    // Click on generate button
    public void GenerateData()
    {
        _bounds = transform.localScale;
        _center = transform.position;

        switch (_method)
        {
            case EMethod.Classic:
                GenerateClassicMethod();
                break;
            default: break;
        }

        // EditorUtility.SetDirty(this);
    }

    private void GenerateClassicMethod()
    {
        // Get the container
        FoliageDataContainer dataContainer = GetComponent<FoliageToolVisualizer>().DataContainer;

        if (dataContainer == null)
        {
            Debug.Log("Add a 'Data container' before generating.");
            return;
        }
        
        // Pour chacun des types de foliage
        for (int i = 0; i < _foliageTypes.Count; i++)
        {
            // Create a new Foliage data
            FoliageData newFoliageData = new FoliageData(
                mesh: _foliageTypes[i].Mesh,
                material: _foliageTypes[i].Material,
                renderShadows: _foliageTypes[i].RenderShadows,
                receiveShadows: _foliageTypes[i].ReceiveShadows
                );

            dataContainer.FoliageData.Add( newFoliageData );

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
                    Vector3 randomScale = RandomUniformScale(minimum: _foliageTypes[i].MinimumScale, maximum: _foliageTypes[i].MaximumScale);

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
                            newFoliageData.Matrice.Add(matrice);
                        }
                    }   
                }
            }   
        }
        EditorUtility.SetDirty(dataContainer);
    }

    private Vector3 RandomPositionInCircle(Vector3 position, float radius)
    {
        Vector3 randomCircle = new Vector3(Random.Range(-radius, radius), 0, Random.Range(-radius, radius));
        return position + randomCircle;
    }

    private Vector3 RandomUniformScale(float minimum, float maximum)
    {
        float randomValue = Random.Range(minimum, maximum);
        return new Vector3(randomValue, randomValue, randomValue);
    }

    private Vector3 RandomNonUniformScale(float minimum, float maximum) 
    {   
        float randomX = Random.Range(minimum, maximum);
        float randomY = Random.Range(minimum, maximum);
        float randomZ = Random.Range(minimum, maximum);
        return new Vector3(randomX, randomY, randomZ);
    }

    public void ClearData()
    {
        FoliageDataContainer foliageDataContainer = GetComponent<FoliageToolVisualizer>().DataContainer;

        if (foliageDataContainer != null)
        {
            foliageDataContainer.Clear();
        }
        else
        {
            Debug.Log("Can't clear file because no data file link.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}