using UnityEngine;
using UnityEngine.UI;

public class TabContent : MonoBehaviour
{
    [SerializeField]
    public GameObject contentPanel;
    public GameAssetRegistry assetRegistry;
    public PlacementManager placementManager;
    public int lowerIndex = 1;
    public int upperIndex = 10;

    public System.Action<string> ItemClicked;

    public void generateContent()
    {
        Debug.Log("Generating content for " + contentPanel.name);
        foreach (Transform child in contentPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        var layoutGroup = contentPanel.GetComponent<GridLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentPanel.AddComponent<GridLayoutGroup>();
        }

        layoutGroup.cellSize = new Vector2(gameObject.GetComponent<RectTransform>().rect.width / 4 - 5, 100);
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layoutGroup.constraintCount = 4;
        layoutGroup.spacing = new Vector2(15, 15);
        contentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, Mathf.CeilToInt((float)(upperIndex - lowerIndex) / 4) * (100 + 50));
        for (int i = lowerIndex; i < upperIndex; i++)
        {
            addGridItem(i);
        }
    }
    private void addGridItem(int id)
    {
        var assetRecord = assetRegistry.GetOrNull(id);
        if (assetRecord == null) return;

        var itemObject = new GameObject("GridItem");
        itemObject.transform.SetParent(contentPanel.transform, false);

        var bgImage = itemObject.AddComponent<Image>();
        bgImage.color = new Color(0.95f, 0.95f, 0.95f, 1f); // light background
        var outline = itemObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);

        var verticalLayout = itemObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childAlignment = TextAnchor.MiddleCenter;
        verticalLayout.spacing = 5;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.padding = new RectOffset(8, 8, 8, 8);

        var buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(itemObject.transform, false);
        var button = buttonObject.AddComponent<Button>();
        var image = buttonObject.AddComponent<Image>();
        image.sprite = assetRecord.GetIcon(SeasonManager.currentSeason);
        image.preserveAspect = true;

        var buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 100;
        buttonLayout.flexibleWidth = 1;


        button.onClick.AddListener(() =>
        {
            Debug.Log($"Clicked: {assetRecord.GetFormattedName()}");
            ItemClicked?.Invoke(assetRecord.GetFormattedName());
            placementManager.setTilePrefab(assetRecord.GetPrefab());
        });

    }


}