using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AINetwork : MonoBehaviour
{
    public Rocket inputScript;

    [Header("Network settings:")]
    public int hiddenLayers = 2;
    public int inputsCount = 12;
    public int outputsCount = 2;
    
    [Header("Actual network")]
    public float[,,] lineStrengths = new float[3,12,12]; //DNA
    //3 columns of dots with lines going into it
    //max of 12 dots per column
    //max 12 inputs per dot
    
    public float[,] dotOffsets = new float[3,12]; //DNA
    //3 columns of dots with lines going into it (without lines doesnt use dot offset)
    //max of 12 dots per column
    
    public float[,] dotResults = new float[4,12];
    //4 columns of dots (including input dots)
    //max of 12 dots per column
    

    // Update is called once per frame
    void Update()
    {
        //Load in inputs
        dotResults[0, 0] = inputScript.rotation;
        dotResults[0, 1] = inputScript.rotationSpeed;
        dotResults[0, 2] = inputScript.verticalSpeed;
        dotResults[0, 3] = inputScript.horizontalSpeed;
        dotResults[0, 4] = inputScript.fuel*2 -1; //Change from 0|1 to -1|1

        dotResults[0, 5] = inputScript.rayResults[0];
        dotResults[0, 6] = inputScript.rayResults[1];
        dotResults[0, 7] = inputScript.rayResults[2];
        dotResults[0, 8] = inputScript.rayResults[3];
        dotResults[0, 9] = inputScript.rayResults[4];
        dotResults[0, 10] = inputScript.rayResults[5];
        dotResults[0, 11] = inputScript.rayResults[6];
        
        //do math for all the remaining dots
        for (int column = 1; column < dotResults.GetLength(0); column++)
        { //Skip first column because it has no lines going into it
            int dotCount = dotResults.GetLength(1);

            if (column == dotResults.GetLength(0)-1) { dotCount = outputsCount; } //Less dots if last column
            
            for (int dot = 0; dot < dotCount; dot++)
            {
                float dotOut = 0;

                int linesCount = lineStrengths.GetLength(2);

                if (column == 1) { linesCount = inputsCount; } //Less lines if first column

                for (int line = 0; line < linesCount; line++)
                {
                    //Add to dotOut per line
                    dotOut += lineStrengths[column -1, dot, line] * dotResults[column -1, line];
                }

                //Normalize dotOut
                dotOut /= dotCount;
                //Devide by dotcount because otherwise value is from -dotCount to dotCount instead of -1 to 1
                
                //Add offset
                dotOut += dotOffsets[column -1, dot];
                //Add offset and after normalizing dotOut with dotCount it is nice :)

                dotResults[column, dot] = Mathf.Clamp(dotOut, -1,1); //Clamp because with offset and all it might go overboard
            }
        }
        
        //unload outputs
        inputScript.thrust = dotResults[3, 0];
        inputScript.rotate = dotResults[3, 1];
    }
}
