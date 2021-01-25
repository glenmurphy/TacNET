using System;
using System.Collections.Generic;
using UnityEngine;

public class Pilot : MonoBehaviour
{
  public Speech speech;

  // Internal
  World world;
  Entity craft;

  Danger danger;
  DateTime lastDetect;
  TimeSpan detectSpan = new TimeSpan(0, 0, 0, 0, 250);
  string preferredCraft;

  public void Start()
  {
    world = GetComponent<World>();
    danger = new Danger(speech);
  }

  public void Update()
  {
    if (!HaveCraft())
      return;

    if (lastDetect < DateTime.Now - detectSpan)
    {
      Detect();
      lastDetect = DateTime.Now;
    }
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

  public Entity GetCraft()
  {
    return craft;
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