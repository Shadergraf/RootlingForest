using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    public abstract class CharacterController : MonoBehaviour
    {
        [FormerlySerializedAs("CharacterMovement")]
        [SerializeField]
        private CharacterMovement m_CharacterMovement;

        public CharacterMovement CharacterMovement => m_CharacterMovement;
    }
}
