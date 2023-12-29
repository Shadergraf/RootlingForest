using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.MaterialPropertyTool
{

    [CreateAssetMenu(fileName ="MaterialPropertyBlock Generator", menuName ="Manatea/Rendering/MatPropBlock/MaterialPropertyGenerator", order = -1000)]
    public class MaterialPropertyBlockGenerator : ScriptableObject
    {

        public static event System.Action<MaterialPropertyBlockGenerator> OnBlockChanged;




        //TODO Cached Block support

        //public List<BufferPropertyDescription> bufferProps;

        public List<ColorPropertyDescription> colorProps = new List<ColorPropertyDescription>();

        public List<FloatPropertyDescription> floatProps = new List<FloatPropertyDescription>();

        //public List<FloatArrayPropertyDescription> floatArrayProps;

        public List<IntPropertyDescription> intProps = new List<IntPropertyDescription>();

        public List<MatrixPropertyDescription> matrixProps = new List<MatrixPropertyDescription>();

        //public List<MatrixArrayPropertyDescription> matrixArrayProps;

        public List<TexturePropertyDescription> textureProps = new List<TexturePropertyDescription>();

        public List<VectorPropertyDescription> vectorProps = new List<VectorPropertyDescription>();

        //public List<VectorArrayPropertyDescription> vectorArrayProps;



        private void OnValidate()
        {
            SubscribeToGenerators();

            OnBlockChanged?.Invoke(this);
        }

        private void SubscribeToGenerators()
        {
            PropertyGenerator.OnGeneratorChanged -= PropertyGenerator_OnGeneratorChanged;
            PropertyGenerator.OnGeneratorChanged += PropertyGenerator_OnGeneratorChanged;
        }

        private void PropertyGenerator_OnGeneratorChanged(ScriptableObject obj)
        {
            try
            {
                if (GetGenerators().Contains((PropertyGenerator)obj))
                {
                    OnBlockChanged?.Invoke(this);
                }
            }
            catch
            {
                //Handle Exception
            }
        }

        private List<PropertyGenerator> GetGenerators()
        {
            var generators = new List<PropertyGenerator>();

            //foreach (var item in bufferProps)
            //{
            //    if(item.propertyType == PropertyType.Generator && item.Generator != null)
            //    {
            //        generators.Add(item.Generator);
            //    }
            //}

            foreach (var item in colorProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }

            foreach (var item in floatProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }

            //foreach (var item in floatArrayProps)
            //{
            //    if (item.propertyType == PropertyType.Generator && item.Generator != null)
            //    {
            //        generators.Add(item.Generator);
            //    }
            //}

            foreach (var item in intProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }

            foreach (var item in matrixProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }

            foreach (var item in textureProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }

            foreach (var item in vectorProps)
            {
                if (item.propertyType == PropertyValue.Generator && item.Generator != null)
                {
                    generators.Add(item.Generator);
                }
            }


            //foreach (var item in vectorArrayProps)
            //{
            //    if (item.propertyType == PropertyType.Generator && item.Generator != null)
            //    {
            //        generators.Add(item.Generator);
            //    }
            //}


            return generators;
        }



        public MaterialPropertyBlock GeneratePropertyBlock(int seed)
        {
            var block = new MaterialPropertyBlock();

            // Add values for all list items

            //foreach (var item in bufferProps)
            //{
            //    block.SetBuffer(item.name, item.GetValue(seed));
            //}

            foreach (var item in colorProps)
            {
                block.SetColor(item.name, item.GetValue(seed));
            }

            foreach (var item in floatProps)
            {
                block.SetFloat(item.name, item.GetValue(seed));
            }

            //foreach (var item in floatArrayProps)
            //{
            //    block.SetFloatArray(item.name, item.GetValue(seed));
            //}

            foreach (var item in intProps)
            {
                block.SetInt(item.name, item.GetValue(seed));
            }

            foreach (var item in matrixProps)
            {
                block.SetMatrix(item.name, item.GetValue(seed));
            }

            foreach (var item in textureProps)
            {
                var value = item.GetValue(seed);

                if (item.name != null && value != null)
                    block.SetTexture(item.name, value);
            }

            foreach (var item in vectorProps)
            {
                block.SetVector(item.name, item.GetValue(seed));
            }


            //foreach (var item in vectorArrayProps)
            //{
            //    block.SetVectorArray(item.name, item.GetValue(seed));
            //}



            return block;
        }

    }

}