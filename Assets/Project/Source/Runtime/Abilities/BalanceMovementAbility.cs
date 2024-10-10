using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static Manatea.RootlingForest.CharacterMovement;

namespace Manatea.RootlingForest
{
    public class BalanceMovementAbility : MonoBehaviour, ICharacterMover
    {
        [SerializeField]
        private CharacterMovement m_CharacterMovement;

        [Space]
        [SerializeField]
        private float m_LedgeDetectionStart = 0.2f;
        [SerializeField]
        private float m_LedgeDetectionEnd = -0.2f;
        [SerializeField]
        private float m_LedgeDetectionDistance = 0.2f;
        [SerializeField]
        private float m_LedgeDetectionRadius = 0.4f;

        [SerializeField]
        [FormerlySerializedAs("m_LedgeBalancingForce")]
        private float m_BalancingForce = 50;
        [SerializeField]
        private float m_BalancingMoveMultiplier = 0.75f;

        [SerializeField]
        private float m_LedgeStableBalancingForce = 150;
        [SerializeField]
        private float m_LedgeStableMoveMultiplier = 0.5f;

        [SerializeField]
        private float m_LedgeBalancingWobbleTime = 0.8f;
        [SerializeField]
        private float m_LedgeBalancingWobbleAmount = 0.45f;

        [SerializeField]
        private float m_StabilizationForce = 50;
        [SerializeField]
        private float m_StabilizationForcePoles = 50;

        [SerializeField]
        private GameplayAttribute m_MoveSpeedAttribute;
        [SerializeField]
        private GameplayAttribute m_LedgeBalancingAttribute;

        [Space]
        [SerializeField]
        private bool m_Debug;
        [SerializeField]
        private bool m_Log;


        GameplayAttributeOwner m_AttributeOwner;

        // Ledge Detection
        // TODO increase iterations and decrease samples to deffer ledge sampling over multiple frames
        private const int c_LedgeDetectionSamples = 8;
        private const int c_LedgeDetectionIterations = 2;
        private const int c_TotalLedgeDetectionSamples = c_LedgeDetectionSamples * c_LedgeDetectionIterations;
        private LedgeSample[] m_LedgeSamples = new LedgeSample[c_LedgeDetectionSamples * c_LedgeDetectionIterations];
        private int m_LedgeDetectionFrame = -1;
        Vector3 m_CurrentLedgeAverageDir;
        float m_CurrentLedgeNoise;
        float m_CurrentLedgeAmount;
        LedgeType m_CurrentLedgeType;

        private float m_MovementSpeedMult;
        private Vector3 m_AdditionalForce;

        private GameplayAttributeModifierInstance m_MovementModifier = new GameplayAttributeModifierInstance() { Type = GameplayAttributeModifierType.Multiplicative };

        private RaycastHit[] m_GroundHits = new RaycastHit[32];


        private struct LedgeSample
        {
            public bool IsLedge;
            public Vector3 Direction;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public RaycastHit Hit;
        }
        public enum LedgeType
        {
            Pole,
            Cliff,
            BalancingBeam,
            UnevenTerrain,
        }

        private void Awake()
        {
            m_AttributeOwner = GetComponentInParent<GameplayAttributeOwner>();
        }

        private void OnEnable()
        {
            m_CharacterMovement.RegisterMover(this);

            if (m_MoveSpeedAttribute && m_AttributeOwner)
            {
                m_AttributeOwner.AddAttributeModifier(m_MoveSpeedAttribute, m_MovementModifier);
            }
        }
        private void OnDisable()
        {
            if (m_MoveSpeedAttribute && m_AttributeOwner)
            {
                m_AttributeOwner.RemoveAttributeModifier(m_MoveSpeedAttribute, m_MovementModifier);
            }

            m_CharacterMovement.UnregisterMover(this);
        }

        public void PreMovement(MovementSimulationState sim)
        {
            float targetMovementSpeedMult = 1;
            Vector3 targetAdditionalForce = Vector3.zero;

            bool ledgeFound = LedgeDetection(sim);
            if (ledgeFound && sim.IsStableGrounded)   // TODO && !sim.m_ScheduledJump 
            {
                #region Analyze Ledge Features

                // Average ledge direction
                m_CurrentLedgeAverageDir = sim.Movement.FeetPos;
                for (int i = 0; i < m_LedgeSamples.Length; i++)
                {
                    Vector3 delta = m_LedgeSamples[i].Direction;
                    delta *= (m_LedgeSamples[i].IsLedge ? 1 : -1);
                    delta /= m_LedgeSamples.Length;
                    if (m_Debug)
                    {
                        Debug.DrawLine(m_CurrentLedgeAverageDir, m_CurrentLedgeAverageDir + delta, m_LedgeSamples[i].IsLedge ? Color.red : Color.green);
                    }
                    m_CurrentLedgeAverageDir += delta;
                }
                m_CurrentLedgeAverageDir -= sim.Movement.FeetPos;

                if (m_Debug)
                {
                    DebugHelper.DrawWireSphere(m_CurrentLedgeAverageDir, 0.2f, Color.blue);
                }

                // Ledge noise level
                m_CurrentLedgeNoise = 0;
                for (int i = 0; i < m_LedgeSamples.Length; i++)
                {
                    if (m_LedgeSamples[i].IsLedge ^ m_LedgeSamples[MMath.Mod(i + 1, m_LedgeSamples.Length)].IsLedge)
                    {
                        m_CurrentLedgeNoise++;
                    }
                }
                m_CurrentLedgeNoise /= m_LedgeSamples.Length;

                // Ledge amount
                m_CurrentLedgeAmount = 0;
                for (int i = 0; i < m_LedgeSamples.Length; i++)
                {
                    if (m_LedgeSamples[i].IsLedge)
                    {
                        m_CurrentLedgeAmount++;
                    }
                }
                m_CurrentLedgeAmount /= m_LedgeSamples.Length;

                if (m_Log)
                {
                    Debug.Log("Noise level: " + m_CurrentLedgeNoise);
                    Debug.Log("Ledge amount: " + m_CurrentLedgeAmount);
                }

                m_CurrentLedgeType = LedgeType.Pole;
                if (m_CurrentLedgeNoise <= 0.2 && m_CurrentLedgeAmount >= 0.75)
                {
                    m_CurrentLedgeType = LedgeType.Pole;
                }
                else if (m_CurrentLedgeNoise <= 0.2 && m_CurrentLedgeAmount < 0.75)
                {
                    m_CurrentLedgeType = LedgeType.Cliff;
                }
                else if (m_CurrentLedgeNoise > 0.2 && m_CurrentLedgeNoise <= 0.55 && m_CurrentLedgeAmount > 0.25)
                {
                    m_CurrentLedgeType = LedgeType.BalancingBeam;
                }
                else
                {
                    m_CurrentLedgeType = LedgeType.UnevenTerrain;
                }
                if (m_Log)
                {
                    Debug.Log("Current Ledge Type: " + m_CurrentLedgeType);
                }

                #endregion


                Vector3 ledgeDir = sim.GroundLowerHit.point - sim.Movement.FeetPos;
                Vector3 ledgeDirProjected = ledgeDir.FlattenY();

                Vector3 ledgeForce = ledgeDirProjected;

                // Balancing wiggle
                if (sim.ScheduledMove != Vector3.zero && m_CurrentLedgeType == LedgeType.BalancingBeam)
                {
                    Vector3 imbalance = Vector3.Cross(sim.ScheduledMove.normalized, Vector3.up);
                    imbalance *= Mathf.PerlinNoise1D(Time.time * sim.ScheduledMove.magnitude * m_LedgeBalancingWobbleTime) * 2 - 1;
                    imbalance *= sim.ScheduledMove.magnitude;
                    imbalance *= m_LedgeBalancingWobbleAmount;
                    imbalance *= MMath.RemapClamped(0.3f, 0.75f, 2, 1, sim.GroundTimer);
                    targetAdditionalForce = imbalance * sim.ScheduledMove.magnitude;
                }

                // Disable ledge force if walking down a cliff
                float intentionalOverride = 0;
                if (m_CurrentLedgeType == LedgeType.Cliff && sim.ScheduledMove != Vector3.zero)
                {
                    intentionalOverride = MMath.RemapClamped(-0.15f, -0.35f, 1, 0, Vector3.Dot(m_CurrentLedgeAverageDir.normalized, sim.ScheduledMove.normalized));
                    intentionalOverride *= MMath.RemapClamped(0.2f, 0.5f, 0, 1, sim.SmoothMoveMagnitude);
                }

                // TODO remove this and put it in a dedicated ability that allows the player to balance better by using their hands
                // Stabilize when holding mouse
                if (m_CurrentLedgeType != LedgeType.Cliff && (Input.GetMouseButton(0) || (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonWest.ReadValue() > 0)))
                {
                    ledgeForce *= m_LedgeStableBalancingForce;
                    targetMovementSpeedMult *= m_LedgeStableMoveMultiplier;
                }
                else
                {
                    if (m_CurrentLedgeType == LedgeType.Cliff)
                    {
                        ledgeForce *= MMath.Lerp(m_BalancingForce, 1, intentionalOverride);
                        targetMovementSpeedMult *= MMath.RemapClamped(0.3f, 0.5f, 1, MMath.Lerp(m_BalancingMoveMultiplier, 1, intentionalOverride), m_CurrentLedgeAmount);
                    }
                    else
                    {
                        ledgeForce *= MMath.Lerp(m_BalancingForce, 1, intentionalOverride);
                        targetMovementSpeedMult *= MMath.Lerp(m_BalancingMoveMultiplier, 1, intentionalOverride);
                    }
                }

                // Allow player to recover once landed
                targetMovementSpeedMult *= MMath.RemapClamped(0, 0.3f, 0.5f, 1, sim.GroundTimer);

                // Balance attribute
                if (sim.Movement.AttributeOwner && sim.Movement.AttributeOwner.TryGetAttributeEvaluatedValue(m_LedgeBalancingAttribute, out float att_balance))
                {
                    ledgeForce *= att_balance;
                }

                // Stabilize player when not moving
                Vector3 stabilizationForce = Vector3.ProjectOnPlane(sim.PreciseGroundLowerHit.normal, sim.GroundLowerHit.normal);
                stabilizationForce = stabilizationForce.FlattenY().normalized + stabilizationForce.y * Vector3.up;
                if (m_CurrentLedgeType == LedgeType.Pole)
                {
                    stabilizationForce *= m_StabilizationForcePoles;
                }
                else
                {
                    stabilizationForce *= m_StabilizationForce;
                }
                float stabilizationForceMult = MMath.Clamp01(1 - sim.ScheduledMove.magnitude);
                stabilizationForceMult += MMath.RemapClamped(0, 0.2f, 2, 0, sim.GroundTimer);
                ledgeForce += stabilizationForce * stabilizationForceMult;

                sim.Movement.Rigidbody.AddForceAtPosition(ledgeForce, sim.Movement.FeetPos, ForceMode.Acceleration);
            }

            m_AdditionalForce = MMath.Damp(m_AdditionalForce, targetAdditionalForce, 5, Time.fixedDeltaTime);
            m_MovementSpeedMult = MMath.Damp(m_MovementSpeedMult, targetMovementSpeedMult, 5, Time.fixedDeltaTime);

            if (m_MoveSpeedAttribute && m_AttributeOwner)
            {
                m_MovementModifier.Value = m_MovementSpeedMult;
            }

            sim.ContactMove += m_AdditionalForce;
        }

        private int DirectionToLedgeId(Vector3 direction)
        {
            return MMath.Mod(MMath.RoundToInt(MMath.DirToAng(direction.XZ()) / MMath.TAU * c_LedgeDetectionSamples), c_LedgeDetectionSamples);
        }

        private bool LedgeDetection(MovementSimulationState sim)
        {
            float radius = m_LedgeDetectionRadius;
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            m_LedgeDetectionFrame++;
            int frame = m_LedgeDetectionFrame % c_LedgeDetectionIterations;

            int ledgeCount = 0;
            int hitCount;

            for (int i = 0; i < c_LedgeDetectionSamples; i++)
            {
                int ledgeId = i * c_LedgeDetectionIterations + frame;

                Vector3 direction = Quaternion.Euler(0, ledgeId / (float)c_TotalLedgeDetectionSamples * -360, 0) * Vector3.right;
                direction *= m_LedgeDetectionDistance;
                Vector3 p1 = sim.Movement.FeetPos + Vector3.up * m_LedgeDetectionStart + direction;
                Vector3 p2 = sim.Movement.FeetPos + Vector3.up * m_LedgeDetectionEnd + direction;

                hitCount = Physics.SphereCastNonAlloc(p1, radius, (p2 - p1).normalized, m_GroundHits, (p2 - p1).magnitude, layerMask, QueryTriggerInteraction.Ignore);
                if (m_Debug)
                {
                    DebugHelper.DrawWireCapsule(p1, p2, m_LedgeDetectionRadius, Color.black * 0.5f);
                }

                RaycastHit groundHitResult = new RaycastHit();
                groundHitResult.distance = float.PositiveInfinity;
                bool groundFound = false;
                for (int j = 0; j < hitCount; j++)
                {
                    var hit = m_GroundHits[j];

                    // Discard overlaps
                    if (hit.distance == 0)
                        continue;
                    // Discard collisions that are further away
                    if (hit.distance > groundHitResult.distance)
                        continue;
                    // Discard self collisions
                    if (hit.collider.transform == sim.Movement.Rigidbody.transform)
                        continue;
                    if (hit.collider.transform.IsChildOf(sim.Movement.Rigidbody.transform))
                        continue;

                    // Precise raycast
                    bool hasPreciseLedge = hit.collider.Raycast(new Ray(hit.point + Vector3.up * 0.01f + Vector3.Cross(Vector3.Cross(hit.normal, Vector3.up), hit.normal) * 0.01f, Vector3.down), out RaycastHit preciseHit, 0.05f);
                    if (!hasPreciseLedge)
                        continue;
                    if (!sim.Movement.IsRaycastHitWalkable(preciseHit))
                        continue;

                    if (m_Debug)
                    {
                        Debug.DrawLine(preciseHit.point, preciseHit.point + preciseHit.normal * 0.1f, Color.cyan);
                        DebugHelper.DrawWireCapsule(p1, p1 + (p2 - p1).normalized * hit.distance, m_LedgeDetectionRadius, Color.green);
                    }

                    // Test if this could be the ground we are currently standing on
                    Plane ledgeGroundPlane = new Plane(preciseHit.normal, preciseHit.point);
                    float feetDistance = MMath.Abs(ledgeGroundPlane.GetDistanceToPoint(sim.Movement.FeetPos));
                    Plane feetGroundPlane = new Plane(sim.Movement.PreciseGroundLowerHit.normal, sim.PreciseGroundLowerHit.point);
                    float contactDistance = MMath.Abs(feetGroundPlane.GetDistanceToPoint(preciseHit.point));
                    if (feetDistance > 0.3f && contactDistance > 0.4f)
                    {
                        if (m_Log)
                        {
                            Debug.Log(feetDistance + " - " + contactDistance);
                        }
                        continue;
                    }

                    groundHitResult = hit;
                    groundFound = true;
                }
                if (m_Debug && !groundFound)
                    DebugHelper.DrawWireCapsule(p1, p2, m_LedgeDetectionRadius, Color.red);

                m_LedgeSamples[ledgeId].IsLedge = !groundFound;
                m_LedgeSamples[ledgeId].Direction = direction;
                m_LedgeSamples[ledgeId].StartPosition = p1;
                m_LedgeSamples[ledgeId].EndPosition = p2;
                m_LedgeSamples[ledgeId].Hit = groundHitResult;

                if (!groundFound)
                {
                    ledgeCount++;
                }
            }

            return ledgeCount > 0;
        }
    }
}
