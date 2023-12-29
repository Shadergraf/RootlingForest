using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Manatea.ModelShuffle
{
    /// <summary>
    /// A little Tool used to shuffle root nodes of a model Prefab
    /// </summary>
    public class ModelShuffler : MonoBehaviour
    {
#if UNITY_EDITOR

        [SerializeField]
        public List<ShuffleCandidate> candidates;

        [System.Serializable]
        public class ShuffleCandidate
        {
            public GameObject obj;
            [Range(0, 1)]
            public float relativeProbability = 1;

            public ShuffleCandidate(GameObject g)
            {
                obj = g;
                relativeProbability = 1;
            }

        }



        /// <summary>
        /// Pick one candidate at random.
        /// Turn off all others
        /// </summary>
        public void Shuffle()
        {

            float pSum = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                pSum += candidates[i].relativeProbability;
            }


            // if weights are 0, pick first element
            if(pSum <= 0.001F)
            {
                Debug.LogWarning("Picked first element, but at least one element should have weight!", this.gameObject);

                for (int i = 1; i < candidates.Count; i++)
                {
                    candidates[i].obj.SetActive(false);
                }

                candidates[0].obj.SetActive(true);
                return;
            }



            float pick = Random.Range(0, pSum);
            float counter = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                if(pick > counter)
                {
                    //if after adding the relativeProbability to the counter we're lower than the picked value, this object is in range
                    counter += candidates[i].relativeProbability;

                    if(pick <= counter)
                    {
                        candidates[i].obj.SetActive(true);
                        continue;
                    }
                    else
                    {
                        // we're not there yet
                        candidates[i].obj.SetActive(false);
                    }
                }
                else
                {
                    //we have already passed the pick
                    candidates[i].obj.SetActive(false);
                }

            }

        }


        void OnValidate()
        {
            if (IsOnDuplicate())
            {
                Shuffle();
            }

        }

        private bool IsOnDuplicate()
        {
            Event e = Event.current;

            return e != null && e.type == EventType.ExecuteCommand && e.commandName == "Duplicate";
        }

#endif
    }
}
