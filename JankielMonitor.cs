using System.Collections.Generic;

namespace JankielsProj
{
    public class JankielMonitor
    {
        private readonly object jankielLock = new object();
        private int counter;
        private int neighborCount = -1;
        private bool bCount = false;
        private Dictionary<int, int> roundMap = new Dictionary<int, int>();
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void DecreaseCounter(bool B, string queuename, int roundNumber)
        {
            lock (jankielLock)
            {
                if (!roundMap.ContainsKey(roundNumber))
                {
                    roundMap.Add(roundNumber, neighborCount);
                //    System.Console.WriteLine($"Jankiel {queuename} ustawia runde {roundNumber} na {roundMap[roundNumber]}");
                }
                roundMap[roundNumber]--;
                bCount = bCount || B;
                if (roundMap[roundNumber] == 0)
                {
                    
                 //   System.Console.WriteLine($" {queuename}Koniec rundy {roundNumber}");
                    mainThreadQueue.Pulse();
                }
            }
        }

        public bool WaitIfNecessary(string queue, int roundNumber)
        {
            lock (jankielLock)
            {
                //System.Console.WriteLine($"{queue} : waitIfNecessary for {neighborCount}, counter {counter}");
              //  System.Console.WriteLine($"{queue} Biore runde {roundNumber} {roundMap.Count}");
                if (roundMap.ContainsKey(roundNumber) && roundMap[roundNumber] > 0)
                {
                 //   System.Console.WriteLine($"Czekam bo runda {roundNumber} trwa");
                    mainThreadQueue.Wait(jankielLock);
                }
           
                bool B = bCount;
                bCount = false;
                return B;
            }
        }

        public void SetNeighborCount(int neighborCount)
        {
            lock (jankielLock)
            {
                this.neighborCount = neighborCount;
                this.counter = this.neighborCount;
            }
        }
    }
}