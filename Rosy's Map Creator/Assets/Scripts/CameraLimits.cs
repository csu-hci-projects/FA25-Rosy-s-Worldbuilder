using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLimits : MonoBehaviour
{
    public float minX, maxX, minZ, maxZ;

    public Vector3 ClampPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        return position;
    }
}