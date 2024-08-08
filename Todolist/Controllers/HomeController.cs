using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Todolist.Helpers;
using Todolist.Models;
using Todolist.Services.Contracts;
using Todolist.ViewModels;

namespace Todolist.Controllers
{
    [Authorize]
    public class HomeController : Controller
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
        public HomeController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }
        public ActionResult Index()
        {
            return View(_requestProcessor.GetIndex(UserId));
        }
        public ActionResult IndexPagination(int Direction)
        {
            var tmp = Helpers.ViewHelper.RenderViewToString(this, "_IndexPartialView", _requestProcessor.GetIndex(UserId, Direction));
            return Json(new JsonResults()
            {
                HasValue = true,
                Html = Helpers.ViewHelper.RenderViewToString(this, "_IndexPartialView", _requestProcessor.GetIndex(UserId, Direction))
            });
        }
        public ActionResult IndexPaginationView(int Direction)
        {
            return View("Index", _requestProcessor.GetIndex(UserId, Direction));
        }
    }
    public class JsonResults
    {
        public string Message { get; set; }
        public string Html { get; set; }
        public bool HasValue { get; set; }
    }
}