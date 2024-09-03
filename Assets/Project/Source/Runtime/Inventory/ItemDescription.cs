using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "RootlingForest/ItemDescription", fileName = "Item Description")]
public class ItemDescription : ScriptableObject
{
    [SerializeField]
    private LocalizedString Name;
}
