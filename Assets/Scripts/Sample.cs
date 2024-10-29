using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField] private Button buttonTiktok;
    [SerializeField] private TMP_Text textAccessToken;

    private void Start()
    {
        buttonTiktok.onClick.AddListener(OnButtonTiktokPressed);
    }

    private void OnButtonTiktokPressed()
    {
        TiktokWrapper.Instance.LoginWithTiktok(OnTiktokLoginCallback);
    }

    private void OnTiktokLoginCallback(bool isSuccess, string accessToken)
    {
        Debug.Log("[Sample] <OnTiktokLoginCallback> ___________ isSuccess = " + isSuccess);
        Debug.Log("[Sample] <OnTiktokLoginCallback> ___________ accessToken = " + accessToken);
        textAccessToken.text = "Access token: " + accessToken;
    }
}
