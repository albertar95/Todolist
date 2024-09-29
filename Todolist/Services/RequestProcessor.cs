using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Todolist.Helpers;
using Todolist.Models;
using Todolist.Models.Dto;
using Todolist.Services.Contracts;
using Todolist.ViewModels;
using static Todolist.Models.TradeModels;

namespace Todolist.Services
{
    public class RequestProcessor : IRequestProcessor
    {
        private readonly IDbRepository _dbRepository;
        public RequestProcessor(IDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        public bool ConvertShields()
        {
            try
            {
                bool result = true;
                var users = _dbRepository.GetList<User>();
                var decryptedUsers = users;
                users.ForEach(x => { decryptedUsers.FirstOrDefault(p => p.NidUser == x.NidUser).Password = Helpers.Encryption.DecryptString(x.Password); });
                var newUsers = decryptedUsers;
                decryptedUsers.ForEach(x => { newUsers.FirstOrDefault(p => p.NidUser == x.NidUser).Password = Helpers.Encryption.Sha256Hash(x.Password); });
                newUsers.ForEach(x => { if (!_dbRepository.Update(x)) result = false; });
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteAccount(Guid nidAccount)
        {
            try
            {
                var account = _dbRepository.Get<Account>(p => p.NidAccount == nidAccount);
                if (account != null)
                    return _dbRepository.Delete<Account>(account);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteGoal(Guid nidGoal)
        {
            try
            {
                var goal = _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
                if (goal != null)
                    return _dbRepository.Delete<Goal>(goal);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteNote(Guid nidNote)
        {
            try
            {
                var note = _dbRepository.Get<Note>(p => p.NidNote == nidNote);
                if (note != null)
                    return _dbRepository.Delete<Note>(note);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteNoteGroup(Guid nidNoteGroup)
        {
            try
            {
                var noteGroup = _dbRepository.Get<NoteGroup>(p => p.NidGroup == nidNoteGroup);
                if (noteGroup != null)
                    return _dbRepository.Delete<NoteGroup>(noteGroup);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteProgress(Guid nidProgress)
        {
            try
            {
                var progress = _dbRepository.Get<Progress>(p => p.NidProgress == nidProgress);
                if (progress != null)
                    return _dbRepository.Delete<Progress>(progress);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteRoutine(Guid nidRoutine)
        {
            try
            {
                var routine = _dbRepository.Get<Routine>(p => p.NidRoutine == nidRoutine);
                if (routine != null)
                    return _dbRepository.Delete<Routine>(routine);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteRoutineProgress(Guid nidRoutineProgress)
        {
            try
            {
                var routineProg = _dbRepository.Get<RoutineProgress>(p => p.NidRoutineProgress == nidRoutineProgress);
                if (routineProg != null)
                    return _dbRepository.Delete<RoutineProgress>(routineProg);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteSchedule(Guid nidSchedule)
        {
            try
            {
                var schedule = _dbRepository.Get<Schedule>(p => p.NidSchedule == nidSchedule);
                if (schedule != null)
                    return _dbRepository.Delete<Schedule>(schedule);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteShield(Guid nidShield)
        {
            try
            {
                var shield = _dbRepository.Get<Shield>(p => p.Id == nidShield);
                if (shield != null)
                    return _dbRepository.Delete<Shield>(shield);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteTask(Guid nidTask)
        {
            try
            {
                var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
                if (task != null)
                    return _dbRepository.Delete<Task>(task);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteTransaction(Guid nidTransaction)
        {
            try
            {
                var tran = _dbRepository.Get<Transaction>(p => p.NidTransaction == nidTransaction);
                if (tran != null)
                {
                    if (UndoTransaction(tran))
                        return _dbRepository.Delete<Transaction>(tran);
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteUser(Guid nidUser)
        {
            try
            {
                var user = _dbRepository.Get<User>(p => p.NidUser == nidUser);
                if (user != null)
                    return _dbRepository.Delete<User>(user);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DoneTask(Guid nidTask)
        {
            try
            {
                var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
                task.TaskStatus = true;
                task.ClosureDate = DateTime.Now;
                task.LastModifiedDate = DateTime.Now;
                return _dbRepository.Update<Task>(task);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Account GetAccount(Guid nidAccount)
        {
            try
            {
                return _dbRepository.Get<Account>(p => p.NidAccount == nidAccount);
            }
            catch (Exception)
            {
                return new Account();
            }
        }

        public FinanceViewModel GetFinacialRecords(Guid nidUser,bool includeAll = false)
        {
            var result = new FinanceViewModel();
            try
            {
                result.Accounts = _dbRepository.GetList<Account>(p => p.UserId == nidUser);
                result.AllTransactions = includeAll;
                PersianCalendar pc = new PersianCalendar();
                var StartOfMonth = pc.ToDateTime(pc.GetYear(DateTime.Now), pc.GetMonth(DateTime.Now), 1, 0, 0, 0, 0);
                var EndOfMonth = pc.ToDateTime(pc.GetYear(StartOfMonth.AddMonths(1).AddDays(3)), pc.GetMonth(StartOfMonth.AddMonths(1).AddDays(3)), 1, 0, 0, 0, 0);
                if (includeAll)
                    result.Transactions = _dbRepository.GetList<Transaction>(p => p.UserId == nidUser);
                else
                    result.Transactions = _dbRepository.GetList<Transaction>(p => p.UserId == nidUser).Where(q => q.CreateDate >= StartOfMonth && q.CreateDate < EndOfMonth).ToList();
                var externalAccounts = _dbRepository.GetList<Account>(p => p.IsBackup == true && p.IsActive == true).GroupBy(p => p.NidAccount).Select(q => q.Key).ToList();
                result.ExternalTransactions = _dbRepository.GetList<Transaction>(p => p.UserId == nidUser)
                    .Where(q => q.CreateDate >= StartOfMonth && q.CreateDate < EndOfMonth && (externalAccounts.Contains(q.PayerAccount) || externalAccounts.Contains(q.RecieverAccount))).ToList();
                result.StartOfMonth = StartOfMonth.Date;
                result.Groups = _dbRepository.GetList<TransactionGroup>();
            }
            catch (Exception)
            {
            }
            return result;
        }

        public GoalPageViewModel GetGoal(Guid nidGoal)
        {
            var result = new GoalPageViewModel() { Tasks = new List<Task>(), Goal = new Goal(), Progresses = new List<Progress>() };
            try
            {
                result.Goal = _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
                result.Tasks = _dbRepository.GetList<Task>(p => p.GoalId == nidGoal);
                result.Progresses = _dbRepository.GetList<Progress>(p => p.Schedule.Task.GoalId == nidGoal);
            }
            catch (Exception)
            {
            }
            return result;
        }

        public GoalViewModel GetGoals(Guid nidUser)
        {
            var result = new GoalViewModel() { Tasks = new List<Task>(), Goals = new List<Goal>() };
            try
            {
                result.Goals = _dbRepository.GetList<Goal>(p => p.UserId == nidUser);
                result.Goals.ToList().ForEach(x => { result.Tasks.AddRange(_dbRepository.GetList<Task>(p => p.GoalId == x.NidGoal)); });
            }
            catch (Exception)
            {
            }
            return result;
        }

        public Goal GetGoalWithoutDependancy(Guid nidGoal)
        {
            try
            {
                return _dbRepository.Get<Goal>(p => p.NidGoal == nidGoal);
            }
            catch (Exception)
            {
                return new Goal();
            }
        }

        public IndexViewModel GetIndex(Guid nidUser, int Direction = 0)
        {
            var result = new IndexViewModel() { AllTasks = new List<Task>(), Schedules = new List<Schedule>(), Progresses = new List<Progress>() };
            try
            {
                string[] DatePeriod = new string[3];
                var weekDates = Helpers.Dates.GetWeekPeriod(Direction);
                DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
                DatePeriod[1] = $"{Direction - 1}";
                DatePeriod[2] = $"{Direction + 1}";
                var persianDates = Helpers.Dates.ToPersianDate(weekDates);
                result.AllGoals = _dbRepository.GetList<Goal>(p => p.UserId == nidUser);
                result.Goals = result.AllGoals.Where(p => p.GoalStatus == 0).ToList();
                result.AllGoals.ForEach(x => { result.AllTasks.AddRange(_dbRepository.GetList<Task>(p => p.GoalId == x.NidGoal)); });
                result.Tasks = result.AllTasks.Where(p => p.TaskStatus == false && result.Goals.GroupBy(x => x.NidGoal).Select(q => q.Key).ToList()
                .Contains(p.GoalId)).ToList() ?? new List<Models.Task>();
                result.AllTasks.ForEach(x => { result.Schedules.AddRange(_dbRepository.GetList<Schedule>(p => p.TaskId == x.NidTask).Where(p => p.ScheduleDate >= weekDates.Item1 && p.ScheduleDate <= weekDates.Item2)); });
                result.Schedules.ForEach(x => { result.Progresses.AddRange(_dbRepository.GetList<Progress>(p => p.ScheduleId == x.NidSchedule)); });
                result.DatePeriodInfo = DatePeriod;
                result.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
                result.StartDate = weekDates.Item1;
                result.EndDate = weekDates.Item2;
            }
            catch (Exception)
            {
            }
            return result;
        }

        public Note GetNote(Guid nidNote)
        {
            try
            {
                return _dbRepository.Get<Note>(p => p.NidNote == nidNote);
            }
            catch (Exception)
            {
                return new Note();
            }
        }

        public NotesViewModel GetNoteGroup(Guid nidNoteGroup)
        {
            var result = new NotesViewModel();
            try
            {
                result.Group = _dbRepository.Get<NoteGroup>(p => p.NidGroup == nidNoteGroup);
                result.Notes = _dbRepository.GetList<Note>(p => p.GroupId == nidNoteGroup);
            }
            catch (Exception)
            {
            }
            return result;
        }

        public List<NoteGroup> GetNoteGroups(Guid nidUser)
        {
            try
            {
                return _dbRepository.GetList<NoteGroup>(p => p.UserId == nidUser);
            }
            catch (Exception)
            {
                return new List<NoteGroup>();
            }
        }

        public List<Note> GetNotes(Guid nidGroup)
        {
            try
            {
                return _dbRepository.GetList<Note>(p => p.GroupId == nidGroup);
            }
            catch (Exception)
            {
                return new List<Note>();
            }
        }

        public Routine GetRoutine(Guid nidRoutine)
        {
            try
            {
                return _dbRepository.Get<Routine>(p => p.NidRoutine == nidRoutine);
            }
            catch (Exception)
            {
                return new Routine();
            }
        }

        public List<RoutineProgress> GetRoutineProgresses(Guid nidRoutine)
        {
            try
            {
                return _dbRepository.GetList<RoutineProgress>(p => p.RoutineId == nidRoutine);
            }
            catch (Exception)
            {
                return new List<RoutineProgress>();
            }
        }

        public RoutineViewModel GetRoutines(Guid nidUser, int Direction = 0)
        {
            var result = new RoutineViewModel() {  RoutineProgresses = new List<RoutineProgress>() };
            try
            {
                result.Routines = _dbRepository.GetList<Routine>(p => p.UserId == nidUser);
                string[] DatePeriod = new string[3];
                var weekDates = Helpers.Dates.GetWeekPeriod(Direction);
                DatePeriod[0] = $"{weekDates.Item1.ToString("dd/MM/yyyy")} - {weekDates.Item2.ToString("dd/MM/yyyy")}";
                DatePeriod[1] = $"{Direction - 1}";
                DatePeriod[2] = $"{Direction + 1}";
                var persianDates = Helpers.Dates.ToPersianDate(weekDates);
                result.Routines.ForEach(x => { result.RoutineProgresses.AddRange(_dbRepository.GetList<RoutineProgress>(p => p.RoutineId == x.NidRoutine)); });
                result.PersianDatePeriodInfo = new string[] { persianDates.Item1, persianDates.Item2 };
                result.DatePeriodInfo = DatePeriod;
                result.StartDate = weekDates.Item1;
                result.EndDate = weekDates.Item2;
            }
            catch (Exception)
            {
            }
            return result;
        }

        public Shield GetShield(Guid nidShield)
        {
            try
            {
                return _dbRepository.Get<Shield>(p => p.Id == nidShield);
            }
            catch (Exception)
            {
                return new Shield();
            }
        }

        public List<Shield> GetShields(Guid nidUser)
        {
            try
            {
                return _dbRepository.GetList<Shield>(p => p.UserId == nidUser);
            }
            catch (Exception)
            {
                return new List<Shield>();
            }
        }

        public Task GetTask(Guid nidTask)
        {
            try
            {
                return _dbRepository.Get<Task>(p => p.NidTask == nidTask);
            }
            catch (Exception)
            {
                return new Task();
            }
        }

        public Transaction GetTransaction(Guid nidTransaction)
        {
            try
            {
                return _dbRepository.Get<Transaction>(p => p.NidTransaction == nidTransaction);
            }
            catch (Exception)
            {
                return new Transaction();
            }
        }

        public List<User> GetUsers()
        {
            try
            {
                return _dbRepository.GetList<User>();
            }
            catch (Exception)
            {
                return new List<User>();
            }
        }

        public List<LendDetailViewModel> LendDetails(Guid nidAccount)
        {
            var result2 = new List<LendDetailViewModel>();
            try
            {
                var lendTrs = _dbRepository.GetList<Transaction>(p => p.PayerAccount == nidAccount && p.TransactionType == 2)
                .GroupBy(q => q.RecieverAccount).Select(w => new { nidAcc = w.Key, amount = w.Sum(x => x.Amount) }).ToList();
                var lendBackTrs = _dbRepository.GetList<Transaction>(p => p.RecieverAccount == nidAccount && p.TransactionType == 3)
                    .GroupBy(q => q.PayerAccount).Select(w => new { nidAcc = w.Key, amount = w.Sum(x => x.Amount) }).ToList();
                var result = new List<LendDetailViewModel>();
                lendTrs.ForEach(x => { result.Add(new LendDetailViewModel() { NidAccount = x.nidAcc, Amount = x.amount }); });
                foreach (var lb in lendBackTrs)
                {
                    if (lendTrs.Any(p => p.nidAcc == lb.nidAcc))
                    {
                        if (lb.amount <= lendTrs.FirstOrDefault(p => p.nidAcc == lb.nidAcc).amount)
                        {
                            var oldAmount = result.FirstOrDefault(p => p.NidAccount == lb.nidAcc).Amount;
                            result.Remove(result.FirstOrDefault(p => p.NidAccount == lb.nidAcc));
                            result.Add(new LendDetailViewModel() { NidAccount = lb.nidAcc, Amount = oldAmount - lb.amount });
                        }
                    }
                }
                foreach (var ln in result)
                {
                    if (ln.Amount != 0)
                    {
                        if (_dbRepository.Get<Account>(p => p.NidAccount == ln.NidAccount).IsActive)
                            result2.Add(new LendDetailViewModel() { NidAccount = ln.NidAccount, Amount = ln.Amount, AccountName = _dbRepository.Get<Account>(p => p.NidAccount == ln.NidAccount).Title });
                    }
                }
            }
            catch (Exception)
            {
            }
            return result2;
        }

        public UserLoginDto LoginUser(string username, string password)
        {
            try
            {
                password = Helpers.Encryption.Sha256Hash(password.Trim());
                var user = _dbRepository.Get<User>(p => p.Username == username.Trim() && p.Password == password);
                if (user != null)
                    return new UserLoginDto() { IsSuccessful = true, IsAdmin = user.IsAdmin, NidUser = user.NidUser, Username = user.Username };
                else
                    return new UserLoginDto() { IsSuccessful = false };
            }
            catch (Exception)
            {
                return new UserLoginDto() { IsSuccessful = false };
            }
        }

        public bool PatchAccount(Account account)
        {
            try
            {
                account.LastModified = DateTime.Now;
                return _dbRepository.Update<Account>(account);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchGoal(Goal goal)
        {
            try
            {
                goal.LastModifiedDate = DateTime.Now;
                return _dbRepository.Update<Goal>(goal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchNote(Note note)
        {
            try
            {
                note.ModifiedDate = DateTime.Now;
                return _dbRepository.Update<Note>(note);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchNoteGroup(NoteGroup noteGroup)
        {
            try
            {
                noteGroup.ModifiedDate = DateTime.Now;
                return _dbRepository.Update<NoteGroup>(noteGroup);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchProgress(Progress progress)
        {
            try
            {
                return _dbRepository.Update<Progress>(progress);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchRoutine(Routine routine)
        {
            try
            {
                return _dbRepository.Update<Routine>(routine);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchShield(Shield shield)
        {
            try
            {
                shield.LastModified = DateTime.Now;
                shield.Password = Helpers.Encryption.RSAEncrypt(shield.Password);
                return _dbRepository.Update<Shield>(shield);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchTask(Task task)
        {
            try
            {
                return _dbRepository.Update<Task>(task);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PatchTransaction(Transaction transaction)
        {
            try
            {
                return _dbRepository.Update<Transaction>(transaction);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostAccount(Account account)
        {
            try
            {
                account.NidAccount = Guid.NewGuid();
                account.CreateDate = DateTime.Now;
                account.LastModified = DateTime.Now;
                return _dbRepository.Add<Account>(account);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostGoal(Goal goal)
        {
            try
            {
                goal.NidGoal = Guid.NewGuid();
                goal.CreateDate = DateTime.Now;
                goal.GoalStatus = 0;
                goal.LastModifiedDate = DateTime.Now;
                return _dbRepository.Add<Goal>(goal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostNote(Note note)
        {
            try
            {
                note.NidNote = Guid.NewGuid();
                note.CreateDate = DateTime.Now;
                note.ModifiedDate = DateTime.Now;
                return _dbRepository.Add<Note>(note);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostNoteGroup(NoteGroup noteGroup)
        {
            try
            {
                noteGroup.NidGroup = Guid.NewGuid();
                noteGroup.CreateDate = DateTime.Now;
                noteGroup.ModifiedDate = DateTime.Now;
                return _dbRepository.Add<NoteGroup>(noteGroup);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostProgress(Progress progress)
        {
            try
            {
                progress.NidProgress = Guid.NewGuid();
                progress.CreateDate = DateTime.Now;
                return _dbRepository.Add<Progress>(progress);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostRoutine(Routine routine)
        {
            try
            {
                routine.NidRoutine = Guid.NewGuid();
                routine.CreateDate = DateTime.Now;
                routine.Status = false;
                return _dbRepository.Add<Routine>(routine);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostRoutineProgress(RoutineProgress routineProgress)
        {
            try
            {
                routineProgress.NidRoutineProgress = Guid.NewGuid();
                routineProgress.CreateDate = DateTime.Now;
                return _dbRepository.Add<RoutineProgress>(routineProgress);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostSchedule(Schedule schedule)
        {
            try
            {
                schedule.NidSchedule = Guid.NewGuid();
                schedule.CreateDate = DateTime.Now;
                return _dbRepository.Add<Schedule>(schedule);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostShield(Shield shield)
        {
            try
            {
                shield.Id = Guid.NewGuid();
                shield.CreateDate = DateTime.Now;
                shield.Password = Helpers.Encryption.RSAEncrypt(shield.Password);
                return _dbRepository.Add<Shield>(shield);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostTask(Task task)
        {
            try
            {
                task.NidTask = Guid.NewGuid();
                task.CreateDate = DateTime.Now;
                task.TaskStatus = false;
                task.LastModifiedDate = DateTime.Now;
                return _dbRepository.Add<Task>(task);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostTransaction(Transaction transaction)
        {
            try
            {
                if (PerformTransaction(transaction))
                {
                    transaction.NidTransaction = Guid.NewGuid();
                    transaction.CreateDate = DateTime.Now;
                    return _dbRepository.Add<Transaction>(transaction);
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PostUser(User user)
        {
            try
            {
                user.NidUser = Guid.NewGuid();
                user.CreateDate = DateTime.Now;
                user.Password = Helpers.Encryption.Sha256Hash(user.Password);
                return _dbRepository.Add<User>(user);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UndoTask(Guid nidTask)
        {
            try
            {
                var task = _dbRepository.Get<Task>(p => p.NidTask == nidTask);
                task.TaskStatus = false;
                task.ClosureDate = null;
                task.LastModifiedDate = DateTime.Now;
                return _dbRepository.Update<Task>(task);
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool PerformTransaction(Transaction Transaction)
        {
            try
            {
                bool IsAccUpdate = false;
                bool IsConditionOk = false;
                var payer = _dbRepository.Get<Account>(p => p.NidAccount == Transaction.PayerAccount);
                var reciever = _dbRepository.Get<Account>(p => p.NidAccount == Transaction.RecieverAccount);
                if (payer != null)
                {
                    if (Transaction.Amount <= payer.Amount)
                    {
                        payer.Amount = payer.Amount - Transaction.Amount;
                        payer.LastModified = DateTime.Now;
                        if (Transaction.TransactionType == 2)
                            payer.LendAmount = payer.LendAmount + Transaction.Amount;
                        if (reciever != null)
                        {
                            reciever.Amount = reciever.Amount + Transaction.Amount;
                            if (Transaction.TransactionType == 3)
                            {
                                if (Transaction.Amount <= reciever.LendAmount)
                                {
                                    reciever.LendAmount = reciever.LendAmount - Transaction.Amount;
                                    IsConditionOk = true;
                                }
                            }
                            else
                                IsConditionOk = true;
                        }
                        if (IsConditionOk)
                        {
                            if (_dbRepository.Update<Account>(payer))
                                IsAccUpdate = _dbRepository.Update<Account>(reciever);
                        }
                    }
                }
                return IsAccUpdate;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool UndoTransaction(Transaction Transaction)
        {
            bool IsAccountOk = false;
            try
            {
                var payer = _dbRepository.Get<Account>(p => p.NidAccount == Transaction.PayerAccount);
                var reciever = _dbRepository.Get<Account>(p => p.NidAccount == Transaction.RecieverAccount);
                if (payer != null && reciever != null)
                {
                    switch (Transaction.TransactionType)
                    {
                        case 1://pay
                            if (reciever.Amount >= Transaction.Amount)
                            {
                                reciever.Amount = reciever.Amount - Transaction.Amount;
                                payer.Amount = payer.Amount + Transaction.Amount;
                                if(_dbRepository.Update(reciever))
                                {
                                    if (_dbRepository.Update(payer))
                                        IsAccountOk = true;
                                    else
                                    {
                                        reciever.Amount = reciever.Amount + Transaction.Amount;
                                        _dbRepository.Update(reciever);
                                    }
                                }
                            }
                            break;
                        case 2://lend
                            if (reciever.Amount >= Transaction.Amount)
                            {
                                reciever.Amount = reciever.Amount - Transaction.Amount;
                                payer.Amount = payer.Amount + Transaction.Amount;
                                if (payer.LendAmount >= Transaction.Amount)
                                {
                                    payer.LendAmount = payer.LendAmount - Transaction.Amount;
                                    if(_dbRepository.Update(reciever))
                                    {
                                        if(_dbRepository.Update(payer))
                                            IsAccountOk = true;
                                        else
                                        {
                                            reciever.Amount = reciever.Amount + Transaction.Amount;
                                            _dbRepository.Update(reciever);
                                        }
                                    }
                                }
                            }
                            break;
                        case 3://lend back
                            if (reciever.Amount >= Transaction.Amount)
                            {
                                reciever.Amount = reciever.Amount - Transaction.Amount;
                                reciever.LendAmount = reciever.LendAmount + Transaction.Amount;
                                payer.Amount = payer.Amount + Transaction.Amount;
                                if(_dbRepository.Update(reciever))
                                {
                                    if(_dbRepository.Update(payer))
                                        IsAccountOk = true;
                                    else
                                    {
                                        reciever.Amount = reciever.Amount + Transaction.Amount;
                                        _dbRepository.Update(reciever);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
            return IsAccountOk;
        }

        public List<TransactionGroup> GetTransactionGroups(Guid nidUser, bool includeAll = false)
        {
            try
            {
                if (!includeAll)
                    return _dbRepository.GetList<TransactionGroup>(p => p.UserId == nidUser && p.IsActive == true);
                else
                    return _dbRepository.GetList<TransactionGroup>(p => p.UserId == nidUser);
            }
            catch (Exception)
            {
                return new List<TransactionGroup>();
            }
        }
        public bool PostTransactionGroups(TransactionGroup transactionGroup)
        {
            try
            {
                transactionGroup.NidTransactionGroup = Guid.NewGuid();
                transactionGroup.CreateDate = DateTime.Now;
                return _dbRepository.Add<TransactionGroup>(transactionGroup);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public TransactionGroup GetTransactionGroup(Guid nidTransactionGroup)
        {
            try
            {
                return _dbRepository.Get<TransactionGroup>(p => p.NidTransactionGroup == nidTransactionGroup);
            }
            catch (Exception)
            {
                return new TransactionGroup();
            }
        }
        public bool DeleteTransactionGroup(Guid nidTransactionGroup)
        {
            try
            {
                var transactionGroup = _dbRepository.Get<TransactionGroup>(p => p.NidTransactionGroup == nidTransactionGroup);
                if (transactionGroup != null)
                {
                    if (!_dbRepository.GetList<Transaction>(p => p.TransactionGroupId == nidTransactionGroup).Any())
                        return _dbRepository.Delete<TransactionGroup>(transactionGroup);
                    else
                    {
                        transactionGroup.IsActive = false;
                        return _dbRepository.Update(transactionGroup);
                    }
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public EditTransactionViewModel GetEditTransaction(Guid nidTransaction,Guid nidUser)
        {
            var result = new EditTransactionViewModel();
            try
            {
                result.Accounts = _dbRepository.GetList<Account>(p => p.UserId == nidUser);
                PersianCalendar pc = new PersianCalendar();
                var StartOfMonth = pc.ToDateTime(pc.GetYear(DateTime.Now), pc.GetMonth(DateTime.Now), 1, 0, 0, 0, 0);
                var EndOfMonth = pc.ToDateTime(pc.GetYear(StartOfMonth.AddMonths(1).AddDays(3)), pc.GetMonth(StartOfMonth.AddMonths(1).AddDays(3)), 1, 0, 0, 0, 0);
                result.Groups = _dbRepository.GetList<TransactionGroup>(p => p.IsActive == true);
                result.Transaction = _dbRepository.Get<Transaction>(p => p.NidTransaction == nidTransaction);
                var trIds = _dbRepository.GetList<Transaction>().OrderBy(p => p.CreateDate).ToArray();
                var nextSelected = Array.FindIndex(trIds, p => p.NidTransaction == nidTransaction) + 1;
                result.NextTrId = trIds.Count() > nextSelected ? trIds[nextSelected].NidTransaction : trIds[0].NidTransaction;
                result.StartOfMonth = StartOfMonth.Date;
            }
            catch (Exception)
            {
            }
            return result;
        }
        public FinancialReportViewModel GetFinancialReport(Guid nidUser)
        {
            var result = new FinancialReportViewModel();
            try
            {
                result.CurrentMonth = Helpers.Dates.CurrentMonth();
                result.MonthlySpenceBarChart = MonthlySpenceBarCalc(Helpers.Dates.CurrentMonth());
                result.MonthlyIncomeBarChart = MonthlyIncomeBarCalc(Helpers.Dates.CurrentMonth());
                result.MonthSpencesBarChart = MonthSpencesBarCalc();
                result.TopFiveGroupBarChart = TopFiveGroupBarCalc();
                result.GroupSpenceBarChart = new Tuple<string, string, decimal>("[]", "[]", 0);
                result.GroupIncomeBarChart = new Tuple<string, string, decimal>("[]", "[]", 0);
                result.FundDistributionPieChart = FundDistributionPieCalc();
                result.FundAccumulationAreaChart = FundAccumulationAreaCalc();
                result.YearlyCardStat = new Tuple<decimal, decimal, decimal>(
                GetInitialYearAmounts().Sum(o => o.Amount)/*initial fund*/,
                GetYearIncomeAmounts()/*total in fund*/,
                GetYearSpenceAmounts()/*total out fund*/);
                result.Groups = _dbRepository.GetList<TransactionGroup>();
            }
            catch (Exception)
            {
            }
            return result;
        }
        public Tuple<string, string, decimal> MonthlySpenceBarCalc(int month)
        {
            try
            {
                var startofmonth = Helpers.Dates.GetStartAndEndOfMonth(month);
                var currentMonthSpenceTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= startofmonth.Item1 && p.CreateDate <= startofmonth.Item2)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                    .GroupBy(a => a.PayerAccount).Select(m => new { acc = m.Key, totalAmount = m.Sum(o => o.Amount) }).ToList();
                if (!currentMonthSpenceTransactions.Any())
                    return new Tuple<string, string, decimal>("[]", "[]", 0);
                string accNames = "[";
                string values = "[";
                var accounts = _dbRepository.GetList<Account>();
                foreach (var tr in currentMonthSpenceTransactions)
                {
                    accNames += "'" + accounts.FirstOrDefault(p => p.NidAccount == tr.acc).Title + "',";
                    values += "'" + tr.totalAmount + "',";
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, currentMonthSpenceTransactions.Max(p => p.totalAmount));
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        public Tuple<string, string, decimal> MonthlyIncomeBarCalc(int month)
        {
            try
            {
                var startofmonth = Helpers.Dates.GetStartAndEndOfMonth(month);
                var currentMonthIncomeTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= startofmonth.Item1 && p.CreateDate <= startofmonth.Item2)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true).Select(r => r.NidAccount).Contains(q.PayerAccount))
                    .GroupBy(a => a.RecieverAccount).Select(m => new { acc = m.Key, totalAmount = m.Sum(o => o.Amount) });
                if (!currentMonthIncomeTransactions.Any())
                    return new Tuple<string, string, decimal>("[]", "[]", 0);
                string accNames = "[";
                string values = "[";
                var accounts = _dbRepository.GetList<Account>();
                foreach (var tr in currentMonthIncomeTransactions)
                {
                    accNames += "'" + accounts.FirstOrDefault(p => p.NidAccount == tr.acc).Title + "',";
                    values += "'" + tr.totalAmount + "',";
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, currentMonthIncomeTransactions.Max(p => p.totalAmount));
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        private Tuple<string, string, decimal> MonthSpencesBarCalc()
        {
            try
            {
                var months = Helpers.Dates.GetMonthsOfYear();
                var accounts = _dbRepository.GetList<Account>();
                var pc = new PersianCalendar();
                string accNames = "[";
                string values = "[";
                decimal maxVal = 0;
                foreach (var m in months)
                {
                    var currentMonthSpenceTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= m.Item1 && p.CreateDate <= m.Item2)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                    .Sum(x => x.Amount);
                    accNames += "'" + GetMonthName(pc.GetMonth(m.Item1)) + "',";
                    values += "'" + currentMonthSpenceTransactions + "',";
                    if (currentMonthSpenceTransactions > maxVal)
                        maxVal = currentMonthSpenceTransactions;
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, maxVal);
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        private Tuple<string, string, decimal> TopFiveGroupBarCalc()
        {
            try
            {
                var year = Helpers.Dates.GetStartOfYear();
                var currentYearSpenceTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= year)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                    .GroupBy(a => a.TransactionGroupId).Select(m => new { acc = m.Key, totalAmount = m.Sum(o => o.Amount) })
                    .OrderByDescending(b => b.totalAmount).Take(5);
                string accNames = "[";
                string values = "[";
                var groups = _dbRepository.GetList<TransactionGroup>();
                foreach (var tr in currentYearSpenceTransactions)
                {
                    accNames += "'" + groups.FirstOrDefault(p => p.NidTransactionGroup == tr.acc).Title + "',";
                    values += "'" + tr.totalAmount + "',";
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, currentYearSpenceTransactions.Max(p => p.totalAmount));
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        public Tuple<string, string, decimal> GroupSpenceBarCalc(Guid NidGroup)
        {
            try
            {
                var months = Helpers.Dates.GetMonthsOfYear();
                var accounts = _dbRepository.GetList<Account>();
                var pc = new PersianCalendar();
                string accNames = "[";
                string values = "[";
                decimal maxVal = 0;
                foreach (var m in months)
                {
                    var currentMonthSpenceTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= m.Item1 && p.CreateDate <= m.Item2
                    && p.TransactionGroupId == NidGroup)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                    .Sum(x => x.Amount);
                    accNames += "'" + GetMonthName(pc.GetMonth(m.Item1)) + "',";
                    values += "'" + currentMonthSpenceTransactions + "',";
                    if (currentMonthSpenceTransactions > maxVal)
                        maxVal = currentMonthSpenceTransactions;
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, maxVal);
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        public Tuple<string, string, decimal> GroupIncomeBarCalc(Guid NidGroup)
        {
            try
            {
                var months = Helpers.Dates.GetMonthsOfYear();
                var accounts = _dbRepository.GetList<Account>();
                var pc = new PersianCalendar();
                string accNames = "[";
                string values = "[";
                decimal maxVal = 0;
                foreach (var m in months)
                {
                    var currentMonthIncomeTransactions = _dbRepository.GetList<Transaction>(p => p.CreateDate >= m.Item1 && p.CreateDate <= m.Item2
                    && p.TransactionGroupId == NidGroup)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.PayerAccount))
                    .Sum(x => x.Amount);
                    accNames += "'" + GetMonthName(pc.GetMonth(m.Item1)) + "',";
                    values += "'" + currentMonthIncomeTransactions + "',";
                    if (currentMonthIncomeTransactions > maxVal)
                        maxVal = currentMonthIncomeTransactions;
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string, decimal>(accNames, values, maxVal);
            }
            catch (Exception)
            {
                return new Tuple<string, string, decimal>("","",0);
            }
        }
        private Tuple<string, string> FundDistributionPieCalc()
        {
            try
            {
                var months = Helpers.Dates.GetMonthsOfYear();
                var accounts = _dbRepository.GetList<Account>(p => p.IsBackup == false && p.IsActive == true);
                var totalFund = accounts.Sum(p => p.Amount);
                var pc = new PersianCalendar();
                string accNames = "[";
                string values = "[";
                foreach (var m in accounts)
                {
                    accNames += "'" + m.Title + "',";
                    values += "'" + Math.Round((m.Amount / totalFund) * 100) + "',";
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string>(accNames, values);
            }
            catch (Exception)
            {
                return new Tuple<string, string>("","");
            }
        }
        private Tuple<string, string> FundAccumulationAreaCalc()
        {
            try
            {
                var months = Helpers.Dates.GetMonthsOfYear();
                var accounts = _dbRepository.GetList<Account>(p => p.IsBackup == false && p.IsActive == true);
                var initialYearAccounts = GetInitialYearAmounts().Sum(o => o.Amount);
                var pc = new PersianCalendar();
                string accNames = "[";
                string values = "[";
                foreach (var m in months)
                {
                    var spence = _dbRepository.GetList<Transaction>(p => p.CreateDate >= m.Item1 && p.CreateDate <= m.Item2)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                    .Sum(x => x.Amount);
                    var income = _dbRepository.GetList<Transaction>(p => p.CreateDate >= m.Item1 && p.CreateDate <= m.Item2)
                    .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.PayerAccount))
                    .Sum(x => x.Amount);
                    initialYearAccounts = initialYearAccounts + income - spence;
                    accNames += "'" + GetMonthName(pc.GetMonth(m.Item1)) + "',";
                    values += "'" + initialYearAccounts + "',";
                }
                accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                values = values.Remove(values.Length - 1, 1) + "]";
                return new Tuple<string, string>(accNames, values);
            }
            catch (Exception)
            {
                return new Tuple<string, string>("","");
            }
        }
        private List<Account> GetInitialYearAmounts()
        {
            var result = new List<Account>();
            try
            {
                var year = Helpers.Dates.GetStartOfYear();
                var accounts = _dbRepository.GetList<Account>(p => p.IsBackup == false && p.IsActive == true);
                foreach (var acc in accounts)
                {
                    var spences = _dbRepository.GetList<Transaction>(p => p.CreateDate >= year)
                    .Where(q => q.PayerAccount == acc.NidAccount).Sum(u => u.Amount);
                    var Incomes = _dbRepository.GetList<Transaction>(p => p.CreateDate >= year)
                    .Where(q => q.RecieverAccount == acc.NidAccount).Sum(u => u.Amount);
                    result.Add(new Account() { NidAccount = acc.NidAccount, Title = acc.Title, Amount = acc.Amount + spences - Incomes });
                }
            }
            catch (Exception)
            {
            }
            return result;
        }
        private decimal GetYearIncomeAmounts()
        {
            try
            {
                var year = Helpers.Dates.GetStartOfYear();
                return _dbRepository.GetList<Transaction>(p => p.CreateDate >= year)
                .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.PayerAccount))
                .Sum(u => u.Amount);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        private decimal GetYearSpenceAmounts()
        {
            try
            {
                var year = Helpers.Dates.GetStartOfYear();
                return _dbRepository.GetList<Transaction>(p => p.CreateDate >= year)
                .Where(q => _dbRepository.GetList<Account>(w => w.IsBackup == true && w.IsActive == true).Select(r => r.NidAccount).Contains(q.RecieverAccount))
                .Sum(u => u.Amount);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        private string GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "farvardin";
                case 2:
                    return "ordibehesht";
                case 3:
                    return "khordad";
                case 4:
                    return "tir";
                case 5:
                    return "mordad";
                case 6:
                    return "shahrivar";
                case 7:
                    return "mehr";
                case 8:
                    return "aban";
                case 9:
                    return "azar";
                case 10:
                    return "dey";
                case 11:
                    return "bahman";
                case 12:
                    return "esfand";
                default:
                    return "";
            }
        }
        public TradeDashboardViewModel GetTradeDashboard(Symbol symbol, Timeframe timeframe)
        {
            TradeDashboardViewModel result = new TradeDashboardViewModel();
            try
            {
                var signal = _dbRepository.GetMax<Signal, DateTime>(q => q.CreateDate, p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.IsActive == true);
                if(signal != null)
                    result.signal = CommonTradeOperations.CastSignalToDto(signal);
                result.candle = _dbRepository.GetMax<AugmentedCandle, DateTime>(q => q.Time, p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe);
                if (result.signal != null)
                    result.SignalProgress = CommonTradeOperations.CalcSignalProgress(result.signal, result.candle);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public NewTradeDashboardViewModel GetTradeDashboard_New(Symbol symbol, Timeframe timeframe,int month)
        {
            NewTradeDashboardViewModel result = new NewTradeDashboardViewModel();
            try
            {
                var signal = _dbRepository.GetMax<Signal, DateTime>(q => q.CreateDate, p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe && p.IsActive == true);
                if (signal != null)
                    result.Signal = CommonTradeOperations.CastSignalToDto(signal);
                result.Candle = _dbRepository.GetMax<AugmentedCandle, DateTime>(q => q.Time, p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe);
                if (result.Signal != null)
                    result.SignalProgress = CommonTradeOperations.CalcSignalProgress(result.Signal, result.Candle);
                result.SignalResultsVM = GetSignalResults(symbol, timeframe, month);
                result.Estimate = CommonTradeOperations.GenerateSignalEstimates(new List<AugmentedCandle>() { result.Candle }).FirstOrDefault().Value;
            }
            catch (Exception)
            {
            }
            return result;
        }
        public MarketDataCredetialViewModel GetMarketDataCredentials(Symbol symbol,Timeframe timeframe)
        {
            var result = new MarketDataCredetialViewModel();
            result.Symbol = symbol;
            result.Timeframe = timeframe;
            result.Credentials = new List<MarketDataCredential>();
            try
            {
                _dbRepository.GetList<MarketDataCredential>(p => p.Symbol == (int)symbol && p.Timeframe == (int)timeframe).ForEach(x => { x.Password = Encryption.DecryptString(x.Password); result.Credentials.Add(x); });
            }
            catch (Exception)
            {
            }
            return result;
        }
        public bool PostMarketDataCredential(MarketDataCredential credential)
        {
            try
            {
                credential.Id = Guid.NewGuid();
                credential.CallCounter = 0;
                credential.Password = Encryption.EncryptString(credential.Password);
                credential.RefreshDate = DateTime.Now;
                return _dbRepository.Add(credential);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool DeleteMarketDataCredential(Guid nidCredential)
        {
            try
            {
                var credential = _dbRepository.Get<MarketDataCredential>(p => p.Id == nidCredential);
                if (credential != null)
                    return _dbRepository.Delete<MarketDataCredential>(credential);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public SignalResultsViewModel GetSignalResults(Symbol symbol, Timeframe timeframe,int currentMonth)
        {
            SignalResultsViewModel result = new SignalResultsViewModel();
            result.Symbol = symbol;
            result.Timeframe = timeframe;
            result.SignalResults = new List<SignalResultDto>();
            result.CurrentMonth = currentMonth;
            var startAndEndOfMonth = Dates.GetStartAndEndOfMonth(result.CurrentMonth);
            result.MonthlyCardStat = new Tuple<int, int, int, int>(0, 0, 0, 0);
            result.MonthlyCardStatPercentage = new Tuple<double, double, double, double>(0, 0, 0, 0);
            try
            {
                var signals = _dbRepository.GetList<SignalResult>(p => p.Signal.Symbol == (int)symbol && p.Signal.Timeframe == (int)timeframe && p.Signal.IsActive == false
                && p.Signal.StartDate >= startAndEndOfMonth.Item1.Date && p.Signal.StartDate <= startAndEndOfMonth.Item2.Date
                , 10000,"Signal");
                if (signals != null)
                    signals.ForEach(x => { result.SignalResults.Add(CommonTradeOperations.CastSignalResultToDto(x)); });
                result.MonthlyCardStat = new Tuple<int, int, int, int>(
                    signals.Count(),
                    signals.Where(p => p.Status == 1).Count(),
                    signals.Where(p => p.Status == 2).Count(),
                    signals.Where(p => p.Status == 3).Count()
                    );
                result.MonthlyCardStatPercentage = new Tuple<double, double, double, double>(
                    signals.Sum(q => q.ProfitPercentage),
                    signals.Where(p => p.Status == 1).Sum(q => q.ProfitPercentage),
                    signals.Where(p => p.Status == 2).Sum(q => q.ProfitPercentage),
                    signals.Where(p => p.Status == 3).Sum(q => q.ProfitPercentage)
                    );
                result.SignalResultsAreaChart = SignalResultAreaCalc(signals);
            }
            catch (Exception)
            {
            }
            return result;
        }
        private Tuple<string, string> SignalResultAreaCalc(List<SignalResult> signals)
        {
            try
            {
                string accNames = "[";
                string values = "[";
                if (signals.Any())
                {
                    var pc = new PersianCalendar();
                    double currentStat = 0;
                    foreach (var m in signals.OrderBy(p => p.Signal.StartDate))
                    {
                        accNames += "'" + pc.GetDayOfMonth(m.CloseDate) + "',";
                        currentStat += m.ProfitPercentage;
                        values += "'" + (int)currentStat + "',";
                    }
                    accNames = accNames.Remove(accNames.Length - 1, 1) + "]";
                    values = values.Remove(values.Length - 1, 1) + "]";
                }
                else
                {
                    accNames += "]";
                    values += "]";
                }
                return new Tuple<string, string>(accNames, values);
            }
            catch (Exception)
            {
                return new Tuple<string, string>("", "");
            }
        }
        public bool DbMaintanence()
        {
            try
            {
                _dbRepository.Raw("alter table AugmentedCandles add Ema100 float null");
                _dbRepository.Raw("alter table AugmentedCandles add Ema200 float null");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}