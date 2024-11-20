using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // Required for TMP_InputField

public class LoginManager : MonoBehaviour
{
    private string apiUrl = "http://localhost/codenamesAPI/Login.php"; // PHP Login URL

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
    }

    // TMP_InputField for email and password
    public TMP_InputField emailInputField; // Assign this in the Inspector
    public TMP_InputField passwordInputField; // Assign this in the Inspector

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

        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
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
            }
            else
            {
                Debug.LogError("Login fout: " + response.message);
            }
        }
    }
}
