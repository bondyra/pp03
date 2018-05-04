using System;
using System.Threading;
using System.Linq;

namespace JankielsProj
{
    public class Application
    {
        private readonly string filePath = "./positions.txt";
        private double hearingRange = 3.0d;

        private static double euclideanDistance (Jankiel a, Jankiel b)
        {
            return Math.Sqrt(Math.Pow(b.X-a.X,2) + Math.Pow(b.Y-a.Y,2));
        }

        private void buildGraph(Jankiel[] jankiels)
        {
            for (int i=0;i<jankiels.Length;i++)
            {
                for (int j=0;j<jankiels.Length;j++)
                {
                    if (i<j && euclideanDistance(jankiels[i],jankiels[j]) <= hearingRange)
                    {
                        jankiels[i].AddNeighbor(jankiels[j]);
                        jankiels[j].AddNeighbor(jankiels[i]);
                    }
                }
            }
            int d = jankiels.Select(x=> x.Neighbors.Count).Max();
            foreach (var jankiel in jankiels)
                jankiel.D = d;
        }
        public void Run ()
        {
            Reader reader = new Reader(this.filePath);
            var jankiels = reader.Read();
            buildGraph(jankiels);
            foreach (var jankiel in jankiels)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    jankiel.Run();
                }).Start();
            }
            while(true);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            (new Application()).Run();
        }
    }
}
