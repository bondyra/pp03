namespace JankielsProj
{
    public class JankielMonitor
    {
        private readonly object jankielLock = new object();
        private int counter;
        private int neighborCount = -1;
        private int bCount = 0;
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void DecreaseCounter (bool B, string queuename)
        {
            lock (jankielLock)
            {
                if (B) bCount++;
                //System.Console.WriteLine($"{queuename}: decrease counter from {counter}.");
                counter--;
                if (counter == 0){
                    System.Console.WriteLine($"{queuename} counter :{counter}  pulsam");
                    mainThreadQueue.Pulse();
                }
            }
        }
        
        public int WaitIfNecessary(string queue)
        {
            lock(jankielLock)
            {
                //System.Console.WriteLine($"{queue} : waitIfNecessary for {neighborCount}, counter {counter}");
                if (counter>0)
                    mainThreadQueue.Wait(jankielLock);
                counter = neighborCount;
                int B = bCount;
                bCount = 0;
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