using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
  // Config
  public static float posLerp = 2;
  public static float rotLerp = 10;

  Color blue = new Color(0, 0.75f, 1f, 1f);
  Color red = new Color(1f, 0.2f, 0, 1);
  Color grey = new Color(0.5f, 0.5f, 0.5f, 1);

  // Externally-provided data
  public Pos pos = new Pos(0, 0, 0);
  public float roll;
  public float pitch;
  public float yaw;
  public float heading;
  public string type;
  public string entityName;
  public string pilot;
  public string color;
  public string coalition;

  // Internal vars
  Color drawColor;
  bool hasTransform = false;
  bool init = false;
  public Dictionary<string, bool> typeIndex = new Dictionary<string, bool>();
  MeshRenderer mesh;
  GameObject model;
  Trail trail = new Trail(120, 1.0f);

  // We position the ships (from Pilot.cs) relative to a point so that we can do things 
  // like scale their position non-linearly with distance (fisheye radar)
  public void Reposition(Pos origin)
  {
    if (!hasTransform) return;
    Vector3 worldPos = pos.GetUnityPosition(origin);

    // Stop initial jump
    if (transform.position.sqrMagnitude == 0)
    {
      transform.position = worldPos;
      transform.rotation = Quaternion.Euler(new Vector3(-pitch, heading, -roll));
    } 
    else
    {
      transform.position = Vector3.Lerp(transform.position, worldPos, posLerp * Time.deltaTime);
      transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(-pitch, heading, -roll)), rotLerp * Time.deltaTime);
    }

    if (!init && type.Length > 0) {
      Init();
    }
  }

  public void SetTransform(Dictionary<string, float> t, DateTime time)
  {
    hasTransform = true;

    if (t.ContainsKey("lon")) pos.lon = t["lon"];
    if (t.ContainsKey("lat")) pos.lat = t["lat"];
    if (t.ContainsKey("alt")) pos.alt = t["alt"];

    if (t.ContainsKey("roll")) roll = t["roll"];
    if (t.ContainsKey("pitch")) pitch = t["pitch"];
    if (t.ContainsKey("yaw")) yaw = t["yaw"];
    if (t.ContainsKey("heading")) heading = t["heading"];

    if (HasType("Air") || HasType("Weapon"))
      trail.Log(time, pos, roll);
  }

  public void SetType(string typeIn) {
    type = typeIn;
    typeIndex.Clear();
    foreach (string subType in typeIn.Split('+')) {
      typeIndex.Add(subType.ToLower(), true);
    }
    UpdateModel();
  }

  public bool HasType(string typeIn) {
    return typeIndex.ContainsKey(typeIn.ToLower());
  }

  public void SetName(string nameIn) {
    entityName = nameIn;
  }

  public void SetColor(string colorIn) {
    color = colorIn;
    UpdateColor();
  }

  public void SetPilot(string pilotIn) {
    pilot = pilotIn;
  }

  public void SetCoalition(string coalitionIn) {
    coalition = coalitionIn;
  }

  private void Init() {
    init = true;
    UpdateModel();
  }

  private void UpdateModel() {
    if (!init) return;

    if (HasType("Human")) {
      return;
    } else if (HasType("Air")) {
      SetModel("air");
      //EnableTrails();
    } else if (HasType("Weapon")) {
      SetModel("missile");
      //EnableTrails();
    } else if (HasType("Ground")) {
      SetModel("ground");
    } else if (HasType("Sea")) {
      SetModel("sea");
    }
  }

  private void SetModel(string name)
  {
    if (model != null)
      Destroy(model);
    
    model = Instantiate(Resources.Load(name) as GameObject);
    model.transform.SetParent(transform, false);
    UpdateColor();
  }

  private void EnableTrails()
  {
    GetComponentInChildren<TrailRenderer>().enabled = true;
  }

  public List<PosLog> GetLog() {
    return trail.GetLog();
  }

  public Color GetColor() {
    return drawColor;
  }

  private void UpdateColor() {
    mesh = GetComponentInChildren<MeshRenderer>();
    if (!mesh) return;

    if (color == "Blue")
      drawColor = blue;
    else if (color == "Red")
      drawColor = red;
    else
      drawColor = grey;
    
    if (HasType("Air")) {
      mesh.material.color = drawColor;
      mesh.material.EnableKeyword("_EMISSION");
      mesh.material.SetColor("_EmissionColor", drawColor * 1.1f);
    } else if (HasType("Weapon")) {
      mesh.material.color = drawColor;
      mesh.material.EnableKeyword("_EMISSION");
      mesh.material.SetColor("_EmissionColor", drawColor * 3.0f);
    } else {
      mesh.material.color = drawColor * 0.75f;
      mesh.material.DisableKeyword("_EMISSION");
      mesh.material.SetColor("_EmissionColor", Color.black);
    }
  }
}