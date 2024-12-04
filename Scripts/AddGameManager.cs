using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class AddGameManager : MonoBehaviour
{
    public TMP_InputField GameNaamInput;
    public TMP_InputField TrefwoordInput;
    public TMP_InputField BetekenisInput;

    public TextMeshProUGUI FeedbackText; // Voor fout- of succesmeldingen
    public Transform TrefwoordenLijstContent; // The Content object of your Scroll View
    public GameObject TrefwoordPrefab; // Een prefab met de naam en betekenis als UI-element

    private string gameName = "";
    private int gameID = -1; // ID van de game in de database
    private string apiUrlCreateGame = "http://localhost/codenamesAPI/CreateGame.php";
    private string apiUrlAddTrefwoord = "http://localhost/codenamesAPI/AddTrefwoord.php";
    private string apiUrlCheckGameName = "http://localhost/codenamesAPI/CheckGameName.php"; // URL to check the game name

    // Wordt aangeroepen door de knop "Game aanmaken"
    public void CreateGame()
    {
        string gameNaam = GameNaamInput.text.Trim();
        if (string.IsNullOrEmpty(gameNaam))
        {
            FeedbackText.text = "Vul een naam in voor de game.";  // This message should appear if input is empty
            return;
        }

        Debug.Log($"Game Name: {gameNaam}");  // Debugging log to check if gameNaam is being populated

        StartCoroutine(CheckGameName(gameNaam));  // Proceed with the game name check
    }


    // New function to check if the game name already exists
    private IEnumerator CheckGameName(string gameNaam)
    {
        WWWForm form = new WWWForm();
        form.AddField("gameNaam", gameNaam);

        // Log the data being sent
        Debug.Log($"Sending Game Name: {gameNaam}");

        UnityWebRequest www = UnityWebRequest.Post(apiUrlCheckGameName, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            FeedbackText.text = "Fout bij het controleren van de gamenaam: " + www.error;
        }
        else
        {
            string result = www.downloadHandler.text;
            GenericResponse response = JsonUtility.FromJson<GenericResponse>(result);

            if (response.status == "error")
            {
                // If game name exists, show error message
                FeedbackText.text = response.message; // "Gamenaam bestaat al"
            }
            else
            {
                // If game name is available, proceed to create the game
                StartCoroutine(CreateGameRequest(gameNaam));
            }
        }
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

        Debug.Log($"Prefab toegevoegd op positie: {newItem.transform.localPosition}");
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
