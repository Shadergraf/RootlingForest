using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.SceneManagement
{
    [Serializable]
    public struct SceneReference : IEquatable<SceneReference>
    {
        [SerializeField]
        private string m_Guid;
        public string Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }


        public SceneReference(string guid)
        {
            m_Guid = guid;
        }


        public bool Equals(SceneReference other)
        {
            return m_Guid.Equals(other.m_Guid);
        }
        public static bool operator ==(SceneReference c1, SceneReference c2)
        {
            return c1.Equals(c2);
        }
        public static bool operator !=(SceneReference c1, SceneReference c2)
        {
            return !c1.Equals(c2);
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals((SceneReference)obj);
        }
        public override int GetHashCode()
        {
            return m_Guid.GetHashCode();
        }
    }
}
