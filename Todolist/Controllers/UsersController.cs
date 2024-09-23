using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Todolist.Models;
using Todolist.Services.Contracts;

namespace Todolist.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private Guid _userId = Guid.Empty;
        public Guid UserId
        {
            get
            {
                if (_userId == Guid.Empty)
                {
                    if (Request.Cookies.AllKeys.Contains("TodolistCookie"))
                        _userId = Guid.Parse(Request.Cookies["TodolistCookie"].Values["NidUser"]);
                    else
                        _userId = Guid.Empty;
                }
                return _userId;
            }
        }
        private readonly IRequestProcessor _requestProcessor;
        public UsersController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }
        public ActionResult Users()
        {
            return View(_requestProcessor.GetUsers());
        }
        public ActionResult AddUser(User user)
        {
            if (_requestProcessor.PostUser(user))
                TempData["UserSuccess"] = $"{user.Username} created successfully";
            else
                TempData["UserError"] = $"an error occured while creating user!";
            return RedirectToAction("Users","Users");
        }
        public ActionResult DeleteUser(Guid NidUser)
        {
            if (NidUser != UserId)
            {
                if (_requestProcessor.DeleteUser(NidUser))
                    TempData["UserSuccess"] = "user deleted successfully";
                else
                    TempData["UserError"] = "an error occured while deleting user!";
            }
            return RedirectToAction("Users", "Users");
        }
        [AllowAnonymous]
        [Route("Login")]
        public ActionResult Login(string ReturnUrl = "")
        {
            return View(new { ReturnUrl });
        }
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            if (Request.Cookies["TodolistCookie"] != null)
            {
                var c = new HttpCookie("TodolistCookie")
                {
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Add(c);
            }

            return RedirectToAction("Login", "Users");
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SubmitLogin(string Username, string Password, string returnUrl = "")
        {
            var loginResult = _requestProcessor.LoginUser(Username, Password);
            if (loginResult.IsSuccessful)
            {
                //build cookie
                var userDataCookie = new HttpCookie("TodolistCookie");
                userDataCookie.Values.Add("NidUser", loginResult.NidUser.ToString());
                userDataCookie.Values.Add("UserLevel", loginResult.IsAdmin ? "Admin" : "Simple");
                userDataCookie.Secure = true;
                userDataCookie.HttpOnly = true;
                userDataCookie.Expires = DateTime.Now.AddHours(8);
                Response.Cookies.Add(userDataCookie);
                FormsAuthentication.SetAuthCookie(loginResult.Username, true);
                if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                else
                    return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["LoginError"] = "error occured in login.try again";
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult DbMaintanence()
        {
            if (_requestProcessor.DbMaintanence())
                TempData["UserSuccess"] = $"process excuted successfully";
            else
                TempData["UserError"] = $"an error occured while executing db scripts!";
            return RedirectToAction("Users", "Users");
        }
    }
}