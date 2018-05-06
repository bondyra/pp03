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
            foreach(var j in jankiels)
            {
                //ustawienie liczby sąsiadów którzy nie grali na pierwszą rundę
                j.firstExchangeMonitor.SetNeighborCount(j.Neighbors.Count);
                j.secondExchangeMonitor.SetNeighborCount(j.Neighbors.Count);
                j.neighborCount = j.Neighbors.Count;
            }
            int round = 0;
            while(true)
            {
                int notPlayedCount = jankiels.Select(x=> x.playedAlready ? 0 : 1).Sum();
                if (notPlayedCount == 0)//można skończyć program
                    break;
                //kolejne przejście pętli to kolejna runda 
                Console.WriteLine($"*** RUNDA {++round} ***");

                WaitHandle[] waitHandles = new WaitHandle[jankiels.Length];
                for (int i = 0; i < jankiels.Length; i++)
                {
                    var j = i;
                    var handle = new EventWaitHandle(false, EventResetMode.ManualReset);

                    //praca każdego z jankiela (który jeszcze nie grał):
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
                //czekanie na zakończenie grania/czekania na granie:
                WaitHandle.WaitAll(waitHandles);
            }
            Console.WriteLine("Wszyscy Jankiele zagrali - koniec.");
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
