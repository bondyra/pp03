using System;

namespace JankielsProj
{
    public class JankielPlayMonitor
    {
        private readonly object jankielPlayLock = new object();
        private bool shouldWait = true;
        private ConditionVariable mainThreadQueue = new ConditionVariable();

        //ENTRIES
        public void NotifyEndPlay()
        {
            lock (jankielPlayLock)
            {
                shouldWait = false;
                mainThreadQueue.Pulse();
            }
        }

        public void WaitForEndPlaying()
        {
            lock (jankielPlayLock)
            {
                if (shouldWait)
                    mainThreadQueue.Wait(jankielPlayLock);
                shouldWait = true;
            }
        }
    }
}