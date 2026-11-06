using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlaceNodesPopulator : MonoBehaviour
{
    [Header("UI References")]
    public Transform scrollContent;        // The Content rect transform inside Scroll View
    public GameObject placeNodePrefab;     // Your place node prefab
    public TMP_Text headerTitleText;       // Optional: To display the category name

    void Start()
    {
        PopulateList();
    }

    void PopulateList()
    {
        if (DataManager.Instance == null || DataManager.Instance.placeDatabase == null)
        {
            Debug.LogError("DataManager or database not found!");
            return;
        }

        int targetArea = DataManager.Instance.selectedArea;
        string targetCategory = DataManager.Instance.selectedCategory;

        if (headerTitleText != null)
        {
            headerTitleText.text = targetCategory;
        }

        // 1. Filter places matching BOTH selectedArea and selectedCategory
        List<PlaceData> filteredPlaces = DataManager.Instance.placeDatabase.places
            .FindAll(p => (targetArea == -1 || p.area == targetArea) && p.categories != null && p.categories.Contains(targetCategory))
            .OrderBy(p => p.completed) 
            .ToList();
            
        // 2. Clear any existing children in the scroll view
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        // 3. Instantiate place nodes into the scrollable content
        foreach (var place in filteredPlaces)
        {
            GameObject node = Instantiate(placeNodePrefab, scrollContent);

            // Set Name Text
            TMP_Text placeText = node.GetComponentInChildren<TMP_Text>();
            if (placeText != null) placeText.text = place.name;

            // Apply completion colors (matching your map style)
            Image nodeImage = node.GetComponent<Image>();
            if (nodeImage != null)
            {
                string hexColor = place.completed ? "#618B25" : "#FFFCF9";
                if (ColorUtility.TryParseHtmlString(hexColor, out Color newImgColor))
                {
                    nodeImage.color = newImgColor;
                }
            }

            if (placeText != null)
            {
                string hexTextColor = place.completed ? "#FFFCF9" : "#14110F";
                if (ColorUtility.TryParseHtmlString(hexTextColor, out Color newTextColor))
                {
                    placeText.color = newTextColor;
                }
            }

            // Hook up click to open details scene (checking if locked)
            Button btn = node.GetComponent<Button>();
            if (btn != null)
            {
                string currentPlaceId = place.id;
                bool isLocked = place.locked;

                btn.onClick.AddListener(() => {
                    if (isLocked)
                    {
                        Debug.Log("This place is locked!");
                        return;
                    }

                    DataManager.Instance.selectedPlaceId = currentPlaceId;
                    SceneManager.LoadScene("PlaceDetailsScene");
                });
            }
        }
    }
}