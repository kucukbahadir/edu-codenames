using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class EditGameManager : MonoBehaviour
{
    public TMP_InputField nameInputField; // Input voor gamenaam
    public Transform keywordContainer; // Container voor keywords en betekenissen
    public GameObject keywordPrefab; // Prefab voor een keyword-betekenis pair
    public Button saveGameNameButton; // Knop voor gamenaam opslaan

    private int gameID; // ID van de game
    private string getGameUrl = "http://localhost/codenamesAPI/GetGameDetails.php"; // Nieuwe URL om gegevens op te halen
    private string updateKeywordUrl = "http://localhost/codenamesAPI/UpdateKeyword.php"; // URL for updating keywords

    void Start()
    {
        // Haal het gameID op uit PlayerPrefs
        gameID = PlayerPrefs.GetInt("EditGameID");

        // Debugging om te controleren welk gameID wordt opgehaald
        Debug.Log("GameID opgehaald uit PlayerPrefs: " + gameID);

        // Start de coroutine om de gamegegevens op te halen
        StartCoroutine(GetGameDetails(gameID));
    }
    public void ChangeToDashboardTeachers()
    {
        SceneManager.LoadScene("DashboardTeachers");
    }

    [System.Serializable]
    public class GameIDRequest
    {
        public int id;
    }

    IEnumerator GetGameDetails(int gameID)
    {
        // Check if gameID is valid
        if (gameID == 0)
        {
            Debug.LogError("Invalid gameID: " + gameID);
            yield break;
        }

        // Create the request object
        GameIDRequest request = new GameIDRequest();
        request.id = gameID;

        // Serialize it to JSON
        string json = JsonUtility.ToJson(request);
        Debug.Log("Sending JSON: " + json);  // Log the JSON being sent

        UnityWebRequest www = new UnityWebRequest(getGameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + www.downloadHandler.text);
            string response = www.downloadHandler.text;
            GameDetails gameDetails = JsonUtility.FromJson<GameDetails>(response);

            if (gameDetails != null)
            {
                Debug.Log("Game name: " + gameDetails.gamenaam);
                Debug.Log("Number of keywords: " + gameDetails.trefwoorden.Count);

                // Set the game name in the input field
                nameInputField.text = gameDetails.gamenaam;

                foreach (Keyword trefwoord in gameDetails.trefwoorden)
                {
                    Debug.Log($"Keyword: {trefwoord.Trefwoord}, Meaning: {trefwoord.Betekenis}, ID: {trefwoord.TrefwoordID}");
                    AddKeywordField(trefwoord.TrefwoordID, trefwoord.Trefwoord, trefwoord.Betekenis);
                }

            }
            else
            {
                Debug.LogError("Game details could not be parsed.");
            }
        }
        else
        {
            Debug.LogError("Error fetching the game: " + www.error);
        }
    }

    void AddKeywordField(int trefwoordID, string keyword, string betekenis)
    {
        GameObject newField = Instantiate(keywordPrefab, keywordContainer);
        TMP_InputField[] inputs = newField.GetComponentsInChildren<TMP_InputField>();

        if (inputs.Length == 2)
        {
            inputs[0].text = keyword; // Trefwoord
            inputs[1].text = betekenis; // Betekenis

            Button updateButton = newField.GetComponentInChildren<Button>();
            if (updateButton != null)
            {
                updateButton.onClick.AddListener(() =>
                {
                    string updatedKeyword = inputs[0].text;
                    string updatedBetekenis = inputs[1].text;
                    Debug.Log($"Updating field. ID: {trefwoordID}, Updated Keyword: {updatedKeyword}, Updated Meaning: {updatedBetekenis}");
                    StartCoroutine(SendUpdatedKeyword(trefwoordID, updatedKeyword, updatedBetekenis));
                });
            }
        }
        else
        {
            Debug.LogError("Expected 2 TMP_InputFields in the prefab, but found " + inputs.Length);
        }
    }

    public void UpdateKeyword(int trefwoordID, string updatedKeyword, string updatedBetekenis)
    {
        Debug.Log($"Updating keyword: {updatedKeyword}, meaning: {updatedBetekenis}");

        // Start coroutine to send the updated data to the server
        StartCoroutine(SendUpdatedKeyword(trefwoordID, updatedKeyword, updatedBetekenis));

    }

    IEnumerator SendUpdatedKeyword(int trefwoordID, string keyword, string meaning)
    {
        Debug.Log($"Preparing to send update. TrefwoordID: {trefwoordID}, Keyword: {keyword}, Meaning: {meaning}");

        if (gameID <= 0 || trefwoordID <= 0)
        {
            Debug.LogError("Invalid gameID or TrefwoordID");
            yield break;
        }

        // Create the update request object
        UpdateKeywordRequest updateData = new UpdateKeywordRequest
        {
            id = gameID,
            trefwoordID = trefwoordID,
            keyword = keyword,
            betekenis = meaning
        };

        // Serialize the data into JSON
        string json = JsonUtility.ToJson(updateData);

        Debug.Log("Sending JSON: " + json); // Log the JSON being sent

        UnityWebRequest www = new UnityWebRequest(updateKeywordUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Keyword updated successfully: " + www.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Failed to update keyword: " + www.error);
        }
    }




    [System.Serializable]
    public class GameDetails
    {
        public string gamenaam; // Name of the game
        public List<Keyword> trefwoorden; // List of keywords
    }

    [System.Serializable]
    public class Keyword
    {
        public int TrefwoordID;  // ID for the keyword
        public string Trefwoord;  // Keyword
        public string Betekenis;  // Meaning
    }
    [System.Serializable]
    public class UpdateKeywordRequest
    {
        public int id; // Game ID
        public int trefwoordID; // Trefwoord ID
        public string keyword; // Keyword
        public string betekenis; // Meaning
    }

}
