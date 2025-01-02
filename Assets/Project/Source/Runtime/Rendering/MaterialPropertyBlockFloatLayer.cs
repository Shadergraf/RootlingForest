using Manatea;
using UnityEditor;
using UnityEngine;

public class MaterialPropertyBlockFloatLayer : MonoBehaviour
{
    [SerializeField]
    private string m_MaterialPropertyName;
    [SerializeField]
    private FloatBlend m_BlendMode;
    [SerializeField]
    [Range(0, 1)]
    private float m_BlendFactor = 1;

    [SerializeField]
    private float m_Value;

    public string MaterialPropertyName => m_MaterialPropertyName;
    public float Value
    {
        get => m_Value;
        set => m_Value = value;
    }


    public enum FloatBlend
    {
        Mix = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3,
        Divide = 4,
    }

    public float Evaluated(float previousValue)
    {
        return EvaluateBlend(previousValue, m_Value, m_BlendFactor, m_BlendMode);
    }

    public static float EvaluateBlend(float A, float B, float blendFactor, FloatBlend blendMode)
    {
        switch (blendMode)
        {
            case FloatBlend.Mix:
                return MMath.Lerp(A, B, blendFactor);
            case FloatBlend.Add:
                return A + MMath.Lerp(0, B, blendFactor);
            case FloatBlend.Subtract:
                return A - MMath.Lerp(0, B, blendFactor);
            case FloatBlend.Multiply:
                return A * MMath.Lerp(1, B, blendFactor);
            case FloatBlend.Divide:
                return A * MMath.Lerp(1, B, blendFactor);
        }

        throw new System.Exception();
    }
}