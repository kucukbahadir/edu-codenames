using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    // API URLs voor communicatie met de backend
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // Controleer toegang via sessie-token
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php"; // Haal spelgegevens op
    private string deleteGameUrl = "http://localhost/codenamesAPI/DeleteGame.php";   // Verwijder een spel

    // UI-elementen voor de tabel
    public Transform tableContent; // De container in de UI waar de tabelrijen worden toegevoegd
    public GameObject tableRowPrefab; // De prefab dat een tabelrij voorstelt

    void Start()
    {
        // Haal de sessie-token op uit PlayerPrefs
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        Debug.Log("Session Token: " + sessionToken);

        // Controleer of er een sessie-token is. Zo niet, ga naar het inlogscherm.
        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("Geen sessie-token gevonden. Doorverwijzen naar login.");
            SceneManager.LoadScene("Login");
            return;
        }

        // Start een coroutine om toegang te controleren
        StartCoroutine(CheckAccess(sessionToken));
    }

    IEnumerator CheckAccess(string sessionToken)
    {
        // Maak een POST-verzoek om toegang te controleren via de sessie-token
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "POST");
        string json = "{\"session_token\":\"" + sessionToken + "\"}"; // JSON-string met de sessie-token
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        // Controleer of het verzoek succesvol was
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Toegangscontrole mislukt: " + www.error);
            SceneManager.LoadScene("Login");
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Resultaat Toegangscontrole: " + jsonResult);

                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Toegang geweigerd: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Toegang verleend: " + response.message);
                    // Haal de lijst met spellen op
                    StartCoroutine(FetchGamesData());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Fout bij JSON-parseren: " + ex.Message);
                SceneManager.LoadScene("Login");
            }
        }
    }

    IEnumerator FetchGamesData()
    {
        // Haal de gegevens van spellen op via een GET-verzoek
        UnityWebRequest www = UnityWebRequest.Get(fetchGamesUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Ophalen van spellen mislukt: " + www.error);
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Spelgegevens: " + jsonResult);

                // Parse de JSON-resultaten in een lijst met spellen
                List<Game> games = JsonUtility.FromJson<GamesResponse>("{\"games\":" + jsonResult + "}").games;

                // Vul de UI-tabel met de spellenlijst
                PopulateTable(games);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Fout bij JSON-parseren: " + ex.Message);
            }
        }
    }

    void PopulateTable(List<Game> games)
    {
        // Verwijder alle bestaande rijen in de tabel
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }

        // Voeg voor elk spel een nieuwe rij toe aan de tabel
        foreach (Game game in games)
        {
            GameObject row = Instantiate(tableRowPrefab, tableContent);

            // Vul de kolommen in de tabelrij met gegevens
            TextMeshProUGUI[] columns = row.GetComponentsInChildren<TextMeshProUGUI>();
            columns[0].text = game.GameID.ToString();  // Eerste kolom: GameID
            columns[1].text = game.Gamenaam;          // Tweede kolom: Naam van het spel
            columns[2].text = FormatKeywords(game.Trefwoorden); // Derde kolom: Lijst met trefwoorden

            // Zoek de verwijderknop en koppel een actie aan de knop
            Button deleteButton = row.GetComponentInChildren<Button>();
            deleteButton.onClick.AddListener(() => DeleteGame(game.GameID));
        }
    }

    string FormatKeywords(List<string> keywords)
    {
        // Formatteer de lijst met trefwoorden voor betere weergave in de tabel
        return string.Join("\n", keywords);
    }

    public void DeleteGame(int gameID)
    {
        // Start een coroutine om een spel te verwijderen
        StartCoroutine(DeleteGameCoroutine(gameID));
    }

    IEnumerator DeleteGameCoroutine(int gameId)
    {
        Debug.Log($"Proberen om spel met ID {gameId} te verwijderen...");

        if (gameId <= 0)
        {
            Debug.LogError("Ongeldige spel-ID.");
            yield break;
        }

        UnityWebRequest www = new UnityWebRequest(deleteGameUrl, "POST");
        string json = JsonUtility.ToJson(new { id = gameId }); // Maak JSON met de spel-ID
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fout bij verbinden met server: " + www.error);
        }
        else
        {
            Debug.Log("Serverantwoord: " + www.downloadHandler.text);
            // Verwerk serverantwoord (bijv. tabel opnieuw laden)
        }
    }

    public void Logout()
    {
        // Verwijder sessie-token en ga naar het inlogscherm
        PlayerPrefs.DeleteKey("SessionToken");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login");
    }

    public void AddGameRedirect()
    {
        // Ga naar de scene om een nieuw spel toe te voegen
        SceneManager.LoadScene("AddGame");
    }

    // Classes voor het verwerken van JSON-data
    [System.Serializable]
    public class ResponseData
    {
        public string status;  // Status van de respons ("success" of "error")
        public string message; // Bericht van de server
    }

    [System.Serializable]
    public class Game
    {
        public int GameID;           // Unieke ID van het spel
        public string Gamenaam;      // Naam van het spel
        public List<string> Trefwoorden; // Lijst van trefwoorden die bij het spel horen
    }

    [System.Serializable]
    public class GamesResponse
    {
        public List<Game> games; // Lijst van spellen die door de server worden teruggegeven
    }
}
