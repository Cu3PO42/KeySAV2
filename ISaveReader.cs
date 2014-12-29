using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KeySAV2
{
    public interface ISaveReader
    {
        string KeyName { get;  }
        void scanSlots(ushort from, ushort to);
        void scanSlots();
        void scanSlots(ushort pos);

        Structures.PKX? getPkx(ushort pos);
    }
}
