using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class TestData
{
    public int ID;
    public string Email;
    public string Password;
}

[System.Serializable]
public class TestDataWrapper
{
    public TestData[] Items; // Matches the "Items" key in JSON
}

public class DatabaseManager : MonoBehaviour
{
    private string apiUrl = "http://localhost/codenamesAPI/getData.php"; // URL van de PHP API

    void Start()
    {
        StartCoroutine(GetDataFromDatabase());
    }

    IEnumerator GetDataFromDatabase()
    {
        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fout bij ophalen van data: " + www.error);
        }
        else
        {
            string jsonResult = www.downloadHandler.text;
            Debug.Log("Ontvangen JSON: " + jsonResult);

            // JSON verwerken
            TestDataWrapper dataWrapper = JsonUtility.FromJson<TestDataWrapper>(jsonResult);
            TestData[] data = dataWrapper.Items;

            // Itereer door de ontvangen data en toon deze in de console
            foreach (var item in data)
            {
                Debug.Log("ID: " + item.ID + ", Email: " + item.Email + ", Password: " + item.Password);
            }
        }
    }
}
