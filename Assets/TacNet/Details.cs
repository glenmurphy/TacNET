using System;
using System.Collections.Generic;
using UnityEngine;

public class Details : MonoBehaviour
{
  public UnityEngine.UI.Text entityName;
  public UnityEngine.UI.Text lonlat;
  public UnityEngine.UI.Text alt;

  public void SetName(string newName)
  {
    entityName.text = newName;
  }

  public void SetLonLat(string newLonLat)
  {
    lonlat.text = newLonLat;
  }

  public void SetAlt(string newAlt)
  {
    alt.text = newAlt;
  }
}