using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    [System.Serializable]
    public class SceneButtonMapping
    {
        public Button button;
        public string targetScene;
    }

    [Header("Menu Buttons Setup")]
    [SerializeField] private SceneButtonMapping[] sceneButtons;

    [Header("Active Scene Styling")]
    [SerializeField] private Color activeColor = new Color(0.318f, 0.271f, 0.239f, 1f); // #51453D

    void Start()
    {
        SetupButtons();
    }

    void SetupButtons()
    {
        if (DataManager.Instance == null) return;

        string currentScene = DataManager.Instance.currentScene;

        foreach (var mapping in sceneButtons)
        {
            if (mapping.button == null) continue;

            // 1. Highlight the button if its target scene matches the current scene
            Image buttonImage = mapping.button.GetComponent<Image>();
            if (buttonImage != null && 
                mapping.targetScene == currentScene && 
                DataManager.Instance.selectedArea == -1 && 
                string.IsNullOrEmpty(DataManager.Instance.selectedCategory) && 
                string.IsNullOrEmpty(DataManager.Instance.selectedPlaceId))
            {
                buttonImage.color = activeColor;
            }

            // 2. Automatically assign the click listener using the scene stored in the array
            string sceneToLoad = mapping.targetScene; // Local copy for the lambda closure
            mapping.button.onClick.AddListener(() => GoToScene(sceneToLoad));
        }
    }

    public void GoToScene(string targetScene)
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.selectedArea = -1;
            DataManager.Instance.selectedCategory = "";
            DataManager.Instance.selectedPlaceId = "";
            DataManager.Instance.lastScene = DataManager.Instance.currentScene;
            DataManager.Instance.currentScene = targetScene;
            SceneManager.LoadScene(targetScene);
        }
    }
}