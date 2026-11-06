using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using System.IO;

public class PlaceDetailsManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    //public Button mapLinkButton;
    public Transform categoryContainer;       // Horizontal layout container
    public GameObject categoryBubblePrefab;   // Bubble prefab
    public Image placeImage;                  // Reference to the Image component
    public Button startButton;

    //[Header("Completion Toggle")]
    //public Toggle completedToggle; // Drag your Toggle component here in the Inspector

    private PlaceData currentPlace; // Keep track of the current place object

    void Start()
    {
        LoadDetails();
    }

    void Update()
    {
        
    }

    void LoadDetails()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager not found!");
            return;
        }

        string placeId = DataManager.Instance.selectedPlaceId;
        currentPlace = DataManager.Instance.placeDatabase.places.Find(p => p.id == placeId);

        if (currentPlace == null)
        {
            Debug.LogError("Place data not found for ID: " + placeId);
            return;
        }

        // Populate Name & Description
        if (nameText != null) nameText.text = currentPlace.name;
        if (descriptionText != null) descriptionText.text = currentPlace.description;

        // Setup Google Maps Link Button
        /*
        if (mapLinkButton != null)
        {
            mapLinkButton.onClick.RemoveAllListeners();
            mapLinkButton.onClick.AddListener(() => {
                if (!string.IsNullOrEmpty(currentPlace.link))
                {
                    Application.OpenURL(currentPlace.link);
                }
            });
        }
        */

        // Populate Categories in Bubbles
        if (categoryContainer != null && categoryBubblePrefab != null && currentPlace.categories != null)
        {
            foreach (Transform child in categoryContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (string cat in currentPlace.categories)
            {
                GameObject bubble = Instantiate(categoryBubblePrefab, categoryContainer);
                TMP_Text bubbleText = bubble.GetComponentInChildren<TMP_Text>();
                if (bubbleText != null) bubbleText.text = cat;
            }
        }

        // Setup Toggle State & Persistence Listener
        /*
        if (completedToggle != null) {
            // Set initial toggle state without firing the listener event
            completedToggle.SetIsOnWithoutNotify(currentPlace.completed);

            // Listen for user clicks to update completion and save to JSON
            completedToggle.onValueChanged.RemoveAllListeners();
            completedToggle.onValueChanged.AddListener((bool isOn) =>
            {
                currentPlace.completed = isOn;

                // Save changes instantly to the JSON file via DataManager
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.SaveData();
                    Debug.Log($"Place '{currentPlace.name}' completion state saved as: {isOn}");
                }
            });
        }
        */
        if (startButton != null) {
            // Bind the button to change the scene to "PlaceStartScene"
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => {
                SceneManager.LoadScene("PlaceStartScene");
            });

            // Get references to the button's text and image components
            TMP_Text btnText = startButton.GetComponentInChildren<TMP_Text>();
            Image btnImage = startButton.GetComponent<Image>();
            Transform childImageTransform = startButton.transform.Find("Background");
            Image btnBackgroundImage = childImageTransform != null ? childImageTransform.GetComponent<Image>() : null;

            // Check if the place is completed
            if (currentPlace.completed) {
                if (btnText != null) {
                    btnText.text = "COMPLETED";
                    if (ColorUtility.TryParseHtmlString("#14110F", out Color newColor))
                    {
                        btnText.color = newColor;
                    }
                }
                if (btnImage != null) {
                    if (ColorUtility.TryParseHtmlString("#14110F", out Color newColor))
                    {
                        btnImage.color = newColor;
                    }
                }
                if (btnBackgroundImage != null) {
                    btnBackgroundImage.enabled = true;
                }
            }
        }

        // Load and Setup Place Image
        if (placeImage != null && currentPlace != null && !string.IsNullOrEmpty(currentPlace.image))
        {
            // Remove file extension since Resources.Load expects asset names without extensions
            string imageName = Path.GetFileNameWithoutExtension(currentPlace.image);
            Sprite loadedSprite = Resources.Load<Sprite>("Images/" + imageName);

            if (loadedSprite != null)
            {
                placeImage.sprite = loadedSprite;

                // Keep height intact and calculate width based on aspect ratio to overflow sides
                RectTransform rectTransform = placeImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float currentHeight = rectTransform.rect.height;
                    if (currentHeight <= 0) currentHeight = rectTransform.sizeDelta.y;

                    float spriteAspect = (float)loadedSprite.rect.width / loadedSprite.rect.height;
                    float targetWidth = currentHeight * spriteAspect;

                    // Switch horizontal anchors to center so it overflows equally to both sides
                    rectTransform.anchorMin = new Vector2(0.5f, rectTransform.anchorMin.y);
                    rectTransform.anchorMax = new Vector2(0.5f, rectTransform.anchorMax.y);
                    rectTransform.sizeDelta = new Vector2(targetWidth, rectTransform.sizeDelta.y);
                }
            }
            else
            {
                Debug.LogWarning($"Could not load image from Resources/Images/{imageName} (filename in data: {currentPlace.image})");
            }
        }
    }
}