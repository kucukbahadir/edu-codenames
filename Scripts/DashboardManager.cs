using UnityEngine;
using System.Collections; // Required for IEnumerator
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class DashboardManager : MonoBehaviour
{
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // PHP access check URL

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
            // Check if the response is valid JSON
            try
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Access check result: " + jsonResult); // Log the response from PHP

                // Ensure that the response is valid JSON
                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status != "success")
                {
                    Debug.LogError("Access denied: " + response.message);
                    SceneManager.LoadScene("Login"); // Redirect to login if access is denied
                }
                else
                {
                    Debug.Log("Access granted: " + response.message);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON parse error: " + ex.Message);
                // Handle the error (e.g., redirect to login if JSON parsing fails)
                SceneManager.LoadScene("Login");
            }
        }
    }


    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public string message;
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
}
