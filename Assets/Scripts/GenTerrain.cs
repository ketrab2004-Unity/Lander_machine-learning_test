using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

public class GenTerrain : MonoBehaviour
{
    public SpriteShapeController shape;
    public Vector3 pos = new Vector3(0, -3.5f,5);
    
    public Vector2 range = new Vector2(9, 1.5f);
    public int pointCount = 10;
    
    private Vector3[] points = new Vector3[50+2];
    public void Generate()
    {
        Spline spline = shape.spline;
        float seed = DateTime.Now.Millisecond;

        for (int i = 0; i < pointCount+2; i++)
        {
            if (i > spline.GetPointCount()-1) //If there aren't enough points add 1
            {
                spline.InsertPointAt(spline.GetPointCount(), Vector3.zero);
            }

            if (i < pointCount) //Gen points
            {
                float iFloat = i + .0001f; //Make i an float because otherwise result of division will be in int
                spline.SetPosition(i, 
                                new Vector3( 
                                            (iFloat/pointCount -.5f) * range.x * 2,
                                            Perlin(i, seed, 5) * range.y * 2 - range.y,
                                            0));
            }
            else //Last 2 points will be at bottom so it is solid
            {
                spline.SetPosition(i,new Vector3(-Mathf.Sign(i-pointCount-1) * range.x, -range.y*2, 0 ));
                //spline.SetPosition(i, new Vector3(0,-10,0));
            }
            points[i] = spline.GetPosition(i);
        }
        
        shape.BakeCollider(); //Bake collider so collision works :)
    }

    float Perlin(float x, float seed, int octaves)
    {
        float num = Mathf.PerlinNoise(x/1000, seed);

        for (int i = 1; i < octaves; i++)
        {
            num += Mathf.PerlinNoise(x/10 * i * Mathf.PI, seed) * 1/i;
        }

        return num;
    }
    
    //TODO temp so remove
    void Start()
    {
        Generate();
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(pos, new Vector3(range.x*2+4, range.y*2, 0));

        for(int i = 0; i < points.Length; i++) 
        {
            Gizmos.color = Color.HSVToRGB((i+0.0001f)/points.Length, 1, 1);
            Gizmos.DrawSphere(points[i]+pos, 0.1f);
        }
    }
    #endif
}
