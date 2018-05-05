namespace JankielsProj
{
    public class JankielPlayMonitor
    {
        private readonly object jankielPlayLock = new object();
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void NotifyEndPlay ()
        {
            lock (jankielPlayLock)
            {
                 mainThreadQueue.Pulse();
            }
        }

        public void WaitForEndPlaying()
        {
            lock (jankielPlayLock)
            {
                mainThreadQueue.Wait(jankielPlayLock);
            }
        }

    }
}