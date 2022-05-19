using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class ShowFunData : MonoBehaviour
{
    public Transform rocketHolder;
    public Controller controller;
    public AINetwork network;
    public Transform highlighter;
    
    [ReadOnly(true)] public float highestPointsEver = Single.NegativeInfinity;
    
    [Header("UI")]
    public TextMeshProUGUI text;

    public Transform dotHolder;
    public Transform lineHolder;
    
    public GameObject dotPrefab;
    public GameObject linePrefab;

    [Header("Ui Settings: (anchors)")]
    public Vector2 networkArea = new Vector2(2.5f,1.5f);
    public float updateArrayTime = .5f;
    
    //private
    private GameObject[,] dots;
    private LineRenderer[,,] lines;

    private List<Rocket> rocketList = new List<Rocket>();
    private Rocket best;

    // Start is called before the first frame update
    void Start()
    {
        SetupNetworkShower();
    }
    
    private float lastUpdate = 0;
    void Update()
    {
        lastUpdate += Time.deltaTime;
        
        if (lastUpdate > updateArrayTime)
        {
            lastUpdate = 0;
            
            rocketList.Clear();
            rocketList.Capacity = controller.rocketsPerGen;

            foreach (Transform rock in rocketHolder)
            {
                rocketList.Add(rock.GetComponent<Rocket>());
            }

            best = rocketList[0];
            float sum = 0;
            for (int i = 0; i < rocketList.Count; i++)
            {
                sum += rocketList[i].points;
                if (rocketList[i].points > best.points)
                {
                    best = rocketList[i];
                }

                if (rocketList[i].points > highestPointsEver)
                {
                    highestPointsEver = rocketList[i].points;
                }
            }
            sum /= rocketList.Count;

            AINetwork bestNet = best.GetComponent<AINetwork>();
            LoadInNetworkShower(bestNet.lineStrengths, bestNet.dotOffsets);
            
            text.text = "Generation: " + controller.currentGeneration +
                        "\n\nThis Generation:" +
                        "\n*Average Score: " + sum +
                        "\n*Best Score: " + best.points +
                        "\n\nOverall:" +
                        "\n*Highest Score: " + highestPointsEver;
        }
        
        LoadInDotResults(best.GetComponent<AINetwork>().dotResults);
        highlighter.SetPositionAndRotation(best.transform.position - Vector3.back*.1f, best.transform.rotation);
    }

    void CleanupNetworkShower()
    {
        foreach (GameObject dot in dots)
        {
            Destroy(dot);
        }
        foreach (LineRenderer line in lines)
        {
            Destroy(line.gameObject);
        }
    }

    void SetupNetworkShower()
    {
        dots = new GameObject[network.dotResults.GetLength(0), network.dotResults.GetLength(1)];

        int columnCount = dots.GetLength(0);
        for (int column = 0; column < columnCount; column++)
        {
            int dotCount = column == columnCount - 1 ? network.outputsCount : dots.GetLength(1);
            for (int dot = 0; dot < dotCount; dot++)
            {
                /*dots[column, dot] = Instantiate(uiDotPrefab, dotHolder).GetComponent<RectTransform>(); //Make dot in dot holder and add tranform into array

                //position
                float hori = 1/(dots.GetLength(0)+.0001f);
                float vert = 1/(dots.GetLength(1)+.0001f);
                
                Vector2 middle = new Vector2(hori * column, vert * dot);
                Vector2 offsetY = new Vector2(0, vert);
                Vector2 offsetX = new Vector2(dotWidth/2,0);

                dots[column, dot].anchorMin = middle - offsetX           +Vector2.right*hori/2;
                dots[column, dot].anchorMax = middle + offsetY + offsetX +Vector2.right*hori/2;
                */

                dots[column, dot] = Instantiate(dotPrefab, dotHolder);

                dots[column, dot].transform.SetPositionAndRotation(dotHolder.position + new Vector3(
                    (column + .5001f) / columnCount * networkArea.x * 2 - networkArea.x,
                    (dot + .5001f) / dotCount * networkArea.y * 2 - networkArea.y, 0), dotPrefab.transform.rotation);

                //extra
                dots[column, dot].name = "Dot: " + column + "; " + dot;
            }
        }

        lines = new LineRenderer[network.lineStrengths.GetLength(0), network.lineStrengths.GetLength(1),
            network.lineStrengths.GetLength(2)];

        for (int column = 0; column < lines.GetLength(0); column++) //Skip first column because input column dont have lines going into them
        {
            int dotCount = column == lines.GetLength(0) - 1 ? network.outputsCount : dots.GetLength(1);
            for (int dot = 0; dot < dotCount; dot++)
            {
                for (int line = 0; line < lines.GetLength(2); line++)
                {
                    /*lines[column, dot, line] = Instantiate(uiLinePrefab, lineHolder).GetComponent<RectTransform>(); //Make line in line holder and add transform into array
                    
                    //position
                    Vector2 dotPos = (dots[column-1, line].anchorMax + dots[column-1, line].anchorMin) / 2; //average so center

                    Vector2 from = dots[column - 1, line].TransformPoint();
                    Vector2 to = dots[column, dot].anchoredPosition;
                    
                    lines[column, dot, line].anchorMin = dotPos - new Vector2(0, lineWidth);
                    lines[column, dot, line].anchorMax = dotPos + new Vector2((from-to).magnitude,lineWidth); //Length is distance between dotPos and dotTarget
                    
                    //rotate
                    //Vector2 difference = AddAspectRatio(dotTarget) - AddAspectRatio(dotPos);
                    Vector2 difference = to - from;
                    
                    lines[column, dot, line].rotation = Quaternion.Euler(0,0, Mathf.Atan2(difference.y,difference.x) *Mathf.Rad2Deg);
                    */

                    lines[column, dot, line] = Instantiate(linePrefab, lineHolder).GetComponent<LineRenderer>();

                    Vector3 offset = Vector3.back * .1f;
                    
                    lines[column, dot, line].SetPositions(
                        new Vector3[2]
                        {
                            dots[column, line].transform.position - offset,
                            dots[column + 1, dot].transform.position - offset
                        });

                    //extra
                    lines[column, dot, line].gameObject.name = "Line: " + column + "; " + dot + "; " + line;
                }
            }
        }
    }

    void LoadInNetworkShower(float[,,] lineStrengths, float[,] dotOffsets)
    {
        for (int column = 0; column < lines.GetLength(0); column++) //Skip first column because input column dont have lines going into them
        {
            int dotCount = column == lines.GetLength(0) - 1 ? network.outputsCount : dots.GetLength(1);
            for (int dot = 0; dot < dotCount; dot++)
            {
                for (int line = 0; line < lines.GetLength(2); line++)
                {
                    float lineF = lineStrengths[column, dot, line];

                    Color col = lineF < 0
                        ? Color.Lerp(Color.clear, Color.red, Mathf.Abs(lineF))
                        : Color.Lerp(Color.clear, Color.green, lineF);
                    /*lines[column, dot, line].colorGradient = new Gradient();
                    lines[column, dot, line].colorGradient.colorKeys = new GradientColorKey[2]{ new GradientColorKey(col,0), new GradientColorKey(col,1)};*/

                    lines[column, dot, line].startColor = col;
                    lines[column, dot, line].endColor = col;
                }
            }
        }
        
        int columnCount = dots.GetLength(0);
        for (int column = 1; column < columnCount; column++) //Skip first column because inputs cant have offsets
        {
            int dotCount = (column == columnCount - 1) ? network.outputsCount : dots.GetLength(1);
            for (int dot = 0; dot < dotCount; dot++)
            {
                float dotF = dotOffsets[column-1, dot];

                dots[column, dot].GetComponent<SpriteRenderer>().color = dotF < 0
                    ? Color.HSVToRGB(0,Mathf.Abs(dotF),1)
                    : Color.HSVToRGB(.4f,Mathf.Abs(dotF),1);
            }
        }
    }

    void LoadInDotResults(float[,] dotResults)
    {
        int columnCount = dots.GetLength(0);
        for (int column = 0; column < columnCount; column++)
        {
            int dotCount = (column == columnCount - 1) ? network.outputsCount : dots.GetLength(1);
            for (int dot = 0; dot < dotCount; dot++)
            {
                dots[column, dot].transform.GetChild(0).GetComponent<TextMesh>().text = (Mathf.Round(dotResults[column, dot]*100)/100).ToString();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, networkArea*2);
    }
    #endif
}
