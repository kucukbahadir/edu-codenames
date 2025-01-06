using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AddGameManager : MonoBehaviour
{
    // Invoervelden voor game naam, trefwoorden en betekenis
    public TMP_InputField GameNaamInput;
    public TMP_InputField TrefwoordInput;
    public TMP_InputField BetekenisInput;

    // Tekst voor feedback en UI-elementen voor de trefwoordenlijst
    public TextMeshProUGUI FeedbackText;
    public Transform TrefwoordenLijstContent;
    public GameObject TrefwoordPrefab;

    // Interne variabelen voor game gegevens
    private string gameName = "";
    private int gameID = -1; // ID van de game in de database
    private string apiUrlCreateGame = "http://localhost/codenamesAPI/CreateGame.php";
    private string apiUrlAddTrefwoord = "http://localhost/codenamesAPI/AddTrefwoord.php";
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php";

    void Start()
    {
        // Haal de sessietoken op uit PlayerPrefs
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        // Debug: Log de sessietoken
        Debug.Log("Session Token in AddGameManager: " + sessionToken);

        // Controleer of er een sessietoken aanwezig is
        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("Geen sessietoken gevonden. Doorsturen naar login.");
            SceneManager.LoadScene("Login"); // Ga naar het inlogscherm
            return;
        }

        // Start een coroutine om de toegang te controleren
        StartCoroutine(CheckAccess(sessionToken));
    }

    // Controleer de toegang op basis van de sessietoken
    IEnumerator CheckAccess(string sessionToken)
    {
        // Maak een POST-request met de sessietoken
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "POST");
        string json = "{\"session_token\":\"" + sessionToken + "\"}"; // JSON-tekst maken
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        // Controleer de status van de aanvraag
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Toegangscontrole mislukt: " + www.error);
            SceneManager.LoadScene("Login"); // Doorsturen naar login bij mislukking
        }
        else
        {
            try
            {
                // Verwerk de JSON-respons
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Resultaat toegangscontrole: " + jsonResult);

                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Toegang geweigerd: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Toegang verleend: " + response.message);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Fout bij JSON-parsing: " + ex.Message);
                SceneManager.LoadScene("Login");
            }
        }
    }

    // Wordt aangeroepen bij het klikken op "Game aanmaken"
    public void CreateGame()
    {
        string gameNaam = GameNaamInput.text.Trim(); // Haal de ingevoerde game naam op
        if (string.IsNullOrEmpty(gameNaam))
        {
            FeedbackText.text = "Vul een naam in voor de game.";
            return;
        }

        // Start een coroutine om de game aan te maken
        StartCoroutine(CreateGameRequest(gameNaam));
    }

    // Wordt aangeroepen bij het klikken op "Toevoegen"
    public void AddTrefwoord()
    {
        if (gameID == -1) // Controleer of er een game is aangemaakt
        {
            FeedbackText.text = "Maak eerst een game aan voordat je trefwoorden kunt toevoegen.";
            return;
        }

        string trefwoord = TrefwoordInput.text.Trim(); // Haal het trefwoord op
        string betekenis = BetekenisInput.text.Trim(); // Haal de betekenis op

        if (string.IsNullOrEmpty(trefwoord) || string.IsNullOrEmpty(betekenis))
        {
            FeedbackText.text = "Vul zowel een trefwoord als een betekenis in.";
            return;
        }

        // Start een coroutine om het trefwoord toe te voegen
        StartCoroutine(AddTrefwoordRequest(gameID, trefwoord, betekenis));
    }

    // Coroutine om een nieuwe game aan te maken
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
            // Verwerk de respons
            string result = www.downloadHandler.text;
            CreateGameResponse response = JsonUtility.FromJson<CreateGameResponse>(result);

            if (response.status == "success")
            {
                FeedbackText.text = "Game succesvol aangemaakt!";
                gameName = gameNaam;
                gameID = response.gameID; // Sla de gameID op voor trefwoorden
            }
            else
            {
                FeedbackText.text = "Fout bij het aanmaken van de game: " + response.message;
            }
        }
    }

    // Coroutine om een trefwoord toe te voegen aan de game
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
            // Verwerk de respons
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

    // Voeg het trefwoord toe aan de UI-lijst
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

    // Wordt aangeroepen om naar het dashboard te gaan
    public void FinishAndGoToDashboard()
    {
        SceneManager.LoadScene("DashboardTeachers");
    }

    // Klassen voor het verwerken van API-responses
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
