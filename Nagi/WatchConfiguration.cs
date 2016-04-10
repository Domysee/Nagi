using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nagi
{
    public class WatchConfiguration
    {
        public int Id { get;}
        public string Folder { get; }
        public IIntegration Integration { get; }

        public WatchConfiguration(int id, string folder, IIntegration integration)
        {
            Id = id;
            Folder = folder;
            Integration = integration;
        }
    }
}
