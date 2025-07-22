using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.Models
{
    public class UserModel
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }     // 채팅에서 표시할 이름
        public string Email { get; set; }

        
        // 비밀번호 설정
        public void SetPassword(string hashedPasswordFromClient)
        {
            Password = hashedPasswordFromClient; // 이미 해시된 상태
        }

        // 비밀번호 검증
        public bool CheckPassword(string hashedPasswordFromClient)
        {
            return Password == hashedPasswordFromClient;
        }
    }
}


