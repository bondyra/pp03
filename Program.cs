using System;
using System.Threading;
using System.Linq;

namespace JankielsProj
{
    public class Application
    {
        private readonly string filePath = "./positions.txt";
        private double hearingRange = 3.0d;
        private int cntr = 0;

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
            foreach(var j in jankiels)
            {
                j.monitor.SetNeighborCount(j.Neighbors.Count);
                j.monitor2.SetNeighborCount(j.Neighbors.Count);
                j.neighborCount = j.Neighbors.Count;
            }

            while(true)
            {
                WaitHandle[] waitHandles = new WaitHandle[jankiels.Length];
                for (int i = 0; i < jankiels.Length; i++)
                {
                    var j = i;
                    var handle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    var thread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        if(jankiels[j].playedAlready == false)
                            jankiels[j].Run();
                        handle.Set();
                    });

                    waitHandles[i] = handle;
                    thread.Start();
                }
                WaitHandle.WaitAll(waitHandles);
                //Console.WriteLine($"{cntr++}");
            }

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
