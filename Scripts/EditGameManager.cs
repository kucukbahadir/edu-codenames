using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;

public class EditGameManager : MonoBehaviour
{
    public TMP_InputField nameInputField; // Input voor gamenaam
    public Transform keywordContainer; // Container voor keywords en betekenissen
    public GameObject keywordPrefab; // Prefab voor een keyword-betekenis pair
    public Button saveGameNameButton; // Knop voor gamenaam opslaan
    public TextMeshProUGUI feedbackText; // Feedback voor de gebruiker

    private int gameID; // ID van de game
    private string getGameUrl = "http://localhost/codenamesAPI/GetGameDetails.php"; // Nieuwe URL om gegevens op te halen
    private string updateKeywordUrl = "http://localhost/codenamesAPI/UpdateKeyword.php"; // URL for updating keywords
    private string updateGameNameUrl = "http://localhost/codenamesAPI/UpdateGame.php"; // URL for updating game name

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
        Debug.Log("Changing scene to DashboardTeachers");
        SceneManager.LoadScene("DashboardTeachers");
    }

    public void SaveGameName()
    {
        Debug.Log("GameID:" + gameID);
        string gameNaam = nameInputField.text.Trim(); // Haal de naam op uit het invoerveld

        if (string.IsNullOrEmpty(gameNaam))
        {
            feedbackText.text = "De gamenaam mag niet leeg zijn.";
            Debug.LogError("De gamenaam is leeg.");
            return;
        }

        if (gameNaam.Length > 20) // Controleer of de gamenaam ≤ 20 tekens is
        {
            feedbackText.text = "De gamenaam mag maximaal 20 tekens bevatten.";
            Debug.LogError("De gamenaam mag maximaal 20 tekens bevatten.");
            return;
        }

        // Debug: Controleer of feedbackText goed is toegewezen
        if (feedbackText == null)
        {
            Debug.LogError("feedbackText is niet toegewezen in de Inspector.");
            return;
        }

        // Start de coroutine om de gamenaam op te slaan
        StartCoroutine(UpdateGameName(gameID, gameNaam));
    }

    private IEnumerator UpdateGameName(int gameID, string gameNaam)
    {
        // Maak JSON data
        UpdateGameNameRequest requestData = new UpdateGameNameRequest { id = gameID, name = gameNaam };
        string json = JsonUtility.ToJson(requestData);

        Debug.Log("Verstuurde JSON: " + json);

        UnityWebRequest www = new UnityWebRequest(updateGameNameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string result = www.downloadHandler.text;
            Debug.Log("Respons van de server: " + result);

            // Verwerk de respons
            UpdateGameResponse response = JsonUtility.FromJson<UpdateGameResponse>(result);
            if (response.status == "success")
            {
                feedbackText.text = "Gamenaam succesvol bijgewerkt!";
                Debug.Log("Gamenaam succesvol bijgewerkt.");
            }
            else
            {
                feedbackText.text = "Fout: " + response.message;
                Debug.LogError("Fout bij updaten gamenaam: " + response.message);
            }
        }
        else
        {
            feedbackText.text = "Fout bij het verbinden met de server.";
            Debug.LogError("Serverfout: " + www.error);
        }
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

                // Verwijder bestaande trefwoorden in de UI
                foreach (Transform child in keywordContainer)
                {
                    Destroy(child.gameObject);
                }

                // Voeg de nieuwe trefwoorden toe aan de UI
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

            // Na een succesvolle update, haal opnieuw de gamegegevens op om de trefwoordenlijst bij te werken
            StartCoroutine(GetGameDetails(gameID));
        }
        else
        {
            Debug.LogError("Failed to update keyword: " + www.error);
        }
    }

    [System.Serializable]
    public class GameIDRequest
    {
        public int id;
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
    public class UpdateGameNameRequest
    {
        public int id;
        public string name;
    }

    [System.Serializable]
    public class UpdateGameResponse
    {
        public string status;
        public string message;
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
