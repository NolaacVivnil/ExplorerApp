using UnityEngine;
using UnityEngine.UI;

public class AlbumPopulator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The parent GameObject (usually a Content panel inside a ScrollRect) where stamps will be spawned.")]
    public Transform contentParent;

    [Tooltip("The Stamp Prefab that contains a UI Image component.")]
    public GameObject stampPrefab;

    void Start()
    {
        PopulateAlbum();
    }

    public void PopulateAlbum()
    {
        if (contentParent == null || stampPrefab == null)
        {
            Debug.LogError("Content Parent or Stamp Prefab is not assigned in AlbumPopulator!");
            return;
        }

        // 1. Delete all existing children of Content beforehand
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Check if DataManager and inventory exist
        if (DataManager.Instance == null || DataManager.Instance.inventory == null)
        {
            Debug.LogWarning("DataManager instance or inventory data not found.");
            return;
        }

        // 3. Loop through stamps in the inventory and populate based on the amount
        var stamps = DataManager.Instance.inventory.stamps;
        foreach (StampItem stampItem in stamps)
        {
            // Instantiate a stamp prefab according to the amount owned
            for (int i = 0; i < stampItem.amount; i++)
            {
                GameObject newStampObj = Instantiate(stampPrefab, contentParent);

                // Get the Image component from the prefab
                Image stampImage = newStampObj.GetComponent<Image>();
                if (stampImage != null)
                {
                    // A. Apply Color
                    if (ColorUtility.TryParseHtmlString(stampItem.color, out Color parsedColor))
                    {
                        stampImage.color = parsedColor;
                    }
                    else if (ColorUtility.TryParseHtmlString("#" + stampItem.color, out Color parsedColorWithHash))
                    {
                        stampImage.color = parsedColorWithHash;
                    }
                    else
                    {
                        stampImage.color = Color.white;
                    }

                    // B. Load and Apply Sprite Image from Assets/Resources/Stamps/
                    if (!string.IsNullOrEmpty(stampItem.path))
                    {
                        Sprite stampSprite = Resources.Load<Sprite>("Stamps/" + stampItem.path);
                        if (stampSprite != null)
                        {
                            stampImage.sprite = stampSprite;
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find stamp sprite at Resources path: Stamps/{stampItem.path}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("StampPrefab is missing a UI Image component!");
                }
            }
        }
    }
}