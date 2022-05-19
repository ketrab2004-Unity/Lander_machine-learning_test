using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flame : MonoBehaviour
{
    public SpriteRenderer sprite;

    public float flipsPerSecond = 15;

    private float time = 0;
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time > 1/flipsPerSecond)
        {
            sprite.flipX = !sprite.flipX;
            time = 0;
        }
    }
}
