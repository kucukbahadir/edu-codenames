using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    // API URLs
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php";
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php";

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

                // Parse the JSON data into a list of games
                List<Game> games = JsonUtility.FromJson<GamesResponse>("{\"games\":" + jsonResult + "}").games;

                // Populate the Unity UI table with the data
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
        // Clear any existing rows in the table
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }

        // Add a new row for each game
        foreach (Game game in games)
        {
            GameObject row = Instantiate(tableRowPrefab, tableContent);

            TextMeshProUGUI[] columns = row.GetComponentsInChildren<TextMeshProUGUI>();
            columns[0].text = game.GameID.ToString(); // First column: GameID
            columns[1].text = game.Gamenaam;         // Second column: Gamenaam
            columns[2].text = string.Join(", ", game.Trefwoorden); // Third column: Trefwoorden
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

    // Classes for JSON parsing
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
