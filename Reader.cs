using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace JankielsProj
{
    public class Reader
    {
        private bool init = true;
        private string path;
        private int jankielsCount = -1;
        public Reader(string path)
        {
            this.path = path;
        }

        public Jankiel[] Read ()
        {
            List<Jankiel> jankiels  = new List<Jankiel>();
            int cnt = 0;
            foreach (var line in File.ReadLines(this.path))
            {
                if (this.init)
                {
                    this.init = false;
                    this.jankielsCount = int.Parse(line);
                }
                else
                {
                    var chunks = line.Split(' ');
                    Debug.Assert(chunks != null && chunks.Length == 2);
                    int x = int.Parse(chunks[0]);
                    int y = int.Parse(chunks[1]);
                    cnt++;
                    jankiels.Add(new Jankiel(x, y, $"Jankiel({x},{y})", jankielsCount));
                }
            }
            Debug.Assert(jankiels.Count == this.jankielsCount);
            return jankiels.ToArray();
        }
    }
}