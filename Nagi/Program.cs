using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nagi
{
    class Program
    {
        static void Main(string[] args)
        {
            var watchDog = new WatchDog();
            watchDog.Start();
            new ConsoleInteraction().Run(watchDog);
        }
    }
}
