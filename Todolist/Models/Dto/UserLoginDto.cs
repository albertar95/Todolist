using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Todolist.Models.Dto
{
    public class UserLoginDto
    {
        public bool IsSuccessful { get; set; }
        public Guid NidUser { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
    }
}