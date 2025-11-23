using UnityEngine;
using UnityEngine.UI;

public class TileIconUI : MonoBehaviour
{
    [SerializeField] private TileRegistry registry;
    [SerializeField] private Image iconImage;     // UnityEngine.UI.Image
    [SerializeField] private string tileId;       // Bind this per slot (or set via code)
    [SerializeField] private Season season;       // Or drive this from your game state

    // Call when the season or tile changes
    public void Refresh()
    {
        if (registry == null || iconImage == null || string.IsNullOrWhiteSpace(tileId))
        {
            if (iconImage) iconImage.enabled = false;
            return;
        }

        var rec = registry.GetOrNull(tileId);
        if (rec == null)
        {
            iconImage.enabled = false;
            return;
        }

        var spr = rec.GetIcon(season);
        iconImage.sprite = spr;
        iconImage.enabled = spr != null;
    }

    // Example helpers for other systems
    public void SetTile(string id)
    {
        tileId = id;
        Refresh();
    }

    public void SetSeason(Season s)
    {
        season = s;
        Refresh();
    }
}
