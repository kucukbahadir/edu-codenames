using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    // URLs voor toegang en data ophalen
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // PHP-URL voor toegang controleren
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php"; // URL om spelgegevens op te halen

    // Referenties naar UI-elementen
    public Transform GamesTable; // Ouderelement voor alle spelrijen
    public GameObject GameRowPrefab; // Prefab voor een rij met een spel
    public GameObject TrefwoordRowPrefab; // Prefab voor een rij met trefwoorden

    void Start()
    {
        // Haal de sessietoken op uit PlayerPrefs
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        // Debug: Log de sessietoken voor debugging
        Debug.Log("Session Token: " + sessionToken);

        // Als er geen sessietoken is, wordt de gebruiker doorgestuurd naar de login-pagina
        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("Geen sessietoken gevonden. Doorsturen naar login.");
            SceneManager.LoadScene("Login"); // Doorsturen naar login-scherm
            return;
        }

        // Start de controle van toegang met het opgehaalde sessietoken
        StartCoroutine(CheckAccess(sessionToken));
    }

    // Coroutine om toegang te controleren via een POST-verzoek naar de server
    IEnumerator CheckAccess(string sessionToken)
    {
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "POST");
        string json = "{\"session_token\":\"" + sessionToken + "\"}"; // JSON-string samenstellen
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        // Wacht tot het verzoek compleet is
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // Foutmelding en doorsturen naar login als het verzoek faalt
            Debug.LogError("Toegang controleren mislukt: " + www.error);
            SceneManager.LoadScene("Login");
        }
        else
        {
            try
            {
                // Verwerk het resultaat van het verzoek
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Resultaat toegang controleren: " + jsonResult);

                // JSON-parseren om te zien of toegang is toegestaan
                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Toegang geweigerd: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Toegang verleend: " + response.message);
                    StartCoroutine(FetchGamesData()); // Haal spelgegevens op als toegang is verleend
                }
            }
            catch (System.Exception ex)
            {
                // Fout bij JSON-parsing en doorsturen naar login
                Debug.LogError("JSON-parserfout: " + ex.Message);
                SceneManager.LoadScene("Login");
            }
        }
    }

    // Coroutine om spelgegevens van de server op te halen
    IEnumerator FetchGamesData()
    {
        UnityWebRequest www = UnityWebRequest.Get(fetchGamesUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // Log foutmelding als het ophalen van gegevens mislukt
            Debug.LogError("Fout bij het ophalen van spelgegevens: " + www.error);
        }
        else
        {
            // Verwerk het antwoord van de server
            string json = www.downloadHandler.text;
            Debug.Log("Spelgegevens: " + json);

            try
            {
                // Parse de JSON-data en vul de tabel
                GamesResponse gamesResponse = JsonUtility.FromJson<GamesResponse>(json);
                PopulateGamesTable(gamesResponse.games);
            }
            catch (System.Exception ex)
            {
                // Log een fout als JSON niet goed wordt verwerkt
                Debug.LogError("JSON-parserfout: " + ex.Message);
            }
        }
    }

    // Vul de tabel met spellen en hun trefwoorden
    void PopulateGamesTable(List<GameData> games)
    {
        // Verwijder oude rijen als de tabel opnieuw wordt gevuld
        foreach (Transform child in GamesTable)
        {
            Destroy(child.gameObject);
        }

        foreach (var game in games)
        {
            // Maak een nieuwe rij voor het spel
            GameObject gameRow = Instantiate(GameRowPrefab, GamesTable);
            TextMeshProUGUI gameNameText = gameRow.GetComponentInChildren<TextMeshProUGUI>();
            gameNameText.text = game.name; // Stel de naam van het spel in

            // Zoek de container voor trefwoorden in de prefab
            Transform trefwoordenParent = gameRow.transform.Find("TrefwoordenContainer");
            if (trefwoordenParent == null)
            {
                Debug.LogError("TrefwoordenContainer ontbreekt in GameRowPrefab.");
                continue;
            }

            // Verberg de trefwoordencontainer standaard
            trefwoordenParent.gameObject.SetActive(false);

            // Voeg functionaliteit toe om de trefwoorden te tonen/verbergen
            gameRow.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                bool isActive = trefwoordenParent.gameObject.activeSelf;
                trefwoordenParent.gameObject.SetActive(!isActive); // Toggle zichtbaar/onzichtbaar
            });

            // Voeg rijen toe voor elk trefwoord
            foreach (var trefwoord in game.trefwoorden)
            {
                GameObject trefwoordRow = Instantiate(TrefwoordRowPrefab, trefwoordenParent);
                TextMeshProUGUI[] texts = trefwoordRow.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = trefwoord.trefwoord; // Stel het trefwoord in
                    texts[1].text = trefwoord.betekenis; // Stel de betekenis in
                }
            }
        }
    }

    // Functie om uit te loggen en de gebruiker terug te sturen naar login
    public void Logout()
    {
        PlayerPrefs.DeleteKey("SessionToken"); // Verwijder de sessietoken uit PlayerPrefs
        PlayerPrefs.Save(); // Sla wijzigingen in PlayerPrefs op
        SceneManager.LoadScene("Login"); // Doorsturen naar het login-scherm
    }

    // Functie om naar het AddGame-scherm te gaan
    public void AddGameRedirect()
    {
        SceneManager.LoadScene("AddGame");
    }

    // Klassen om gegevens uit de JSON-structuur te deserialiseren
    [System.Serializable]
    public class ResponseData
    {
        public string status; // Status van de toegang (bijv. "success" of "error")
        public string message; // Bericht van de server
    }

    [System.Serializable]
    public class TrefwoordData
    {
        public string trefwoord; // Trefwoord
        public string betekenis; // Betekenis van het trefwoord
    }

    [System.Serializable]
    public class GameData
    {
        public string name; // Naam van het spel
        public List<TrefwoordData> trefwoorden; // Lijst van trefwoorden bij het spel
    }

    [System.Serializable]
    public class GamesResponse
    {
        public List<GameData> games; // Lijst van alle spellen
    }
}
