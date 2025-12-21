using System.Collections;
using UnityEngine;

public class DamageOverlayUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float basePeakAlpha = 0.25f;
    [SerializeField] private float maxPeakAlpha = 0.55f;
    [SerializeField] private float fadeIn = 0.05f;
    [SerializeField] private float fadeOut = 0.25f;

    private Player target;
    private Coroutine co;


    public void Bind(Player p)
    {
        target = p;
        if (target != null) target.OnDamaged += OnDamaged;

        if (group != null) group.alpha = 0f;
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (target != null) target.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(int damage)
    {
        if (group == null) return;

        float t = Mathf.Clamp01(damage / 30f);
        float peak = Mathf.Lerp(basePeakAlpha, maxPeakAlpha, t);

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(FlashCo(peak));
    }

    private IEnumerator FlashCo(float peak)
    {
        float a = group.alpha;

        float inT = 0f;
        while (inT < fadeIn)
        {
            inT += Time.deltaTime;
            group.alpha = Mathf.Lerp(a, peak, inT / fadeIn);
            yield return null;
        }
        group.alpha = peak;

        float outT = 0f;
        while (outT < fadeOut)
        {
            outT += Time.deltaTime;
            group.alpha = Mathf.Lerp(peak, 0f, outT / fadeOut);
            yield return null;
        }
        group.alpha = 0f;
        co = null;
    }
}