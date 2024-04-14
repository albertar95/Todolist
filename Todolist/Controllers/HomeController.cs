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
    public class HomeController : Controller
    {
        private readonly IRequestProcessor _requestProcessor;
        public HomeController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }
        //user section
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
            return RedirectToAction("Users");
        }
        public ActionResult DeleteUser(Guid NidUser)
        {
            if(NidUser != GetUserId())
            {
                if (_requestProcessor.DeleteUser(NidUser))
                    TempData["UserSuccess"] = "user deleted successfully";
                else
                    TempData["UserError"] = "an error occured while deleting user!";
            }
            return RedirectToAction("Users");
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

            return RedirectToAction("Login");
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SubmitLogin(string Username, string Password, string returnUrl = "")
        {
            var loginResult = _requestProcessor.LoginUser(Username,Password);
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
                return RedirectToAction("Login");
            }
        }

        //goal section
        public ActionResult Goals()
        {
            return View(_requestProcessor.GetGoals(GetUserId()));
        }
        public ActionResult AddGoal()
        {
            return View();
        }
        public ActionResult SubmitAddGoal(Goal goal)
        {
            goal.UserId = GetUserId();
            if (_requestProcessor.PostGoal(goal))
                TempData["GoalSuccess"] = $"{goal.Title} created successfully";
            else
                TempData["GoalError"] = $"an error occured while creating goal!";
            return RedirectToAction("Goals");
        }
        public ActionResult Goal(Guid NidGoal)
        {
            return View(_requestProcessor.GetGoal(NidGoal));
        }
        public ActionResult SubmitAddTask(string NidGoal, string Title, string TWeight, string Description = "")
        {
            Models.Task newtask = new Models.Task() { Description = Description, GoalId = Guid.Parse(NidGoal), Title = Title, UserId = GetUserId(), TaskWeight = byte.Parse(TWeight) };
            if (_requestProcessor.PostTask(newtask))
                TempData["GoalPageSuccess"] = $"{newtask.Title} created successfully";
            else
                TempData["GoalPageError"] = $"an error occured while creating task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult DeleteTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.DeleteTask(NidTask))
                TempData["GoalPageSuccess"] = $"task deleted successfully";
            else
                TempData["GoalPageError"] = $"an error occured while deleting task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult DeleteProgress(Guid NidProgress, Guid NidGoal)
        {
            if (_requestProcessor.DeleteProgress(NidProgress))
                TempData["GoalPageSuccess"] = $"progress deleted successfully";
            else
                TempData["GoalPageError"] = $"an error occured while deleting task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult DoneTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.DoneTask(NidTask))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult UndoTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.UndoTask(NidTask))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult SubmitEditTask(string NidTask, Guid NidGoal, string Title, string TWeight)
        {
            var task = _requestProcessor.GetTask(Guid.Parse(NidTask));
            task.Title = Title;
            task.LastModifiedDate = DateTime.Now;
            task.TaskWeight = byte.Parse(TWeight);
            if (_requestProcessor.PatchTask(task))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return RedirectToAction("Goal", new { NidGoal = NidGoal });
        }
        public ActionResult SubmitEditTaskDescription(string NidTask, Guid NidGoal, string Description)
        {
            var task = _requestProcessor.GetTask(Guid.Parse(NidTask));
            task.Description = Description;
            task.LastModifiedDate = DateTime.Now;
            if (_requestProcessor.PatchTask(task))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return Json(new JsonResults() { HasValue = true });
        }
        public ActionResult EditGoal(Guid NidGoal)
        {
            return View(_requestProcessor.GetGoalWithoutDependancy(NidGoal));
        }
        public ActionResult SubmitEditGoal(Goal goal)
        {
            if (_requestProcessor.PatchGoal(goal))
                TempData["GoalPageSuccess"] = $"goal edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing goal!";
            return RedirectToAction("Goal", new { NidGoal = goal.NidGoal });
        }
        public ActionResult CloseGoal(Guid NidGoal)
        {
            var goal = _requestProcessor.GetGoalWithoutDependancy(NidGoal);
            goal.GoalStatus = 1;
            if (_requestProcessor.PatchGoal(goal))
                TempData["GoalPageSuccess"] = $"goal edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing goal!";
            return RedirectToAction("Goals");
        }
        public ActionResult SubmitDeleteGoal(Guid NidGoal)
        {
            if (_requestProcessor.DeleteGoal(NidGoal))
                TempData["GoalSuccess"] = $"goal deleted successfully";
            else
                TempData["GoalError"] = $"an error occured while deleting goal!";
            return RedirectToAction("Goals");
        }
        public ActionResult OpenGoal(Guid NidGoal)
        {
            var goal = _requestProcessor.GetGoalWithoutDependancy(NidGoal);
            goal.GoalStatus = 0;
            if (_requestProcessor.PatchGoal(goal))
                TempData["GoalPageSuccess"] = $"goal edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing goal!";
            return RedirectToAction("Goals");
        }
        //generals
        public ActionResult Index()
        {
            return View(_requestProcessor.GetIndex(GetUserId()));
        }
        public ActionResult IndexPagination(int Direction)
        {
            var tmp = Helpers.ViewHelper.RenderViewToString(this, "_IndexPartialView", _requestProcessor.GetIndex(GetUserId(), Direction));
            return Json(new JsonResults()
            {
                HasValue = true,
                Html = Helpers.ViewHelper.RenderViewToString(this, "_IndexPartialView", _requestProcessor.GetIndex(GetUserId(), Direction))
            });
        }
        public ActionResult IndexPaginationView(int Direction)
        {
            return View("Index",_requestProcessor.GetIndex(GetUserId(),Direction));
        }
        public ActionResult SubmitAddSchedule(Models.Schedule schedule, int Direction = 0)
        {
            _requestProcessor.PostSchedule(schedule);
            return RedirectToAction("IndexPaginationView", new { Direction = Direction });
        }
        public ActionResult SubmitAddProgress(Models.Progress progress, int Direction = 0)
        {
            progress.UserId = GetUserId();
            _requestProcessor.PostProgress(progress);
            return RedirectToAction("IndexPaginationView", new { Direction = Direction });
        }
        public ActionResult SubmitEditProgress(Models.Progress progress, int Direction = 0)
        {
            _requestProcessor.PatchProgress(progress);
            return RedirectToAction("IndexPaginationView", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteProgress(string NidProgress, int Direction = 0)
        {
            _requestProcessor.DeleteProgress(Guid.Parse(NidProgress));
            return RedirectToAction("IndexPaginationView", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteSchedule(string NidSchedule, int Direction = 0)
        {
            _requestProcessor.DeleteSchedule(Guid.Parse(NidSchedule));
            return RedirectToAction("IndexPaginationView", new { Direction = Direction });
        }
        private Guid GetUserId()
        {
            if (Request.Cookies.AllKeys.Contains("TodolistCookie"))
                return Guid.Parse(Request.Cookies["TodolistCookie"].Values["NidUser"]);
            else
                return Guid.Empty;
        }
        //notes section
        public ActionResult NoteGroups()
        {
            return View(_requestProcessor.GetNoteGroups(GetUserId()));
        }
        public ActionResult AddNoteGroup()
        {
            return View();
        }
        public ActionResult SubmitAddNoteGroup(NoteGroup group)
        {
            group.UserId = GetUserId();
            if (_requestProcessor.PostNoteGroup(group))
                TempData["GroupSuccess"] = $"{group.Title} created successfully";
            else
                TempData["GroupError"] = $"an error occured while creating group!";
            return RedirectToAction("NoteGroups");
        }
        public ActionResult NoteGroup(Guid NidGroup)
        {
            return View(_requestProcessor.GetNoteGroup(NidGroup));
        }
        public ActionResult EditNoteGroup(Guid NidGroup)
        {
            return View(_requestProcessor.GetNoteGroup(NidGroup).Group);
        }
        public ActionResult SubmitEditNoteGroup(NoteGroup group)
        {
            if (_requestProcessor.PatchNoteGroup(group))
                TempData["NoteSuccess"] = $"group edited successfully";
            else
                TempData["NoteError"] = $"an error occured while editing group!";
            return RedirectToAction("NoteGroup", new { NidGroup = group.NidGroup });
        }
        public ActionResult SubmitDeleteNoteGroup(Guid NidGroup)
        {
            if (_requestProcessor.DeleteNoteGroup(NidGroup))
                TempData["GroupSuccess"] = $"group deleted successfully";
            else
                TempData["GroupError"] = $"an error occured while deleting group!";
            return RedirectToAction("NoteGroups");
        }
        public ActionResult AddNote(Guid NidGroup)
        {
            var note = new Note() { GroupId = NidGroup };
            return View(note);
        }
        public ActionResult SubmitAddNote(Note note)
        {
            if (_requestProcessor.PostNote(note))
                TempData["NoteSuccess"] = $"{note.Title} created successfully";
            else
                TempData["NoteError"] = $"an error occured while creating note!";
            return RedirectToAction("NoteGroup", new { NidGroup = note.GroupId });
        }
        public ActionResult Note(Guid NidNote)
        {
            return View(_requestProcessor.GetNote(NidNote));
        }
        [AllowAnonymous]
        [ValidateInput(false)]
        public ActionResult SubmitEditNote(Note note)
        {
            if (_requestProcessor.PatchNote(note))
                TempData["NoteSuccess"] = $"note edited successfully";
            else
                TempData["NoteError"] = $"an error occured while editing note!";
            return RedirectToAction("NoteGroup", new { NidGroup = note.GroupId });
        }
        public ActionResult SubmitDeleteNote(Guid NidNote)
        {
            var note = _requestProcessor.GetNote(NidNote);
            if (_requestProcessor.DeleteNote(NidNote))
                TempData["NoteSuccess"] = $"note deleted successfully";
            else
                TempData["NoteError"] = $"an error occured while deleting note!";
            return RedirectToAction("NoteGroup", new { NidGroup = note.GroupId });
        }
        //account section
        public ActionResult FinancialRecords(bool IncludeAll = false)
        {
            return View(_requestProcessor.GetFinacialRecords(GetUserId()));
        }
        public ActionResult SubmitAddAccount(string Title, decimal Amount, bool IsActive,bool IsBackup = false)
        {
            Account account = new Account() { Amount = Amount,IsActive = IsActive,LendAmount = 0,Title = Title,UserId = GetUserId(), IsBackup = IsBackup };
            if (_requestProcessor.PostAccount(account))
                TempData["FinanceSuccess"] = $"{account.Title} created successfully";
            else
                TempData["FinanceError"] = $"an error occured while creating account!";
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitAddTransaction(byte TrType, Guid PayerAccount, Guid RecieverAccount, decimal Amount, string Reason = "")
        {
            Transaction tr = new Transaction() {Amount = Amount,TransactionType = TrType,PayerAccount = PayerAccount,RecieverAccount = RecieverAccount,TransactionReason = Reason,UserId = GetUserId() };
            if (_requestProcessor.PostTransaction(tr))
                TempData["FinanceSuccess"] = $"transaction created successfully";
            else
                TempData["FinanceError"] = $"an error occured while creating transaction!";
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitEditTransaction(Guid NidTr, byte TrType, Guid PayerAccount, Guid RecieverAccount, decimal Amount, string Reason)
        {
            var tr = _requestProcessor.GetTransaction(NidTr);
            tr.TransactionType = TrType;
            tr.PayerAccount = PayerAccount;
            tr.RecieverAccount = RecieverAccount;
            tr.Amount = Amount;
            tr.TransactionReason = Reason;
            if (_requestProcessor.PatchTransaction(tr))
                TempData["FinanceSuccess"] = $"transaction edited successfully";
            else
                TempData["FinanceError"] = $"an error occured while editing transaction!";
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitDeleteTransaction(Guid NidTr)
        {
            if (_requestProcessor.DeleteTransaction(NidTr))
                TempData["FinanceSuccess"] = $"transaction deleted successfully";
            else
                TempData["FinanceError"] = $"an error occured while deleting transaction!";
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult GetTrById(Guid NidTransaction)
        {
            var tr = _requestProcessor.GetTransaction(NidTransaction);
            if (tr.NidTransaction == Guid.Empty)
                return Json(new { HasValue = false });
            else
                return Json(new
                {
                    HasValue = true,
                    NidTr = tr.NidTransaction.ToString(),
                    TrType = tr.TransactionType.ToString(),
                    PAccount = tr.PayerAccount.ToString(),
                    RAccount = tr.RecieverAccount.ToString(),
                    Reason = tr.TransactionReason.ToString(),
                    Amount = ((int)(tr.Amount)).ToString()
                });
        }
        public ActionResult Account(Guid NidAccount)
        {
            return View(_requestProcessor.GetAccount(NidAccount));
        }
        public ActionResult SubmitEditAccount(Account account)
        {
            if (_requestProcessor.PatchAccount(account))
                TempData["AccountSuccess"] = $"account edited successfully";
            else
                TempData["AccountError"] = $"an error occured while editing account!";
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitDeleteAccount(Guid NidAccount)
        {
            if (_requestProcessor.DeleteAccount(NidAccount))
                TempData["FinanceSuccess"] = $"account deleted successfully";
            else
                TempData["FinanceError"] = $"an error occured while deleting account!";
            return RedirectToAction("FinancialRecords");
        }
        //shield section
        public ActionResult Shields(bool IncludeAll = false)
        {
            return View(_requestProcessor.GetShields(GetUserId()));
        }
        public ActionResult AddShield()
        {
            return View();
        }
        public ActionResult SubmitAddShield(Shield shield)
        {
            shield.UserId = GetUserId();
            if (_requestProcessor.PostShield(shield))
                TempData["ShieldSuccess"] = $"{shield.Title} created successfully";
            else
                TempData["ShieldError"] = $"an error occured while creating Shield!";
            return RedirectToAction("Shields");
        }
        public ActionResult SubmitEditShield(Shield shield)
        {
            if (_requestProcessor.PatchShield(shield))
                TempData["ShieldSuccess"] = $"{shield.Title} edited successfully";
            else
                TempData["ShieldError"] = $"an error occured while editing Shield!";
            return RedirectToAction("Shields");
        }
        public ActionResult SubmitDeleteShield(Guid NidShield)
        {
            if (_requestProcessor.DeleteShield(NidShield))
                TempData["ShieldSuccess"] = $"Shield deleted successfully";
            else
                TempData["ShieldError"] = $"an error occured while deleting Shield!";
            return RedirectToAction("Shields");
        }
        public ActionResult EditShield(Guid NidShield)
        {
            return View(_requestProcessor.GetShield(NidShield));
        }
        public ActionResult ShieldDetail(Guid NidShield)
        {
            return View(_requestProcessor.GetShield(NidShield));
        }

        //routine section
        public ActionResult Routines()
        {
            return View(_requestProcessor.GetRoutines(GetUserId()));
        }
        [HttpPost]
        public ActionResult SubmitAddRoutine(Routine routine)
        {
            routine.UserId = GetUserId();
            if (_requestProcessor.PostRoutine(routine))
                TempData["RoutineSuccess"] = $"{routine.Title} created successfully";
            else
                TempData["RoutineError"] = $"an error occured while creating routine!";
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitDeleteRoutine(Guid NidRoutine)
        {
            if (_requestProcessor.DeleteRoutine(NidRoutine))
                TempData["RoutineSuccess"] = $"routine deleted successfully";
            else
                TempData["RoutineError"] = $"an error occured while deleting routine!";
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitDeleteRoutine2(Guid NidRoutine, int Direction = 0)
        {
            if (_requestProcessor.DeleteRoutine(NidRoutine))
                TempData["RoutineSuccess"] = $"routine deleted successfully";
            else
                TempData["RoutineError"] = $"an error occured while deleting routine!";
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitDoneRoutine(RoutineProgress Progress)
        {
            if (_requestProcessor.PostRoutineProgress(Progress))
                TempData["RoutineSuccess"] = $"routine done successfully";
            else
                TempData["RoutineError"] = $"an error occured while doning routine!";
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitUnDoneRoutine(RoutineProgress Progress)
        {
            var progresses = _requestProcessor.GetRoutineProgresses(Progress.RoutineId);
            progresses.ForEach(x => { if(x.ProgressDate == Progress.ProgressDate.Date) _requestProcessor.DeleteRoutineProgress(x.NidRoutineProgress); });
            return RedirectToAction("Routines");
        }
        public ActionResult RoutineCalendar()
        {
            return View(_requestProcessor.GetRoutines(GetUserId()));
        }
        public ActionResult IndexPagination2(int Direction)
        {
            return Json(new JsonResults() { HasValue = true, Html = Helpers.ViewHelper.RenderViewToString(this, "_RoutineCalendarPartialView", _requestProcessor.GetRoutines(GetUserId(),Direction)) });
        }
        public ActionResult IndexPaginationView2(int Direction)
        {
            return View("RoutineCalendar", _requestProcessor.GetRoutines(GetUserId(),Direction));
        }
        public ActionResult SubmitEditRoutine(Routine Routine, int Direction = 0)
        {
            var current = _requestProcessor.GetRoutine(Routine.NidRoutine);
            current.Title = Routine.Title;
            current.FromDate = Routine.FromDate;
            current.Todate = Routine.Todate;
            if (_requestProcessor.PatchRoutine(current))
                TempData["RoutineSuccess"] = $"routine edited successfully";
            else
                TempData["RoutineError"] = $"an error occured while editing routine!";
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteRoutineProgress(Guid NidRoutineProgress, int Direction = 0)
        {
            _requestProcessor.DeleteRoutineProgress(NidRoutineProgress);
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitAddRoutineProgress(RoutineProgress Progress, int Direction = 0)
        {
            if (_requestProcessor.PostRoutineProgress(Progress))
                TempData["RoutineSuccess"] = $"routine done successfully";
            else
                TempData["RoutineError"] = $"an error occured while doning routine!";
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult LendDetail(Guid NidAccount)
        {
            var details = _requestProcessor.LendDetails(NidAccount);
            if(details.Any())
                return Json(new JsonResults() {  HasValue = true, Html = Helpers.ViewHelper.RenderViewToString(this, "_LendDetailPartialView", details)});
            else
                return Json(new JsonResults() {  HasValue = false });
        }
    }
    public class JsonResults
    {
        public string Message { get; set; }
        public string Html { get; set; }
        public bool HasValue { get; set; }
    }
}