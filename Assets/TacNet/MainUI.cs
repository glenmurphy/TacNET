using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
  public float ClickSize = 250f;

  public GameObject loginPanel;
  InputField hostname;
  InputField port;
  InputField password;
  InputField craftName;
  Button connect;
  
  public GameObject connectingPanel;

  public GameObject controlsPanel;
  GameObject modeButton;
  GameObject nextButton;
  GameObject resetButton;
  GameObject nearButton;
  GameObject prevButton;
  GameObject disconnectButton;

  public GameObject postProcessing;

  public Details details;

  public Speech speech;

  public Text statusDisplay;
  Pilot pilot;
  World world;
  UserCamera userCamera;
  Camera cam;
  InputProcess input;

  bool enablePostProcessing = true;
  int passiveFrameRate; // after drags are done; configured in Start();
  int backgroundFrameRate = 15; // when we don't have focus

  // Understand what state we're in so we can modify clocks accordingly
  DateTime lastInteractionTime = DateTime.Now;
  TimeSpan interactionSpeedUpDuration = new TimeSpan(0, 0, 5);
  TimeSpan interactionTouchDuration = new TimeSpan(0, 0, 0, 0, 500);
  bool hasFocus = true;

  public Entity selectedEntity;

  void Awake()
  {
    hostname = loginPanel.transform.Find("Input-Hostname").GetComponent<InputField>();
    port = loginPanel.transform.Find("Input-Port").GetComponent<InputField>();
    password = loginPanel.transform.Find("Input-Password").GetComponent<InputField>();
    craftName = loginPanel.transform.Find("Input-CraftName").GetComponent<InputField>();
    connect = loginPanel.transform.Find("Button-Connect").GetComponent<Button>();

    modeButton = controlsPanel.transform.Find("ButtonMode").gameObject;
    nextButton = controlsPanel.transform.Find("ButtonNext").gameObject;
    resetButton = controlsPanel.transform.Find("ButtonReset").gameObject;
    nearButton = controlsPanel.transform.Find("ButtonNear").gameObject;
    prevButton = controlsPanel.transform.Find("ButtonPrev").gameObject;
    disconnectButton = controlsPanel.transform.Find("ButtonDisconnect").gameObject;

    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("hostname")))
      hostname.text = PlayerPrefs.GetString("hostname");
    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("port")))
      port.text = PlayerPrefs.GetString("port");
    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("password")))
      password.text = PlayerPrefs.GetString("password");
    if (!String.IsNullOrEmpty(PlayerPrefs.GetString("craftname")))
      craftName.text = PlayerPrefs.GetString("craftname");

    loginPanel.SetActive(true);
    controlsPanel.SetActive(false);
    connectingPanel.SetActive(false);
    details.gameObject.SetActive(false);

    connect.onClick.AddListener(HandleConnect);
    EventSystem.current.SetSelectedGameObject(connect.gameObject, null);

    modeButton.GetComponent<Button>().onClick.AddListener(HandleMode);
    nextButton.GetComponent<Button>().onClick.AddListener(HandleNext);
    resetButton.GetComponent<Button>().onClick.AddListener(HandleReset);
    nearButton.GetComponent<Button>().onClick.AddListener(HandleNear);
    prevButton.GetComponent<Button>().onClick.AddListener(HandlePrev);
    disconnectButton.GetComponent<Button>().onClick.AddListener(HandleDisconnect);

    world = GameObject.FindObjectsOfType<World>()[0];
    pilot = GameObject.FindObjectsOfType<Pilot>()[0];
    userCamera = transform.parent.GetComponent<UserCamera>();
    cam = transform.parent.GetComponent<Camera>();
    input = transform.parent.GetComponent<InputProcess>();
    input.OnPan += HandlePan;
    input.OnZoom += HandleZoom;
    input.OnClick += HandleClick;
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
    if (loginPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
    {
      HandleConnect();
    }
    
    // We update the position of each entity here so that we can guarantee that the camera movement
    // happens after the movement update
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      entry.Value.Reposition(world.basePos);
    }
    userCamera.UpdateCamera();

    UpdatePerformance();
    UpdateSelected();
  }

  void UpdateSelected() {
    if (!selectedEntity) return;

    details.transform.position = cam.WorldToScreenPoint(selectedEntity.posCache);

    details.SetLonLat(selectedEntity.pos.lon, selectedEntity.pos.lat);
    Entity craft = pilot.GetCraft();
    if (!craft)
      details.SetAlt(selectedEntity.pos.GetAltFt());
    else
    {
      float bearing = craft.pos.GetBearingTo(selectedEntity.pos);
      float range = craft.pos.GetDistanceToNM(selectedEntity.pos);
      float alt = (int)selectedEntity.pos.GetAltFt();
      details.SetBRA(bearing, range, alt);
    }
  }

  // Performance management ---------------------------------------------------
  public bool InSpeedUp() {
    return (lastInteractionTime > DateTime.Now - interactionSpeedUpDuration);
  }
  public bool InTouch() {
    return (lastInteractionTime > DateTime.Now - interactionTouchDuration);
  }
  // 1 - touch just happened
  // 0 - at limit of touch duration
  public float InTouchLerp() {
    float l = 1f - 
      (float)((DateTime.Now - lastInteractionTime).TotalMilliseconds / interactionTouchDuration.TotalMilliseconds);
    return l < 0 ? 0 : l;
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
  public void HandlePan(object sender, InputEventArgs e)
  {
    lastInteractionTime = DateTime.Now;
    userCamera.HandleDrag(e.pos.x, e.pos.y, e.delta.x, e.delta.y);
  }

  public void HandleZoom(object sender, InputEventArgs e)
  {
    lastInteractionTime = DateTime.Now;
    userCamera.HandleScroll(-e.delta.y);
  }

  // Takes coordinates in screen coordinates (0,0 at bottom left), and not event coordinates
  void HandleClick(object sender, InputEventArgs e)
  {
    float x = e.pos.x;
    float y = e.pos.y;
    Debug.Log(x + ", " + y);

    Vector3 screenPos;

    float minDistance = Single.MaxValue;
    float distance;
    Entity closest = null;

    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      if (!entry.Value.HasModel())
        continue;
      if (entry.Value == selectedEntity)
        continue;

      screenPos = cam.WorldToScreenPoint(entry.Value.posCache);
      distance = Mathf.Pow(screenPos.x - x, 2) + Mathf.Pow(screenPos.y - y, 2);

      if (distance < minDistance) {
        minDistance = distance;
        closest = entry.Value;
      }
    }
    
    // should normalize ClickSize by Camera.pixelWidth;
    if (closest != null && minDistance < Mathf.Pow(ClickSize, 2))
      HandleSelect(closest);
    else
      HandleSelect(null);
  }

  void HandleSelect(Entity e)
  {
    if (selectedEntity) selectedEntity.Deselect();

    if (e) {
      e.Select();
      details.SetName(e.entityName);
      details.transform.position = cam.WorldToScreenPoint(e.posCache);
      details.gameObject.SetActive(true);
    } else {
      details.gameObject.SetActive(false);
    }
    
    selectedEntity = e;
  }

  void HandleConnect() {
    PlayerPrefs.SetString("hostname", hostname.text);
    PlayerPrefs.SetString("port", port.text);
    PlayerPrefs.SetString("password", password.text);

    world.Login(hostname.text, port.text, password.text);
    
    pilot.SetPreferredCraft(craftName.text);
    PlayerPrefs.SetString("craftname", craftName.text);

    loginPanel.SetActive(false);
    connectingPanel.SetActive(true);
  }

  void HandleDisconnect() {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  void HandleMode()
  {
    userCamera.NextMode();
  }

  void HandleNext()
  {
    pilot.NextCraft();
    userCamera.Reset();
  }

  void HandlePrev()
  {
    pilot.PrevCraft();
    userCamera.Reset();
  }

  void HandleReset()
  {
    pilot.ResetCraft();
    userCamera.Reset();
  }

  void HandleNear()
  {
    pilot.FindMinCraft();
    userCamera.Reset();
  }

  public void HandleClientError() {
    StartCoroutine(ErrorCoroutine());
  }

  IEnumerator ErrorCoroutine()
  {
    statusDisplay.text = "";
    connectingPanel.SetActive(true);
    controlsPanel.SetActive(false);
    connectingPanel.GetComponentInChildren<Text>().text = "Connection Failed";
    yield return new WaitForSeconds(2);
    HandleDisconnect();
  }

  public void HandleClientConnected() {
    // speech.Say(new Speech.Call[] {Speech.Call.CONNECTED});
    loginPanel.SetActive(false);
    connectingPanel.SetActive(false);
    controlsPanel.SetActive(true);
    statusDisplay.text = "Waiting for " + craftName.text;
  }
}