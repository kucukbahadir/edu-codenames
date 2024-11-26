using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // Required for TMP_InputField

public class LoginManager : MonoBehaviour
{
    private string loginUrl = "http://localhost/codenamesAPI/Login.php"; // PHP Login URL
    private string checkAccessUrl = "http://localhost/codenamesAPI/CheckAccess.php"; // PHP Check Access URL

    [System.Serializable]
    public class LoginData
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public string message;
        public string token; // Token for authentication
    }

    // TMP_InputField for email and password
    public TMP_InputField emailInputField; // Assign this in the Inspector
    public TMP_InputField passwordInputField; // Assign this in the Inspector

    private string sessionToken; // Token received from the server

    // Called when the login button is clicked
    public void SubmitLogin()
    {
        string email = emailInputField.text; // Get email from TMP_InputField
        string password = passwordInputField.text; // Get password from TMP_InputField

        StartCoroutine(Login(email, password));
    }

    IEnumerator Login(string email, string password)
    {
        LoginData loginData = new LoginData { email = email, password = password };
        string jsonData = JsonUtility.ToJson(loginData);

        UnityWebRequest www = new UnityWebRequest(loginUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fout bij login: " + www.error);
        }
        else
        {
            string jsonResult = www.downloadHandler.text;
            Debug.Log("Ontvangen JSON: " + jsonResult);

            ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

            if (response.status == "success")
            {
                Debug.Log("Login succesvol: " + response.message);
                sessionToken = response.token; // Store the token for future requests
            }
            else
            {
                Debug.LogError("Login fout: " + response.message);
            }
        }
    }

    // Check access using the stored token
    public void CheckAccess()
    {
        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("Geen sessie-token. Gebruiker is niet ingelogd.");
            return;
        }

        StartCoroutine(VerifyAccess());
    }

    IEnumerator VerifyAccess()
    {
        UnityWebRequest www = new UnityWebRequest(checkAccessUrl, "GET");
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fout bij verificatie: " + www.error);
        }
        else
        {
            string jsonResult = www.downloadHandler.text;
            Debug.Log("Ontvangen JSON bij verificatie: " + jsonResult);

            ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

            if (response.status == "success")
            {
                Debug.Log("Toegang toegestaan: " + response.message);
                // Proceed to teacher's dashboard or restricted area
            }
            else
            {
                Debug.LogError("Toegang geweigerd: " + response.message);
            }
        }
    }
}
