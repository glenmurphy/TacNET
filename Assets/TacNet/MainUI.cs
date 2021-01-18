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

  public Speech speech;

  public Text statusDisplay;
  public Pilot pilot;

  bool enablePostProcessing = true;
  int passiveFrameRate; // after drags are done; configured in Start();
  int backgroundFrameRate = 15; // when we don't have focus

  // Understand what state we're in so we can modify clocks accordingly
  DateTime lastInteractionTime = DateTime.Now;
  TimeSpan interactionSpeedUpDuration = new TimeSpan(0, 0, 5);
  TimeSpan interactionTouchDuration = new TimeSpan(0, 0, 0, 0, 500);
  bool hasFocus = true;

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
      passiveFrameRate = 15;
      Screen.sleepTimeout = SleepTimeout.NeverSleep;
      RenderSettings.fog = false;
    } 
    else
    {
      enablePostProcessing = true;
      passiveFrameRate = -1;
      RenderSettings.fog = true;
    }

    UpdatePerformance();
  }

  void Update() {
    if (loginPanel.activeSelf && Input.GetKeyDown(KeyCode.Return)) {
      HandleConnect();
    }
    UpdatePerformance();
  }

  // Performance management ---------------------------------------------------
  public bool InSpeedUp() {
    return (lastInteractionTime > DateTime.Now - interactionSpeedUpDuration);
  }
  public bool InTouch() {
    return (lastInteractionTime > DateTime.Now - interactionTouchDuration);
  }

  void UpdatePerformance()
  {
    if (hasFocus)
    {
      if (InSpeedUp())
        Application.targetFrameRate = -1;
      else
        Application.targetFrameRate = passiveFrameRate;
      postProcessing.SetActive(enablePostProcessing);
    }
    else
    {
      Application.targetFrameRate = backgroundFrameRate;
      postProcessing.SetActive(false);
    }
  }

  void OnApplicationFocus(bool focus)
  {
    hasFocus = focus;
  }

  void OnApplicationPause(bool isPaused)
  {
    hasFocus = !isPaused;
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

      /*
      if (touch.position.y < Screen.height * 0.8f)
      {
        controlsPanel.SetActive(!controlsPanel.activeSelf);
      }
      */
    }
  }

  public void OnGUI()
  {
    if (IsLoggingIn()) return;

    Event e = Event.current;

    if (e.type == EventType.ScrollWheel)
    {
      pilot.ZoomBy(e.delta.y);
      lastInteractionTime = DateTime.Now;
    }
    else if (e.type == EventType.MouseDrag)
    {
      pilot.RotateBy(e.delta.x * 0.5f);
      pilot.ZoomBy(-e.delta.y * 0.1f);
      lastInteractionTime = DateTime.Now;
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
    pilot.PrevCraft();
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
    speech.Say(new Speech.Call[] {Speech.Call.CONNECTED});

    loginPanel.SetActive(false);
    connectingPanel.SetActive(false);
    controlsPanel.SetActive(true);
    statusDisplay.text = "Waiting for " + craftName.text;
  }
}