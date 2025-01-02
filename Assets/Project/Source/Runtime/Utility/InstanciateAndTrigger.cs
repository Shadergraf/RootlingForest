using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Project.Source.Runtime.Utility
{
    public class InstanciateAndTrigger : MonoBehaviour
    {
        [SerializeField]
        private bool m_InstanciateInWorldSpace = true;
        [SerializeField]
        private UnityEvent m_Trigger;


        public void Instanciate()
        {
            GameObject go;
            if (m_InstanciateInWorldSpace)
                go = GameObject.Instantiate(gameObject, null);
            else
                go = GameObject.Instantiate(gameObject);
            go.transform.position = gameObject.transform.position;
            go.transform.rotation = gameObject.transform.rotation;
            go.transform.localScale = gameObject.transform.localScale;
            go.GetComponent<InstanciateAndTrigger>().m_Trigger.Invoke();
        }
    }
}