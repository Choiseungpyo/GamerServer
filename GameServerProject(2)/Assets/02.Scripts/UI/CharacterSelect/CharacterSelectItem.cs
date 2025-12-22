using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectItem : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statText;
    [SerializeField] private TMP_Text selectedText;

    private int id;
    private Action<int> onClick;

    public void Bind(CharacterStat data, bool selected, Action<int> onClick)
    {
        this.id = data.id;
        this.onClick = onClick;

        nameText.text = data.name;
        statText.text = "HP " + data.hp + " SPD " + data.moveSpeed + " ATK " + data.attackPower;
        selectedText.text = selected ? "SELECTED" : "";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => this.onClick(this.id));
    }
}