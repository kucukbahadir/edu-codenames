using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class NewKeywordManager : MonoBehaviour
{
    public TMP_InputField keywordInputField; // Input voor het trefwoord
    public TMP_InputField meaningInputField; // Input voor de betekenis
    public Button createButton; // Knop voor aanmaken
    public TextMeshProUGUI feedbackText; // Feedback voor de gebruiker
    private string apiKey = "06e49cf4e293d0f530a00386d6882e07d599eac1fac4a585881fe9d749a106a2";

    private string addKeywordUrl = "http://localhost/codenamesAPI/AddKeyword.php"; // API voor het toevoegen van trefwoorden
    private int gameID; // Huidige GameID

    void Start()
    {
        // Haal het gameID op uit PlayerPrefs
        gameID = PlayerPrefs.GetInt("EditGameID");

        if (gameID <= 0)
        {
            Debug.LogError("Geen geldig GameID gevonden in PlayerPrefs.");
        }

        // Verbind de knop met de functie
        createButton.onClick.AddListener(AddNewKeyword);
    }

    public void AddNewKeyword()
    {
        string keyword = keywordInputField.text.Trim();
        string meaning = meaningInputField.text.Trim();

        // Validatie
        if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(meaning))
        {
            feedbackText.text = "Trefwoord en betekenis mogen niet leeg zijn.";
            return;
        }

        // Start de coroutine om het trefwoord toe te voegen
        StartCoroutine(SendNewKeyword(keyword, meaning));
    }

    private IEnumerator SendNewKeyword(string keyword, string meaning)
    {
        if (gameID <= 0)
        {
            Debug.LogError("Geen geldig GameID beschikbaar.");
            yield break;
        }

        // Maak het verzoekobject
        AddKeywordRequest newKeywordData = new AddKeywordRequest
        {
            gameID = gameID,
            keyword = keyword,
            betekenis = meaning
        };

        // Serialiseer naar JSON
        string json = JsonUtility.ToJson(newKeywordData);

        Debug.Log("Verstuurde JSON voor nieuw trefwoord: " + json);

        UnityWebRequest www = new UnityWebRequest(addKeywordUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        www.SetRequestHeader("Authorization", apiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Nieuw trefwoord succesvol toegevoegd: " + www.downloadHandler.text);
            feedbackText.text = "Trefwoord succesvol toegevoegd.";

            // Herlaad de huidige scène
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogError("Fout bij het toevoegen van trefwoord: " + www.error);
            feedbackText.text = "Fout bij het toevoegen van trefwoord.";
        }
    }



    [System.Serializable]
    public class AddKeywordRequest
    {
        public int gameID;
        public string keyword;
        public string betekenis;
    }
}
