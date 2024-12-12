using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Cache;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PokerGameClasses;
using System.Security.Cryptography;
using Firebase;
using Firebase.Auth;
using Firebase.Database;


public class Login : MonoBehaviour
{
    // Player data fields
    private string playerLogin;
    private string playerPassword;
    private string IP;  // IP field is still present, but unused

    // UI elements
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TMP_InputField loginField;  // Assuming you also have a login field for email

    // Popup for error messages
    public GameObject PopupWindow;

    // Firebase Authentication reference
    private FirebaseAuth auth;

    void Start()
    {
        // Initialize Firebase Auth
        auth = FirebaseAuth.DefaultInstance;
        this.passwordField.contentType = TMP_InputField.ContentType.Password;
        this.passwordField.asteriskChar = '*';
    }

    void Update()
    {
        // IP-related logic could go here, but it's not being used right now
    }

    public void OnLoginButton()
    {
        // Validate login and password
        if (string.IsNullOrEmpty(this.playerLogin) || string.IsNullOrEmpty(this.playerPassword))
        {
            ShowPopup("Please enter both email and password.");
            return;
        }

        // Start the login process
        StartCoroutine(LoginUser(this.playerLogin, this.playerPassword));
    }

    IEnumerator LoginUser(string email, string password)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;

        string databaseUrl = "https://royal-mazzi-fiverrgame-default-rtdb.firebaseio.com/";  // Replace with your Firebase Realtime Database URL
        DatabaseReference dbReference = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl).RootReference;

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            // Handle login failure
            Debug.LogError($"Login failed: {loginTask.Exception}");
            ShowPopup("Login failed. " + loginTask.Exception.Message);
        }
        else
        {
            // Successfully logged in
            FirebaseUser loggedInUser = loginTask.Result.User;
            string userId = loggedInUser.UserId;

            Debug.Log($"User logged in successfully with ID: {userId}");

            // Fetch additional user data from Firebase Database using Dictionary
            dbReference.Child("users").Child(userId).GetValueAsync().ContinueWith(dbTask =>
            {
                if (dbTask.IsCompleted)
                {
                    var userData = dbTask.Result;
                    if (userData.Exists)
                    {
                        // Use a dictionary to get the user data
                        Dictionary<string, object> userInfo = userData.Value as Dictionary<string, object>;
                        if (userInfo != null)
                        {
                            string nickname = userInfo.ContainsKey("nickname") ? userInfo["nickname"].ToString() : "No nickname";
                            string emailFromDb = userInfo.ContainsKey("email") ? userInfo["email"].ToString() : "No email";

                            Debug.Log($"User's nickname: {nickname}");
                            Debug.Log($"User's email: {emailFromDb}");
                            FirebaseManager.Instance.SetPlayerNickname(nickname);
                            FirebaseManager.Instance.SetPlayerEmail(emailFromDb);

                            // Optionally, you can store this info locally or use it elsewhere in your game
                        }
                    }
                    else
                    {
                        Debug.LogWarning("User data does not exist.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to retrieve user data: {dbTask.Exception}");
                }
            });

            // Proceed to the next scene (e.g., main menu or game screen)
            
            SceneManager.LoadScene("MainMenu"); // Change this to your desired scene
        }
    }


    // Helper function to show error popups
    void ShowPopup(string text)
    {
        var popup = Instantiate(PopupWindow, transform.position, Quaternion.identity, transform);
        popup.GetComponent<TextMeshProUGUI>().text = text;
    }

    // Functions to update player login and password fields
    public void ReadLogin(string login)
    {
        this.playerLogin = login;
    }

    public void ReadPassword(string password)
    {
        this.playerPassword = password;
    }

    // IP-related functions are still here, but they're not being used
    public bool ValidateIP(string IP)
    {
        System.Net.IPAddress parsedIP;
        if (IP.Contains("."))
            return System.Net.IPAddress.TryParse(IP, out parsedIP);

        return false;
    }

    public void ReadIP(string IP)
    {
        if (IP.Length == 0)
        {
            this.IP = null;
            return;
        }
        this.IP = IP;
        MyGameManager.Instance.ServerIP = IP;
    }
}
