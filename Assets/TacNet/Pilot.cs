using System;
using System.Collections.Generic;
using UnityEngine;

public class Pilot : MonoBehaviour
{
  // Config
  private static float posLerp = 1f;
  private static float rotLerp = 2f;
  private static float groundDrawY = 0.001f;

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
  Entity target;
  int targetIndex = 0;
  public float zoom = 0; // -1 to 1
  public float viewAngle = 0;

  public void Start()
  {
    world = worldObject.GetComponent<World>();
  }

  public void Update()
  {
    // We update the position of each entity here so that we can guarantee that the camera movement
    // happens after the movement update
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;
      e.Reposition(world.basePos);
    }

    if (!HaveTarget())
      return;
    // Camera Position
    Transform targetTransform = target.gameObject.transform;
    float angle = (-target.yaw + 90) * Mathf.Deg2Rad;
    Vector3 newPos = new Vector3(targetTransform.position.x - 5f * Mathf.Cos(angle + viewAngle),
                                 targetTransform.position.y + 5f,
                                 targetTransform.position.z - 5f * Mathf.Sin(angle + viewAngle));
    transform.position = Vector3.Lerp(transform.position, newPos, posLerp * Time.deltaTime);

    // Camera Rotation
    Vector3 cameraTarget = targetTransform.position;
    cameraTarget.y += 0.5f;
    Quaternion newRot = Quaternion.LookRotation(cameraTarget - transform.position);
    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, rotLerp * Time.deltaTime);
  }

  void Detect()
  {
    List<Entity> danger = new List<Entity>();

    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;

      if (e.HasType("Weapon")) {
        float distance = target.pos.GetDistanceTo(e.pos);
        float aspect = Math.Abs((e.heading - e.pos.GetBearingTo(target.pos) + 360) % 360);
        if (distance < 60 && aspect < 20)
          danger.Add(e);
      }
    }
  }

  public void DrawEntityDetails(Entity e) {
    Vector3 ground = new Vector3(e.transform.position.x, groundDrawY, e.transform.position.z);

    Shapes.Draw.LineThickness = 0.1f;
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

      trail.AddPoint(e.transform.position);
      Shapes.Draw.Polyline(trail, closed:false, thickness:0.3f, e.GetColor());
    }
  }

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
      if (e == target) {
        Vector3 ground = new Vector3(e.transform.position.x, groundDrawY, e.transform.position.z);

        Shapes.Draw.LineThickness = 0.08f;
        Shapes.Draw.Disc(ground, Vector3.up, Pos.ConvertNmToUnity(10), colorRadar1fill);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(10), 0.01f, colorRadar1);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(20), 0.03f, colorRadar2);
        Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(40), 0.06f, colorRadar3);
      }
    }
  }

  public void OnGUI()
  {
    Event e = Event.current;
    if (e.type == EventType.MouseDown && e.button == 0 && e.isMouse)
    {
        if (e.mousePosition.x < Screen.width / 2)
          targetIndex -= targetIndex > 0 ? 1 : 0;
        else
          targetIndex++;
        FindTarget();
        SavePreferredTarget();
    }
    else if (e.type == EventType.KeyDown)
    {
      if (e.keyCode == KeyCode.A) {
        FindMinTarget();
        SavePreferredTarget();
      }
    }
  }

  public void SetTarget(Entity entity)
  {
    if (target != entity) {
      viewAngle = 0;
    }
    
    target = entity;
    GetComponentInChildren<UnityEngine.UI.Text>().text = entity.pilot;
  }

  public void SavePreferredTarget()
  {
    if (target != null)
      PlayerPrefs.SetString("preferredTarget", target.pilot);
  }

  public bool FindTargetByName(string name)
  {
    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value.pilot.Equals(name)) {
        SetTarget(entry.Value);
        return true;
      }
    }
    return false;
  }

  public bool FindTarget()
  {
    List<Entity> targets = new List<Entity>();

    foreach (KeyValuePair<string, Entity> entry in world.entities)
    {
      if (entry.Value.HasType("FixedWing")) {
        targets.Add(entry.Value);
      }
    }
    if (targets.Count > 0) {
      SetTarget(targets[targetIndex % targets.Count]);
      return true;
    }
    return false;
  }

  public void FindMinTarget()
  {
    Entity minTarget = target;
    float minDistance = Single.MaxValue;
    float distance; 
    foreach (KeyValuePair<string, Entity> e in world.entities)
    {
      if (!e.Value.HasType("FixedWing"))
        continue;
      foreach (KeyValuePair<string, Entity> other in world.entities)
      {
        if (e.Value == other.Value || 
            e.Value.coalition == other.Value.coalition ||
            !other.Value.HasType("FixedWing"))
          continue;
        distance = e.Value.pos.GetDistanceTo(other.Value.pos);
        if (distance < minDistance && e.Value != target) { // to toggle closest
          minDistance = distance;
          minTarget = e.Value;
        }
      }
    }

    if (minTarget)
      SetTarget(minTarget);
  }

  public bool HaveTarget()
  {
    if (target || !world)
      return true;
    
    if (FindTargetByName(PlayerPrefs.GetString("preferredTarget")))
      return true;
    return false;
  }
}