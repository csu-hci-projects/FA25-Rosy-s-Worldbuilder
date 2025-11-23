using UnityEngine;

public class SlowCameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 25f;
    [SerializeField] private float height = 15f;
    [SerializeField] private float speed = 5f;

    private float angle = 0f;

    void Start()
    {
        if (target == null) target = GameObject.FindWithTag("MapCenter")?.transform;
    }

    void Update()
    {
        if (target == null) return;

        angle += speed * Time.deltaTime;
        if (angle >= 360f) angle -= 360f;

        float rad = angle * Mathf.Deg2Rad;

        float x = target.position.x + Mathf.Cos(rad) * radius;
        float z = target.position.z + Mathf.Sin(rad) * radius;

        transform.position = new Vector3(x, height, z);
        transform.LookAt(target);
    }
}