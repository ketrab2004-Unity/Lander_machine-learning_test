using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Rocket : MonoBehaviour
{
    public GameObject flame;
    public Rigidbody2D rb2D;

    [ReadOnly] public float points = 100;
    [ReadOnly] public bool active = true;
    
    [Tooltip("By how much vertical and horizontal speed should be divided to normalize.")]
    public float speedDivider = 4;
    public float rotationSpeedDivider = 100;
    
    [Header("Inputs")]
    [Range(0, 1)] public float thrust = 0;
    [Range(-1, 1)] public float rotate = 0;

    [Header("Outputs")]
    [ReadOnly] public float rotation = 0;
    [ReadOnly] public float rotationSpeed = 0;
    [ReadOnly] public float verticalSpeed = 0;
    [ReadOnly] public float horizontalSpeed = 0;
    [ReadOnly] public float fuel = 1;

    [ReadOnly] public float[] rayResults = new float[7];
    
    [Header("Settings")]
    public float rayLength = 5;
    public float[] rayAngles = new float[7];

    public float thrustStrength = 10;
    public float rotateStrength = 10;

    public float fuelPerSecond = .1f; //Multiplied by thrustforce so this much fuel at full throttle
    public float fuelPerRotate = .01f; //Multiplied by rotateforce so this much fuel at full rotate throttle

    public float landCheckOffset = .225f;
    public Vector2 landCheckSize = new Vector2(.275f,.025f);

    [Header("Points settings:")]
    public float succesfulLandingPoints = 250;
    public float impactPointsMultiplier = 2.5f;
    public float fuelPointsMultiplier = 500;
    public float notLandingLoss = 1000;

    private void Start()
    {
        if (rayResults.Length != rayAngles.Length) { Debug.LogWarning("Ray results array isn't the same length as ray angles"); }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (fuel > 0 && active)
        {
            rb2D.AddForce(transform.up * (Mathf.Max(Mathf.Min(thrust, 1), 0) * thrustStrength * Time.fixedDeltaTime), ForceMode2D.Impulse);
            rb2D.AddTorque(Mathf.Max(-1, Mathf.Min(1, -rotate)) * rotateStrength * Time.fixedDeltaTime, ForceMode2D.Impulse);
            
            fuel -= (Mathf.Max(Mathf.Min(thrust, 1), 0) * fuelPerSecond + //Fuel for throttle
                    Mathf.Max(Mathf.Min(Mathf.Abs(rotate), 1), 0) * fuelPerRotate) //Fuel for rotate
                    * Time.fixedDeltaTime; //Together multiplied by fixedDelta
        }

        flame.SetActive(thrust > 0);

        for (int i = 0; i < rayAngles.Length; i++) //Shoot rays for rayResults
        {
            RaycastHit2D ray =
                Physics2D.Raycast(transform.position,
                    transform.rotation * Quaternion.Euler(new Vector3(0, 0, rayAngles[i])) * Vector3.down,
                    rayLength, LayerMask.GetMask("Ground"));
            
            //Rayresults go from 0 to 1 so divide by max length
            rayResults[i] = ray.distance / rayLength;

            if (!ray.collider) { rayResults[i] = 1; } //If hit nothing ray is max length
        }

        rotation = Vector2.SignedAngle(transform.up, Vector2.up) /180;
        rotationSpeed = Mathf.Min(1,Mathf.Max(-1,rb2D.angularVelocity / rotationSpeedDivider));
        verticalSpeed = Mathf.Min(1, Mathf.Max(-1, rb2D.velocity.y/speedDivider));
        horizontalSpeed = Mathf.Min(1, Mathf.Max(-1, rb2D.velocity.x/speedDivider));

        bool touchdown = Physics2D.OverlapBox(transform.position + (-transform.up * landCheckOffset), landCheckSize,
            rotation * 180, LayerMask.GetMask("Ground"));

        float speeds = Mathf.Abs(rotationSpeed) + Mathf.Abs(verticalSpeed) + Mathf.Abs(horizontalSpeed);
        
        if (touchdown && active && Mathf.Round(speeds*10000)/10000 == 0)
        { // touchdown && active && Rot+Vert+Hor ==0 //Rounding so very small speeds will be ignored
            points += succesfulLandingPoints;

            points -= (1 - fuel) * fuelPointsMultiplier;
            
            active = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (active) //Only lose points for collion if active
        {
            Vector2 normal = other.GetContact(0).normal; //Get direction of collision

            Vector2 impact = rb2D.velocity * -normal.normalized; //Normalize normal just in case :)

            points -= impact.magnitude * impactPointsMultiplier;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        
        Gizmos.color = Color.green;
        for (int i = 0; i < rayAngles.Length; i++)
        {
            Gizmos.DrawLine(/*transform.position*/ Vector3.zero,
                /*transform.position +
                transform.rotation * */Quaternion.Euler(new Vector3(0,0,rayAngles[i]))
                * Vector3.down * rayLength * rayResults[i]);
        }
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0, -landCheckOffset,0), landCheckSize);
    }
    #endif
}
