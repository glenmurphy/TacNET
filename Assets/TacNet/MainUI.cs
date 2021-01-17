using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
  public GameObject loginPanel;
  InputField hostname;
  InputField port;
  InputField password;
  InputField craftName;
  Button connect;
  
  public GameObject connectingPanel;

  public GameObject controlsPanel;
  GameObject nextButton;
  GameObject resetButton;
  GameObject nearButton;
  GameObject prevButton;
  GameObject disconnectButton;

  public GameObject postProcessing;

  public Text statusDisplay;
  public Pilot pilot;

  bool enablePostProcessing = true;
  int idealFrameRate = -1;

  void Awake()
  {
    hostname = loginPanel.transform.Find("Input-Hostname").GetComponent<InputField>();
    port = loginPanel.transform.Find("Input-Port").GetComponent<InputField>();
    password = loginPanel.transform.Find("Input-Password").GetComponent<InputField>();
    craftName = loginPanel.transform.Find("Input-CraftName").GetComponent<InputField>();
    connect = loginPanel.transform.Find("Button-Connect").GetComponent<Button>();

    nextButton = controlsPanel.transform.Find("ButtonNext").gameObject;
    resetButton = controlsPanel.transform.Find("ButtonReset").gameObject;
    nearButton = controlsPanel.transform.Find("ButtonNear").gameObject;
    prevButton = controlsPanel.transform.Find("ButtonPrev").gameObject;
    disconnectButton = controlsPanel.transform.Find("ButtonDisconnect").gameObject;

    //if (!String.IsNullOrEmpty(PlayerPrefs.GetString("hostname")))
    //  hostname.text = PlayerPrefs.GetString("hostname");
    hostname.text = "home.glenmurphy.com";
    port.text = PlayerPrefs.GetString("port");
    password.text = PlayerPrefs.GetString("password");

    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("craftname")))
      craftName.text = PlayerPrefs.GetString("craftname");

    loginPanel.SetActive(true);
    controlsPanel.SetActive(false);
    connectingPanel.SetActive(false);

    connect.onClick.AddListener(HandleConnect);
    nextButton.GetComponent<Button>().onClick.AddListener(HandleNext);
    resetButton.GetComponent<Button>().onClick.AddListener(HandleReset);
    nearButton.GetComponent<Button>().onClick.AddListener(HandleNear);
    prevButton.GetComponent<Button>().onClick.AddListener(HandlePrev);
    disconnectButton.GetComponent<Button>().onClick.AddListener(HandleDisconnect);
  }

  void Start()
  {
    if (Application.platform == RuntimePlatform.Android)
    {
      enablePostProcessing = false;
      idealFrameRate = 30;
      Screen.sleepTimeout = SleepTimeout.NeverSleep;
    } 
    else
    {
      enablePostProcessing = true;
      idealFrameRate = -1;
    }

    ReduceSpeed(false);
  }

  void Update() {
    if (loginPanel.activeSelf && Input.GetKeyDown(KeyCode.Return)) {
      HandleConnect();
    }
  }

  // Performance management ---------------------------------------------------
  void ReduceSpeed(bool isPaused)
  {
    if (isPaused)
    {
      Application.targetFrameRate = 15;
      postProcessing.SetActive(false);
    }
    else
    {
      Application.targetFrameRate = idealFrameRate;
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

  public bool IsLoggingIn() {
    return (loginPanel.activeSelf || connectingPanel.activeSelf);
  }

  // Input management ---------------------------------------------------------
  void ProcessTouchInput()
  {
    if (IsLoggingIn()) return;
    if (Input.touchCount == 0) return;

    for (int i = 0; i < Input.touchCount; i++)
    {
      Touch touch = Input.GetTouch(i);
      if (touch.phase != TouchPhase.Began)
        continue;

      if (touch.position.y < Screen.height * 0.8f)
      {
        controlsPanel.SetActive(!controlsPanel.activeSelf);
      }
    }
  }

  public void OnGUI()
  {
    if (IsLoggingIn()) return;

    Event e = Event.current;

    if (e.type == EventType.ScrollWheel)
    {
      pilot.ZoomBy(e.delta.y);
    }
    else if (e.type == EventType.MouseDrag)
    {
      pilot.RotateBy(e.delta.x);
      pilot.ZoomBy(-e.delta.y * 0.2f);
    }
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

  void HandleDisconnect() {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  void HandleNext()
  {
    pilot.NextCraft();
  }

  void HandlePrev()
  {
    pilot.NextCraft();
  }

  void HandleReset()
  {
    pilot.ResetCraft();
  }

  void HandleNear()
  {
    pilot.FindMinCraft();
  }

  public void HandleClientError() {
    HandleDisconnect();
  }

  public void HandleClientConnected() {
    loginPanel.SetActive(false);
    connectingPanel.SetActive(false);
    controlsPanel.SetActive(true);
    statusDisplay.text = "Waiting for " + craftName.text;
  }
}