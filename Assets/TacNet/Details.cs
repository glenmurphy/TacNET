using System;
using System.Collections.Generic;
using UnityEngine;

public class Details : MonoBehaviour
{
  public UnityEngine.UI.Text entityName;
  public UnityEngine.UI.Text lonlat;
  public UnityEngine.UI.Text alt;

  public static string ConvertToDMM(float degrees) {
    int d = (int)degrees;
    string m = Convert.ToString((int)(((degrees - d) * 0.6) * 1000000))
      .PadLeft(6, '0').Insert(2, ".");
    return d + "°" + m;
  }

  public void SetName(string newName)
  {
    entityName.text = newName;
  }

  public void SetLonLat(float lon, float lat)
  {
    lonlat.text = ConvertToDMM(lon) + ", " + ConvertToDMM(lat);
  }

  public void SetAlt(float newAlt)
  {
    alt.text = (int)newAlt + "'";
  }

  public void SetBRA(float b, float r, float a)
  {
    alt.text = (int)b + "° " + (int)r + "nm " + (int)a + "'";
  }
}