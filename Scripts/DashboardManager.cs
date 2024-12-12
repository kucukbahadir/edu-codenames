using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // URL voor toegang controleren
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php"; // URL om spelgegevens op te halen

    public Transform GamesTable; // Container voor spelrijen
    public GameObject GameRowPrefab; // Prefab voor een spelrij
    public GameObject TrefwoordRowPrefab; // Prefab voor trefwoordrij

    void Start()
    {
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        Debug.Log("Session Token: " + sessionToken);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("Geen sessietoken gevonden. Doorsturen naar login.");
            SceneManager.LoadScene("Login");
            return;
        }

        StartCoroutine(CheckAccess(sessionToken));
    }

    IEnumerator CheckAccess(string sessionToken)
    {
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "POST");
        string json = "{\"session_token\":\"" + sessionToken + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Toegang controleren mislukt: " + www.error);
            SceneManager.LoadScene("Login");
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Resultaat toegang controleren: " + jsonResult);

                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Toegang geweigerd: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Toegang verleend: " + response.message);
                    StartCoroutine(FetchGamesData());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON-parserfout: " + ex.Message);
                SceneManager.LoadScene("Login");
            }
        }
    }

    IEnumerator FetchGamesData()
    {
        UnityWebRequest www = UnityWebRequest.Get(fetchGamesUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fout bij het ophalen van spelgegevens: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            Debug.Log("Spelgegevens: " + json);

            try
            {
                GamesResponse gamesResponse = JsonUtility.FromJson<GamesResponse>(json);
                PopulateGamesTable(gamesResponse.games);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON-parserfout: " + ex.Message);
            }
        }
    }

    void PopulateGamesTable(List<GameData> games)
    {
        foreach (Transform child in GamesTable)
        {
            Destroy(child.gameObject); // Verwijder oude rijen
        }

        Debug.Log("Populeren van tabel gestart. Aantal spellen: " + games.Count);

        foreach (var game in games)
        {
            Debug.Log("Bezig met verwerken van spel: " + game.name);

            GameObject gameRow = Instantiate(GameRowPrefab, GamesTable);
            TextMeshProUGUI gameNameText = gameRow.GetComponentInChildren<TextMeshProUGUI>();

            if (gameNameText == null)
            {
                Debug.LogError("TextMeshProUGUI component niet gevonden in GameRowPrefab.");
                continue;
            }

            gameNameText.text = game.name;

            Transform trefwoordenParent = gameRow.transform.Find("TrefwoordenContainer");
            if (trefwoordenParent == null)
            {
                Debug.LogError("TrefwoordenContainer ontbreekt in GameRowPrefab.");
                continue;
            }

            Debug.Log("Aantal trefwoorden voor " + game.name + ": " + game.trefwoorden.Count);

            foreach (var trefwoord in game.trefwoorden)
            {
                GameObject trefwoordRow = Instantiate(TrefwoordRowPrefab, trefwoordenParent);
                TextMeshProUGUI[] texts = trefwoordRow.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length < 2)
                {
                    Debug.LogError("TrefwoordRowPrefab heeft niet genoeg TextMeshProUGUI componenten.");
                    continue;
                }

                texts[0].text = trefwoord.trefwoord;
                texts[1].text = trefwoord.betekenis;
            }

            trefwoordenParent.gameObject.SetActive(false);

            gameRow.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                bool isActive = trefwoordenParent.gameObject.activeSelf;
                trefwoordenParent.gameObject.SetActive(!isActive);
            });
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("SessionToken");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login");
    }

    public void AddGameRedirect()
    {
        SceneManager.LoadScene("AddGame");
    }

    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public string message;
    }

    [System.Serializable]
    public class TrefwoordData
    {
        public string trefwoord;
        public string betekenis;
    }

    [System.Serializable]
    public class GameData
    {
        public string name;
        public List<TrefwoordData> trefwoorden;
    }

    [System.Serializable]
    public class GamesResponse
    {
        public List<GameData> games;
    }
}
