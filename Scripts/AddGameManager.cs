using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AddGameManager : MonoBehaviour
{
    public TMP_InputField GameNaamInput;
    public TMP_InputField TrefwoordInput;
    public TMP_InputField BetekenisInput;

    public TextMeshProUGUI FeedbackText;
    public Transform TrefwoordenLijstContent;
    public GameObject TrefwoordPrefab;

    private string gameName = "";
    private int gameID = -1; // ID van de game in de database
    private string apiUrlCreateGame = "http://localhost/codenamesAPI/CreateGame.php";
    private string apiUrlAddTrefwoord = "http://localhost/codenamesAPI/AddTrefwoord.php";
    private string apiUrlCheckGameName = "http://localhost/codenamesAPI/CheckGameName.php";
    [SerializeField] private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php";


    void Start()
    {
        // Get the session token from PlayerPrefs
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        // Debug: Log the session token in Unity
        Debug.Log("Session Token in AddGameManager: " + sessionToken);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("No session token found. Redirecting to login.");
            SceneManager.LoadScene("Login"); // Redirect to Login scene
            return;
        }

        StartCoroutine(CheckAccess(sessionToken));
    }

    IEnumerator CheckAccess(string sessionToken)
    {
        // Prepare the POST request with the session token
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "POST");
        string json = "{\"session_token\":\"" + sessionToken + "\"}"; // Construct JSON string
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Access check failed: " + www.error);
            SceneManager.LoadScene("Login"); // Redirect to Login if check fails
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Access check result in AddGameManager: " + jsonResult);

                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Access denied in AddGameManager: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Access granted in AddGameManager: " + response.message);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse error in AddGameManager: " + ex.Message);
                SceneManager.LoadScene("Login");
            }
        }
    }


    // Wordt aangeroepen door de knop "Game aanmaken"
    public void CreateGame()
    {
        string gameNaam = GameNaamInput.text.Trim();
        if (string.IsNullOrEmpty(gameNaam))
        {
            FeedbackText.text = "Vul een naam in voor de game.";
            return;
        }

        StartCoroutine(CreateGameRequest(gameNaam));
    }

    // Wordt aangeroepen door de knop "Toevoegen"
    public void AddTrefwoord()
    {
        if (gameID == -1)
        {
            FeedbackText.text = "Maak eerst een game aan voordat je trefwoorden kunt toevoegen.";
            return;
        }

        string trefwoord = TrefwoordInput.text.Trim();
        string betekenis = BetekenisInput.text.Trim();

        if (string.IsNullOrEmpty(trefwoord) || string.IsNullOrEmpty(betekenis))
        {
            FeedbackText.text = "Vul zowel een trefwoord als een betekenis in.";
            return;
        }

        StartCoroutine(AddTrefwoordRequest(gameID, trefwoord, betekenis));
    }

    private IEnumerator CreateGameRequest(string gameNaam)
    {
        WWWForm form = new WWWForm();
        form.AddField("gameNaam", gameNaam);

        UnityWebRequest www = UnityWebRequest.Post(apiUrlCreateGame, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            FeedbackText.text = "Fout bij het aanmaken van de game: " + www.error;
        }
        else
        {
            string result = www.downloadHandler.text;
            CreateGameResponse response = JsonUtility.FromJson<CreateGameResponse>(result);

            if (response.status == "success")
            {
                FeedbackText.text = "Game succesvol aangemaakt!";
                gameName = gameNaam;
                gameID = response.gameID; // Haal de gameID op voor verdere trefwoorden
            }
            else
            {
                FeedbackText.text = "Fout bij het aanmaken van de game: " + response.message;
            }
        }
    }

    private IEnumerator AddTrefwoordRequest(int gameID, string trefwoord, string betekenis)
    {
        WWWForm form = new WWWForm();
        form.AddField("gameID", gameID);
        form.AddField("trefwoord", trefwoord);
        form.AddField("betekenis", betekenis);

        UnityWebRequest www = UnityWebRequest.Post(apiUrlAddTrefwoord, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            FeedbackText.text = "Fout bij het toevoegen van het trefwoord: " + www.error;
        }
        else
        {
            string result = www.downloadHandler.text;
            GenericResponse response = JsonUtility.FromJson<GenericResponse>(result);

            if (response.status == "success")
            {
                FeedbackText.text = "Trefwoord succesvol toegevoegd!";
                AddToTrefwoordenLijst(trefwoord, betekenis);

                // Reset de invoervelden
                TrefwoordInput.text = "";
                BetekenisInput.text = "";
            }
            else
            {
                FeedbackText.text = "Fout bij het toevoegen van het trefwoord: " + response.message;
            }
        }
    }

    void AddToTrefwoordenLijst(string trefwoord, string betekenis)
    {
        GameObject newItem = Instantiate(TrefwoordPrefab, TrefwoordenLijstContent);
        TextMeshProUGUI[] texts = newItem.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = $"Trefwoord: {trefwoord}";
            texts[1].text = $"Betekenis: {betekenis}";
        }
    }

    public void FinishAndGoToDashboard()
    {
        SceneManager.LoadScene("DashboardTeachers");
    }
    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public string message;
    }

    [System.Serializable]
    public class CreateGameResponse
    {
        public string status;
        public string message;
        public int gameID; // De ID van de aangemaakte game
    }

    [System.Serializable]
    public class GenericResponse
    {
        public string status;
        public string message;
    }
}
