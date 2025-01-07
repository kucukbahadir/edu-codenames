using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class EditGameManager : MonoBehaviour
{
    public TMP_InputField nameInputField; // Input voor gamenaam
    public Transform keywordContainer; // Container voor keywords en betekenissen
    public GameObject keywordPrefab; // Prefab voor een keyword-betekenis pair
    public Button saveGameNameButton; // Knop voor gamenaam opslaan

    private int gameID; // ID van de game
    private string getGameUrl = "http://localhost/codenamesAPI/GetGameDetails.php"; // Nieuwe URL om gegevens op te halen

    void Start()
    {
        // Haal het gameID op uit PlayerPrefs
        gameID = PlayerPrefs.GetInt("EditGameID");

        // Debugging om te controleren welk gameID wordt opgehaald
        Debug.Log("GameID opgehaald uit PlayerPrefs: " + gameID);

        // Start de coroutine om de gamegegevens op te halen
        StartCoroutine(GetGameDetails(gameID));
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
                    Debug.Log("Keyword: " + trefwoord.Trefwoord + ", Meaning: " + trefwoord.Betekenis); // Log each keyword and meaning
                    AddKeywordField(trefwoord.Trefwoord, trefwoord.Betekenis);
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


    void AddKeywordField(string keyword, string betekenis)
    {
        // Debugging: log the values being passed
        Debug.Log("Setting keyword: " + keyword + ", meaning: " + betekenis);

        // Create a new keyword and meaning field
        GameObject newField = Instantiate(keywordPrefab, keywordContainer);
        TMP_InputField[] inputs = newField.GetComponentsInChildren<TMP_InputField>();

        // Check if we have the correct number of input fields in the prefab
        if (inputs.Length == 2)
        {
            // Set the text for the keyword and meaning
            inputs[0].text = keyword; // Trefwoord
            inputs[1].text = betekenis; // Betekenis
        }
        else
        {
            Debug.LogError("Expected 2 TMP_InputFields in the prefab, but found " + inputs.Length);
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
        public string Trefwoord;  // Keyword
        public string Betekenis;  // Meaning
    }

}
