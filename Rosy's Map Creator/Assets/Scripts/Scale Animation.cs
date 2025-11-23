using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class ScaleAnimation : MonoBehaviour
{
    public float duration = 1.0f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Vector3 startScale = new Vector3(.3f, .3f, .3f);

    void Awake()
    {
        transform.localScale = startScale;
        StartCoroutine(ScaleTo(Vector3.one, duration));
    }
    System.Collections.IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 initialScale = transform.localScale;
        float time = 0;

        while (time < duration)
        {
            float t = time / duration;
            float curveValue = scaleCurve.Evaluate(t);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, curveValue);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }
    void Start()
    {

    }

    void Update()
    {

    }
}
