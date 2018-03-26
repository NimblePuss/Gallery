using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gallery.Api.Models
{
    public class UserApi
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string PhotoUser { get; set; }
        public string PhotoUserName { get; set; }
        public string Password { get; set; }
        //public string ConfirmPassword { get; set; }

    }
}