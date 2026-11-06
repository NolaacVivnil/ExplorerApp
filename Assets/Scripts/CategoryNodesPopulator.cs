using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CategoryNodesPopulator : MonoBehaviour
{
    [Header("UI References")]
    public Transform nodesVerticalContainer;   // NodesVerticalContainer in the hierarchy
    public GameObject categoryNodePrefab;      // CategoryNodePrefab button

    void Start()
    {
        PopulateCategories();
    }

    void PopulateCategories()
    {
        if (DataManager.Instance == null || DataManager.Instance.placeDatabase == null)
        {
            Debug.LogError("DataManager or placeDatabase not found!");
            return;
        }

        // 1. Delete all previous children of container before populating
        foreach (Transform child in nodesVerticalContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Find unique categories across all areas from placeDatabase
        List<string> uniqueCategories = DataManager.Instance.placeDatabase.places
            .Where(p => p.categories != null)
            .SelectMany(p => p.categories)
            .Distinct()
            .ToList();

        // 3. Populate container with unique category buttons
        foreach (string category in uniqueCategories)
        {
            GameObject node = Instantiate(categoryNodePrefab, nodesVerticalContainer);
            
            // Name button accordingly
            node.name = category;

            // Set Text
            TMP_Text categoryText = node.GetComponentInChildren<TMP_Text>();
            if (categoryText != null)
            {
                categoryText.text = category;
            }

            // Check if all places from database within that category are completed
            List<PlaceData> categoryPlaces = DataManager.Instance.placeDatabase.places
                .Where(p => p.categories != null && p.categories.Contains(category))
                .ToList();

            bool allCompleted = categoryPlaces.Count > 0 && categoryPlaces.All(p => p.completed);

            // Setup button and text colors
            Image nodeImage = node.GetComponent<Image>();
            if (nodeImage != null)
            {
                string hexColor = allCompleted ? "#618B25" : "#FFFCF9";
                if (ColorUtility.TryParseHtmlString(hexColor, out Color newImgColor))
                {
                    nodeImage.color = newImgColor;
                }
            }

            if (categoryText != null)
            {
                string hexTextColor = allCompleted ? "#FFFCF9" : "#14110F";
                if (ColorUtility.TryParseHtmlString(hexTextColor, out Color newTextColor))
                {
                    categoryText.color = newTextColor;
                }
            }

            // Setup button click hooks
            Button btn = node.GetComponent<Button>();
            if (btn != null)
            {
                string targetCategory = category;
                btn.onClick.AddListener(() => {
                    DataManager.Instance.lastScene = DataManager.Instance.currentScene;
                    DataManager.Instance.selectedArea = -1;
                    DataManager.Instance.selectedCategory = targetCategory;
                    SceneManager.LoadScene("ByCategoryScene");
                });
            }
        }

        // 4. Populate container with one more custom button: "WHAT'S NEARBY?"
        GameObject nearbyNode = Instantiate(categoryNodePrefab, nodesVerticalContainer);
        nearbyNode.name = "WHAT'S NEARBY?";

        TMP_Text nearbyText = nearbyNode.GetComponentInChildren<TMP_Text>();
        if (nearbyText != null)
        {
            nearbyText.text = "WHAT'S NEARBY?";
        }

        // Leave space for your custom click hook assignment later (no color changes applied)
        Button nearbyBtn = nearbyNode.GetComponent<Button>();
        if (nearbyBtn != null)
        {
            nearbyBtn.onClick.AddListener(() => {
                // TODO: Add your custom click hook for WHAT'S NEARBY? here
                Debug.Log("WHAT'S NEARBY not implemented yet. TODO!");
            });
        }
    }
}