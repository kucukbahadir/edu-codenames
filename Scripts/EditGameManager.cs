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
    private string apiKey = "06e49cf4e293d0f530a00386d6882e07d599eac1fac4a585881fe9d749a106a2";


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
        UpdateGameNameRequest requestData = new UpdateGameNameRequest { id = gameID, name = gameNaam };
        string json = JsonUtility.ToJson(requestData);

        Debug.Log("Verstuurde JSON: " + json);

        UnityWebRequest www = new UnityWebRequest(updateGameNameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", apiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string result = www.downloadHandler.text;
            Debug.Log("Respons van de server: " + result);

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
        if (gameID == 0)
        {
            Debug.LogError("Invalid gameID: " + gameID);
            yield break;
        }

        GameIDRequest request = new GameIDRequest { id = gameID };
        string json = JsonUtility.ToJson(request);

        Debug.Log("Sending JSON: " + json);

        UnityWebRequest www = new UnityWebRequest(getGameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", apiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + www.downloadHandler.text);
            GameDetails gameDetails = JsonUtility.FromJson<GameDetails>(www.downloadHandler.text);

            if (gameDetails != null)
            {
                nameInputField.text = gameDetails.gamenaam;

                // Debug: Check if keywords exist
                if (gameDetails.trefwoorden == null || gameDetails.trefwoorden.Count == 0)
                {
                    Debug.LogWarning("No keywords received from the server.");
                }
                else
                {
                    Debug.Log("Number of keywords received: " + gameDetails.trefwoorden.Count);
                    foreach (var keyword in gameDetails.trefwoorden)
                    {
                        Debug.Log($"Keyword: {keyword.Trefwoord}, Meaning: {keyword.Betekenis}");
                        AddKeywordField(keyword.TrefwoordID, keyword.Trefwoord, keyword.Betekenis);
                    }
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

        www.SetRequestHeader("Authorization", apiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Keyword updated successfully: " + www.downloadHandler.text);

            // Na een succesvolle update, laad de huidige scène opnieuw
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

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
    public class UpdateKeywordRequest
    {
        public int id; // Game ID
        public int trefwoordID; // Trefwoord ID
        public string keyword; // Keyword
        public string betekenis; // Meaning
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
}
