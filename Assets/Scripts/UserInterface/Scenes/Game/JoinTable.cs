using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System;
using UnityEngine.SceneManagement;

public class JoinTable : MonoBehaviour
{
    // UI Elements
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private GameObject tableTemplate;
    [SerializeField] private GameObject noTableText;
    [SerializeField] private GameObject tablesContainer;

    // GameTableInfo list to store fetched tables
    private List<GameTableInfo> gameTableList = new List<GameTableInfo>();

    // Information about errors, player messages
    public GameObject PopupWindow;

    // Currently chosen table index
    private int chosenTable = -1;
    private string chosenTableName;

    // Info about the selected table
    [SerializeField] private TMP_Text InfoPlayersCount;
    [SerializeField] private TMP_Text InfoBotsCount;
    [SerializeField] private TMP_Text InfoMinChips;
    [SerializeField] private TMP_Text InfoMinXP;

    private int id = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Load the tables from Firebase when the scene starts
        LoadTablesFromFirebase();
    }

    // Load the tables from Firebase
    public void LoadTablesFromFirebase()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("Rooms") // Reference to the "Rooms" node in Firebase
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    // Clear the current game table list
                    gameTableList.Clear();

                    // Parse the retrieved data
                    DataSnapshot roomsSnapshot = task.Result;
                    foreach (var room in roomsSnapshot.Children)
                    {
                        // Parse the room data into GameTableInfo object
                        string roomCode = room.Key;
                        string hostIP = room.Child("hostIP").Value.ToString();
                        int playersJoined = Convert.ToInt32(room.Child("playersJoined").Value);
                        string createdAt = room.Child("createdAt").Value.ToString();
                        string hostEmail = room.Child("hostEmail").Value.ToString();
                        string hostNickname = room.Child("hostNickname").Value.ToString();
                        string hostUserId = room.Child("hostUserId").Value.ToString();

                        // Create GameTableInfo and add it to the list
                        GameTableInfo newTable = new GameTableInfo(roomCode, hostNickname, playersJoined.ToString(), "0", "0", "0");
                        gameTableList.Add(newTable);
                    }

                    // Update the table display in the UI
                    DisplayTables();
                }
                else
                {
                    Debug.LogWarning("No rooms available or unable to fetch rooms from Firebase.");
                    // Show a "No tables available" message
                    noTableText.SetActive(true);
                }
            });
    }

    // Display the loaded tables
    public void DisplayTables()
    {
        // Delete previous tables if any
        DeleteTablesOnCanvas();

        // Instantiate and display new table buttons
        GameObject table;
        GameObject tableList = GameObject.FindGameObjectWithTag("TablesList");
        GameObject tableContainer = Instantiate(tablesContainer, tableList.transform);

        for (int i = 0; i < gameTableList.Count; i++)
        {
            table = Instantiate(tableTemplate, tableContainer.transform);
            GameObject tableNameGameObject = table.transform.Find("Button/TableName").gameObject;
            // Set the table name on the button
            tableNameGameObject.GetComponent<TMP_Text>().text = gameTableList[i].Name;
            Button tableButton = table.transform.Find("Button").gameObject.GetComponent<Button>();
            int index = i; // Capture the current index for the button callback
            tableButton.onClick.AddListener(() => OnTableButton(index));
        }
    }

    // Handle table button click (set chosen table)
    void OnTableButton(int index)
    {
        Debug.Log("Table selected: " + index);
        chosenTable = index;
        UpdateGameTableInfo(gameTableList[chosenTable]);
    }

    // Update the UI with the selected table's information
    private void UpdateGameTableInfo(GameTableInfo gameTable)
    {

        // Debugging the selected game table information
        Debug.Log("Chosen Table Name: " + gameTable.Name);
        this.chosenTableName = gameTable.Name;

        Debug.Log("Human Count: " + gameTable.HumanCount);
        this.InfoPlayersCount.text = gameTable.HumanCount;

        Debug.Log("Bot Count: " + gameTable.BotCount);
        this.InfoBotsCount.text = gameTable.BotCount;

        Debug.Log("Min Chips: " + gameTable.minChips);
        this.InfoMinChips.text = gameTable.minChips;

        Debug.Log("Min XP: " + gameTable.minXp);
        this.InfoMinXP.text = gameTable.minXp;

    }

    // Delete previous table UI elements
    void DeleteTablesOnCanvas()
    {
        id = 0;
        GameObject tables = GameObject.FindGameObjectWithTag("TablesList");
        GameObject tablesContainer;
        try
        {
            tablesContainer = tables.transform.Find("TablesContainer(Clone)").gameObject;
            Destroy(tablesContainer);
        }
        catch (Exception e) { }
    }

    // Join button logic
    public void OnJoinButton()
    {
        if (gameTableList.Count == 0)
        {
            Debug.Log("There are no game tables to join.");
            if (PopupWindow)
            {
                ShowPopup("There are no game tables to join. Create one first.");
            }
            return;
        }

        if (chosenTable == -1)
        {
            Debug.Log("You didn't choose any game table.");
            if (PopupWindow)
            {
                ShowPopup("You didn't choose any game table.");
            }
            return;
        }

        // Retrieve the selected game table
        GameTableInfo gameTable = gameTableList[chosenTable];
        Debug.Log("Joining table: " + gameTable.Name);

        // Show a popup message for success/failure here

        // Proceed to join the game table or display errors
        // Add your logic to join the table in Firebase
    }

    // Show popup messages
    void ShowPopup(string text)
    {
        var popup = Instantiate(PopupWindow, transform.position, Quaternion.identity, transform);
        popup.GetComponent<TextMeshProUGUI>().text = text;
    }

    // Back to menu button
    public void OnBackToMenuButton()
    {
        // Go back to the PlayMenu scene
        SceneManager.LoadScene("PlayMenu");
    }
}

// Class to store information about each game table
[System.Serializable]
public class GameTableInfo
{
    public string Name;
    public string Host;
    public string HumanCount;
    public string BotCount;
    public string minXp;
    public string minChips;

    public GameTableInfo(string name, string host, string humanCount, string botCount, string minXp, string minChips)
    {
        Name = name;
        Host = host;
        HumanCount = humanCount;
        BotCount = botCount;
        this.minXp = minXp;
        this.minChips = minChips;
    }
}
