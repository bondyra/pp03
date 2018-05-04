namespace JankielsProj
{
    public class JankielPlayMonitor
    {
        private readonly object jankielPlayLock = new object();
        int nPlaying = -1;
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void NotifyEndPlay ()
        {
            lock (jankielPlayLock)
            {
                if (nPlaying == -1) throw new System.Exception("pizda");
                if (--nPlaying == 0)
                    mainThreadQueue.Pulse();
            }
        }

        public void WaitForEndPlaying(int anyonePlaying)
        {
            lock (jankielPlayLock)
            {
                nPlaying = anyonePlaying;
                mainThreadQueue.Wait(jankielPlayLock);
                nPlaying = -1;
            }
        }

    }
}