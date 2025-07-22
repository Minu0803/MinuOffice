using ServerMessenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.DTO
{
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public List<RoomDto> ChatList { get; set; }
        public List<UserModel> UserList { get; set; }  // 서버에 있는 기존 모델 재활용
    }

}
