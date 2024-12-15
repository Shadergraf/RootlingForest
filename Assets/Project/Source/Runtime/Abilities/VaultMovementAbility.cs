using System.Collections.Generic;
using UnityEngine;
using static Manatea.RootlingForest.CharacterMovement;

namespace Manatea.RootlingForest
{
    public class VaultMovementAbility : BaseAbility, ICharacterMover
    {
        [SerializeField]
        private CharacterMovement m_CharacterMovement;

        [Space]
        [SerializeField]
        private float m_VaultingDetectionDistance;
        [SerializeField]
        private float m_VaultingMaxHeight;
        [SerializeField]
        private float m_VaultingMaxTime;
        [SerializeField]
        private float m_VaultingForce;

        [Space]
        [SerializeField]
        private bool m_Debug;


        private bool m_VaultingActive;
        private float m_VaultingTimer;

        private static RaycastHit[] m_GroundHits = new RaycastHit[32];
        private static List<RaycastHit> m_ValidHits = new List<RaycastHit>(32);
        private static Collider[] m_Overlaps = new Collider[8];


        protected override void AbilityEnabled()
        {
            m_CharacterMovement.RegisterMover(this);
        }
        protected override void AbilityDisabled()
        {
            m_CharacterMovement.UnregisterMover(this);
        }


        void ICharacterMover.PreMovement(MovementSimulationState sim, float dt)
        {
            bool vaultingValid = DetectVaulting(sim, out RaycastHit vaultingHit);
            if (vaultingValid)
            {
                Vector3 vaultingDir = (vaultingHit.point - sim.Movement.FeetPos).FlattenY().normalized;
                if (!m_VaultingActive)
                {
                    if (Vector3.Dot(sim.ScheduledMove.normalized, vaultingDir) > 0.4 && sim.Movement.Rigidbody.linearVelocity.FlattenY().magnitude < 1 && sim.IsStableGrounded)
                    {
                        m_VaultingTimer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        m_VaultingTimer = 0;
                    }
                    if (m_VaultingTimer > 0.1)
                    {
                        m_VaultingActive = true;
                        m_VaultingTimer = 0;
                    }
                }

                if (m_VaultingActive)
                {
                    m_VaultingTimer += Time.fixedDeltaTime;
                    if (sim.Movement.Rigidbody.linearVelocity.y < 0.5f)
                    {
                        Vector3 vaultingForce = Vector3.up;
                        vaultingForce *= m_VaultingForce;
                        sim.Movement.Rigidbody.AddForce(vaultingForce, ForceMode.VelocityChange);
                    }
                    sim.ContactMove = vaultingDir;

                    if (MMath.Abs(vaultingHit.point.y - sim.Movement.FeetPos.y) < sim.Movement.CalculateFootprintRadius() * 0.005f
                        || m_VaultingTimer > m_VaultingMaxTime
                        || (m_VaultingTimer > 0.2 && sim.IsStableGrounded))
                    {
                        m_VaultingActive = false;
                    }
                }
            }
        }

        private bool DetectVaulting(MovementSimulationState sim, out RaycastHit vaultingHit)
        {
            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);

            float radius = sim.Movement.CalculateFootprintRadius() - CharacterMovement.SKIN_THICKNESS * 0.5f;
            float height = sim.Movement.CalculateBodyHeight();
            // TODO the top should start further up to account for ledges with ramps on top. These dont get picked up right now as the raycast starts inside those ramps
            Vector3 top = sim.Movement.FeetPos + sim.Movement.Rigidbody.rotation * Vector3.forward * m_VaultingDetectionDistance + Vector3.up * (m_VaultingMaxHeight + radius);
            Vector3 bottom = sim.Movement.FeetPos + sim.Movement.Rigidbody.rotation * Vector3.forward * m_VaultingDetectionDistance + Vector3.up * radius;
            if (Vector3.Dot(top - bottom, Vector3.down) > 0)
            {
                vaultingHit = new RaycastHit();
                return false;
            }
            int hitCount = Physics.SphereCastNonAlloc(top, radius, Vector3.down, m_GroundHits, (top - bottom).magnitude, layerMask, QueryTriggerInteraction.Ignore);

            if (m_Debug)
            {
                DebugHelper.DrawWireCapsule(top, bottom, radius, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            m_ValidHits.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                // Discard overlaps
                if (m_GroundHits[i].distance == 0)
                    continue;
                // Discard self collisions
                if (m_GroundHits[i].collider.transform == sim.Movement.Rigidbody.transform)
                    continue;
                if (m_GroundHits[i].collider.transform.IsChildOf(sim.Movement.Rigidbody.transform))
                    continue;
                if (m_GroundHits[i].normal.y <= 0)
                    continue;
                if (!sim.Movement.IsRaycastHitWalkable(m_GroundHits[i]))
                    continue;

                m_ValidHits.Add(m_GroundHits[i]);
            }
            m_ValidHits.Sort((a, b) => b.distance.CompareTo(a.distance));

            for (int i = 0; i < m_ValidHits.Count; i++)
            {
                Vector3 targetA = top + Vector3.down * (m_ValidHits[i].distance - 0.05f);
                if (m_Debug)
                {
                    DebugHelper.DrawWireSphere(m_ValidHits[i].point, 0.05f, Color.red);
                    DebugHelper.DrawWireSphere(top, 0.05f, Color.blue);
                    DebugHelper.DrawWireSphere(targetA, 0.05f, Color.green);
                }

                hitCount = Physics.OverlapCapsuleNonAlloc(targetA, targetA + Vector3.up * (height - radius * 2), radius, m_Overlaps, layerMask, QueryTriggerInteraction.Ignore);
                bool validVolume = true;
                for (int j = 0; j < hitCount; j++)
                {
                    // Discard self collisions
                    if (m_Overlaps[j].transform == sim.Movement.Rigidbody.transform)
                        continue;
                    if (m_Overlaps[j].transform.IsChildOf(sim.Movement.Rigidbody.transform))
                        continue;
                    validVolume = false;
                }
                if (validVolume)
                {
                    vaultingHit = m_ValidHits[i];
                    if (m_Debug)
                    {
                        DebugHelper.DrawWireCapsule(targetA, targetA + Vector3.up * (height - radius * 2), radius, Color.green);
                    }
                    return true;
                }
            }

            vaultingHit = new RaycastHit();
            return false;
        }
    }
}
