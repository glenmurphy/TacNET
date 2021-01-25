using System;
using UnityEngine;

public class InputEventArgs : EventArgs
{
  public Vector2 pos { get; set; }
  public Vector2 delta { get; set; }
}

class InputProcess : MonoBehaviour
{
  public event EventHandler<InputEventArgs> OnPan;
  public event EventHandler<InputEventArgs> OnZoom;
  public event EventHandler<InputEventArgs> OnClick;
  
  InputEventArgs args = new InputEventArgs(); // mem
  TimeSpan clickTime = new TimeSpan(0, 0, 0, 0, 500);

  public void Start()
  {
    Input.simulateMouseWithTouches = false;
  }

  public void Update()
  {
    ProcessTouch();
    ProcessMouse();
  }

  bool ShouldTrack(bool startPhase, TouchPhase phase)
  {
    if (startPhase && phase == TouchPhase.Began) return false;
    if (!startPhase && (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)) 
      return false;
    return true;
  }

  // Average x, y, and distance (z) of all the fingers
  public Vector3 GetNewFingerPos() { return GetAverageFingerPos(false); }
  public Vector3 GetExistingFingerPos() { return GetAverageFingerPos(true); }
  public Vector3 GetAverageFingerPos(bool startPhase)
  {
    Vector3 avg = new Vector3(0, 0, 0);
    if (Input.touchCount == 0) return avg;
    
    Touch touch;
    float total = 0;
    for (int i = 0; i < Input.touchCount; i++)
    {
      touch = Input.GetTouch(i);
      if (!ShouldTrack(startPhase, touch.phase)) continue;
      avg.x += touch.position.x;
      avg.y += touch.position.y;
      total++;
    }
    
    avg.x /= total;
    avg.y /= total;

    // Calculate distance from the center
    for (int i = 0; i < Input.touchCount; i++)
    {
      touch = Input.GetTouch(i);
      if (!ShouldTrack(startPhase, touch.phase)) continue;

      avg.z += Mathf.Sqrt(
        Mathf.Pow(avg.x - touch.position.x, 2) + 
        Mathf.Pow(avg.y - touch.position.y, 2));
    }
    avg.z /= total;

    return avg;
  }

  DateTime fingerDownTime = DateTime.Now;
  Vector2 fingerDownPos = new Vector2(0, 0);
  Vector3 lastFingerPos = new Vector3(0, 0, 0);
  public void ProcessTouch() 
  {
    if (Input.touchCount == 0) return;

    // Detect clicks
    if (Input.touchCount == 1) {
      Touch touch = Input.GetTouch(0);
      if (touch.phase == TouchPhase.Began) {
        fingerDownTime = DateTime.Now;
        fingerDownPos = touch.position;
      }
      if (touch.phase == TouchPhase.Ended &&
          fingerDownTime > DateTime.Now - clickTime && 
          (touch.position - fingerDownPos).magnitude < 50) {
        Click(touch.position.x, touch.position.y);
      }
    }

    bool moved = false;
    for (int i = 0; i < Input.touchCount; i++)
    {
      if (Input.GetTouch(i).phase == TouchPhase.Moved) moved = true;
    }

    // Get the average finger pos ignoring new fingers so we can calculate the delta from last
    // frame correctly
    Vector3 current = GetExistingFingerPos();
    if (moved)
    {
      Pan(current.x, current.y, 
          current.x - lastFingerPos.x,
          current.y - lastFingerPos.y);
      Zoom((current.z - lastFingerPos.z) * 0.03f);
    }
    // Get the average finger position ignoring removed fingers so the next frame calculates
    // the delta correctly
    lastFingerPos = GetNewFingerPos();
  }

  bool mouseDown = false;
  DateTime mouseDownTime = DateTime.Now;
  Vector2 lastMousePos = new Vector2(0, 0);
  public void ProcessMouse()
  {
    float x = Input.mousePosition.x;
    float y = Input.mousePosition.y;

    if (mouseDown == true)
    {
      Pan(x, y, x - lastMousePos.x, y - lastMousePos.y);
    }
    if (Input.GetMouseButtonDown(0))
    {
      mouseDown = true;
      mouseDownTime = DateTime.Now;
      lastMousePos.Set(x, y);
    }
    if (Input.GetMouseButtonUp(0))
    {
      mouseDown = false;
      if (mouseDownTime > DateTime.Now - clickTime)
        Click(x, y);
    }
    if (Input.mouseScrollDelta.y != 0)
    {
      Zoom(Input.mouseScrollDelta.y);
    }
    
    lastMousePos.Set(x, y);
  }

  void Pan(float x, float y, float deltaX, float deltaY)
  {
    args.pos = new Vector2(x, y);
    args.delta = new Vector2(deltaX, deltaY);
    OnPan?.Invoke(this, args);
  }

  void Zoom(float amt)
  {
    args.delta = new Vector2(amt, amt);
    OnZoom?.Invoke(this, args);
  }

  void Click(float x, float y)
  {
    args.pos = new Vector2(x, y);
    OnClick?.Invoke(this, args);
  }
}