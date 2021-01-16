using System;
using UnityEngine;

public class Pos
{
  // Universal constants
  public const float EarthRadius = 6376.5f; // km
  public const float DegreeLengthEquator = 111.321543f; // km of 1 lon degree
  public const float UnityScale = 0.25f; // 1km = N unity units
  public const float NmToKm = 1.852f;

  public static float ConvertNmToUnity(float nm) {
    return (nm * NmToKm) * UnityScale;
  }
  public static float ConvertKmToNm(float km) {
    return km / NmToKm;
  }

  public float lon;
  public float lat;
  public float alt;

  public Pos(float lonIn, float latIn, float altIn)
  {
    lon = lonIn;
    lat = latIn;
    alt = altIn;
  }

  public bool IsZero() {
    return (lon == 0 && lat == 0 && alt == 0);
  }

  // This uses the pos' longitude, and scales it with the latitude, which results a
  // pseudocylindrical projection - this means that relative positions of units close
  // to each other and further away from the origin are more accurately portrayed, at
  // the expense of the overall map view being slightly pinched.
  public Vector3 GetUnityPosition(Pos origin)
  {
    float lonScale = Mathf.Cos(lat * Mathf.Deg2Rad) * DegreeLengthEquator * UnityScale;
    float latScale = DegreeLengthEquator * UnityScale;
    float altScale = 0.001f * UnityScale; // alt is in meters
    
    float relativeLon = lon - origin.lon;
    float relativeLat = lat - origin.lat;

    return new Vector3(relativeLon * latScale, alt * altScale, relativeLat * lonScale);
  }

  // http://csharphelper.com/blog/2019/03/calculate-the-great-circle-distance-between-two-latitudes-and-longitudes-in-c/
  public float GetDistanceTo(Pos dest)
  {
    float lat1 = Mathf.Deg2Rad * lat;
    float lon1 = Mathf.Deg2Rad * lon;
    float lat2 = Mathf.Deg2Rad * dest.lat;
    float lon2 = Mathf.Deg2Rad * dest.lon;
    float d_lat = lat2 - lat1;
    float d_lon = lon2 - lon1;
    float h = Mathf.Sin(d_lat / 2) * Mathf.Sin(d_lat / 2) +
              Mathf.Cos(lat1) * Mathf.Cos(lat2) *
              Mathf.Sin(d_lon / 2) * Mathf.Sin(d_lon / 2);

    return 2 * EarthRadius * Mathf.Asin(Mathf.Sqrt(h));
  }

  public float GetDistanceToNM(Pos dest)
  {
    return ConvertKmToNm(GetDistanceTo(dest));
  }

  public float GetBearingTo(Pos dest)
  {
    float dLon = (dest.lon - lon);

    float y = Mathf.Sin(Mathf.Deg2Rad * dLon) * Mathf.Cos(Mathf.Deg2Rad * dest.lat);
    float x = Mathf.Cos(Mathf.Deg2Rad * lat) * Mathf.Sin(Mathf.Deg2Rad * dest.lat) - 
              Mathf.Sin(Mathf.Deg2Rad * lat) * Mathf.Cos(Mathf.Deg2Rad * dest.lat) * 
              Mathf.Cos(Mathf.Deg2Rad * dLon);

    var brng = Mathf.Rad2Deg * Mathf.Atan2(y, x);
    brng = (brng + 360) % 360;
    return brng;
  }
}