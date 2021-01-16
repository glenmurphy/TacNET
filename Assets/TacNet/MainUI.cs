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
  GameObject disconnectPanel;

  public GameObject postProcessing;

  Text targetName;

  void Awake()
  {
    loginPanel = transform.Find("LoginPanel").gameObject;
    hostname = loginPanel.transform.Find("Input-Hostname").GetComponent<InputField>();
    port = loginPanel.transform.Find("Input-Port").GetComponent<InputField>();
    password = loginPanel.transform.Find("Input-Password").GetComponent<InputField>();
    connect = loginPanel.transform.Find("Button-Connect").GetComponent<Button>();

    connectingPanel = transform.Find("ConnectingPanel").gameObject;

    disconnectPanel = transform.Find("ButtonDisconnect").gameObject;

    targetName = GameObject.Find("CraftName").GetComponent<Text>();

    hostname.text = PlayerPrefs.GetString("hostname");
    port.text = PlayerPrefs.GetString("port");
    password.text = PlayerPrefs.GetString("password");

    loginPanel.SetActive(true);
    connectingPanel.SetActive(false);
    disconnectPanel.SetActive(false);

    connect.onClick.AddListener(HandleConnect);
    disconnectPanel.GetComponent<Button>().onClick.AddListener(HandleDisconnect);
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

  // Performance management ---------------------------------------------------
  void ReduceSpeed(bool isPaused)
  {
    if (isPaused) {
      Application.targetFrameRate = 15;
      if (postProcessing) postProcessing.SetActive(false);
    } else {
      Application.targetFrameRate = -1;
      if (postProcessing) postProcessing.SetActive(true);
    }
  }

  void OnApplicationFocus(bool hasFocus)
  {
    ReduceSpeed(!hasFocus);
  }

  void OnApplicationPause(bool isPaused)
  {
    ReduceSpeed(isPaused);
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

  public void HandleDisconnect() {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  public void HandleClientError() {
    HandleDisconnect();
  }

  public void HandleClientConnected() {
    loginPanel.SetActive(false);
    connectingPanel.SetActive(false);
    disconnectPanel.SetActive(true);
    targetName.text = "Awaiting data";
  }
}