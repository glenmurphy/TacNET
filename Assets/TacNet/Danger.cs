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
      defeated = false;
    }
    public Entity entity;
    public DateTime lastAlert;
    public float lastRange;
    public bool defeated;
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
      // ignore defeated
      if (tracked[e.id].defeated == true)
        return;
      
      // if it's time for an update
      if (tracked[e.id].lastAlert < DateTime.Now - warningGap) {
        // check to see if it's still a threat
        if (speech.GetAspectCall(aspect) == Speech.Call.COLD ||
            distance > tracked[e.id].lastRange) {
          tracked[e.id].defeated = true;
        } else {
          Call(bearing, distance, aspect);
          tracked[e.id].lastAlert = DateTime.Now;
        }

        tracked[e.id].lastRange = distance;
      }
    } else {
      tracked.Add(e.id, new Tracked(e, distance));
      if (aspect < 90 || aspect > 270) {
        Call(bearing, distance, aspect);
        tracked[e.id].lastAlert = DateTime.Now;
      }
    }
  }
}