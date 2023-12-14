using System.Diagnostics;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    public class StageState
    {
        public StageName StageName { get; }

        private readonly Stopwatch stopwatch;
        public long StayTimeSeconds
        {
            get
            {
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds / 1000;
            }
        }

        public int NumberOfTextChatsSent { get; private set; }

        public StageState(StageName stageName)
        {
            StageName = stageName;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void CountUpTextChats() => NumberOfTextChatsSent++;
    }
}
