using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Manatea
{
    [Serializable]
    public class WeightedPropabilitySpace<T>
    {
        [SerializeField]
        protected List<WeightedPropabilityElement<T>> m_Propabilities = new List<WeightedPropabilityElement<T>>();

        public float GetTotalWeights() => m_Propabilities.Sum(p => p.Weight);
        public int Count => m_Propabilities.Count;


        public T SampleOrDefault()
        {
            if (Count == 0)
                return default(T);

            return UniformSample(Random.value);
        }
        public T SampleOrDefault(float point)
        {
            if (Count == 0)
                return default(T);

            return UniformSample(point);
        }

        /// <summary>
        /// Sample a random point within the propability space
        /// </summary>
        public T RandomSample()
        {
            return UniformSample(Random.value);
        }
        /// <summary>
        /// Sample a uniform point within the propability space
        /// </summary>
        /// <param name="point">A point from 0..1 spanning the entire propability space</param>
        /// <returns>The element at that uniform weight point across the propability space</returns>
        public T UniformSample(float point)
        {
            if (point <= 0)
                return m_Propabilities[0].Data;
            int count = Count;
            if (point >= 1)
                return m_Propabilities[count - 1].Data;

            point *= GetTotalWeights();

            float weight = 0;
            for (int i = 0; i < count; i++)
            {
                var element = m_Propabilities[i];
                weight += element.Weight;
                if (weight >= point)
                    return m_Propabilities[i].Data;
            }

            return m_Propabilities[count - 1].Data;
        }


        public void AddElement(float weight, T data)
        {
            WeightedPropabilityElement<T> element = new WeightedPropabilityElement<T>();
            element.Weight = weight;
            element.Data = data;
            m_Propabilities.Add(element);
        }

        public void RemoveElementAt(int index) => m_Propabilities.RemoveAt(index);

        public void ClearElements() => m_Propabilities.Clear();

        public T GetDataOfElementAt(int index) => m_Propabilities[index].Data;
        public float GetWeightOfElementAt(int index) => m_Propabilities[index].Weight;

        public void SetDataOfElementAt(int index, T newData)
        {
            var element = m_Propabilities[index];
            element.Data = newData;
            m_Propabilities[index] = element;
        }

        public void SetWeightOfElement(T elementData, float newWeight)
        {
            int index = m_Propabilities.FindIndex(e => e.Data.Equals(elementData));
            SetWeightOfElementAt(index, newWeight);
        }
        public void SetWeightOfElementAt(int index, float newWeight)
        {
            var element = m_Propabilities[index];
            element.Weight = newWeight;
            m_Propabilities[index] = element;
        }


        [Serializable]
        public struct WeightedPropabilityElement<U>
        {
            public U Data;
            public float Weight;
        }
    }
}