using System;
using System.Collections;
using System.Collections.Generic;

class Danger
{
  class Tracked
  {
    public Tracked(Entity entityIn, float lastRangeIn) {
      entity = entityIn;
      lastAlert = DateTime.Now;
      lastRange = lastRangeIn;
    }
    public Entity entity;
    public DateTime lastAlert;
    public float lastRange;
  }

  Speech speech;
  TimeSpan warningGap = new TimeSpan(0, 0, 15);
  Dictionary<string, Tracked> tracked; // id : tracked

  public Danger(Speech speechObj) {
    tracked = new Dictionary<string, Tracked>();
    speech = speechObj;
  }

  public void Reset()
  {
    tracked.Clear();
  }

  public void Call(float bearing, float distance, float aspect)
  {
    speech.Say(new Speech.Call[] {
      Speech.Call.MISSILE,
      speech.GetBearingCall(bearing),
      speech.GetDistanceCall(distance),
      speech.GetAspectCall(aspect),
    });
  }

  public void Update(Entity e, float bearing, float distance, float aspect)
  {
    if (tracked.ContainsKey(e.id)) {
      Console.WriteLine(e.id);
      if (tracked[e.id].lastAlert < DateTime.Now - warningGap) {
        Call(bearing, distance, aspect);
        tracked[e.id].lastAlert = DateTime.Now;
      }
    } else {
      tracked.Add(e.id, new Tracked(e, distance));
      Call(bearing, distance, aspect);
    }
  }
}