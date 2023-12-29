using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.MaterialPropertyTool 
{
    [ExecuteAlways]
    public class MaterialPropertyBlockApplicator : MonoBehaviour
    {
        [SerializeField]
        private Renderer[] renderers;

        [SerializeField]
        private MaterialPropertyBlockGenerator blockGenerator;

        [SerializeField]
        private int targetIndex;

        [SerializeField]
        private PropBlockOverride[] overrides;

        private void Awake()
        {
            ApplyPropBlock(); // TODO more efficient
        }

        private void Reset()
        {
            if (renderers == null)
            {
                var r = this.GetComponent<MeshRenderer>();

                if (r != null)
                {
                    renderers = new Renderer[] { r }; //put own Mesh renderer as default if possible
                }
                else
                {
                    renderers = new Renderer[0];
                }
            }


            //Update the Block Generator when the Block changes
            MaterialPropertyBlockGenerator.OnBlockChanged -= OnGlobalBlockChange;
            MaterialPropertyBlockGenerator.OnBlockChanged += OnGlobalBlockChange;
        }

        private void OnDestroy()
        {
            MaterialPropertyBlockGenerator.OnBlockChanged -= OnGlobalBlockChange;

            RemovePropBlock();
        }

        private void OnValidate()
        {
            if (targetIndex < 0)
                targetIndex = 0;
            if (targetIndex > 127)
                targetIndex = 127;

            MaterialPropertyBlockGenerator.OnBlockChanged -= OnGlobalBlockChange;
            MaterialPropertyBlockGenerator.OnBlockChanged += OnGlobalBlockChange;

            ApplyPropBlock();
        }

        /// <summary>
        /// Update the Material Property Blocks when one of the Generators involved changes
        /// </summary>
        private void OnGlobalBlockChange(MaterialPropertyBlockGenerator obj)
        {
            if (blockGenerator != null && obj.Equals(blockGenerator))
            {
                ApplyPropBlock();
            }
        }


        private MaterialPropertyBlock GeneratePropBlock()
        {
            if(this == null || this?.transform == null)
            {
                OnDestroy();
                return null;
            }

            return blockGenerator?.GeneratePropertyBlock(this.transform.position.GetHashCode());
        }

        private void ApplyOverrides(ref MaterialPropertyBlock baseBlock)
        {
            if(baseBlock == null)
            {
                baseBlock = new MaterialPropertyBlock();
            }

            if (overrides == null)
                overrides = new PropBlockOverride[0];

            for (int i = 0; i < overrides.Length; i++)
            {
                var propName = overrides[i].targetProperty;

                switch (overrides[i].propertyTargetType)
                {
                     case PropBlockOverride.PropertyTargetType.Color:
                        {
                            baseBlock.SetColor(propName, overrides[i].GetOverrideValue(baseBlock.GetColor(propName)));
                        }
                        break;
                    case PropBlockOverride.PropertyTargetType.Float:
                        {
                            baseBlock.SetFloat(propName, overrides[i].GetOverrideValue(baseBlock.GetFloat(propName)));
                        }
                        break;
                    case PropBlockOverride.PropertyTargetType.Int:
                        {
                            baseBlock.SetInt(propName, overrides[i].GetOverrideValue(baseBlock.GetInt(propName)));
                        }
                        break;
                    case PropBlockOverride.PropertyTargetType.Matrix:
                        {
                            baseBlock.SetMatrix(propName, overrides[i].GetOverrideValue(baseBlock.GetMatrix(propName)));
                        }
                        break;
                    case PropBlockOverride.PropertyTargetType.Texture:
                        {
                            //Note: Texture mode does not support mixing
                            var tex = overrides[i].GetOverrideValue(null);
                            if (tex)
                            {
                                baseBlock.SetTexture(propName, tex);
                            }
                            //skip otherwise
                        }
                        break;
                    case PropBlockOverride.PropertyTargetType.Vector:
                        {
                            baseBlock.SetVector(propName, overrides[i].GetOverrideValue(baseBlock.GetVector(propName)));
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Generate a new PropertyBlock and push it to the renderer
        /// </summary>
        private void ApplyPropBlock()
        {
            foreach (var renderer in renderers)
            {
                if (this != null && (renderer == null || targetIndex < 0 || targetIndex > renderer.sharedMaterials.Length - 1))
                {
                    return;
                }

                var block = GeneratePropBlock();

                ApplyOverrides(ref block);

                renderer.SetPropertyBlock(block, targetIndex);
            }
        }

        /// <summary>
        /// Remove the currently active PropertyBlock
        /// </summary>
        private void RemovePropBlock()
        {
            foreach (var renderer in renderers)
            {
                if (this != null && (renderer == null || targetIndex < 0 || targetIndex > renderer.sharedMaterials.Length - 1))
                {
                    return;
                }

                renderer.SetPropertyBlock(null, targetIndex);
            }
        }

    }
}