using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger.DTO
{
    public class LoginRequestDto
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }
}
