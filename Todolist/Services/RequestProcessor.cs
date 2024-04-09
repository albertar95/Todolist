using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Todolist.Models;
using Todolist.Models.Dto;
using Todolist.Services.Contracts;
using Todolist.ViewModels;

namespace Todolist.Services
{
    public class RequestProcessor : IRequestProcessor
    {
        private readonly IDbRepository _dbRepository;
        public RequestProcessor(IDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        public bool DeleteAccount(Guid nidAccount)
        {
            throw new NotImplementedException();
        }

        public bool DeleteGoal(Guid nidGoal)
        {
            var goal = _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
            if (goal != null)
                return _dbRepository.Delete<Goal>(goal);
            else
                return false;
        }

        public bool DeleteNote(Guid nidNote)
        {
            var note = _dbRepository.Get<Note>(p => p.NidNote == nidNote);
            if (note != null)
                return _dbRepository.Delete<Note>(note);
            else
                return false;
        }

        public bool DeleteNoteGroup(Guid nidNoteGroup)
        {
            var noteGroup = _dbRepository.Get<NoteGroup>(p => p.NidGroup == nidNoteGroup);
            if (noteGroup != null)
                return _dbRepository.Delete<NoteGroup>(noteGroup);
            else
                return false;
        }

        public bool DeleteProgress(Guid nidProgress)
        {
            var progress = _dbRepository.Get<Progress>(p => p.NidProgress == nidProgress);
            if (progress != null)
                return _dbRepository.Delete<Progress>(progress);
            else
                return false;
        }

        public bool DeleteSchedule(Guid nidSchedule)
        {
            var schedule = _dbRepository.Get<Schedule>(p => p.NidSchedule == nidSchedule);
            if (schedule != null)
                return _dbRepository.Delete<Schedule>(schedule);
            else
                return false;
        }

        public bool DeleteTask(Guid nidTask)
        {
            var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
            if (task != null)
                return _dbRepository.Delete<Task>(task);
            else
                return false;
        }

        public bool DeleteTransaction(Guid nidTransaction)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUser(Guid nidUser)
        {
            var user = _dbRepository.Get<User>(p => p.NidUser == nidUser);
            if (user != null)
                return _dbRepository.Delete<User>(user);
            else
                return false;
        }

        public bool DoneTask(Guid nidTask)
        {
            var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
            task.TaskStatus = true;
            task.ClosureDate = DateTime.Now;
            task.LastModifiedDate = DateTime.Now;
            return _dbRepository.Update<Task>(task);
        }

        public Account GetAccount(Guid nidAccount)
        {
            throw new NotImplementedException();
        }

        public FinanceViewModel GetFinacialRecords(Guid nidUser,bool includeAll = false)
        {
            var result = new FinanceViewModel();
            result.Accounts = _dbRepository.GetList<Account>(p => p.UserId == nidUser);
            result.AllTransactions = includeAll;
            if(includeAll)
                result.Transactions = _dbRepository.GetList<Transaction>(p => p.UserId == nidUser);
            else
            {
                PersianCalendar pc = new PersianCalendar();
                var StartOfMonth = pc.ToDateTime(pc.GetYear(DateTime.Now), pc.GetMonth(DateTime.Now), 1, 0, 0, 0, 0);
                var EndOfMonth = pc.ToDateTime(pc.GetYear(StartOfMonth.AddMonths(1).AddDays(3)), pc.GetMonth(StartOfMonth.AddMonths(1).AddDays(3)), 1, 0, 0, 0, 0);
                result.Transactions = _dbRepository.GetList<Transaction>(p => p.UserId == nidUser && p.CreateDate.Date >= StartOfMonth && p.CreateDate < EndOfMonth);
            }
            return result;
        }

        public GoalPageViewModel GetGoal(Guid nidGoal)
        {
            var result = new GoalPageViewModel() { Tasks = new List<Task>(), Goal = new Goal(), Progresses = new List<Progress>() };
            result.Goal = _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
            result.Tasks = _dbRepository.GetList<Task>(p => p.GoalId == nidGoal);
            result.Progresses = _dbRepository.GetList<Progress>(p => p.Schedule.Task.GoalId == nidGoal);
            return result;
        }

        public GoalViewModel GetGoals(Guid nidUser)
        {
            var result = new GoalViewModel() { Tasks = new List<Task>(), Goals = new List<Goal>() };
            result.Goals = _dbRepository.GetList<Goal>(p => p.UserId == nidUser);
            result.Goals.ToList().ForEach(x => { result.Tasks.AddRange(_dbRepository.GetList<Task>(p => p.GoalId == x.NidGoal)); });
            return result;
        }

        public Goal GetGoalWithoutDependancy(Guid nidGoal)
        {
            return _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
        }

        public IndexViewModel GetIndex(Guid nidUser, int Direction = 0)
        {
            var result = new IndexViewModel();
            result.AllGoals = _dbRepository.GetList<Goal>(p => p.UserId == nidUser);
            result.Goals = result.AllGoals.Where(p => p.GoalStatus == 0).ToList();
            result.AllGoals.ForEach(x => { result.AllTasks.AddRange(_dbRepository.GetList<Task>(p => p.GoalId == x.NidGoal)); });
            result.Tasks = result.AllTasks.Where(p => p.TaskStatus == false && result.Goals.GroupBy(x => x.NidGoal).Select(q => q.Key).ToList()
            .Contains(p.GoalId)).ToList() ?? new List<Models.Task>();
            result.AllTasks.ForEach(x => { result.Schedules.AddRange(_dbRepository.GetList<Schedule>(p => p.TaskId == x.NidTask)); });
            result.Schedules.ForEach(x => { result.Progresses.AddRange(_dbRepository.GetList<Progress>(p => p.ScheduleId == x.NidSchedule)); });
            string[] DatePeriod = new string[3];
            var weekDates = Helpers.Dates.GetWeekPeriod(Direction);
            DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
            DatePeriod[1] = $"{Direction - 1}";
            DatePeriod[2] = $"{Direction + 1}";
            var persianDates = Helpers.Dates.ToPersianDate(weekDates);
            result.DatePeriodInfo = DatePeriod;
            result.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
            result.StartDate = weekDates.Item1;
            result.EndDate = weekDates.Item2;
            return result;
        }

        public Note GetNote(Guid nidNote)
        {
            return _dbRepository.Get<Note>(p => p.NidNote == nidNote);
        }

        public NotesViewModel GetNoteGroup(Guid nidNoteGroup)
        {
            var result = new NotesViewModel();
            result.Group = _dbRepository.Get<NoteGroup>(p => p.NidGroup == nidNoteGroup);
            result.Notes = _dbRepository.GetList<Note>(p => p.GroupId == nidNoteGroup);
            return result;
        }

        public List<NoteGroup> GetNoteGroups(Guid nidUser)
        {
            return _dbRepository.GetList<NoteGroup>(p => p.UserId == nidUser);
        }

        public List<Note> GetNotes(Guid nidGroup)
        {
            return _dbRepository.GetList<Note>(p => p.GroupId == nidGroup);
        }

        public Task GetTask(Guid nidTask)
        {
            return _dbRepository.Get<Task>(p => p.NidTask == nidTask);
        }

        public Transaction GetTransaction(Guid nidTransaction)
        {
            throw new NotImplementedException();
        }

        public List<User> GetUsers()
        {
            return _dbRepository.GetList<User>();
        }

        public UserLoginDto LoginUser(string username, string password)
        {
            var user = _dbRepository.Get<User>(p => p.Username == username.Trim() && p.Password == Helpers.Encryption.EncryptString(password.Trim()));
            if (user != null)
                return new UserLoginDto() {  IsSuccessful = true, IsAdmin = user.IsAdmin, NidUser = user.NidUser, Username = user.Username };
            else
                return new UserLoginDto() { IsSuccessful = false };
        }

        public bool PatchAccount(Account account)
        {
            throw new NotImplementedException();
        }

        public bool PatchGoal(Goal goal)
        {
            goal.LastModifiedDate = DateTime.Now;
            return _dbRepository.Update<Goal>(goal);
        }

        public bool PatchNote(Note note)
        {
            note.ModifiedDate = DateTime.Now;
            return _dbRepository.Update<Note>(note);
        }

        public bool PatchNoteGroup(NoteGroup noteGroup)
        {
            noteGroup.ModifiedDate = DateTime.Now;
            return _dbRepository.Update<NoteGroup>(noteGroup);
        }

        public bool PatchProgress(Progress progress)
        {
            return _dbRepository.Update<Progress>(progress);
        }

        public bool PatchTask(Task task)
        {
            return _dbRepository.Update<Task>(task);
        }

        public bool PatchTransaction(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public bool PostAccount(Account account)
        {
            throw new NotImplementedException();
        }

        public bool PostGoal(Goal goal)
        {
            goal.NidGoal = Guid.NewGuid();
            goal.CreateDate = DateTime.Now;
            goal.GoalStatus = 0;
            goal.LastModifiedDate = DateTime.Now;
            return _dbRepository.Add<Goal>(goal);
        }

        public bool PostNote(Note note)
        {
            note.NidNote = Guid.NewGuid();
            note.CreateDate = DateTime.Now;
            note.ModifiedDate = DateTime.Now;
            return _dbRepository.Add<Note>(note);
        }

        public bool PostNoteGroup(NoteGroup noteGroup)
        {
            noteGroup.NidGroup = Guid.NewGuid();
            noteGroup.CreateDate = DateTime.Now;
            noteGroup.ModifiedDate = DateTime.Now;
            return _dbRepository.Add<NoteGroup>(noteGroup);
        }

        public bool PostProgress(Progress progress)
        {
            progress.NidProgress = Guid.NewGuid();
            progress.CreateDate = DateTime.Now;
            return _dbRepository.Add<Progress>(progress);
        }

        public bool PostSchedule(Schedule schedule)
        {
            schedule.NidSchedule = Guid.NewGuid();
            schedule.CreateDate = DateTime.Now;
            return _dbRepository.Add<Schedule>(schedule);
        }

        public bool PostTask(Task task)
        {
            task.NidTask = Guid.NewGuid();
            task.CreateDate = DateTime.Now;
            task.TaskStatus = false;
            task.LastModifiedDate = DateTime.Now;
            return _dbRepository.Add<Task>(task);
        }

        public bool PostTransaction(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public bool PostUser(User user)
        {
            user.NidUser = Guid.NewGuid();
            user.CreateDate = DateTime.Now;
            user.Password = Helpers.Encryption.EncryptString(user.Password);
            return _dbRepository.Add<User>(user);
        }

        public bool UndoTask(Guid nidTask)
        {
            var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
            task.TaskStatus = false;
            task.ClosureDate = null;
            task.LastModifiedDate = DateTime.Now;
            return _dbRepository.Update<Task>(task);
        }
    }
}