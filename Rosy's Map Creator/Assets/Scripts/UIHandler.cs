using UnityEngine;
using UnityEngine.UIElements;

public class UIHandler : MonoBehaviour
{
    public SeasonManager seasonManager;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        Button nextButton = root.Q<Button>("NextSeasonButton");

        nextButton.clicked += OnNextSeasonClicked;
    }

    private void OnDisable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        Button nextButton = root.Q<Button>("NextSeasonButton");
        nextButton.clicked -= OnNextSeasonClicked;
    }

    private void OnNextSeasonClicked()
    {
        seasonManager.NextSeason();
    }
}