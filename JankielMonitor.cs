namespace JankielsProj
{
    public class JankielMonitor
    {
        private readonly object jankielLock = new object();
        private int counter;
        private int neighborCount = -1;
        private bool wasBReceived = false;
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void DecreaseCounter(bool bReceived)
        {
            //wywolywane przez asynchroniczne receivery
            lock (jankielLock)
            {
                wasBReceived = wasBReceived || bReceived;
                counter--;
                if (counter == 0){
                    mainThreadQueue.Pulse();
                }
            }
        }
        
        public bool WaitIfNecessary()
        {
            lock(jankielLock)
            {
                //wywolywane przez glowny watek algorytmu
                if (counter>0) //czekaj tylko jesli trzeba
                    mainThreadQueue.Wait(jankielLock);
                counter = neighborCount;
                bool wasB = wasBReceived;
                wasBReceived = false;
                return wasB;
            }
        }

        public void SetNeighborCount(int neighborCount)
        {
            lock(jankielLock)
            {
                //ustawiane na poczatek pracy algorytmu
                this.neighborCount = neighborCount;
                this.counter = this.neighborCount;
            }
        }
    }
}