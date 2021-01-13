using System;
using System.Collections.Generic;
using UnityEngine;

public class PosLog
{
  public PosLog(DateTime timeIn, Pos posIn, float rollIn, int indexIn) {
    time = timeIn;
    pos = new Pos(posIn.lon, posIn.lat, posIn.alt);
    roll = rollIn;
    posCache = Vector3.zero;
    index = indexIn;
  }
  public DateTime time;
  public Pos pos;
  public float roll;
  public int index;
  
  public Vector3 posCache;
}

public class Trail
{
  private List<PosLog> log;
  private TimeSpan logIncrement;
  private TimeSpan logLength;
  private int count = 0;

  public Trail(int length, float increment)
  {
    log = new List<PosLog>();
    logIncrement = new TimeSpan(0, 0, 0, 0, (int)(increment * 1000));
    logLength = new TimeSpan(0, 0, 0, (int)length);
  }

  public void Log(DateTime time, Pos pos, float roll) {
    if (log.Count == 0 || time > log[log.Count - 1].time + logIncrement)
    {
      log.Add(new PosLog(time, pos, roll, count++));
    }

    // We need to put this elsewhere
    while (log.Count > 0 && log[0].time < time - logLength)
      log.RemoveAt(0);
  }
  
  public List<PosLog> GetLog() {
    return log;
  }
}