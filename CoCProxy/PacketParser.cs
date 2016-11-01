using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

//https://github.com/clanner/cocdp/wiki/Protocol#packet-format

namespace CoCProxy007
{
    public struct PacketParser
    {
        public static ushort GetPacketID(byte[] buffer, int length)
        {
            byte[] packetId = new byte[2];

            using (MemoryStream memory = new MemoryStream(buffer, 0 , length))
            {
                // read packet id from buffer
                memory.Read(packetId, 0, 2);
            }

            return (ushort)((packetId[0] << 8) | packetId[1]);
        }
    }


}
