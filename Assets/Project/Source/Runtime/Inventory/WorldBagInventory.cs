using Manatea.GameplaySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class WorldBagInventory : MonoBehaviour
    {
        [SerializeField]
        private GameplayTagFilter m_TagFilter;
        [SerializeField]
        private GameplayEffect m_InventoryEffect;
        [SerializeField]
        private GameObject m_BagPrefab;

        [SerializeField]
        private GrabAbility m_GrabAbility;
        [SerializeField]
        private GameObject m_BagSpawnPoint;


        private WorldBag m_CurrentBag;

        private List<GameObject> m_Items = new();
        private List<GameObject> m_ItemConstraints = new();
        private List<GameplayEffectInstance> m_ItemEffects = new();

        public WorldBag WorldBag => m_CurrentBag;

        public static int GlobalBagCount
        { get; private set; }


        private void Awake()
        {
            SpawnBag();

            m_CurrentBag.Inventory = this;

            GlobalBagCount++;
        }

        private void OnDestroy()
        {
            DestroyBag();

            GlobalBagCount--;
        }


        private void SpawnBag()
        {
            m_CurrentBag = Instantiate(m_BagPrefab).GetComponent<WorldBag>();

            m_CurrentBag.transform.position = new Vector3(0, -250, GlobalBagCount * 10);
        }
        private void DestroyBag()
        {
            // Could have already been destroyed by unity
            if (m_CurrentBag)
                Destroy(m_CurrentBag.gameObject);
        }

        public bool CouldAddItem(GameObject item)
        {
            var tagOwner = item.GetComponent<GameplayTagOwner>();
            if (!tagOwner || !tagOwner.SatisfiesTagFilter(m_TagFilter))
                return false;

            return true;
        }

        public void AddItem(GameObject item)
        {
            if (!CouldAddItem(item))
                return;

            item.transform.SetParent(m_CurrentBag.ItemContainer);
            item.transform.position = m_CurrentBag.transform.position;
            item.transform.rotation = Quaternion.identity;

            Rigidbody itemRigid = item.GetComponent<Rigidbody>();
            itemRigid.linearVelocity = Vector3.zero;
            itemRigid.angularVelocity = Vector3.zero;

            GameObject itemConstraint = Instantiate(m_CurrentBag.ItemConstraintPrefab);
            m_ItemConstraints.Add(itemConstraint);
            itemConstraint.transform.SetParent(m_CurrentBag.ItemContainer);
            itemConstraint.transform.position = m_CurrentBag.transform.position;
            itemConstraint.transform.rotation = Quaternion.identity;

            Physics.SyncTransforms();

            ConfigurableJoint constraintJoint = itemConstraint.GetComponent<ConfigurableJoint>();
            constraintJoint.connectedBody = itemRigid;

            GameplayEffectInstance itemEffect = item.GetComponent<GameplayEffectOwner>().AddEffect(m_InventoryEffect);
            m_ItemEffects.Add(itemEffect);

            m_Items.Add(item);
        }

        public void RemoveItem(GameObject item)
        {
            if (!m_Items.Contains(item))
                return;

            int id = m_Items.IndexOf(item);

            Destroy(m_ItemConstraints[id].gameObject);

            item.transform.SetParent(null);
            Transform target = m_BagSpawnPoint.transform;
            item.transform.position = target.transform.position;
            item.transform.rotation = target.transform.rotation;

            Rigidbody itemRigid = item.GetComponent<Rigidbody>();
            itemRigid.linearVelocity = Vector3.zero;
            itemRigid.angularVelocity = Vector3.zero;

            Physics.SyncTransforms();


            item.GetComponent<GameplayEffectOwner>().RemoveEffect(m_ItemEffects[id]);

            m_ItemConstraints.RemoveAt(id);
            m_ItemEffects.RemoveAt(id);
            m_Items.RemoveAt(id);

            m_GrabAbility.enabled = false;
            m_GrabAbility.Target = item.GetComponent<Rigidbody>();
            m_GrabAbility.enabled = true;
        }

        public bool Contains(GameObject item)
        {
            return m_Items.Contains(item);
        }
    }
}
