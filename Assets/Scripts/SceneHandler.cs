using UnityEngine;
using UnityEngine.SceneManagement;



public class SceneHandler : MonoBehaviour
{
    [Header("Map Reference")]
    [Tooltip("Leave blank if not in Map scene")]
    [SerializeField] private MapZoomController zoomController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoBackButtonCallback()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.GoBack();
        }
    }

    public void GoToAreaButtonCallback(int area)
    {
        // If the map is currently moving, zooming, or hasn't settled back to origin, block the click!
        if (zoomController != null && !zoomController.CanRegisterClick())
        {
            return; // Exit immediately, do nothing!
        }
        
        if (DataManager.Instance != null)
        {
            DataManager.Instance.selectedArea = area;
            SceneManager.LoadScene("ByAreaScene");
        }
    }
}
