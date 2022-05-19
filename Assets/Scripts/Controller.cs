using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject rocketPrefab;

    public GenTerrain terrainGenerator;

    [Header("Current")] [Unity.Collections.ReadOnly] public int currentGeneration = 0;

    [Header("Learning Settings:")]
    public int rocketsPerGen = 25;
    public float genLength = 10;
    [Range(0, 1)] public float keepFromLastGenPerc = .1f;
    [Range(0, 1)] public float maxChildOffset = .1f; //Max difference between child and parent dna

    GameObject CreateRocket(float[,,] lineStrengths, float[,] dotOffsets)
    {
        GameObject rocket = rocketPrefab; //Set to prefab else "might not be initialized before accesing" says rider
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf)
            {
                rocket = child.gameObject; //Set rocket to inactive rocket

                break; //Break when found inactive rocket to reuse
            }
        }

        if (rocket == rocketPrefab) //If no inactive rocket found to reuse instantiate new rocket
        {
            rocket = Instantiate(rocketPrefab, transform);
        }

        rocket.SetActive(false); //Not active just incase

        //Prepare rocket for use
        Rocket rocketScript = rocket.GetComponent<Rocket>();

        rocketScript.fuel = 1; //Full fuel
        rocketScript.points = 100; //Standard beginning points
        rocketScript.active = true;

        //Prepare aiNetwork
        AINetwork network = rocket.GetComponent<AINetwork>();

        network.lineStrengths = lineStrengths;
        network.dotOffsets = dotOffsets;

        //Position rocket
        rocket.transform.SetPositionAndRotation(rocketPrefab.transform.position, rocketPrefab.transform.rotation);
        
        //Activate rocket
        rocket.SetActive(true);

        return rocket;
    }

    float[,,] GenRandomLineStrengths()
    {
        AINetwork net = rocketPrefab.GetComponent<AINetwork>();
        float[,,] output = new float[net.lineStrengths.GetLength(0), net.lineStrengths.GetLength(1),
            net.lineStrengths.GetLength(2)];

        for (int column = 0; column < net.lineStrengths.GetLength(0); column++) //Fill all 3 dimensions with data
        {
            for (int dot = 0; dot < net.lineStrengths.GetLength(1); dot++)
            {
                for (int line = 0; line < net.lineStrengths.GetLength(2); line++)
                {
                    output[column, dot, line] = Random.Range(-1f, 1f);
                }
            }
        }

        return output;
    }

    float[,] GenRandomDotOffsets()
    {
        AINetwork net = rocketPrefab.GetComponent<AINetwork>();
        float[,] output = new float[net.dotOffsets.GetLength(0),net.dotOffsets.GetLength(1)];

        for (int column = 0; column < net.dotOffsets.GetLength(0); column++)
        {
            for (int dot = 0; dot < net.dotOffsets.GetLength(1); dot++)
            {
                output[column, dot] = Random.Range(-1f, 1f);
            }
        }

        return output;
    }

    float[,,] GenChildLineStrengths(float[,,] parent)
    {
        float[,,] output = new float[parent.GetLength(0),parent.GetLength(1),parent.GetLength(2)];

        for (int column = 0; column < output.GetLength(0); column++) //Fill all 3 dimensions with data
        {
            for (int dot = 0; dot < output.GetLength(1); dot++)
            {
                for (int line = 0; line < output.GetLength(2); line++)
                {
                    float old = parent[column, dot, line];
                    output[column, dot, line] = Random.Range(Mathf.Clamp(old -maxChildOffset, -1,1),
                                                            Mathf.Clamp(old +maxChildOffset, -1,1)); 
                    //output[column, dot, line] = Mathf.Clamp(old + Random.Range(-maxChildOffset, maxChildOffset), -1, 1);
                }
            }
        }

        return output;
    }

    float[,] GenChildDotOffsets(float[,] parent)
    {
        float[,] output = new float[parent.GetLength(0),parent.GetLength(1)];

        for (int column = 0; column < output.GetLength(0); column++)
        {
            for (int dot = 0; dot < output.GetLength(1); dot++)
            {
                float old = parent[column, dot];
                output[column, dot] = Random.Range(Mathf.Clamp(old -maxChildOffset, -1,1),
                                                    Mathf.Clamp(old +maxChildOffset, -1,1)); 
                //output[column, dot] = Mathf.Clamp(old + Random.Range(-maxChildOffset, maxChildOffset), -1, 1);
            }
        }

        return output;
    }

    //Begin generations progress
    void Start()
    {
        for (int i = 0; i < rocketsPerGen; i++)
        {
            CreateRocket(GenRandomLineStrengths(), GenRandomDotOffsets());
        }
    }

    static int SortByScore(GameObject a, GameObject b)
    { //Sort function for sorting bestRockets list
        return a.GetComponent<Rocket>().points.CompareTo(b.GetComponent<Rocket>().points);
    }
    
    private float timeSinceLastGen = 0;
    // Update is called once per frame
    void Update()
    {
        timeSinceLastGen += Time.deltaTime;

        if (timeSinceLastGen > genLength)
        {
            timeSinceLastGen = 0;
            currentGeneration++; //Update currentGen count
            
            terrainGenerator.Generate(); //Gen new terrain every gen
            
            List<GameObject> bestRockets = new List<GameObject>(rocketsPerGen); //List with capacity of rocketsPerGen for performance :)
            
            //deactivate old rockets and put into list to find best rockets
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false); //Deactivate rocket for reusage

                Rocket roc = child.GetComponent<Rocket>();
                roc.points -= roc.active ? roc.notLandingLoss : 0; //If active/notLanded get notLandingPenalty
                
                bestRockets.Add(child.gameObject); //Add rocket to list
            }
            
            bestRockets.Sort(SortByScore); //Sort rockets based on score

            int numOfTopRockets = Mathf.RoundToInt(rocketsPerGen * keepFromLastGenPerc);
            
            float[][,,] bestLineStrengths = new float[numOfTopRockets][,,];
            float[][,] bestDotOffsets = new float[numOfTopRockets][,];
            
            for (int i = 0; i < numOfTopRockets; i++)
            {
                AINetwork network = bestRockets[i].GetComponent<AINetwork>();

                bestLineStrengths[i] = network.lineStrengths;
                bestDotOffsets[i] = network.dotOffsets;
            }

            for (int i = 0; i < numOfTopRockets; i++) //Create new rockets with same dna as top
            {
                CreateRocket(bestLineStrengths[i], bestDotOffsets[i]);
            }

            for (int i = 0; i < rocketsPerGen - numOfTopRockets; i++) //Create other rockets (-toprockets so we dont have too many)
            {
                int parentI = i % numOfTopRockets; //Loop over top rockets for parentIndex
                
                CreateRocket(GenChildLineStrengths(bestLineStrengths[parentI]),
                            GenChildDotOffsets(bestDotOffsets[parentI]));
            }
        }
    }
}
