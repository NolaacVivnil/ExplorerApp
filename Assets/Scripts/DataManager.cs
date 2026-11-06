using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[Serializable]
public class TaskStatus
{
    public bool completed;
}

[Serializable]
public class PhotoTask
{
    public bool completed;
    public string path;
}

[Serializable]
public class QuizQuestion
{
    public string question;
    public List<string> answers;
}

[Serializable]
public class QuizTask
{
    public bool completed;
    public List<QuizQuestion> questions;
}

[Serializable]
public class RewardTask
{
    public bool completed;
    public string reward;
}

[Serializable]
public class PlaceTasks
{
    public TaskStatus arrive;
    public PhotoTask photo;
    public QuizTask quiz;
    public RewardTask reward;
}

[Serializable]
public class PlaceData
{
    public string id;
    public string name;
    public string description;
    public string link;
    public List<string> categories;
    public int area;
    public string image;
    public bool locked;
    public PlaceTasks tasks;

    // Self-updating property: checks completion only for tasks actually present in the JSON
    public bool completed
    {
        get
        {
            if (tasks == null) return false;

            bool hasAtLeastOneTask = false;

            if (tasks.arrive != null)
            {
                hasAtLeastOneTask = true;
                if (!tasks.arrive.completed) return false;
            }

            if (tasks.photo != null && tasks.photo.path != null)
            {
                hasAtLeastOneTask = true;
                if (!tasks.photo.completed) return false;
            }

            if (tasks.quiz != null && tasks.quiz.questions != null && tasks.quiz.questions.Count > 0)
            {
                hasAtLeastOneTask = true;
                if (!tasks.quiz.completed) return false;
            }

            if (tasks.reward != null && tasks.reward.reward != null)
            {
                hasAtLeastOneTask = true;
                if (!tasks.reward.completed) return false;
            }

            return hasAtLeastOneTask;
        }
    }
}

[Serializable]
public class PlaceDatabase
{
    public List<PlaceData> places;
}

[Serializable]
public class StampItem
{
    public string color;
    public string path;
    public int amount;
}

[Serializable]
public class InventoryData
{
    public int gold;
    public int gems;
    public List<StampItem> stamps = new List<StampItem>();
}

public class DataManager : MonoBehaviour
{
    // Singleton instance so other scripts can access it easily anywhere
    public static DataManager Instance { get; private set; }
    public PlaceDatabase placeDatabase;
    private string savePath;

    [Header("Inventory State")]
    public InventoryData inventory = new InventoryData();
    private string inventorySavePath;
    
    // Simple encryption key & salt configuration for offline storage security
    private readonly byte[] encryptionKey = Encoding.UTF8.GetBytes("A3f9#K2p$9vL1zQ8"); // Must be 16, 24, or 32 bytes (16 bytes here)
    private readonly byte[] encryptionIV = Encoding.UTF8.GetBytes("8ZqL1vP9#k2F3a6%");  // Must be 16 bytes

    [Header("Current State")]
    public string lastScene = "Home";
    public string currentScene = "Home";
    public int selectedArea = -1;
    public string selectedCategory = "";
    public string selectedPlaceId = "";

    void Awake()
    {
        // 1. Singleton Enforcement: Ensure only one DataManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Survives scene changes!

        // 2. Set up save paths for Android (persistentDataPath)
        savePath = Path.Combine(Application.persistentDataPath, "places_save.json");
        inventorySavePath = Path.Combine(Application.persistentDataPath, "inventory.dat");

        // 3. Load game data and secure inventory on app startup
        LoadData();
        LoadInventory();
    }

    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            placeDatabase = JsonUtility.FromJson<PlaceDatabase>(json);
            Debug.Log($"Loaded place data from persistent storage: {savePath}.");
        }
        else
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("places");
            if (jsonFile != null)
            {
                placeDatabase = JsonUtility.FromJson<PlaceDatabase>(jsonFile.text);
                Debug.Log("Loaded default place data from Resources.");
            }
            else
            {
                Debug.LogError("Places JSON file not found in Resources!");
                placeDatabase = new PlaceDatabase { places = new List<PlaceData>() };
            }
        }
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(placeDatabase, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Place game data saved.");
    }

    #region Secure Inventory Methods

    public void LoadInventory()
    {
        if (File.Exists(inventorySavePath))
        {
            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(inventorySavePath);
                string json = DecryptStringFromBytes(encryptedBytes, encryptionKey, encryptionIV);
                inventory = JsonUtility.FromJson<InventoryData>(json);
                Debug.Log("Loaded secure inventory successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load inventory securely, initializing default. Error: {e.Message}");
                inventory = new InventoryData();
            }
        }
        else
        {
            inventory = new InventoryData();
            SaveInventory();
        }
    }

    public void SaveInventory()
    {
        try
        {
            string json = JsonUtility.ToJson(inventory);
            byte[] encryptedBytes = EncryptStringToBytes(json, encryptionKey, encryptionIV);
            File.WriteAllBytes(inventorySavePath, encryptedBytes);
            Debug.Log("Secure inventory saved.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save inventory securely: {e.Message}");
        }
    }

    // Helper: Add or remove Gold
    public void AddGold(int amount)
    {
        inventory.gold += amount;
        SaveInventory();
    }

    public bool SpendGold(int amount)
    {
        if (inventory.gold >= amount)
        {
            inventory.gold -= amount;
            SaveInventory();
            return true;
        }
        return false;
    }

    // Helper: Add or remove Gems
    public void AddGems(int amount)
    {
        inventory.gems += amount;
        SaveInventory();
    }

    public bool SpendGems(int amount)
    {
        if (inventory.gems >= amount)
        {
            inventory.gems -= amount;
            SaveInventory();
            return true;
        }
        return false;
    }

    // Helper: Manage Stamps
    public void AddStamp(string color, string path, int amount)
    {
        StampItem existingStamp = inventory.stamps.Find(s => s.color == color && s.path == path);
        if (existingStamp != null)
        {
            existingStamp.amount += amount;
        }
        else
        {
            inventory.stamps.Add(new StampItem { color = color, path = path, amount = amount });
        }
        SaveInventory();
    }

    public bool RemoveStamp(string color, string path, int amount)
    {
        StampItem existingStamp = inventory.stamps.Find(s => s.color == color && s.path == path);
        if (existingStamp != null && existingStamp.amount >= amount)
        {
            existingStamp.amount -= amount;
            if (existingStamp.amount <= 0)
            {
                inventory.stamps.Remove(existingStamp);
            }
            SaveInventory();
            return true;
        }
        return false;
    }

    public void ClaimReward(string rewardString)
    {
        if (string.IsNullOrEmpty(rewardString)) return;

        // Split multiple rewards separated by ';'
        string[] rewardEntries = rewardString.Split(';');

        foreach (string entry in rewardEntries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length < 2) continue;

            string rewardType = parts[0].ToLower();

            switch (rewardType)
            {
                case "gold":
                    if (int.TryParse(parts[1], out int goldAmount))
                    {
                        AddGold(goldAmount);
                    }
                    break;

                case "gems":
                    if (int.TryParse(parts[1], out int gemsAmount))
                    {
                        AddGems(gemsAmount);
                    }
                    break;

                case "stamp":
                    // Format expected: stamp : color : path : amount
                    if (parts.Length >= 4)
                    {
                        string color = parts[1];
                        string path = parts[2];
                        if (int.TryParse(parts[3], out int stampAmount))
                        {
                            AddStamp(color, path, stampAmount);
                        }
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown reward type encountered: {rewardType}");
                    break;
            }
        }

        Debug.Log($"Successfully claimed reward: {rewardString}");
    }

    #endregion

    #region AES Encryption Helpers

    private byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
    {
        if (string.IsNullOrEmpty(plainText)) throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0) throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0) throw new ArgumentNullException("IV");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
    }

    private string DecryptStringFromBytes(byte[] cipherTextBytes, byte[] Key, byte[] IV)
    {
        if (cipherTextBytes == null || cipherTextBytes.Length <= 0) throw new ArgumentNullException("cipherTextBytes");
        if (Key == null || Key.Length <= 0) throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0) throw new ArgumentNullException("IV");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherTextBytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }

    #endregion

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GoBack();
        }
    }

    public void GoBack()
    {
        if (!string.IsNullOrEmpty(selectedPlaceId))
        {
            selectedPlaceId = "";
            SceneManager.LoadScene("ByCategoryScene");
        }
        else if (!string.IsNullOrEmpty(selectedCategory) && selectedArea != -1)
        {
            selectedCategory = "";
            SceneManager.LoadScene("ByAreaScene");
        }
        else if (!string.IsNullOrEmpty(selectedCategory) && selectedArea == -1)
        {
            selectedCategory = "";
            SceneManager.LoadScene(currentScene);
        }
        else if (selectedArea != -1) {
            selectedArea = -1;
            SceneManager.LoadScene(currentScene);
        }
        else if (lastScene != currentScene)
        {
            selectedArea = -1;
            currentScene = lastScene;
            SceneManager.LoadScene(lastScene);
        }
    }
}