using System;
using System.Collections.Generic;
using UnityEngine;

public class Details : MonoBehaviour
{
  public UnityEngine.UI.Text entityName;
  public UnityEngine.UI.Text lonlat;
  public UnityEngine.UI.Text alt;
  Entity craft;

  public static string ConvertToDMM(float degrees) {
    int d = (int)degrees;
    string m = Convert.ToString((int)(((degrees - d) * 0.6) * 1000000))
      .PadLeft(6, '0').Insert(2, ".");//.Insert(5, "<color=#777>") + "</color>";
    return d + "°" + m;
  }

  public static string LatFormat(float lat) {
    return ConvertToDMM(Math.Abs(lat)) + 
        (lat > 0 ? "'N" : "'S");
  }
  public static string LonFormat(float lon) {
    return ConvertToDMM(Math.Abs(lon)) + 
        (lon > 0 ? "'E" : "'W");
  }

  public void SetName(string newName)
  {
    entityName.text = newName;
  }

  public void SetLatLon(float lat, float lon)
  {
    lonlat.text = LatFormat(lat) + " " + LonFormat(lon);
  }

  public void SetAlt(float newAlt)
  {
    alt.text = (int)newAlt + "'";
  }

  public void SetBRA(float b, float r, float a)
  {
    if (r < 0.5)
      alt.text = String.Format("{0:n0}'", a);
    else
      alt.text = (int)b + "° <color=#555>/</color> " + 
                 String.Format("{0:F1}", r) + " <color=#555>/</color> " + 
                 String.Format("{0:n0}'", a);
  }
}