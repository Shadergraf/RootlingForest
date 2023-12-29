using System;

namespace Manatea
{
    [Serializable]
    public struct Tuple2<A, B>
    {
        public A Element1;
        public B Element2;
    }

    [Serializable]
    public struct Tuple3<A, B, C>
    {
        public A Element1;
        public B Element2;
        public C Element3;
    }

    [Serializable]
    public struct Tuple4<A, B, C, D>
    {
        public A Element1;
        public B Element2;
        public C Element3;
        public D Element4;
    }
}