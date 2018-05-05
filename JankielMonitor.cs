namespace JankielsProj
{
    public class JankielMonitor
    {
        private readonly object jankielLock = new object();
        private int counter;
        private int neighborCount = -1;
        private bool bCount = false;
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void DecreaseCounter(bool B, string queuename)
        {
            lock (jankielLock)
            {
                bCount = bCount || B;
                //System.Console.WriteLine($"{queuename}: decrease counter from {counter}.");
                counter--;
                if (counter == 0){
                   // System.Console.WriteLine($"{queuename} counter :{counter}  pulsam");
                    mainThreadQueue.Pulse();
                }
            }
        }
        
        public bool WaitIfNecessary(string queue)
        {
            lock(jankielLock)
            {
                System.Console.WriteLine($"{queue} : waitIfNecessary for {neighborCount}, counter {counter}");
                if (counter>0)
                    mainThreadQueue.Wait(jankielLock);
                counter = neighborCount;
                bool B = bCount;
                bCount = false;
                return B; 
            }
        }

        public void SetNeighborCount(int neighborCount)
        {
            lock(jankielLock)
            {
                this.neighborCount = neighborCount;
                this.counter = this.neighborCount;
            }
        }
    }
}