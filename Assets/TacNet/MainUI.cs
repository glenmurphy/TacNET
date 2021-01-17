using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
  public GameObject loginPanel;
  public InputField hostname;
  public InputField port;
  public InputField password;
  public Button connect;
  
  public GameObject connectingPanel;
  public GameObject disconnectButton;

  public GameObject postProcessing;

  public InputField craftName;
  public Text craftDisplay;
  public Pilot pilot;

  bool enablePostProcessing = true;

  void Awake()
  {
    //loginPanel = transform.Find("LoginPanel").gameObject;
    //hostname = loginPanel.transform.Find("Input-Hostname").GetComponent<InputField>();
    //port = loginPanel.transform.Find("Input-Port").GetComponent<InputField>();
    //password = loginPanel.transform.Find("Input-Password").GetComponent<InputField>();
    //connect = loginPanel.transform.Find("Button-Connect").GetComponent<Button>();

    //connectingPanel = transform.Find("ConnectingPanel").gameObject;
    //disconnectButton = transform.Find("ButtonDisconnect").gameObject;

    //targetName = GameObject.Find("CraftName").GetComponent<Text>();

    //if (!String.IsNullOrEmpty(PlayerPrefs.GetString("hostname")))
    //  hostname.text = PlayerPrefs.GetString("hostname");
    hostname.text = "home.glenmurphy.com";

    port.text = PlayerPrefs.GetString("port");
    password.text = PlayerPrefs.GetString("password");

    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("craftname")))
      craftName.text = PlayerPrefs.GetString("craftname");

    loginPanel.SetActive(true);
    connectingPanel.SetActive(false);
    disconnectButton.SetActive(false);

    connect.onClick.AddListener(HandleConnect);
    disconnectButton.GetComponent<Button>().onClick.AddListener(HandleDisconnect);
  }

  void Start()
  {
    enablePostProcessing = (Application.platform != RuntimePlatform.Android);
    ReduceSpeed(false);
    Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
      postProcessing.SetActive(false);
    } else {
      Application.targetFrameRate = -1;
      postProcessing.SetActive(enablePostProcessing);
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
    
    pilot.SetPreferredCraft(craftName.text);
    PlayerPrefs.SetString("craftname", craftName.text);

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
    disconnectButton.SetActive(true);
    craftDisplay.text = "Awaiting data";
  }
}