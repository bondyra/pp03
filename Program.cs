using System;
using System.Threading;

namespace JankielsProj
{
    public class Jankiels
    {
        private readonly string filePath = "./positions.txt";
        private double hearingRange = 3.0d;

        private static double euclideanDistance (Jankiel a, Jankiel b)
        {
            return Math.Sqrt(Math.Pow(b.X-a.X,2) + Math.Pow(b.Y-a.Y,2));
        }

        private void buildTopology(Jankiel[] jankiels)
        {
            for (int i=0;i<jankiels.Length;i++)
            {
                for (int j=0;j<jankiels.Length;j++)
                {
                    if (i<j && euclideanDistance(jankiels[i],jankiels[j]) <= hearingRange)
                    {
                        jankiels[i].addNeighbor(jankiels[j]);
                        jankiels[j].addNeighbor(jankiels[i]);
                    }
                }
            }
        }
        public void run ()
        {
            JankielsReader reader = new JankielsReader(this.filePath);
            var jankiels = reader.Read();
            buildTopology(jankiels);
            foreach (var jankiel in jankiels)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    jankiel.run();
                }).Start();
            }
            while(true);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            (new Jankiels()).run();
        }
    }
}
