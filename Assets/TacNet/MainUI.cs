using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
  GameObject loginPanel;
  InputField hostname;
  InputField port;
  InputField password;
  Button connect;
  
  GameObject connectingPanel;
  Text targetName;

  void Start()
  {
    loginPanel = transform.Find("LoginPanel").gameObject;
    hostname = loginPanel.transform.Find("Input-Hostname").GetComponent<InputField>();
    port = loginPanel.transform.Find("Input-Port").GetComponent<InputField>();
    password = loginPanel.transform.Find("Input-Password").GetComponent<InputField>();
    connect = loginPanel.transform.Find("Button-Connect").GetComponent<Button>();

    connectingPanel = transform.Find("ConnectingPanel").gameObject;

    targetName = GameObject.Find("TargetName").GetComponent<Text>();

    hostname.text = PlayerPrefs.GetString("hostname");
    port.text = PlayerPrefs.GetString("port");
    password.text = PlayerPrefs.GetString("password");

    loginPanel.SetActive(true);
    connectingPanel.SetActive(false);
    connect.onClick.AddListener(HandleConnect);
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.Tab))
    {
        GameObject c = EventSystem.current.currentSelectedGameObject;
        if (c == null) { return; }
    
        Selectable s = c.GetComponent<Selectable>();
        if (s == null) { return; }

        Selectable jump = Input.GetKey(KeyCode.LeftShift)
            ? s.FindSelectableOnUp() : s.FindSelectableOnDown();
        if (jump != null) { jump.Select(); }
    }
    if (Input.GetKeyDown(KeyCode.Return)) {
      HandleConnect();
    }
  }

  public bool IsUIVisible() {
    return (loginPanel.activeSelf || connectingPanel.activeSelf);
  }

  void HandleConnect() {
    PlayerPrefs.SetString("hostname", hostname.text);
    PlayerPrefs.SetString("port", port.text);
    PlayerPrefs.SetString("password", password.text);

    GameObject.FindObjectsOfType<World>()[0].Login(hostname.text, port.text, password.text);
    loginPanel.SetActive(false);
    connectingPanel.SetActive(true);
  }

  public void HandleClientError() {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  public void HandleClientConnected() {
    loginPanel.SetActive(false);
    connectingPanel.SetActive(false);
    targetName.text = "CONNECTED";
  }
}