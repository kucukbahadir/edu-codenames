using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    // API URLs
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php";
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php";
    private string deleteGameUrl = "http://localhost/codenamesAPI/DeleteGame.php";

    // UI elements for the table
    public Transform tableContent; // Assign the table content container in the Inspector
    public GameObject tableRowPrefab; // Assign a prefab for table rows in the Inspector

    void Start()
    {
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        Debug.Log("Session Token: " + sessionToken);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("No session token found. Redirecting to login.");
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
            Debug.LogError("Access check failed: " + www.error);
            SceneManager.LoadScene("Login");
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Access Check Result: " + jsonResult);

                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Access denied: " + response.message);
                    SceneManager.LoadScene("Login");
                }
                else
                {
                    Debug.Log("Access granted: " + response.message);
                    StartCoroutine(FetchGamesData());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse error: " + ex.Message);
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
            Debug.LogError("Failed to fetch games: " + www.error);
        }
        else
        {
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Games Data: " + jsonResult);

                List<Game> games = JsonUtility.FromJson<GamesResponse>("{\"games\":" + jsonResult + "}").games;
                PopulateTable(games);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse error: " + ex.Message);
            }
        }
    }

    void PopulateTable(List<Game> games)
    {
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Game game in games)
        {
            GameObject row = Instantiate(tableRowPrefab, tableContent);

            TextMeshProUGUI[] columns = row.GetComponentsInChildren<TextMeshProUGUI>();
            columns[0].text = game.GameID.ToString();  // Eerste kolom: GameID
            columns[1].text = game.Gamenaam;          // Tweede kolom: Gamenaam
            columns[2].text = FormatKeywords(game.Trefwoorden); // Derde kolom: Trefwoorden

            // Verwijderen knop
            Button deleteButton = row.GetComponentInChildren<Button>();
            deleteButton.onClick.AddListener(() => DeleteGame(game.GameID));

            // Aanpassen knop toevoegen
            Button editButton = row.transform.Find("Aanpassen").GetComponent<Button>();
            editButton.onClick.AddListener(() => EditGame(game.GameID, game.Gamenaam, game.Trefwoorden));
        }
    }


    string FormatKeywords(List<string> keywords)
    {
        return string.Join("\n", keywords);
    }

    public void DeleteGame(int gameID)
    {
        Debug.Log("Sending Game ID: " + gameID);
        StartCoroutine(DeleteGameCoroutine(gameID));
    }

    IEnumerator DeleteGameCoroutine(int gameId)
    {
        if (gameId <= 0)
        {
            Debug.LogError("Invalid game ID: " + gameId);
            yield break;
        }

        Debug.Log("Preparing to send Game ID: " + gameId);

        string json = "{\"id\":" + gameId + "}";
        Debug.Log("JSON Being Sent: " + json);

        UnityWebRequest www = new UnityWebRequest(deleteGameUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Server connection error: " + www.error);
        }
        else
        {
            string serverResponse = www.downloadHandler.text;
            Debug.Log("Serverantwoord: " + serverResponse);

            try
            {
                var response = JsonUtility.FromJson<ResponseData>(serverResponse);
                Debug.Log("Server Response Status: " + response.status);
                Debug.Log("Server Response Message: " + response.message);

                if (response.status == "success")
                {
                    Debug.Log("Game succesvol verwijderd.");
                    // Tabel opnieuw inladen na succesvolle verwijdering
                    StartCoroutine(FetchGamesData());
                }
                else
                {
                    Debug.LogError("Game kon niet worden verwijderd: " + response.message);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error parsing server response: " + ex.Message);
            }
        }
    }
    public void EditGame(int gameID, string gamenaam, List<string> trefwoorden)
    {
        // Bewaar de gegevens in PlayerPrefs om door te geven aan de nieuwe scene
        PlayerPrefs.SetInt("EditGameID", gameID);
        PlayerPrefs.SetString("EditGameName", gamenaam);
        PlayerPrefs.SetString("EditGameKeywords", string.Join(",", trefwoorden));
        PlayerPrefs.Save();

        // Navigeer naar de EditGame-scene
        SceneManager.LoadScene("EditGame");
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
    public class Game
    {
        public int GameID;
        public string Gamenaam;
        public List<string> Trefwoorden;
    }

    [System.Serializable]
    public class GamesResponse
    {
        public List<Game> games;
    }
}
