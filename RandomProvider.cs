using System;

namespace JankielsProj
{
    public static class RandomProvider
    {
        //synchroniczny dostęp do losowanej liczby (C# Random nie jest thread-safe)
        private static object obj = new object();
        private static Random r = new Random(997);

        public static bool Binom(double prob)
        {
            lock (obj)
            {
                double val = r.NextDouble();
                return val < prob;
            }            
        }
    }
}