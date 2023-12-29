using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.MaterialPropertyTool
{
    public class MaterialPropertyDescription<T>
    {
        /// <summary>
        /// The name of the Property
        /// </summary>
        public string name;

        public PropertyValue propertyType;

        public T constValue;

        [SerializeField]
        private PropertyGenerator<T> generator;

        public PropertyGenerator<T> Generator
        {
            get
            {
                return (propertyType == PropertyValue.Generator) ? generator : null;
            }
        }

        public T GetValue(int seed)
        {
            if(this.propertyType == PropertyValue.Constant || generator == null)
            {
                return constValue;
            }
            else
            {
                return generator.GeneratePropertyValue(seed);
            }
        }

    }


    // Fun Serialization stuff for Unity


    //[System.Serializable]
    //public class BufferPropertyDescription : PropertyDescription<ComputeBuffer>
    //{

    //}




    [System.Serializable]
    public class ColorPropertyDescription : MaterialPropertyDescription<Color>
    {

    }




    [System.Serializable]
    public class FloatPropertyDescription : MaterialPropertyDescription<float>
    {

    }

    //[System.Serializable]
    //public class FloatArrayPropertyDescription : PropertyDescription<float[]>
    //{

    //}




    [System.Serializable]
    public class IntPropertyDescription : MaterialPropertyDescription<int>
    {

    }




    [System.Serializable]
    public class MatrixPropertyDescription : MaterialPropertyDescription<Matrix4x4>
    {

    }

    //[System.Serializable]
    //public class MatrixArrayPropertyDescription : PropertyDescription<Matrix4x4[]>
    //{

    //}




    [System.Serializable]
    public class TexturePropertyDescription : MaterialPropertyDescription<Texture2D>
    {

    }




    [System.Serializable]
    public class VectorPropertyDescription : MaterialPropertyDescription<Vector4>
    {

    }

    //[System.Serializable]
    //public class VectorArrayPropertyDescription : PropertyDescription<Vector4[]>
    //{

    //}

}