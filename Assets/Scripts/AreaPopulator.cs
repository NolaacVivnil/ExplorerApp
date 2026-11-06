using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AreaPopulator : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject categoryNodePrefab;
    public Transform categoriesContainer;

    [Header("Layout Settings")]
    public float nodeSpacingMultiplier = 0.7f;

    void Start()
    {
        PopulateArea();
    }

    public void PopulateArea()
    {
        if (DataManager.Instance == null || DataManager.Instance.placeDatabase == null)
        {
            Debug.LogError("DataManager or database not found!");
            return;
        }

        List<PlaceData> filteredPlaces = DataManager.Instance.placeDatabase.places
            .FindAll(p => p.area == DataManager.Instance.selectedArea);

        HashSet<string> uniqueCategories = new HashSet<string>();
        foreach (var place in filteredPlaces)
        {
            if (place.categories != null)
            {
                foreach (var cat in place.categories)
                {
                    uniqueCategories.Add(cat);
                }
            }
        }

        List<string> categoriesList = new List<string>(uniqueCategories);
        int totalCategories = categoriesList.Count;

        if (totalCategories == 0) return;

        float nodeWidth = 400f;
        if (categoryNodePrefab != null)
        {
            RectTransform prefabRect = categoryNodePrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                nodeWidth = Mathf.Max(prefabRect.rect.width, prefabRect.rect.height);
            }
        }

        float minCircumference = totalCategories * nodeWidth * nodeSpacingMultiplier;
        float calculatedRadius = minCircumference / (2f * Mathf.PI);
        //calculatedRadius = Mathf.Max(calculatedRadius, nodeWidth);

        float maxNodeDistance = calculatedRadius; // Track maximum reach from center (0,0)

        for (int i = 0; i < totalCategories; i++)
        {
            string category = categoriesList[i];
            
            float angle = (i * (360f / totalCategories) - 90) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * calculatedRadius;
            float y = Mathf.Sin(angle) * calculatedRadius;

            GameObject catNode = Instantiate(categoryNodePrefab, categoriesContainer);
            catNode.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            
            TMP_Text catText = catNode.GetComponentInChildren<TMP_Text>();
            if (catText != null) catText.text = category;

            Button btn = catNode.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCategoryClicked(category));
            }

            
            Image catImage = catNode.GetComponent<Image>();
            if (catImage != null)
            {
                string hexColor = "#FFFCF9";
                if (ColorUtility.TryParseHtmlString(hexColor, out Color newImgColor))
                {
                    catImage.color = newImgColor;
                }
            }

            if (catText != null)
            {
                string hexTextColor = "#14110F";
                if (ColorUtility.TryParseHtmlString(hexTextColor, out Color newTextColor))
                {
                    catText.color = newTextColor;
                }
            }

        }
    }

    void OnCategoryClicked(string category)
    {
        DataManager.Instance.selectedCategory = category;
        SceneManager.LoadScene("ByCategoryScene");
    }
}