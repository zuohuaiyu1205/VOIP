using Montage.Voip.Rtp;
using System.Collections.Generic;

namespace Montage.Voip
{
    internal class PacketComparer : IComparer<RtpPacket>
    {
        public virtual int Compare(RtpPacket x, RtpPacket y)
        {
            return (int)((ushort)x.SequenceNumber - (ushort)y.SequenceNumber);
        }
    }
}
