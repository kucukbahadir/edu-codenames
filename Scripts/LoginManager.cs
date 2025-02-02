using System.Collections; // Required for IEnumerator
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using UnityEngine.SceneManagement; // Required for scene management
using TMPro; // Required for TMP_InputField

public class LoginManager : MonoBehaviour
{
    private string apiKey = "06e49cf4e293d0f530a00386d6882e07d599eac1fac4a585881fe9d749a106a2";
    private string apiUrl = "http://localhost/codenamesAPI/Login.php"; // PHP Login URL


    public TMP_InputField emailInputField; // Assign this in the Inspector
    public TMP_InputField passwordInputField; // Assign this in the Inspector
    public TMP_Text errorText; // Text object to display error messages
    private string sessionToken;

    // SubmitLogin is called when the user presses the Login button
    public void SubmitLogin()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;

        StartCoroutine(Login(email, password)); // Start the coroutine for login
    }

    IEnumerator Login(string email, string password)
    {
        LoginData loginData = new LoginData { email = email, password = password };
        string jsonData = JsonUtility.ToJson(loginData);

        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", apiKey);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowErrorMessage("Er is een probleem met de verbinding. Probeer het later opnieuw.");
            yield break; // Use yield break to exit the coroutine
        }
        else
        {
            string jsonResult = www.downloadHandler.text;

            // Check if the response starts with a JSON object
            if (!jsonResult.StartsWith("{") && !jsonResult.StartsWith("["))
            {
                ShowErrorMessage("Unexpected response from server. Received: " + jsonResult);
                yield break;
            }

            try
            {
                ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

                if (response.status == "success")
                {
                    sessionToken = response.token;
                    PlayerPrefs.SetString("SessionToken", sessionToken);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("DashboardTeachers");
                }
                else
                {
                    ShowErrorMessage("Email en/of wachtwoord fout.");
                    ResetInputFields();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("JSON Parsing error: " + ex.Message);
                yield break;
            }

        }
    }


    // Display the error message in the ErrorMSG text
    private void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
    }

    // Reset the input fields for email and password
    private void ResetInputFields()
    {
        if (emailInputField != null)
        {
            emailInputField.text = "";
        }

        if (passwordInputField != null)
        {
            passwordInputField.text = "";
        }
    }

    
    public void Logout()
    {
        sessionToken = null; // Clear the session token
        PlayerPrefs.DeleteKey("SessionToken"); // Remove the token from PlayerPrefs
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login"); // Redirect to login screen
    }

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
}
