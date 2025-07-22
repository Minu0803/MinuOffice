using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.Common
{
    public interface IRoomSender
    {
        void RoomCreated(RoomModel room);
    }
}
