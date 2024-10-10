using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Manatea.RootlingForest
{
    public class VariationSelector : MonoBehaviour
    {
        [SerializeField]
        private List<VariationElement> m_Variations;


        private void Start()
        {
            int random = Random.Range(0, m_Variations.Count);
            for (int i = 0; i < m_Variations.Count; i++)
            {
                bool chosen = i == random;
                for (int j = 0; j < m_Variations[i].ObjectsToEnable.Count; j++)
                {
                    m_Variations[i].ObjectsToEnable[j].SetActive(chosen);
                    if (chosen)
                        m_Variations[i].OnElementChosen.Invoke();
                }
            }
        }


        [System.Serializable]
        public class VariationElement
        {
            public List<GameObject> ObjectsToEnable;
            public UnityEvent OnElementChosen;
        }
    }
}
