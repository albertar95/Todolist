using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Todolist.Helpers;
using Todolist.Models;
using Todolist.Services.Contracts;

namespace Todolist.Controllers
{
    [Authorize]
    public class SchedulesController : Controller
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
        public SchedulesController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }

        //goal section
        public ActionResult Goals()
        {
            return View(_requestProcessor.GetGoals(UserId));
        }
        public ActionResult AddGoal()
        {
            return View();
        }
        public ActionResult SubmitAddGoal(Goal goal)
        {
            goal.UserId = UserId;
            if (_requestProcessor.PostGoal(goal))
                TempData["GoalSuccess"] = $"{goal.Title} created successfully";
            else
                TempData["GoalError"] = $"an error occured while creating goal!";
            return RedirectToAction("Goals", "Schedules");
        }
        public ActionResult Goal(Guid NidGoal)
        {
            return View(_requestProcessor.GetGoal(NidGoal));
        }
        public ActionResult SubmitAddTask(string NidGoal, string Title, string TWeight, string Description = "")
        {
            Models.Task newtask = new Models.Task() { Description = Description, GoalId = Guid.Parse(NidGoal), Title = Title, UserId = UserId, TaskWeight = byte.Parse(TWeight) };
            if (_requestProcessor.PostTask(newtask))
                TempData["GoalPageSuccess"] = $"{newtask.Title} created successfully";
            else
                TempData["GoalPageError"] = $"an error occured while creating task!";
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
        }
        public ActionResult DeleteTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.DeleteTask(NidTask))
                TempData["GoalPageSuccess"] = $"task deleted successfully";
            else
                TempData["GoalPageError"] = $"an error occured while deleting task!";
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
        }
        public ActionResult DeleteProgress(Guid NidProgress, Guid NidGoal)
        {
            if (_requestProcessor.DeleteProgress(NidProgress))
                TempData["GoalPageSuccess"] = $"progress deleted successfully";
            else
                TempData["GoalPageError"] = $"an error occured while deleting task!";
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
        }
        public ActionResult DoneTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.DoneTask(NidTask))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
        }
        public ActionResult UndoTask(Guid NidTask, Guid NidGoal)
        {
            if (_requestProcessor.UndoTask(NidTask))
                TempData["GoalPageSuccess"] = $"task edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing task!";
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
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
            return RedirectToAction("Goal", "Schedules", new { NidGoal = NidGoal });
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
            return RedirectToAction("Goal", "Schedules", new { NidGoal = goal.NidGoal });
        }
        public ActionResult CloseGoal(Guid NidGoal)
        {
            var goal = _requestProcessor.GetGoalWithoutDependancy(NidGoal);
            goal.GoalStatus = 1;
            if (_requestProcessor.PatchGoal(goal))
                TempData["GoalPageSuccess"] = $"goal edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing goal!";
            return RedirectToAction("Goals", "Schedules");
        }
        public ActionResult SubmitDeleteGoal(Guid NidGoal)
        {
            if (_requestProcessor.DeleteGoal(NidGoal))
                TempData["GoalSuccess"] = $"goal deleted successfully";
            else
                TempData["GoalError"] = $"an error occured while deleting goal!";
            return RedirectToAction("Goals", "Schedules");
        }
        public ActionResult OpenGoal(Guid NidGoal)
        {
            var goal = _requestProcessor.GetGoalWithoutDependancy(NidGoal);
            goal.GoalStatus = 0;
            if (_requestProcessor.PatchGoal(goal))
                TempData["GoalPageSuccess"] = $"goal edited successfully";
            else
                TempData["GoalPageError"] = $"an error occured while editing goal!";
            return RedirectToAction("Goals", "Schedules");
        }
        public ActionResult SubmitAddSchedule(Models.Schedule schedule, int Direction = 0)
        {
            _requestProcessor.PostSchedule(schedule);
            return RedirectToAction("IndexPaginationView", "Home", new { Direction = Direction });
        }
        public ActionResult SubmitAddProgress(Models.Progress progress, int Direction = 0)
        {
            progress.UserId = UserId;
            _requestProcessor.PostProgress(progress);
            return RedirectToAction("IndexPaginationView", "Home", new { Direction = Direction });
        }
        public ActionResult SubmitEditProgress(Models.Progress progress, int Direction = 0)
        {
            _requestProcessor.PatchProgress(progress);
            return RedirectToAction("IndexPaginationView", "Home", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteProgress(string NidProgress, int Direction = 0)
        {
            _requestProcessor.DeleteProgress(Guid.Parse(NidProgress));
            return RedirectToAction("IndexPaginationView", "Home", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteSchedule(string NidSchedule, int Direction = 0)
        {
            _requestProcessor.DeleteSchedule(Guid.Parse(NidSchedule));
            return RedirectToAction("IndexPaginationView", "Home", new { Direction = Direction });
        }

        //routine section
        public ActionResult Routines()
        {
            return View(_requestProcessor.GetRoutines(UserId));
        }
        [HttpPost]
        public ActionResult SubmitAddRoutine(Routine routine)
        {
            routine.UserId = UserId;
            if (_requestProcessor.PostRoutine(routine))
                TempData["RoutineSuccess"] = $"{routine.Title} created successfully";
            else
                TempData["RoutineError"] = $"an error occured while creating routine!";
            return RedirectToAction("Routines", "Schedules");
        }
        public ActionResult SubmitDeleteRoutine(Guid NidRoutine)
        {
            if (_requestProcessor.DeleteRoutine(NidRoutine))
                TempData["RoutineSuccess"] = $"routine deleted successfully";
            else
                TempData["RoutineError"] = $"an error occured while deleting routine!";
            return RedirectToAction("Routines", "Schedules");
        }
        public ActionResult SubmitDeleteRoutine2(Guid NidRoutine, int Direction = 0)
        {
            if (_requestProcessor.DeleteRoutine(NidRoutine))
                TempData["RoutineSuccess"] = $"routine deleted successfully";
            else
                TempData["RoutineError"] = $"an error occured while deleting routine!";
            return RedirectToAction("RoutineCalendar", "Schedules", new { Direction = Direction });
        }
        public ActionResult SubmitDoneRoutine(RoutineProgress Progress)
        {
            if (_requestProcessor.PostRoutineProgress(Progress))
                TempData["RoutineSuccess"] = $"routine done successfully";
            else
                TempData["RoutineError"] = $"an error occured while doning routine!";
            return RedirectToAction("Routines", "Schedules");
        }
        public ActionResult SubmitUnDoneRoutine(RoutineProgress Progress)
        {
            var progresses = _requestProcessor.GetRoutineProgresses(Progress.RoutineId);
            progresses.ForEach(x => { if (x.ProgressDate == Progress.ProgressDate.Date) _requestProcessor.DeleteRoutineProgress(x.NidRoutineProgress); });
            return RedirectToAction("Routines", "Schedules");
        }
        public ActionResult RoutineCalendar(int Direction = 0)
        {
            return View(_requestProcessor.GetRoutines(UserId,Direction));
        }
        public ActionResult IndexPagination2(int Direction)
        {
            return Json(new JsonResults() { HasValue = true, Html = Helpers.ViewHelper.RenderViewToString(this, "_RoutineCalendarPartialView", _requestProcessor.GetRoutines(UserId, Direction)) });
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
            return RedirectToAction("RoutineCalendar", "Schedules", new { Direction = Direction });
        }
        public ActionResult SubmitDeleteRoutineProgress(Guid NidRoutineProgress, int Direction = 0)
        {
            _requestProcessor.DeleteRoutineProgress(NidRoutineProgress);
            return RedirectToAction("RoutineCalendar", "Schedules", new { Direction = Direction });
        }
        public ActionResult SubmitAddRoutineProgress(RoutineProgress Progress, int Direction = 0)
        {
            if (_requestProcessor.PostRoutineProgress(Progress))
                TempData["RoutineSuccess"] = $"routine done successfully";
            else
                TempData["RoutineError"] = $"an error occured while doning routine!";
            return RedirectToAction("RoutineCalendar", "Schedules", new { Direction = Direction });
        }
    }
}