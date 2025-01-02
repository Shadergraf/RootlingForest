using Manatea;
using Manatea.GameplaySystem;
using UnityEngine;

public class MaterialPropertyBlockLayerStack : MonoBehaviour
{
    [SerializeField]
    private Renderer[] m_Renderers;
    [SerializeField]
    private MaterialPropertyBlockFloatLayer[] m_LayerStack;

    private MaterialPropertyBlock m_PropertyBlock;


    private void Awake()
    {
        m_PropertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        foreach (var renderer in m_Renderers)
        {
            renderer.SetPropertyBlock(m_PropertyBlock);
        }
    }


    private void Update()
    {
        UpdatePropertyBlock();

        foreach (var renderer in m_Renderers)
        {
            renderer.SetPropertyBlock(m_PropertyBlock);
        }
    }

    private void UpdatePropertyBlock()
    {
        m_PropertyBlock.Clear();
        for (int i = 0; i < m_LayerStack.Length; i++)
        {
            var layer = m_LayerStack[i];
            if (!layer.enabled)
                continue;
            m_PropertyBlock.SetFloat(layer.MaterialPropertyName, layer.Evaluated(m_PropertyBlock.GetFloat(layer.MaterialPropertyName)));
        }
    }
}
