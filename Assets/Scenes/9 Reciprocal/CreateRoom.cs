using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine.UI;

public class CreateRoom : MonoBehaviour
{

    // Przyciski menu
    [SerializeField] private Button joinTableButton;
    [SerializeField] private Button createTableButton;
    [SerializeField] private Button getChipsButton;
    [SerializeField] private Button changeSettingsButton;

    // Informacje o graczu wyœwietlane na ekranie obok menu
    [SerializeField] private TMP_Text InfoPlayerNick;
    [SerializeField] private TMP_Text InfoPlayerChips;
    [SerializeField] private TMP_Text InfoPlayerXP;
    // Firebase Database reference
    private DatabaseReference dbReference;

    // Room code and host IP
    private string roomCode;
    private string hostIP;

    [SerializeField] private GameObject ModeSelectionScreen;
    void Start()
    {
        // Initialize Firebase Realtime Database
        string databaseUrl = "https://royal-mazzi-fiverrgame-default-rtdb.firebaseio.com/";  // Replace with your Firebase Realtime Database URL
        dbReference = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl).RootReference;

        // Fetch host's IP address
        hostIP = GetLocalIPAddress();
        if (string.IsNullOrEmpty(hostIP))
        {
            Debug.LogError("Failed to get local IP address.");
        }
    }



    private string GenerateRoomCode()
    {
        // Generate a random 6-digit number as a room code
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }

    private string GetLocalIPAddress()
    {
        // Get the local IP address of the host machine
        try
        {
            foreach (var address in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error retrieving local IP address: {ex.Message}");
        }
        return null;
    }


    public void OnShowModeScelectionScreen() { 
    
        ModeSelectionScreen.gameObject.SetActive(true);
    }
    public void OnHideModeSelectionScreen() {

        ModeSelectionScreen.gameObject.SetActive(false);
    }
    public void OnCreateRoomButton(int mode)
    {
        if (mode == 0) {
            SceneManager.LoadScene("9 Reciprocal");

        }
        else if (mode == 1) {
            SceneManager.LoadScene("10 Reciprocal");

        }
        else if (mode == 2) {
            SceneManager.LoadScene("11 Reciprocal");

        }

        // Generate a 6-digit room code
        roomCode = GenerateRoomCode();

        // Store room data in Firebase
        CreateRoomInFirebase(roomCode, hostIP , mode);
    }
    private void CreateRoomInFirebase(string roomCode, string hostIP , int mode)
    {
        // Define room data
        var roomData = new
        {
            hostIP = hostIP,
            playersJoined = 1,  // Host counts as the first player
            createdAt = DateTime.UtcNow.ToString("o") // Timestamp for room creation
        };

        // Store room data in Firebase
        dbReference.Child("Rooms").Child(roomCode).SetRawJsonValueAsync(JsonUtility.ToJson(roomData))
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"Room created successfully! Room Code: {roomCode}");
                    TransitionToGameScene(mode);
                }
                else
                {
                    Debug.LogError($"Failed to create room: {task.Exception}");
                }
            });
    }

    private void TransitionToGameScene(int mode)
    {
        if (mode == 0)
        {
            SceneManager.LoadScene("9 Reciprocal");
        }
        else if (mode == 1)
        {
            SceneManager.LoadScene("10 Reciprocal");
        }
        else if (mode == 2)
        {
            SceneManager.LoadScene("11 Reciprocal");
        }
    }
}
