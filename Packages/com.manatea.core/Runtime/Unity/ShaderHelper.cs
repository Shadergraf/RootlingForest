using System.Collections;
using UnityEngine;

namespace Manatea
{
    public static class ShaderHelper
    {

        private static int m_GlobalTimeId = Shader.PropertyToID("_Time");
        public static int GlobalTimeId => m_GlobalTimeId;
        public static Vector4 GlobalTimeValue => new Vector4(Time.time / 20f, Time.time, Time.time * 2f, Time.time * 3f);

        private static int m_GlobalSinTimeId = Shader.PropertyToID("_SinTime");
        public static int GlobalSinTimeId => m_GlobalSinTimeId;
        public static Vector4 GlobalSinTimeValue => new Vector4(MMath.Sin(Time.time / 8f), MMath.Sin(Time.time / 4f), MMath.Sin(Time.time / 2f), MMath.Sin(Time.time));


        private static int m_GlobalCosTimeId = Shader.PropertyToID("_CosTime");
        public static int GlobalCosTimeId => m_GlobalCosTimeId;
        public static Vector4 GlobalCosTimeValue => new Vector4(MMath.Cos(Time.time / 8f), MMath.Cos(Time.time / 4f), MMath.Cos(Time.time / 2f), MMath.Cos(Time.time));


        private static int m_GlobalDeltaTimeId = Shader.PropertyToID("_DeltaTime");
        public static int GlobalDeltaTimeId => m_GlobalDeltaTimeId;
        public static Vector4 GlobalDeltaTimeValue => new Vector4(Time.deltaTime, 1f / Time.deltaTime, Time.smoothDeltaTime, 1f / Time.smoothDeltaTime);

    }
}