using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeySAV2
{
    interface ISaveReader
    {
        void scanSlots(ushort from, ushort to);
        void scanSlots();
        void scanSlots(ushort pos);

        Structures.PKX? getPkx(ushort pos);
    }
}
