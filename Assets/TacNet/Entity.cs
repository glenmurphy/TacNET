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
  public string id;
  public float roll;
  public float pitch;
  public float yaw;
  public float heading;
  public string type;
  public string entityName;
  public string pilot;
  public string color;
  public string coalition;

  // Used for figuring out the pos in unity-space
  public Vector3 posCache = Vector3.zero;
  public bool posCacheValid = false;

  // Internal vars
  Color drawColor;
  bool hasTransform = false;
  bool init = false;
  public Dictionary<string, bool> typeIndex = new Dictionary<string, bool>();
  MeshRenderer mesh;
  GameObject model;
  Trail trail = new Trail(120, 2f);

  bool selected = false;

  // We position the ships (from Pilot.cs) relative to a point so that we can do things 
  // like scale their position non-linearly with distance (fisheye radar)
  Vector3 rot = new Vector3(0, 0, 0); // mem
  public void Reposition(Pos origin)
  {
    if (!hasTransform) return;

    if (!posCacheValid)
      posCache = pos.GetUnityPosition(origin);
    posCacheValid = true;

    // Stop initial jump
    rot.Set(-pitch, heading, -roll);
    if (transform.position.sqrMagnitude == 0)
    {
      transform.position = posCache;
      transform.rotation = Quaternion.Euler(rot);
    } 
    else
    {
      transform.position = Vector3.Lerp(transform.position, posCache, posLerp * Time.deltaTime);
      transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rot), rotLerp * Time.deltaTime);
    }

    if (!init && type.Length > 0) {
      Init();
    }
  }

  public void SetTransform(Dictionary<string, float> t, DateTime time)
  {
    hasTransform = true;
    posCacheValid = false;
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

  public void Select()
  {
    selected = true;
  }

  public void Deselect() 
  {
    selected = false;
  }

  public bool IsSelected()
  {
    return selected;
  }

  public bool HasType(string typeIn)
  {
    return typeIndex.ContainsKey(typeIn.ToLower());
  }

  public void SetID(string idIn)
  {
    id = idIn;
  }

  public void SetName(string nameIn)
  {
    entityName = nameIn;
  }

  public void SetColor(string colorIn)
  {
    color = colorIn;
    UpdateColor();
  }

  public void SetPilot(string pilotIn)
  {
    pilot = pilotIn;
  }

  public void SetCoalition(string coalitionIn)
  {
    coalition = coalitionIn;
  }

  private void Init()
  {
    init = true;
    UpdateModel();
  }

  private void UpdateModel()
  {
    if (!init) return;

    if (HasType("Human"))
      return;
    else if (HasType("Air"))
      SetModel("air");
    else if (HasType("Weapon"))
      SetModel("missile");
    else if (HasType("Ground"))
      SetModel("ground");
    else if (HasType("Sea"))
      SetModel("sea");
  }

  private void SetModel(string name)
  {
    if (model != null)
      Destroy(model);
    
    model = Instantiate(Resources.Load(name) as GameObject);
    model.transform.SetParent(transform, false);
    UpdateColor();
  }

  public bool HasModel() {
    return (model != null);
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