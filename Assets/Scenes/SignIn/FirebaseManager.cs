using Firebase;
using Firebase.Extensions;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour
{

    public static FirebaseManager Instance { get; private set; }  // Singleton instance

    private FirebaseApp firebaseApp;
    private string databaseUrl = "https://royal-mazzi-fiverrgame-default-rtdb.firebaseio.com/";  // Replace with your Firebase Realtime Database URL

    private string playerEmail;
    private string playerNickname;
    private string playerUserId;

    void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);  // Destroy duplicate
        }
        else
        {
            Instance = this;  // Set this instance as the singleton
            DontDestroyOnLoad(gameObject);  // Make sure this object is not destroyed when loading new scenes
        }
    }

    void Start()
    {
        if (firebaseApp == null)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.Exception != null)
                {
                    Debug.LogError("Firebase initialization failed: " + task.Exception);
                }
                else
                {
                    firebaseApp = FirebaseApp.DefaultInstance;

                    // Manually set the database URL
                    FirebaseDatabase.GetInstance(firebaseApp, databaseUrl);  // This line specifies the URL
                    Debug.Log("Firebase initialized successfully.");
                }
            });
        }
    }

    // Setter method for player's email
    public void SetPlayerEmail(string email)
    {
        playerEmail = email;
        Debug.Log("Player Email Set: " + playerEmail);
    }

    // Setter method for player's nickname
    public void SetPlayerNickname(string nickname)
    {
        playerNickname = nickname;
        Debug.Log("Player Nickname Set: " + playerNickname);
    }

    // Setter method for player's user ID
    public void SetPlayerUserId(string userId)
    {
        playerUserId = userId;
        Debug.Log("Player User ID Set: " + playerUserId);
    }

    // Getter method for player's email
    public string GetPlayerEmail()
    {
        return playerEmail;
    }

    // Getter method for player's nickname
    public string GetPlayerNickname()
    {
        return playerNickname;
    }

    // Getter method for player's user ID
    public string GetPlayerUserId()
    {
        return playerUserId;
    }


}
