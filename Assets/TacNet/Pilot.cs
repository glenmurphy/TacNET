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
  MainUI mainUI;
  Speech speech;

  Danger danger;
  DateTime lastDetect;
  TimeSpan detectSpan = new TimeSpan(0, 0, 0, 0, 250);
  string preferredCraft;

  public void Start()
  {
    world = worldObject.GetComponent<World>();
    mainUI = GetComponentInChildren<MainUI>();
    speech = GetComponentInChildren<Speech>();
    danger = new Danger(speech);
  }

  Vector3 newPos = new Vector3(0, 0, 0);
  public void Update()
  {
    // We update the position of each entity here so that we can guarantee that the camera movement
    // happens after the movement update

    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      entry.Value.Reposition(world.basePos);
    }

    if (!HaveCraft())
      return;
    
    // Camera Position
    Transform craftTransform = craft.gameObject.transform;
    float angle = (-craft.yaw + 90 + viewAngle) * Mathf.Deg2Rad;
    newPos.Set(craftTransform.position.x - viewZoom * Mathf.Cos(angle),
               craftTransform.position.y + viewZoom * viewHeight,
               craftTransform.position.z - viewZoom * Mathf.Sin(angle));
    
    float lerp = (mainUI.InTouch() ? posLerp * 10f : posLerp) * Time.deltaTime;
    transform.position = Vector3.Lerp(transform.position, newPos, lerp);

    // Camera Rotation
    Vector3 cameraTarget = craftTransform.position;
    cameraTarget.y += (viewZoom * viewHeight) * 0.1f;
    Quaternion newRot = Quaternion.LookRotation(cameraTarget - transform.position);

    lerp = (mainUI.InTouch() ? rotLerp * 10f : rotLerp) * Time.deltaTime;
    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, lerp);

    if (lastDetect < DateTime.Now - detectSpan)
    {
      Detect();
      lastDetect = DateTime.Now;
    }
  }

  // Render management --------------------------------------------------------
  Vector3 ground;
  public void OnPostRender() {
    Shapes.Draw.LineGeometry = Shapes.LineGeometry.Volumetric3D;
    Shapes.Draw.LineThicknessSpace = Shapes.ThicknessSpace.Pixels;

    // Draw radar rings around our focused object
    if (craft) {
      ground.Set(craft.transform.position.x, groundDrawY, craft.transform.position.z);
      Shapes.Draw.LineThickness = 0.08f;
      Shapes.Draw.Disc(ground, Vector3.up, Pos.ConvertNmToUnity(10), colorRadar1fill);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(10), 0.01f, colorRadar1);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(20), 0.03f, colorRadar2);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(40), 0.06f, colorRadar3);
    }

    Shapes.Draw.LineThickness = 0.125f;
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;
      if (e.HasType("Human"))
        continue;

      if (craft && craft.pos.GetDistanceToNM(e.pos) > 32f)
        continue;

      // Draw the vertical height stem and trails
      if (e.HasType("Air") || e.HasType("Weapon")) {
        DrawEntityDetails(e);
      }
    }
  }

  List<PosLog> log; //mem
  public void DrawEntityDetails(Entity e) {
    ground.Set(e.transform.position.x, groundDrawY, e.transform.position.z + 0.01f);

    Shapes.Draw.Line(e.transform.position, ground, colorHeight);
    Shapes.Draw.Ring(ground, Vector3.up, 0.01f, 0.005f, colorHeight);

    using (Shapes.PolylinePath trail = new Shapes.PolylinePath()) {
      log = e.GetLog();
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
  public void ZoomBy(float amount)
  {
    viewZoom += amount * 0.5f;
    if (viewZoom < 1) viewZoom = 1;
    if (viewZoom > 10) viewZoom = 10;
  }

  public void RotateBy(float amount)
  {
    viewAngle -= amount * 0.25f;
    viewAngle = viewAngle % 360;
  }

  // Craft management --------------------------------------------------------
  public void NextCraft()
  {
    FindCraft(1);
  }

  public void PrevCraft()
  {
    FindCraft(-1);
  }

  public void ResetCraft()
  {
    if (!String.IsNullOrEmpty(preferredCraft))
    {
      FindCraftByName(preferredCraft);
    }
    else
    {
      FindCraft(0);
    }
  }

  public void SetCraft(Entity entity)
  {
    if (craft != entity) {
      viewAngle = 0;
    }
    
    craft = entity;
    GameObject.Find("StatusDisplay").GetComponent<UnityEngine.UI.Text>().text = entity.pilot;
    danger.Reset();
  }

  public void SetPreferredCraft(string name)
  {
    preferredCraft = name;
    FindCraftByName(name);
  }

  public bool FindCraftByName(string name)
  {
    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value.pilot.Equals(name, StringComparison.OrdinalIgnoreCase)) {
        SetCraft(entry.Value);
        return true;
      }
    }
    return false;
  }

  public bool FindCraft(int incr)
  {
    List<Entity> crafts = new List<Entity>();
    int craftIndex = 0;

    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value == craft)
        craftIndex = crafts.Count;
      if (entry.Value.HasType("FixedWing"))
        crafts.Add(entry.Value);
    }
    
    if (crafts.Count > 0) {
      craftIndex = (craftIndex + incr + crafts.Count) % crafts.Count;
      SetCraft(crafts[craftIndex]);
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
    Entity enemy;
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      enemy = entry.Value;

      if (enemy.coalition == craft.coalition) continue;
      if (!enemy.HasType("Missile")) continue;

      float distance = craft.pos.GetDistanceToNM(enemy.pos);
      if (distance > 35) continue;

      float bearing = craft.pos.GetBearingTo(enemy.pos); // compass
      float heading = (bearing - craft.heading + 360) % 360;           // nose
      float aspect = Math.Abs(((bearing - 180) - enemy.heading + 720) % 360);

      danger.Update(enemy, heading, distance, aspect);
    }
  }
}