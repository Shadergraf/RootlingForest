using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    public abstract class BaseCharacterController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody m_Self;
        [FormerlySerializedAs("CharacterMovement")]
        [SerializeField]
        private CharacterMovement m_CharacterMovement;

        public Rigidbody Self => m_Self;
        public CharacterMovement CharacterMovement => m_CharacterMovement;
    }
}
