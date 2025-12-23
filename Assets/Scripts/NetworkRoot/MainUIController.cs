using UnityEngine;
using UnityEngine.UI;

public class MainUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelSender;
    [SerializeField] private GameObject panelReceiver;

    [Header("Buttons")]
    [SerializeField] private Button senderButton;
    [SerializeField] private Button receiverButton;

    void Awake()
    {
        panelMain.SetActive(true);
        panelSender.SetActive(false);
        panelReceiver.SetActive(false);

        senderButton.onClick.AddListener(ShowSender);
        receiverButton.onClick.AddListener(ShowReceiver);
    }

    private void ShowSender()
    {
        panelMain.SetActive(false);
        panelSender.SetActive(true);
        panelReceiver.SetActive(false);
    }

    private void ShowReceiver()
    {
        panelMain.SetActive(false);
        panelSender.SetActive(false);
        panelReceiver.SetActive(true);
    }
}
