using ClientMessenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger.DTO
{
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public List<RoomModel> ChatList { get; set; }
        public List<UserModel> UserList { get; set; }
    }
}
