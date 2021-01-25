using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Attach to main camera object
class UserCamera : MonoBehaviour
{
  Pilot pilot;
  World world;
  MainUI mainUI;

  private static float posLerp = 1f;
  private static float rotLerp = 2f;
  [Range (1, 30)]
  public float viewZoom = 5f;

  [Range (0, 2)]
  public float viewHeight = 1f;

  public float viewAngle = 0;

  // Config
  private static float groundDrawY = 0.001f;

  // Color of the height bars
  private static Color colorHeight = new Color(1, 1, 1, 0.75f);

  // Color of the concentric radar range rings, inner to outer
  private static Color colorRadar1 = new Color(0.5f, 0.5f, 0.5f, 0.75f);
  private static Color colorRadar1fill = new Color(0.5f, 0.5f, 0.5f, 0.02f);

  private static Color colorRadar2 = new Color(0.3f, 0.3f, 0.3f, 0.75f);
  private static Color colorRadar3 = new Color(0.1f, 0.1f, 0.1f, 0.75f);

  enum Mode
  {
    FOLLOW,
    FREE
  }

  Mode mode = Mode.FOLLOW;
  Vector3 targetPos;
  Quaternion targetRot;

  void Start()
  {
    world = GameObject.FindObjectsOfType<World>()[0];
    pilot = GameObject.FindObjectsOfType<Pilot>()[0];
    mainUI = GameObject.FindObjectsOfType<MainUI>()[0];
    targetPos = transform.position;
    targetRot = transform.rotation;
  }

  public void UpdateCamera()
  {
    if (mode == Mode.FOLLOW)
      UpdateFollowCam();
    else
      UpdateFreeCam();
  
    float timeLerp = mainUI.InTouchLerp() * 20f + 1f;
    transform.position = Vector3.Lerp(transform.position, targetPos, 
      posLerp * timeLerp * Time.deltaTime);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 
      rotLerp * timeLerp * Time.deltaTime);
  }

  void UpdateFreeCam()
  {
  }

  void UpdateFollowCam()
  {
    Entity craft = pilot.GetCraft();
    if (!craft) return;

    // Camera Position
    Transform craftTransform = craft.gameObject.transform;
    float angle = (-craft.yaw + 90 + viewAngle) * Mathf.Deg2Rad;
    targetPos.Set(craftTransform.position.x - viewZoom * Mathf.Cos(angle),
                  craftTransform.position.y + viewZoom * viewHeight,
                  craftTransform.position.z - viewZoom * Mathf.Sin(angle));

    // Camera Rotation
    Vector3 cameraTarget = craftTransform.position;
    cameraTarget.y += (viewZoom * viewHeight) * 0.1f;
    targetRot = Quaternion.LookRotation(cameraTarget - transform.position);
  }

  // Input management ---------------------------------------------------------
  public void HandleDrag(float x, float y, float byX, float byY)
  {
    if (mode == Mode.FOLLOW) {
      RotateBy(byX * 0.5f);
    } else {
      // using the height as the distance is a rough approximation - 
      // should just figure out the actual distance to the plane
      Vector3 current = GetComponent<Camera>().ScreenToWorldPoint(
          new Vector3(x, y, targetPos.y));
      Vector3 prev = GetComponent<Camera>().ScreenToWorldPoint(
          new Vector3(x - byX, y - byY, targetPos.y));

      targetPos.x -= current.x - prev.x;
      targetPos.z -= current.z - prev.z;
    }
  }

  public void HandleScroll(float z)
  {
    if (mode == Mode.FOLLOW) {
      ZoomBy(z);
    } else {
      targetPos += transform.forward * -z;
      if (targetPos.y < 1)
        targetPos.y = 1;
      else if (targetPos.y > 30)
        targetPos.y = 30;
    }
  }

  public void Reset()
  {
    // Doesn't apply in follow, because the follow logic does its own thing
    if (mode == Mode.FREE) {
      targetRot = Quaternion.Euler(75, 0, 0);

      Entity craft = pilot.GetCraft();
      if (craft) {
        targetPos = craft.gameObject.transform.position;
        targetPos.y = 20;
        targetPos.z -= 20 * Mathf.Cos(75 * Mathf.Deg2Rad);
      } else {
        targetPos = transform.position;
      }
    }
  }

  public void NextMode()
  {
    if (mode == Mode.FOLLOW) {
      mode = Mode.FREE;
    } else {
      mode = Mode.FOLLOW;
    }

    Reset();
  }

  // View management ----------------------------------------------------------
  void ZoomBy(float amount)
  {
    viewZoom += amount * 0.5f;
    if (viewZoom < 1) viewZoom = 1;
    if (viewZoom > 30) viewZoom = 30;
  }

  void RotateBy(float amount)
  {
    viewAngle -= amount * 0.25f;
    viewAngle = viewAngle % 360;
  }

  // Render management --------------------------------------------------------
  Vector3 ground;
  public void OnPostRender() {
    Entity craft = pilot.GetCraft();

    Shapes.Draw.LineGeometry = Shapes.LineGeometry.Volumetric3D;
    Shapes.Draw.LineThicknessSpace = Shapes.ThicknessSpace.Pixels;

    // Draw radar rings around our focused object
    if (craft)
    {
      ground.Set(craft.transform.position.x, groundDrawY, craft.transform.position.z);
      Shapes.Draw.LineThickness = 0.08f;
      Shapes.Draw.Disc(ground, Vector3.up, Pos.ConvertNmToUnity(10), colorRadar1fill);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(10), 0.01f, colorRadar1);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(20), 0.03f, colorRadar2);
      Shapes.Draw.Ring(ground, Vector3.up, Pos.ConvertNmToUnity(40), 0.06f, colorRadar3);
    }

    // A guess for detail draw distance in the top-down view
    float sqDrawDist = Mathf.Pow(transform.position.y * 1.85f, 2); 
    Shapes.Draw.LineThickness = 0.125f;
    foreach (KeyValuePair<string, Entity> entry in world.entities) {
      Entity e = entry.Value;
      if (e.HasType("Human"))
        continue;

      if (mode == Mode.FOLLOW && craft && craft.pos.GetDistanceToNM(e.pos) > 32f) {
        continue;
      } else if (mode == Mode.FREE && craft && 
        (transform.position - e.posCache).sqrMagnitude > sqDrawDist) {
        continue;
      }

      // Draw the vertical height stem and trails
      if (e.HasType("Air") || e.HasType("Weapon"))
        DrawEntityAirDetails(e);
    }
  }

  List<PosLog> log; //mem
  public void DrawEntityAirDetails(Entity e) {
    Entity craft = pilot.GetCraft();
    ground.Set(e.transform.position.x, groundDrawY, e.transform.position.z + 0.01f);

    // Vertical height line
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
}