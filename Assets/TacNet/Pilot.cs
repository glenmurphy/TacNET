using System;
using System.Collections.Generic;
using UnityEngine;

public class Pilot : MonoBehaviour
{
  // Config
  private static float posLerp = 1f;
  private static float rotLerp = 2f;
  private static float groundDrawY = 0.001f;
  
  [Range (1, 10)]
  public float viewZoom = 5f;

  [Range (0, 2)]
  public float viewHeight = 1f;

  public float viewAngle = 0;

  // Color of the height bars
  private static Color colorHeight = new Color(1, 1, 1, 0.75f);

  // Color of the concentric radar range rings, inner to outer
  private static Color colorRadar1 = new Color(0.5f, 0.5f, 0.5f, 0.75f);
  private static Color colorRadar1fill = new Color(0.5f, 0.5f, 0.5f, 0.02f);

  private static Color colorRadar2 = new Color(0.3f, 0.3f, 0.3f, 0.75f);
  private static Color colorRadar3 = new Color(0.1f, 0.1f, 0.1f, 0.75f);

  // Internal
  public GameObject worldObject;
  World world;
  Entity craft;
  int craftIndex = 0;
  MainUI mainUI;
  Speech speech;
  string preferredCraft;

  Danger danger;

  public void Start()
  {
    world = worldObject.GetComponent<World>();
    mainUI = GetComponentInChildren<MainUI>();
    speech = GetComponentInChildren<Speech>();
    danger = new Danger(speech);
  }

  public void Update()
  {
     ProcessTouchInput();

    // We update the position of each entity here so that we can guarantee that the camera movement
    // happens after the movement update
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;
      e.Reposition(world.basePos);
    }

    if (!HaveCraft())
      return;
    
    // Camera Position
    Transform craftTransform = craft.gameObject.transform;
    float angle = (-craft.yaw + 90 + viewAngle) * Mathf.Deg2Rad;
    Vector3 newPos = new Vector3(craftTransform.position.x - viewZoom * Mathf.Cos(angle),
                                 craftTransform.position.y + viewZoom * viewHeight,
                                 craftTransform.position.z - viewZoom * Mathf.Sin(angle));
    transform.position = Vector3.Lerp(transform.position, newPos, posLerp * Time.deltaTime);

    // Camera Rotation
    Vector3 cameraTarget = craftTransform.position;
    cameraTarget.y += (viewZoom * viewHeight) * 0.1f;
    Quaternion newRot = Quaternion.LookRotation(cameraTarget - transform.position);
    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, rotLerp * Time.deltaTime);

    Detect();
  }

  void ProcessTouchInput()
  {
    if (Input.touchCount > 0){
      for (int i = 0; i < Input.touchCount; i++) {
        Touch touch = Input.GetTouch(i);
        if (touch.phase != TouchPhase.Began)
          continue;
        
        if (touch.position.x < Screen.width / 2)
          PrevCraft();
        else
          NextCraft();
      }
    }
  }

  // Input management ---------------------------------------------------------
  public void OnGUI()
  {
    if (mainUI.IsUIVisible()) {
      return;
    }

    Event e = Event.current;
    if (e.type == EventType.KeyDown)
    {
      if (e.keyCode == KeyCode.A) {
        FindMinCraft();
      } else if (e.keyCode == KeyCode.LeftArrow) {
        PrevCraft();
      } else if (e.keyCode == KeyCode.RightArrow) {
        NextCraft();
      }
    }
    else if (e.type == EventType.ScrollWheel)
    {
      ZoomBy(e.delta.y);
    }
    else if (e.type == EventType.MouseDrag)
    {
      RotateBy(e.delta.x);
    }
  }

  // Render management --------------------------------------------------------
  public void OnPostRender() {
    Shapes.Draw.LineGeometry = Shapes.LineGeometry.Volumetric3D;
    Shapes.Draw.LineThicknessSpace = Shapes.ThicknessSpace.Pixels;

    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;
      if (e.HasType("Human"))
        continue;

      // Draw the vertical height stem and trails
      if (e.HasType("Air") || e.HasType("Weapon")) {
        DrawEntityDetails(e);
      }

      // Draw radar rings around our focused object
      if (e == craft) {
        Vector3 ground = new Vector3(e.transform.position.x, groundDrawY, e.transform.position.z);

        Shapes.Draw.LineThickness = 0.08f;
        Shapes.Draw.Disc(ground, Vector3.up, Pos.ConvertNmToUnity(10), colorRadar1fill);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(10), 0.01f, colorRadar1);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(20), 0.03f, colorRadar2);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(40), 0.06f, colorRadar3);
      }
    }
  }

  public void DrawEntityDetails(Entity e) {
    Vector3 ground = new Vector3(e.transform.position.x, groundDrawY, e.transform.position.z + 0.01f);

    Shapes.Draw.LineThickness = 0.125f;
    Shapes.Draw.Line(e.transform.position, ground, colorHeight);
    Shapes.Draw.Ring(ground, Vector3.up, 0.01f, 0.005f, colorHeight);

    using (Shapes.PolylinePath trail = new Shapes.PolylinePath()) {
      List<PosLog> log = e.GetLog();
      // Draws from oldest first; skip the most recent one in case the trail is ahead of
      // the aircraft (from lerping aircraft position)
      for (int i = 0; i < log.Count - 2; i++) {
        if (log[i].posCache == Vector3.zero)
          log[i].posCache = log[i].pos.GetUnityPosition(world.basePos);

        trail.AddPoint(log[i].posCache);
      }
      if (trail.Count > 0) {
        trail.AddPoint(e.transform.position);
        Shapes.Draw.Polyline(trail, closed:false, thickness:0.3f, e.GetColor());
      }
    }
  }

  // View management ----------------------------------------------------------
  private void ZoomBy(float amount)
  {
    viewZoom += amount * 0.5f;
    if (viewZoom < 1) viewZoom = 1;
    if (viewZoom > 10) viewZoom = 10;
  }

  private void RotateBy(float amount)
  {
    viewAngle -= amount * 0.25f;
    viewAngle = viewAngle % 360;
  }

  // Craft management --------------------------------------------------------
  private void NextCraft()
  {
    craftIndex++;
    FindCraft();
  }

  private void PrevCraft()
  {
    craftIndex -= craftIndex > 0 ? 1 : 0;
    FindCraft();
  }

  public void SetCraft(Entity entity)
  {
    if (craft != entity) {
      viewAngle = 0;
    }
    
    craft = entity;
    GameObject.Find("CraftName").GetComponent<UnityEngine.UI.Text>().text = entity.pilot;
    preferredCraft = entity.pilot;
    danger.Reset();
  }

  public bool FindCraftByName(string name)
  {
    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value.pilot.Equals(name)) {
        SetCraft(entry.Value);
        return true;
      }
    }
    return false;
  }

  public bool FindCraft()
  {
    List<Entity> crafts = new List<Entity>();

    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value.HasType("FixedWing")) {
        crafts.Add(entry.Value);
      }
    }
    if (crafts.Count > 0) {
      SetCraft(crafts[craftIndex % crafts.Count]);
      return true;
    }
    return false;
  }

  public bool FindMinCraft()
  {
    Entity minCraft = craft;
    float minDistance = Single.MaxValue;
    float distance; 
    foreach (KeyValuePair<string, Entity> e in world.entities)
    {
      // base object must be a plane
      if (!e.Value.HasType("FixedWing"))
        continue;
      foreach (KeyValuePair<string, Entity> other in world.entities)
      {
        // ignore anything that isn't an enemy plane or missile
        if (e.Value == other.Value || 
            !(other.Value.HasType("FixedWing") || other.Value.HasType("Missile")) ||
            e.Value.coalition == other.Value.coalition)
          continue;
        distance = e.Value.pos.GetDistanceTo(other.Value.pos);

        if (distance < minDistance && e.Value != craft) { // to toggle closest
          minDistance = distance;
          minCraft = e.Value;
        }
      }
    }

    if (minCraft) {
      SetCraft(minCraft);
      return true;
    }
    return false;
  }

  public bool HaveCraft()
  {
    if (craft || !world)
      return true;
    
    if (!String.IsNullOrEmpty(preferredCraft)) {
      return FindCraftByName(preferredCraft);
    }

    if (FindMinCraft())
      return true;
  
    return false;
  }

  // Warning management -------------------------------------------------------
  void Detect()
  {
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity enemy = entry.Value;

      if (enemy.coalition == craft.coalition) continue;
      if (!enemy.HasType("Missile")) continue;

      float distance = craft.pos.GetDistanceToNM(enemy.pos);
      if (distance > 50) continue;

      float bearing = craft.pos.GetBearingTo(enemy.pos); // compass
      float heading = (bearing - craft.heading + 360) % 360;           // nose
      float aspect = Math.Abs(((bearing - 180) - enemy.heading + 720) % 360);

      Debug.Log("Danger: " + bearing + " " + heading + " " + distance + " " + aspect);
      danger.Update(enemy, heading, distance, aspect);
    }
  }
}