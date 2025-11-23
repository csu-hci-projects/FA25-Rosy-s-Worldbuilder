using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    public RainbowArt.CleanFlatUI.TabView tabView;


    private void Start()
    {
        RefreshAllTabs();

    }

    public void RefreshAllTabs()
    {
        for (int i = 0; i < 4; i++)
        {
            tabView.GetTabs()[i].view.GetComponent<TabContent>().generateContent();
        }
    }



}
