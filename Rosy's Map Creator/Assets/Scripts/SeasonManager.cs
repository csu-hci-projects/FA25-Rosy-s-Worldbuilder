using Unity.VisualScripting;
using UnityEngine;

public enum Season
{
    Spring = 0,
    Summer = 1,
    Fall = 2,
    Winter = 3
}

public class SeasonManager : MonoBehaviour
{
    public Material springMaterialRef;
    public Material summerMaterialRef;
    public Material fallMaterialRef;
    public Material winterMaterialRef;
    public Season startingSeason = Season.Spring;
    public TabManager tabManager;

    public static Material springMaterial;
    public static Material summerMaterial;
    public static Material fallMaterial;
    public static Material winterMaterial;
    public static Season currentSeason;

    public static Season GetCurrentSeason()
    {
        return currentSeason;
    }

    public static void SetCurrentSeason(Season season)
    {
        currentSeason = season;
    }

    void Awake()
    {
        springMaterial = springMaterialRef;
        summerMaterial = summerMaterialRef;
        fallMaterial = fallMaterialRef;
        winterMaterial = winterMaterialRef;
        currentSeason = startingSeason;
    }

    public static Material GetMaterialForSeason()
    {
        return currentSeason switch
        {
            Season.Spring => springMaterial,
            Season.Summer => summerMaterial,
            Season.Fall => fallMaterial,
            Season.Winter => winterMaterial,
            _ => null
        };
    }
    public void SetSeason(Season newSeason)
    {
        currentSeason = newSeason;
        Debug.Log("Season changed to " + newSeason);
    }

    public void NextSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
        Debug.Log("Season changed to " + currentSeason);
    }
    public void SummerSeason()
    {
        currentSeason = Season.Summer;
        Debug.Log("Season changed to " + currentSeason);
        tabManager.RefreshAllTabs();
    }
    public void WinterSeason()
    {
        currentSeason = Season.Winter;
        Debug.Log("Season changed to " + currentSeason);
        tabManager.RefreshAllTabs();
    }
    public void SpringSeason()
    {
        currentSeason = Season.Spring;
        Debug.Log("Season changed to " + currentSeason);
        tabManager.RefreshAllTabs();
    }
    public void FallSeason()
    {
        currentSeason = Season.Fall;
        Debug.Log("Season changed to " + currentSeason);
        tabManager.RefreshAllTabs();
    }
}
