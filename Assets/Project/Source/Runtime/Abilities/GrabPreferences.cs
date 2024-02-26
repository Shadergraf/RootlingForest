using Manatea;
using UnityEngine;

public class GrabPreferences : MonoBehaviour
{
    [SerializeField]
    private bool m_CollisionEnabled;
    [SerializeField]
    private LocationRule m_LocationRule;
    [SerializeField]
    private RotationRule m_RotationRule;
    [SerializeField]
    private float m_RotationLimit = 0;
    [SerializeField]
    private bool m_UseOrientations;
    [SerializeField]
    private GrabOrientation[] m_Orientations;
    [SerializeField]
    private bool m_UpdateGrabLocation;
    [SerializeField]
    private bool m_AllowOverlapAfterDrop = true;

    public bool CollisionEnabled => m_CollisionEnabled;
    public LocationRule LocationRule => m_LocationRule;
    public RotationRule RotationRule => m_RotationRule;
    public float RotationLimit => m_RotationLimit;
    public bool UseOrientations => m_UseOrientations;
    public GrabOrientation[] Orientations => m_Orientations;
    public bool UpdateGrabLocation => m_UpdateGrabLocation;
    public bool AllowOverlapAfterDrop => m_AllowOverlapAfterDrop;
}

public enum LocationRule
{
    Center = 0,
    Bounds = 1,
    XAxis = 2,
    YAxis = 3,
    ZAxis = 4,
}
public enum RotationRule
{
    Locked = 0,
    Limited = 1,
    Free = 2,
}
