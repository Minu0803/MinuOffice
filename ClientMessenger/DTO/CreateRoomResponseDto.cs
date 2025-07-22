using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.DTO
{
    public class CreateRoomResponseDto
    {
        public bool IsSuccess { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public List<string> ParticipantIds { get; set; }
    }
}
