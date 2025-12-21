using UnityEngine;
using UnityEngine.UI;

public class LocalHpBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private Player target;

    public void Bind(Player p)
    {
        if (target != null)
            target.OnHpChanged -= OnHpChanged;

        target = p;

        if (target != null)
        {
            target.OnHpChanged += OnHpChanged;
            OnHpChanged(target.Hp, target.MaxHp);
        }
        else
        {
            SetValue(0, 0);
        }
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnHpChanged -= OnHpChanged;
    }

    private void OnHpChanged(int hp, int maxHp)
    {
        SetValue(hp, maxHp);
    }

    private void SetValue(int hp, int maxHp)
    {
        if (slider == null) return;

        if (maxHp <= 0)
        {
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            return;
        }

        slider.minValue = 0;
        slider.maxValue = maxHp;
        slider.value = Mathf.Clamp(hp, 0, maxHp);
    }
}