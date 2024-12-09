using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class DashboardManager : MonoBehaviour
{
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // PHP access check URL
    private string fetchGamesUrl = "http://localhost/codenamesAPI/GetGamesData.php"; // URL to fetch games data

    public Transform GamesTable; // Parent for all game rows
    public GameObject GameRowPrefab; // Prefab for a game row
    public GameObject TrefwoordRowPrefab; // Prefab for a trefwoord row

    void Start()
    {
        // Get the session token from PlayerPrefs
        string sessionToken = PlayerPrefs.GetString("SessionToken", "");

        // Debug: Log the session token in Unity
        Debug.Log("Session Token: " + sessionToken);

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
                Debug.Log("Access check result: " + jsonResult);

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
            Debug.LogError("Error fetching games data: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            Debug.Log("Games data: " + json);

            // Deserialize and populate the table
            try
            {
                GamesResponse gamesResponse = JsonUtility.FromJson<GamesResponse>(json);
                PopulateGamesTable(gamesResponse.games);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse error: " + ex.Message);
            }
        }
    }

    void PopulateGamesTable(List<GameData> games)
    {
        foreach (var game in games)
        {
            // Create a row for the game
            GameObject gameRow = Instantiate(GameRowPrefab, GamesTable);
            TextMeshProUGUI gameNameText = gameRow.GetComponentInChildren<TextMeshProUGUI>();
            gameNameText.text = game.name;

            Transform trefwoordenParent = gameRow.transform.Find("TrefwoordenContainer");
            if (trefwoordenParent == null)
            {
                Debug.LogError("TrefwoordenContainer is missing in GameRowPrefab.");
                continue;
            }

            // Hide the TrefwoordenContainer by default
            trefwoordenParent.gameObject.SetActive(false);

            // Add button functionality for folding/unfolding
            gameRow.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                bool isActive = trefwoordenParent.gameObject.activeSelf;
                trefwoordenParent.gameObject.SetActive(!isActive);
            });

            // Add Trefwoord rows
            foreach (var trefwoord in game.trefwoorden)
            {
                GameObject trefwoordRow = Instantiate(TrefwoordRowPrefab, trefwoordenParent);
                TextMeshProUGUI[] texts = trefwoordRow.GetComponentsInChildren<TextMeshProUGUI>();
                texts[0].text = trefwoord.trefwoord;
                texts[1].text = trefwoord.betekenis;
            }
        }
    }

    // Logout function to clear the session token and redirect to login
    public void Logout()
    {
        PlayerPrefs.DeleteKey("SessionToken"); // Clear the session token from PlayerPrefs
        PlayerPrefs.Save(); // Save the changes to PlayerPrefs
        SceneManager.LoadScene("Login"); // Redirect to Login scene
    }

    public void AddGameRedirect()
    {
        SceneManager.LoadScene("AddGame"); // Redirect to add game screen
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
