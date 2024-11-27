using System.Collections; // Required for IEnumerator
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using UnityEngine.SceneManagement; // Required for scene management
using TMPro; // Required for TMP_InputField

public class LoginManager : MonoBehaviour
{
    private string apiUrl = "http://localhost/codenamesAPI/Login.php"; // PHP Login URL
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

    // Coroutine that sends the login data to the server and handles the response
    IEnumerator Login(string email, string password)
    {
        LoginData loginData = new LoginData { email = email, password = password };
        string jsonData = JsonUtility.ToJson(loginData);

        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowErrorMessage("Er is een probleem met de verbinding. Probeer het later opnieuw.");
        }
        else
        {
            string jsonResult = www.downloadHandler.text;
            ResponseData response = JsonUtility.FromJson<ResponseData>(jsonResult);

            if (response.status == "success")
            {
                // Save the session token after a successful login
                sessionToken = response.token;
                PlayerPrefs.SetString("SessionToken", sessionToken); // Save session token to PlayerPrefs
                PlayerPrefs.Save();
                Debug.Log("Login succesvol.");
                SceneManager.LoadScene("DashboardTeachers"); // Load Teacher's Dashboard
            }
            else
            {
                ShowErrorMessage("Email en/of wachtwoord fout.");
                ResetInputFields();
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
    // Logout function to clear the session token and redirect to the login scene
    public void Logout()
    {
        sessionToken = null; // Clear the session token
        PlayerPrefs.DeleteKey("SessionToken"); // Remove the token from PlayerPrefs
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login"); // Redirect to login screen
    }

    
    public void Logout()
    {
        sessionToken = null; // Clear the session token
        PlayerPrefs.DeleteKey("SessionToken"); // Remove the token from PlayerPrefs
        PlayerPrefs.Save();
        SceneManager.LoadScene("Login"); // Redirect to login screen
    }


}
