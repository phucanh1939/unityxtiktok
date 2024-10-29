using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Networking;
using System.Collections.Generic;

public class TiktokWrapper : MonoBehaviour
{
    // Singleton instance
    public static TiktokWrapper Instance { get; private set; }

    private WebViewObject webViewObject;
    
    // TikTok app credentials
    // https://www.tiktok.com/v2/auth/authorize?client_key=sbaw2x0ehavqt3ung5&redirect_uri=https://www.eternals.game/auth/tiktok/callback/&response_type=code&scope=user.info.basic&state=random_state_string
    private readonly string clientKey = "sbaw2x0ehavqt3ung5";  // Replace with your TikTok client_key
    private readonly string clientSecret = "3blk1K4UqJcjoiAy3AWPZho4Mx5VhwvJ";  // Replace with your TikTok client_secret
    private readonly string redirectUri = "https://www.eternals.game/auth/tiktok/callback";  // Set this in TikTok developer portal

    private Action<bool, string> loginCallback;  // Store the callback for later use
    private string authCode;
    private string accessToken;

    private void Awake()
    {
        // Ensure only one instance of GameSDK exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Public method to initiate TikTok login
    public void LoginWithTiktok(Action<bool, string> callback)
    {
        loginCallback = callback;  // Store the callback for later use

        // Build the TikTok OAuth URL
        string authUrl = $"https://www.tiktok.com/v2/auth/authorize?client_key={clientKey}&redirect_uri={redirectUri}&response_type=code&scope=user.info.basic&state=random_state_string";

        // Create and show the UnityWebView
        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log($"Message from WebView: {msg}");
            },
            err: (msg) =>
            {
                Debug.LogError($"WebView Error: {msg}");
            },
            httpErr: (msg) =>
            {
                Debug.LogError($"HTTP Error: {msg}");
            },
            started: (msg) =>
            {
                Debug.Log($"WebView started loading: {msg}");
            },
            ld: OnPageLoaded // Callback for page load
        );
        
        webViewObject.SetMargins(0, 0, 0, 0);
        webViewObject.SetVisibility(true);
        webViewObject.LoadURL(authUrl);  // Load the TikTok login page
        Debug.Log("____________ START URL: " + authUrl);
    }

    // Callback for when the page has finished loading
    private void OnPageLoaded(string url)
    {
        Debug.Log("________On Page Loaded: " + url);

        // Check if redirected URL contains the authorization code
        if (url.StartsWith(redirectUri))
        {
            var queryParams = ParseUrlParameters(url);
            if (queryParams.ContainsKey("code"))
            {
                authCode = queryParams["code"];
                webViewObject.SetVisibility(false);  // Hide WebView once code is obtained
                Destroy(webViewObject.gameObject);   // Destroy WebView after use
                Debug.Log("________ authCode: " + authCode);
                // Start exchanging the code for an access token
                StartCoroutine(ExchangeCodeForAccessToken(authCode));
            }
        }
    }

    // Parse URL parameters
    private Dictionary<string, string> ParseUrlParameters(string url)
    {
        Uri uri = new Uri(url);
        string query = uri.Query;
        string[] queryParams = query.TrimStart('?').Split('&');
        Dictionary<string, string> parameters = new Dictionary<string, string>();

        foreach (string param in queryParams)
        {
            string[] keyValue = param.Split('=');
            if (keyValue.Length == 2)
            {
                parameters.Add(keyValue[0], keyValue[1]);
            }
        }
        return parameters;
    }

    // Exchange the authorization code for access token
    private IEnumerator ExchangeCodeForAccessToken(string code)
    {
        string tokenUrl = "https://open.tiktokapis.com/v2/oauth/token/";

        WWWForm form = new WWWForm();
        form.AddField("client_key", clientKey);
        form.AddField("client_secret", clientSecret);
        form.AddField("code", code);
        form.AddField("grant_type", "authorization_code");
        form.AddField("redirect_uri", "https://www.eternals.game/auth/tiktok/callback");

        using (UnityWebRequest www = UnityWebRequest.Post(tokenUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("_________ Error exchanging code: " + www.error);
                loginCallback?.Invoke(false, null);  // Call the callback with failure
            }
            else
            {
                // Get the access token from the response
                string responseText = www.downloadHandler.text;
                Debug.Log("_________ Access Token Response: " + responseText);

                // Extract access token (assuming response contains a field named "access_token")
                var tokenData = JsonUtility.FromJson<TikTokAccessTokenResponse>(responseText);
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.access_token))
                {
                    accessToken = tokenData.access_token;
                    loginCallback?.Invoke(true, tokenData.access_token);  // Success
                }
                else
                {
                    loginCallback?.Invoke(false, null);  // Failure
                }
            }
        }
    }
}

// Class to map TikTok access token response
[System.Serializable]
public class TikTokAccessTokenResponse
{
    public string access_token;
}
