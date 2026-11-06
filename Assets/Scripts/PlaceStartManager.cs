using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class PlaceStartManager : MonoBehaviour
{
    // Task text mapping placed at the top of the script
    private readonly Dictionary<string, string> taskTextMapping = new Dictionary<string, string>()
    {
        { "arrive", "Physically arrive!" },
        { "photo", "Take a photo!" },
        { "quiz", "Take a quiz!" },
        { "reward", "Reward!" }
    };

    [Header("UI References")]
    public TMP_Text nameText;
    public Image placeImage;                  // Reference to the main place Image component
    public Transform contentContainer;        // Content GameObject of the ScrollView
    public GameObject taskPrefab;             // Task prefab to instantiate
    public GameObject mapsLinkPrefab;         // Maps link prefab to instantiate for "arrive" task

    private PlaceData currentPlace;
    private List<string> activeTaskKeys = new List<string>();

    void Start()
    {
        LoadDetails();
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

        // 1. Update NameText according to currentPlace.name
        if (nameText != null)
        {
            nameText.text = currentPlace.name;
        }

        // 2. Update Image according to currentPlace.image (16:9 crop/fill handling)
        if (placeImage != null && currentPlace != null && !string.IsNullOrEmpty(currentPlace.image))
        {
            string imageName = Path.GetFileNameWithoutExtension(currentPlace.image);
            Sprite loadedSprite = Resources.Load<Sprite>("Images/" + imageName);

            if (loadedSprite != null)
            {
                placeImage.sprite = loadedSprite;
                placeImage.preserveAspect = false; // Fill container, cropping original as needed
            }
            else
            {
                Debug.LogWarning($"Could not load image from Resources/Images/{imageName}");
            }
        }

        // 3. Populate ScrollView Content with tasks
        PopulateTasks();
    }

    void PopulateTasks()
    {
        if (contentContainer == null || taskPrefab == null || currentPlace == null || currentPlace.tasks == null)
            return;

        // Delete all children of Content
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        activeTaskKeys.Clear();

        // 1. Determine which tasks actually exist in JSON and build active ordered key list
        if (currentPlace.tasks.arrive != null)
        {
            activeTaskKeys.Add("arrive");
        }

        if (currentPlace.tasks.photo != null && currentPlace.tasks.photo.path != null)
        {
            activeTaskKeys.Add("photo");
        }

        if (currentPlace.tasks.quiz != null && currentPlace.tasks.quiz.questions != null && currentPlace.tasks.quiz.questions.Count > 0)
        {
            activeTaskKeys.Add("quiz");
        }

        if (currentPlace.tasks.reward != null && currentPlace.tasks.reward.reward != null)
        {
            activeTaskKeys.Add("reward");
        }

        // 2. Instantiate and setup task UI items in sequential order
        foreach (string taskKey in activeTaskKeys)
        {
            bool initialCompletedState = GetTaskCompletedState(taskKey);

            CreateTaskItem(taskKey, initialCompletedState, (val) => {
                SetTaskCompletedState(taskKey, val);
            });
        }
    }

    private bool GetTaskCompletedState(string taskKey)
    {
        if (currentPlace == null || currentPlace.tasks == null) return false;

        switch (taskKey)
        {
            case "arrive":
                return currentPlace.tasks.arrive != null && currentPlace.tasks.arrive.completed;
            case "photo":
                return currentPlace.tasks.photo != null && currentPlace.tasks.photo.completed;
            case "quiz":
                return currentPlace.tasks.quiz != null && currentPlace.tasks.quiz.completed;
            case "reward":
                return currentPlace.tasks.reward != null && currentPlace.tasks.reward.completed;
            default:
                return false;
        }
    }

    private void SetTaskCompletedState(string taskKey, bool completed)
    {
        if (currentPlace == null || currentPlace.tasks == null) return;

        switch (taskKey)
        {
            case "arrive":
                if (currentPlace.tasks.arrive != null) currentPlace.tasks.arrive.completed = completed;
                break;
            case "photo":
                if (currentPlace.tasks.photo != null) currentPlace.tasks.photo.completed = completed;
                break;
            case "quiz":
                if (currentPlace.tasks.quiz != null) currentPlace.tasks.quiz.completed = completed;
                break;
            case "reward":
                if (currentPlace.tasks.reward != null) currentPlace.tasks.reward.completed = completed;
                break;
        }
    }

    private void CreateTaskItem(string taskKey, bool initialCompletedState, System.Action<bool> onToggleCompleted)
    {
        if (!taskTextMapping.ContainsKey(taskKey)) return;

        GameObject taskObj = Instantiate(taskPrefab, contentContainer);
        bool currentCompleted = initialCompletedState;

        // Update Text (TMP) of TaskPrefab
        TMP_Text taskText = taskObj.GetComponentInChildren<TMP_Text>();
        if (taskText != null)
        {
            taskText.text = taskTextMapping[taskKey];
        }

        // Update initial Image color of Circle child and HintText if completed
        UpdateCircleImageColor(taskObj, currentCompleted);
        UpdateTaskHint(taskObj, currentCompleted);

        // Update TaskPrefab AutoCustomSpacing.cs public value if task is "arrive"
        if (taskKey == "arrive")
        {
            Component autoSpacing = taskObj.GetComponent("AutoCustomSpacing");
            if (autoSpacing != null)
            {
                System.Reflection.FieldInfo field = autoSpacing.GetType().GetField("extraSpacing");
                if (field != null)
                {
                    field.SetValue(autoSpacing, 50f);
                }
            }

            // Instantiate MapsLinkPrefab immediately after Task Prefab if it's the "arrive" task
            if (mapsLinkPrefab != null)
            {
                GameObject mapsLinkObj = Instantiate(mapsLinkPrefab, contentContainer);
                Button mapsButton = mapsLinkObj.GetComponent<Button>();
                if (mapsButton == null)
                {
                    mapsButton = mapsLinkObj.GetComponentInChildren<Button>();
                }

                if (mapsButton != null)
                {
                    mapsButton.onClick.RemoveAllListeners();
                    mapsButton.onClick.AddListener(() =>
                    {
                        if (!string.IsNullOrEmpty(currentPlace.link))
                        {
                            Application.OpenURL(currentPlace.link);
                        }
                    });
                }
            }
        }

        // Add listener to TaskPrefab button property
        Button taskButton = taskObj.GetComponent<Button>();
        if (taskButton == null)
        {
            taskButton = taskObj.GetComponentInChildren<Button>();
        }

        // Disable button from the start if the task is already completed
        if (taskButton != null)
        {
            taskButton.interactable = !currentCompleted;

            taskButton.onClick.RemoveAllListeners();
            taskButton.onClick.AddListener(() =>
            {
                // Verify if previous active task was completed
                if (!IsPreviousTaskCompleted(taskKey))
                {
                    string warningMsg = "Please complete the previous task first!";
                    Debug.LogWarning(warningMsg);
                    ShowToast(warningMsg);
                    return;
                }

                // Check specific task conditions
                bool conditionPassed = false;
                switch (taskKey)
                {
                    case "arrive":
                        conditionPassed = CheckArriveTask();
                        break;
                    case "photo":
                        conditionPassed = CheckPhotoTask();
                        break;
                    case "quiz":
                        conditionPassed = CheckQuizTask();
                        break;
                    case "reward":
                        conditionPassed = true;
                        break;
                    default:
                        conditionPassed = true;
                        break;
                }

                if (!conditionPassed)
                {
                    Debug.LogWarning($"Task condition check failed for task: {taskKey}");
                    return;
                }

                // Toggle completion state
                currentCompleted = !currentCompleted;

                // Handle reward claim when completing reward task
                if (taskKey == "reward" && currentCompleted)
                {
                    if (DataManager.Instance != null && currentPlace.tasks.reward != null)
                    {
                        DataManager.Instance.ClaimReward(currentPlace.tasks.reward.reward);
                    }
                }

                // Update model state via callback
                onToggleCompleted(currentCompleted);

                // Update visual circle image color and HintText dynamically
                UpdateCircleImageColor(taskObj, currentCompleted);
                UpdateTaskHint(taskObj, currentCompleted);

                // Disable button once task is completed
                if (currentCompleted)
                {
                    taskButton.interactable = false;
                }

                // Save changes instantly to device storage via DataManager
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.SaveData();
                    Debug.Log($"Task '{taskKey}' completion state updated to: {currentCompleted} and saved.");
                }
            });
        }
    }

    private bool IsPreviousTaskCompleted(string taskKey)
    {
        int currentIndex = activeTaskKeys.IndexOf(taskKey);
        
        // If it's the first task in sequence, there is no previous task requirement
        if (currentIndex <= 0)
        {
            return true;
        }

        // Retrieve true previous task key
        string previousTaskKey = activeTaskKeys[currentIndex - 1];
        return GetTaskCompletedState(previousTaskKey);
    }

    // Condition Checkers
    private bool CheckArriveTask()
    {
        // TODO: Add location/arrival checking logic
        Debug.Log("Checking Arrive Task condition...");
        return true;
    }

    private bool CheckPhotoTask()
    {
        // TODO: Add photo taking/validation checking logic
        Debug.Log("Checking Photo Task condition...");
        return true;
    }

    private bool CheckQuizTask()
    {
        // TODO: Add quiz completion checking logic
        Debug.Log("Checking Quiz Task condition...");
        return true;
    }

    private void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // UnityPlayer
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // Toast.makeText(activity, message, Toast.LENGTH_SHORT)
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            
            // Run on Android UI Thread
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>(
                    "makeText", 
                    activity, 
                    message, 
                    toastClass.GetStatic<int>("LENGTH_SHORT")
                );
                toastObject.Call("show");
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to show Android Toast: " + e.Message);
        }
#else
        Debug.Log("Toast Notification: " + message);
#endif
    }

    private void UpdateCircleImageColor(GameObject taskObj, bool isCompleted)
    {
        // Locate the "Circle" child transform and update its Image component color
        Transform circleTransform = taskObj.transform.Find("Circle");
        if (circleTransform != null)
        {
            Image circleImage = circleTransform.GetComponent<Image>();
            if (circleImage != null)
            {
                string hexColor = isCompleted ? "#618B25" : "#9C1C2F";
                if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
                {
                    circleImage.color = color;
                }
            }
        }
    }

    private void UpdateTaskHint(GameObject taskObj, bool isCompleted)
    {
        // Locate the "HintText" child and update its text when completed
        Transform hintTransform = taskObj.transform.Find("HintText");
        if (hintTransform != null)
        {
            TMP_Text hintText = hintTransform.GetComponent<TMP_Text>();
            if (hintText != null && isCompleted)
            {
                hintText.text = "COMPLETED!";
            }
        }
    }
}