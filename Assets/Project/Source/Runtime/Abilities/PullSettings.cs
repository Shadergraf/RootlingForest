using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PullLocation
{
    Center = 0,
    Bounds = 1,
    UpdatedBounds = 2,
}

public class PullSettings : MonoBehaviour
{
    public PullLocation PullLocation;
}
