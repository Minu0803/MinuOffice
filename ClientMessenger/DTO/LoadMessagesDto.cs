using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.DTO
{
    public class LoadMessagesRequestDto
    {
        public int RoomId { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }

}
