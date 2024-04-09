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
            if (_requestProcessor.DeleteUser(NidUser))
                TempData["UserSuccess"] = "user deleted successfully";
            else
                TempData["UserError"] = "an error occured while deleting user!";
            return RedirectToAction("Users");
        }
        [AllowAnonymous]
        public ActionResult Login(string ReturnUrl = "")
        {
            return View("Login", ReturnUrl);
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
                return Guid.Parse(Request.Cookies["TodolistCookie"].Values["UserLevel"]);
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
        public ActionResult SubmitAddAccount(string Title, decimal Amount, bool IsActive)
        {
            Guid NidUser = Guid.Parse(User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value);
            Account account = new Account()
            {
                Amount = Amount,
                CreateDate = DateTime.Now,
                IsActive = IsActive,
                LastModified = DateTime.Now,
                LendAmount = 0,
                NidAccount = Guid.NewGuid(),
                Title = Title,
                UserId = NidUser
            };
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(account), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync($"{BaseAddress}/AddAccount", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["FinanceSuccess"] = $"{account.Title} created successfully";
                else
                    TempData["FinanceError"] = $"an error occured while creating account!";
            }
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitAddTransaction(byte TrType, Guid PayerAccount, Guid RecieverAccount, decimal Amount, string Reason)
        {
            Guid NidUser = Guid.Parse(User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value);
            Transaction tr = new Transaction()
            {
                Amount = Amount,
                CreateDate = DateTime.Now,
                TransactionType = TrType,
                PayerAccount = PayerAccount,
                RecieverAccount = RecieverAccount,
                TransactionReason = Reason,
                NidTransaction = Guid.NewGuid(),
                UserId = NidUser
            };
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(tr), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync($"{BaseAddress}/AddTransaction", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["FinanceSuccess"] = $"transaction created successfully";
                else
                    TempData["FinanceError"] = $"an error occured while creating transaction!";
            }
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitEditTransaction(Guid NidTr, byte TrType, Guid PayerAccount, Guid RecieverAccount, decimal Amount, string Reason)
        {
            using (HttpClient client = new HttpClient())
            {
                Transaction tr = new Transaction();
                var trresponse = await client.GetAsync($"{BaseAddress}/GetTransactionById/{NidTr}");
                if (trresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await trresponse.Content.ReadAsStringAsync();
                    tr = JsonConvert.DeserializeObject<Transaction>(stringResponse) ?? new Transaction();
                }
                tr.TransactionType = TrType;
                tr.PayerAccount = PayerAccount;
                tr.RecieverAccount = RecieverAccount;
                tr.Amount = Amount;
                tr.TransactionReason = Reason;
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject(tr), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{BaseAddress}/EditTransaction", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["FinanceSuccess"] = $"transaction edited successfully";
                else
                    TempData["FinanceError"] = $"an error occured while editing transaction!";
            }
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult SubmitDeleteTransaction(Guid NidTr)
        {
            using (HttpClient client = new HttpClient())
            {
                Transaction tr = new Transaction();
                var trresponse = await client.GetAsync($"{BaseAddress}/GetTransactionById/{NidTr}");
                if (trresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await trresponse.Content.ReadAsStringAsync();
                    tr = JsonConvert.DeserializeObject<Transaction>(stringResponse) ?? new Transaction();
                }
                if (tr.NidTransaction != Guid.Empty)
                {
                    var response = await client.DeleteAsync($"{BaseAddress}/DeleteTransaction/{NidTr}");
                    if (response.IsSuccessStatusCode)
                        TempData["FinanceSuccess"] = $"transaction deleted successfully";
                    else
                        TempData["FinanceError"] = $"an error occured while deleting transaction!";
                }
                else
                    TempData["FinanceError"] = $"an error occured while deleting transaction!";
            }
            return RedirectToAction("FinancialRecords");
        }
        public ActionResult GetTrById(Guid NidTransaction)
        {
            using (HttpClient client = new HttpClient())
            {
                Transaction tr = new Transaction();
                var trresponse = await client.GetAsync($"{BaseAddress}/GetTransactionById/{NidTransaction}");
                if (trresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await trresponse.Content.ReadAsStringAsync();
                    tr = JsonConvert.DeserializeObject<Transaction>(stringResponse) ?? new Transaction();
                }
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
        }
        public ActionResult Account(Guid NidAccount)
        {
            FinanceViewModel model = new FinanceViewModel();
            using (HttpClient client = new HttpClient())
            {
                Account account = new Account();
                var response = await client.GetAsync($"{BaseAddress}/GetAccountById/{NidAccount}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    account = JsonConvert.DeserializeObject<Account>(stringResponse) ?? new Account();
                }
                return View(account);
            }
        }
        public ActionResult SubmitEditAccount(Account account)
        {
            using (HttpClient client = new HttpClient())
            {
                account.LastModified = DateTime.Now;
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject(account), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{BaseAddress}/EditAccount", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["AccountSuccess"] = $"account edited successfully";
                else
                    TempData["AccountError"] = $"an error occured while editing account!";
            }
            return RedirectToAction("Account", new { NidAccount = account.NidAccount });
        }
        public ActionResult SubmitDeleteAccount(Guid NidAccount)
        {
            using (HttpClient client = new HttpClient())
            {
                Account acc = new Account();
                var accresponse = await client.GetAsync($"{BaseAddress}/GetAccountById/{NidAccount}");
                if (accresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await accresponse.Content.ReadAsStringAsync();
                    acc = JsonConvert.DeserializeObject<Account>(stringResponse) ?? new Account();
                }
                if (acc.NidAccount != Guid.Empty)
                {
                    var response = await client.DeleteAsync($"{BaseAddress}/DeleteAccount/{NidAccount}");
                    if (response.IsSuccessStatusCode)
                        TempData["FinanceSuccess"] = $"account deleted successfully";
                    else
                        TempData["FinanceError"] = $"an error occured while deleting account!";
                }
                else
                    TempData["FinanceError"] = $"an error occured while deleting account!";
            }
            return RedirectToAction("FinancialRecords");
        }
        //shield section
        public ActionResult Shields(bool IncludeAll = false)
        {
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                List<Shield> shields = new List<Shield>();
                var response = await client.GetAsync($"{BaseAddress}/GetShieldsByUserId/{NidUser}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    shields = JsonConvert.DeserializeObject<List<Shield>>(stringResponse) ?? new List<Shield>();
                }
                return View(shields);
            }
        }
        public ActionResult AddShield()
        {
            return View();
        }
        public ActionResult SubmitAddShield(Shield shield)
        {
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                shield.UserId = Guid.Parse(NidUser);
                shield.CreateDate = DateTime.Now;
                shield.Id = Guid.NewGuid();
                shield.Password = Helpers.Encryption.EncryptString(shield.Password.Trim());
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject(shield), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseAddress}/AddShield", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["ShieldSuccess"] = $"{shield.Title} created successfully";
                else
                    TempData["ShieldError"] = $"an error occured while creating Shield!";
            }
            return RedirectToAction("Shields");
        }
        public ActionResult SubmitEditShield(Shield shield)
        {
            using (HttpClient client = new HttpClient())
            {
                shield.LastModified = DateTime.Now;
                shield.Password = Helpers.Encryption.EncryptString(shield.Password.Trim());
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject(shield), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{BaseAddress}/EditShield", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["ShieldSuccess"] = $"{shield.Title} edited successfully";
                else
                    TempData["ShieldError"] = $"an error occured while editing Shield!";
            }
            return RedirectToAction("Shields");
        }
        public ActionResult SubmitDeleteShield(Guid NidShield)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{BaseAddress}/DeleteShield/{NidShield}");
                if (response.IsSuccessStatusCode)
                    TempData["ShieldSuccess"] = $"Shield deleted successfully";
                else
                    TempData["ShieldError"] = $"an error occured while deleting Shield!";
            }
            return RedirectToAction("Shields");
        }
        public ActionResult EditShield(Guid NidShield)
        {
            var shield = new Shield();
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync($"{BaseAddress}/GetShieldById/{NidShield}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    shield = JsonConvert.DeserializeObject<Shield>(stringResponse) ?? new Shield();
                }
            }
            return View(shield);
        }
        public ActionResult ShieldDetail(Guid NidShield)
        {
            var shield = new Shield();
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync($"{BaseAddress}/GetShieldById/{NidShield}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    shield = JsonConvert.DeserializeObject<Shield>(stringResponse) ?? new Shield();
                }
            }
            return View(shield);
        }

        //routine section
        public ActionResult Routines()
        {
            RoutineViewModel model = new RoutineViewModel();
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                List<Routine> routines = new List<Routine>();
                List<RoutineProgress> routineProgresses = new List<RoutineProgress>();
                var response = await client.GetAsync($"{BaseAddress}/GetRoutinesByUserId/{NidUser}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    routines = JsonConvert.DeserializeObject<List<Routine>>(stringResponse) ?? new List<Routine>();
                }
                foreach (var rt in routines)
                {
                    var response2 = await client.GetAsync($"{BaseAddress}/GetProgressesByRoutineId/{rt.NidRoutine}");
                    if (response2.StatusCode == HttpStatusCode.OK)
                    {
                        var stringResponse2 = await response2.Content.ReadAsStringAsync();
                        routineProgresses.AddRange(JsonConvert.DeserializeObject<List<RoutineProgress>>(stringResponse2));
                    }
                }
                model.Routines = routines;
                model.RoutineProgresses = routineProgresses;
                return View(model);
            }
        }
        [HttpPost]
        public ActionResult SubmitAddRoutine(Routine routine)
        {
            routine.NidRoutine = Guid.NewGuid();
            string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
            routine.UserId = Guid.Parse(NidUser);
            routine.CreateDate = DateTime.Now;
            routine.Status = false;
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(routine), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync($"{BaseAddress}/AddRoutine", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"{routine.Title} created successfully";
                else
                    TempData["RoutineError"] = $"an error occured while creating routine!";
            }
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitDeleteRoutine(Guid NidRoutine)
        {
            Routine routine = new Routine();
            using (HttpClient client = new HttpClient())
            {
                var routineresponse = await client.GetAsync($"{BaseAddress}/GetRoutineById/{NidRoutine}");
                if (routineresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await routineresponse.Content.ReadAsStringAsync();
                    routine = JsonConvert.DeserializeObject<Routine>(stringResponse) ?? new Routine();
                }
                var response = await client.DeleteAsync($"{BaseAddress}/DeleteRoutine/{NidRoutine}");
                if (response.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"routine deleted successfully";
                else
                    TempData["RoutineError"] = $"an error occured while deleting routine!";
            }
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitDeleteRoutine2(Guid NidRoutine, int Direction = 0)
        {
            Routine routine = new Routine();
            using (HttpClient client = new HttpClient())
            {
                var routineresponse = await client.GetAsync($"{BaseAddress}/GetRoutineById/{NidRoutine}");
                if (routineresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await routineresponse.Content.ReadAsStringAsync();
                    routine = JsonConvert.DeserializeObject<Routine>(stringResponse) ?? new Routine();
                }
                var response = await client.DeleteAsync($"{BaseAddress}/DeleteRoutine/{NidRoutine}");
                if (response.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"routine deleted successfully";
                else
                    TempData["RoutineError"] = $"an error occured while deleting routine!";
            }
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitDoneRoutine(RoutineProgress Progress)
        {
            Progress.NidRoutineProgress = Guid.NewGuid();
            Progress.CreateDate = DateTime.Now;
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(Progress), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync($"{BaseAddress}/AddRoutineProgress", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"routine done successfully";
                else
                    TempData["RoutineError"] = $"an error occured while doning routine!";
            }
            return RedirectToAction("Routines");
        }
        public ActionResult SubmitUnDoneRoutine(RoutineProgress Progress)
        {
            List<RoutineProgress> routine = new List<RoutineProgress>();
            using (HttpClient client = new HttpClient())
            {
                var routineresponse = await client.GetAsync($"{BaseAddress}/GetProgressesByRoutineId/{Progress.RoutineId}");
                if (routineresponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await routineresponse.Content.ReadAsStringAsync();
                    routine = JsonConvert.DeserializeObject<List<RoutineProgress>>(stringResponse) ?? new List<RoutineProgress>();
                    foreach (var rp in routine)
                    {
                        if (rp.ProgressDate == Progress.ProgressDate.Date)
                            await client.DeleteAsync($"{BaseAddress}/DeleteRoutineProgress/{rp.NidRoutineProgress}");
                    }
                }
            }
            return RedirectToAction("Routines");
        }
        public ActionResult RoutineCalendar()
        {
            RoutineViewModel model = new RoutineViewModel();
            string[] DatePeriod = new string[3];
            var weekDates = Helpers.Dates.GetWeekPeriod();
            DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
            DatePeriod[1] = "-1";
            DatePeriod[2] = "1";
            var persianDates = Helpers.Dates.ToPersianDate(weekDates);
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                List<Routine> routines = new List<Routine>();
                List<RoutineProgress> routineProgresses = new List<RoutineProgress>();
                var response = await client.GetAsync($"{BaseAddress}/GetRoutinesByUserId/{NidUser}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    routines = JsonConvert.DeserializeObject<List<Routine>>(stringResponse) ?? new List<Routine>();
                }
                foreach (var rt in routines)
                {
                    var response2 = await client.GetAsync($"{BaseAddress}/GetProgressesByRoutineId/{rt.NidRoutine}");
                    if (response2.StatusCode == HttpStatusCode.OK)
                    {
                        var stringResponse2 = await response2.Content.ReadAsStringAsync();
                        routineProgresses.AddRange(JsonConvert.DeserializeObject<List<RoutineProgress>>(stringResponse2));
                    }
                }
                model.Routines = routines;
                model.RoutineProgresses = routineProgresses;
                model.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
                model.DatePeriodInfo = DatePeriod;
                model.StartDate = weekDates.Item1;
                model.EndDate = weekDates.Item2;
                return View(model);
            }
        }
        public ActionResult IndexPagination2(int Direction)
        {
            RoutineViewModel model = new RoutineViewModel();
            string[] DatePeriod = new string[3];
            var weekDates = Helpers.Dates.GetWeekPeriod(Direction);
            DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
            DatePeriod[1] = $"{Direction - 1}";
            DatePeriod[2] = $"{Direction + 1}";
            var persianDates = Helpers.Dates.ToPersianDate(weekDates);
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                List<Routine> routines = new List<Routine>();
                List<RoutineProgress> routineProgresses = new List<RoutineProgress>();
                var response = await client.GetAsync($"{BaseAddress}/GetRoutinesByUserId/{NidUser}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    routines = JsonConvert.DeserializeObject<List<Routine>>(stringResponse) ?? new List<Routine>();
                }
                foreach (var rt in routines)
                {
                    var response2 = await client.GetAsync($"{BaseAddress}/GetProgressesByRoutineId/{rt.NidRoutine}");
                    if (response2.StatusCode == HttpStatusCode.OK)
                    {
                        var stringResponse2 = await response2.Content.ReadAsStringAsync();
                        routineProgresses.AddRange(JsonConvert.DeserializeObject<List<RoutineProgress>>(stringResponse2));
                    }
                }
                model.Routines = routines;
                model.RoutineProgresses = routineProgresses;
                model.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
                model.DatePeriodInfo = DatePeriod;
                model.StartDate = weekDates.Item1;
                model.EndDate = weekDates.Item2;
                return Json(new JsonResults() { HasValue = true, Html = await Helpers.RenderViewToString.RenderViewAsync(this, "_RoutineCalendarPartialView", model, true) });
            }
        }
        public ActionResult IndexPaginationView2(int Direction)
        {
            RoutineViewModel model = new RoutineViewModel();
            string[] DatePeriod = new string[3];
            var weekDates = Helpers.Dates.GetWeekPeriod(Direction);
            DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
            DatePeriod[1] = $"{Direction - 1}";
            DatePeriod[2] = $"{Direction + 1}";
            var persianDates = Helpers.Dates.ToPersianDate(weekDates);
            using (HttpClient client = new HttpClient())
            {
                string NidUser = User.Claims.Where(p => p.Type == "NidUser").FirstOrDefault().Value;
                List<Routine> routines = new List<Routine>();
                List<RoutineProgress> routineProgresses = new List<RoutineProgress>();
                var response = await client.GetAsync($"{BaseAddress}/GetRoutinesByUserId/{NidUser}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    routines = JsonConvert.DeserializeObject<List<Routine>>(stringResponse) ?? new List<Routine>();
                }
                foreach (var rt in routines)
                {
                    var response2 = await client.GetAsync($"{BaseAddress}/GetProgressesByRoutineId/{rt.NidRoutine}");
                    if (response2.StatusCode == HttpStatusCode.OK)
                    {
                        var stringResponse2 = await response2.Content.ReadAsStringAsync();
                        routineProgresses.AddRange(JsonConvert.DeserializeObject<List<RoutineProgress>>(stringResponse2));
                    }
                }
                model.Routines = routines;
                model.RoutineProgresses = routineProgresses;
                model.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
                model.DatePeriodInfo = DatePeriod;
                model.StartDate = weekDates.Item1;
                model.EndDate = weekDates.Item2;
                return View("RoutineCalendar", model);
            }
        }
        public ActionResult SubmitEditRoutine(Routine Routine, int Direction = 0)
        {
            using (HttpClient client = new HttpClient())
            {
                Routine r = new Routine();
                var response = await client.GetAsync($"{BaseAddress}/GetRoutineById/{Routine.NidRoutine}");
                if (response.IsSuccessStatusCode)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    r = JsonConvert.DeserializeObject<Routine>(stringResponse);
                    r.Todate = Routine.Todate;
                    r.FromDate = Routine.FromDate;
                    r.Title = Routine.Title;
                    r.ModifiedDate = DateTime.Now;
                }
                StringContent stringContent = new StringContent(JsonConvert.SerializeObject(r), Encoding.UTF8, "application/json");
                var response2 = await client.PatchAsync($"{BaseAddress}/EditRoutine", stringContent);
                if (response2.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"routine edited successfully";
                else
                    TempData["RoutineError"] = $"an error occured while editing routine!";
            }
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteRoutineProgress(Guid NidRoutineProgress, int Direction = 0)
        {
            List<RoutineProgress> routine = new List<RoutineProgress>();
            using (HttpClient client = new HttpClient())
            {
                var routineresponse = await client.DeleteAsync($"{BaseAddress}/DeleteRoutineProgress/{NidRoutineProgress}");
            }
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
        public ActionResult SubmitAddRoutineProgress(RoutineProgress Progress, int Direction = 0)
        {
            Progress.NidRoutineProgress = Guid.NewGuid();
            Progress.CreateDate = DateTime.Now;
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(Progress), Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync($"{BaseAddress}/AddRoutineProgress", stringContent);
                if (response.IsSuccessStatusCode)
                    TempData["RoutineSuccess"] = $"routine done successfully";
                else
                    TempData["RoutineError"] = $"an error occured while doning routine!";
            }
            return RedirectToAction("IndexPaginationView2", new { Direction = Direction });
        }
    }
    public class JsonResults
    {
        public string Message { get; set; }
        public string Html { get; set; }
        public bool HasValue { get; set; }
    }
}