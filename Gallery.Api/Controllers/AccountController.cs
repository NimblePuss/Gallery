using Gallery.Api.Models;
using Gallery.BAL.DTO;
using Gallery.BAL.Encryption;
using Gallery.BAL.Interfaces;
using Gallery.BAL.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web.Http;
using System.Web.Security;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Web.Http.Cors;

namespace Gallery.Api.Controllers
{
    [EnableCors("http://localhost:56238", "*", "*")]
    public class AccountController : ApiController
    {
        private readonly IUserService userService;
        private readonly IRoleService roleService;
        private readonly IFileService fileService;
        CustomRoleProvider roleProvider;

        public AccountController(IUserService userService,
                            IRoleService roleService,
                            CustomRoleProvider roleProvider,
                            IFileService fileService)
        {
            this.userService = userService;
            this.roleService = roleService;
            this.roleProvider = roleProvider;
            this.fileService = fileService;
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        [Route("api/account/authorize")]
        public IHttpActionResult GetForAdmin()
        {
            var identity = (ClaimsIdentity)User.Identity;
            var roles = identity.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            return Ok("Hello, " + identity.Name + " Role: " + string.Join(",", roles.ToList()));
        }

        //[Authorize(Roles = "moderator, user")]
        [HttpGet]
        [Route("api/account/getCurrentUser")]
        public HttpResponseMessage GetCurrentUserClaims()
        {
            var curUser = userService.GetAllElements().FirstOrDefault(log => log.Login == User.Identity.Name);
            if (User.Identity.IsAuthenticated)
            {
                if (curUser != null)
                {
                    var identity = (ClaimsIdentity)User.Identity;
                    IEnumerable<Claim> claims = identity.Claims;
                    if (identity.IsAuthenticated)
                    {
                        if (curUser != null)
                        {
                            var user = new RegisterUserModel()
                            {
                                Id = (int)curUser.Id,
                                Name = identity.Name,
                                Login = curUser.Login,
                                Password = curUser.Password,
                                PhotoUser = curUser.PhotoUser,
                                //RoleId = (int)curUser.RoleId,
                                //Roles = curUser.Roles,
                                Email = curUser.Email
                            };
                            return Request.CreateResponse(HttpStatusCode.OK, user);
                        }
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "error");
                }
            }


            return Request.CreateResponse(HttpStatusCode.InternalServerError, "error");

        }

        public void LogOff()
        {
            FormsAuthentication.SignOut();
        }



        [HttpPost]
        [Route("api/account/register")]
        [System.Web.Mvc.ValidateAntiForgeryToken]
        public async Task<HttpResponseMessage> Register()
        {
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "ups");
            }

            Dictionary<string, RegisterUserModel> attributes = new Dictionary<string, RegisterUserModel>();
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            string fileName = "";
            Byte[] newAvatarForCurrUser = null;
            foreach (var file in provider.Contents)
            {
                if (file.Headers.ContentDisposition.FileName != null)
                {
                    fileName = file.Headers.ContentDisposition.FileName.Trim('\"');
                    var buffer = await file.ReadAsByteArrayAsync();
                    files.Add(fileName, buffer);
                    newAvatarForCurrUser = buffer;
                }
                else
                {
                    foreach (NameValueHeaderValue p in file.Headers.ContentDisposition.Parameters)
                    {
                        string name = p.Value;
                        if (name.StartsWith("\"") && name.EndsWith("\""))
                        {
                            name = name.Substring(1, name.Length - 2);
                        }
                        string value = await file.ReadAsStringAsync();
                        RegisterUserModel user = JsonConvert.DeserializeObject<RegisterUserModel>(value);
                        user.PhotoUser = fileName;
                        var login = userService.GetAllElements().FirstOrDefault(log => log.Login == user.Login);
                        if (login != null)
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "error");
                        }
                        else
                        {
                            string source = user.Password;
                            using (MD5 md5Hash = MD5.Create())
                            {
                                string hash = EncryptPassword.GetMd5Hash(md5Hash, source);

                                var newUser = new UserDTO()
                                {
                                    Id = user.Id,
                                    Name = user.Name,
                                    Login = user.Login,
                                    Email = user.Email,
                                    Password = hash,
                                    RoleId = 3,
                                    PhotoUser = user.PhotoUser,
                                    //Images = 
                                    //Friends = 
                                };
                                userService.Create(newUser);

                                var userWithId = userService.GetCurrentUser(newUser.Login);
                                if (!string.IsNullOrWhiteSpace(fileName) && newAvatarForCurrUser != null)
                                {
                                    MemoryStream stream = new MemoryStream(newAvatarForCurrUser);
                                    user.PhotoUser = fileService.UploadFile(stream, fileName, userWithId.Id);
                                    newUser.Id = userWithId.Id;
                                    newUser.PhotoUser = user.PhotoUser;
                                    userService.Update(newUser);
                                }
                            }
                        }
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, "OK");
        }



        [HttpPost]
        [Route("api/account/registerUser")]
        //[System.Web.Mvc.ValidateAntiForgeryToken]
        public IHttpActionResult RegisterUser(UserApi userApi) // [FromBody] UserApi userApi     UserApi userApi
        {
            // file = Request.Files[0];
            if (ModelState.IsValid)
            {
                if (userApi != null)
                {
                    var login = userService.GetAllElements().FirstOrDefault(log => log.Login == userApi.Login); // log => log.Login == "Lisa"
                    if (login != null)
                    {
                        return Ok("error");
                    }

                    string source = userApi.Password;
                    using (MD5 md5Hash = MD5.Create())
                    {
                        string hash = EncryptPassword.GetMd5Hash(md5Hash, source);
                        var us = new UserDTO
                        {
                            //Id = userApi.Id,
                            Name = userApi.Name,
                            Login = userApi.Login,
                            Password = hash,
                            Email = userApi.Email,
                            //PhotoUser = "",
                            //Roles = userApi.Roles,
                            RoleId = 3
                        };

                        userService.Create(us);

                        var base64Data = Regex.Match(userApi.PhotoUser, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                        var binData = Convert.FromBase64String(base64Data);

                        Stream stream = new MemoryStream(binData);

                        var userWithId = userService.GetCurrentUser(us.Login);

                        if (!string.IsNullOrWhiteSpace(userApi.PhotoUserName))
                        {
                            userApi.PhotoUser = fileService.UploadFile(stream, userApi.PhotoUserName, userWithId.Id);
                            us.Id = userWithId.Id;
                            us.PhotoUser = userApi.PhotoUser;
                            userService.Update(us);
                        }
                        FormsAuthentication.SetAuthCookie(userApi.Login, false);

                        //return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Ok");
                        return Ok("OK");
                    }
                }
                return Ok("OK");
            }
            //return Request.CreateResponse(HttpStatusCode.OK, "ups");
            return Ok("ups");
        }
    }
}
