using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

using System;
using System.Text.RegularExpressions;

using PokerGameClasses;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using Firebase;




// Ekran do rejestracji nowego u¿ytkownika
public class CreatePlayer : MonoBehaviour
{
    [SerializeField] private Button createButton;
    [SerializeField] private Button backToMenuButton;

    [SerializeField] private TMP_InputField newPasswordField;
    [SerializeField] private TMP_InputField confirmPasswordField;

    // informacje o b³êdach, komunikaty dla gracza
    public GameObject PopupWindow;

    // dane z formularza
    private string playerNick;
    private string password1;
    private string password2;
    private string login;

    private FirebaseAuth auth;

    // Start is called before the first frame update
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        this.playerNick = null;
        this.password1 = null;
        this.password2 = null;
        this.login = null;

        this.newPasswordField.contentType = TMP_InputField.ContentType.Password;
        this.newPasswordField.asteriskChar = '*';

        this.confirmPasswordField.contentType = TMP_InputField.ContentType.Password;
        this.confirmPasswordField.asteriskChar = '*';
    }

    // Update is called once per frame
    void Update()
    {
        // wciœniêcie enter robi to samo co wciœniêcie przycisku 'Create'
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (this.playerNick != null && this.password1 != null && this.password2 != null && this.login != null)
                this.OnCreateButton();
        }
    }

    public void OnBackToMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnCreateButton()
    {
        Debug.Log("Create button was clicked.");

        if (!string.IsNullOrEmpty(this.login))
        {
            if (!string.IsNullOrEmpty(this.playerNick))
            {
                if (this.password1 == this.password2 && !string.IsNullOrEmpty(this.password1))
                {
                    Debug.Log("Parameters are good.");
                    StartCoroutine(CreateNewUser(this.login, this.password1, this.playerNick)); // Call Firebase function
                }
                else
                {
                    ShowWrongInputPopup("Passwords are different.");
                }
            }
            else
            {
                ShowWrongInputPopup("Add your nick.");
            }
        }
        else
        {
            ShowWrongInputPopup("Add your login.");
        }
    }


    IEnumerator CreateNewUser(string email, string password, string nickname)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;

        string databaseUrl = "https://royal-mazzi-fiverrgame-default-rtdb.firebaseio.com/";  // Replace with your Firebase Realtime Database URL
        DatabaseReference dbReference = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl).RootReference;

        // Firebase Authentication to create user
        var createUserTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => createUserTask.IsCompleted);

        if (createUserTask.Exception != null)
        {
            Debug.LogError($"User creation failed: {createUserTask.Exception}");
            ShowWrongInputPopup("Failed to create account. " + createUserTask.Exception.Message);
            yield break;
        }

        FirebaseUser newUser = createUserTask.Result.User;
        string userId = newUser.UserId;

        Debug.Log($"User created successfully with ID: {userId}");

        // Save additional data (nickname) to Realtime Database
        var updateProfileTask = newUser.UpdateUserProfileAsync(new UserProfile { DisplayName = nickname });
        yield return new WaitUntil(() => updateProfileTask.IsCompleted);

        if (updateProfileTask.Exception != null)
        {
            Debug.LogError($"Failed to update user profile: {updateProfileTask.Exception}");
            yield break;
        }

        Debug.Log("User profile updated with nickname.");

        // Create a dictionary for the user data
        Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "email", email },
        { "nickname", nickname }
    };

        // Save user information to the database using the dictionary
        var setValueTask = dbReference.Child("users").Child(userId).SetValueAsync(userData);
        yield return new WaitUntil(() => setValueTask.IsCompleted);

        if (setValueTask.Exception != null)
        {
            Debug.LogError($"Failed to save user data: {setValueTask.Exception}");
            yield break;
        }

        Debug.Log("User data saved in Firebase Database.");
        SceneManager.LoadScene("Login"); // Navigate to login scene
    }


    public void ShowWrongInputPopup(string text)
    {
        var popup = Instantiate(PopupWindow, transform.position, Quaternion.identity, transform);
        popup.GetComponent<TextMeshProUGUI>().text = text;
    }

    public void ReadPlayerNick(string nick)
    {
        if (nick.Length == 0)
        {
            this.playerNick = null;
            return;
        }

        this.playerNick = nick;
        Debug.Log(this.playerNick);
    }

    public void ReadLogin(string login)
    {
        if (login.Length == 0)
        {
            this.login = null;
            return;
        }

        this.login = login;
        Debug.Log(this.login);
    }
    public void ReadPassword1(string password)
    {
        if (password.Length == 0)
        {
            this.password1 = null;
            return;
        }

        this.password1 = password;
        Debug.Log(this.password1);
    }
    public void ReadPassword2(string password)
    {
        if (password.Length == 0)
        {
            this.password2 = null;
            return;
        }

        this.password2 = password;
        Debug.Log(this.password2);
    }
}
