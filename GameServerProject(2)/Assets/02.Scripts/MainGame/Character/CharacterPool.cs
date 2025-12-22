using UnityEngine;

public class CharacterPool : MultipleObjectPoolBase<CharacterType, Character>
{
    private readonly CharacterDatabase characterDb;
    private readonly Transform poolRoot;

    public CharacterPool(CharacterDatabase characterDb, Transform poolRoot, int defaultCapacity = 10, int maxSize = 64, bool collectionCheck = false)
        : base(defaultCapacity, maxSize, collectionCheck)
    {
        this.characterDb = characterDb;
        this.poolRoot = poolRoot;
    }

    protected override Character CreateItem(CharacterType key)
    {
        if (characterDb == null) return null;

        int characterId = (int)key;

        if (!characterDb.TryGetVisual(characterId, out var visual) || visual == null) return null;
        if (visual.modelPrefab == null) return null;

        var go = Object.Instantiate(visual.modelPrefab, poolRoot);
        go.SetActive(false);

        var view = go.GetComponent<Character>();
        if (view == null)
        {
            Object.Destroy(go);
            return null;
        }

        return view;
    }

    protected override void OnGet(CharacterType key, Character component)
    {
        if (component == null) return;
        component.gameObject.SetActive(true);
    }

    protected override void OnRelease(CharacterType key, Character component)
    {
        if (component == null) return;
        component.transform.SetParent(poolRoot, false);
        component.gameObject.SetActive(false);
    }
}