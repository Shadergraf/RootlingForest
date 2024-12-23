using Manatea;
using Manatea.GameplaySystem;
using Manatea.RootlingForest.Abilities;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class GrabPreferences : GameplayFeaturePreference
    {
        [SerializeField]
        private bool m_CollisionEnabled;
        [SerializeField]
        private AbilityPriority m_Priority = AbilityPriority.Default;
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
        [SerializeField]
        private bool m_UseCustomThrow = false;
        [SerializeField]
        private float m_ThrowRotation = 0;
        [SerializeField]
        private bool m_UseDropThrow = false;
        [SerializeField]
        private Vector3 m_DropForce;
        [SerializeField]
        private float m_DropTorque;

        public bool CollisionEnabled => m_CollisionEnabled;
        public AbilityPriority Priority => m_Priority;
        public LocationRule LocationRule => m_LocationRule;
        public RotationRule RotationRule => m_RotationRule;
        public float RotationLimit => m_RotationLimit;
        public bool UseOrientations => m_UseOrientations;
        public GrabOrientation[] Orientations => m_Orientations;
        public bool UpdateGrabLocation => m_UpdateGrabLocation;
        public bool AllowOverlapAfterDrop => m_AllowOverlapAfterDrop;
        public bool UseCustomThrow => m_UseCustomThrow;
        public float ThrowRotation => m_ThrowRotation;
        public bool UseDropThrow => m_UseDropThrow;
        public Vector3 DropForce => m_DropForce;
        public float DropTorque => m_DropTorque;
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
}
