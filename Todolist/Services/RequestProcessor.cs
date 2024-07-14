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

        public bool ConvertShields()
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

        public bool DeleteAccount(Guid nidAccount)
        {
            var account = _dbRepository.Get<Account>(p => p.NidAccount == nidAccount);
            if (account != null)
                return _dbRepository.Delete<Account>(account);
            else
                return false;
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

        public bool DeleteRoutine(Guid nidRoutine)
        {
            var routine = _dbRepository.Get<Routine>(p => p.NidRoutine == nidRoutine);
            if (routine != null)
                return _dbRepository.Delete<Routine>(routine);
            else
                return false;
        }

        public bool DeleteRoutineProgress(Guid nidRoutineProgress)
        {
            var routineProg = _dbRepository.Get<RoutineProgress>(p => p.NidRoutineProgress == nidRoutineProgress);
            if (routineProg != null)
                return _dbRepository.Delete<RoutineProgress>(routineProg);
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

        public bool DeleteShield(Guid nidShield)
        {
            var shield = _dbRepository.Get<Shield>(p => p.Id == nidShield);
            if (shield != null)
                return _dbRepository.Delete<Shield>(shield);
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
            return _dbRepository.Get<Account>(p => p.NidAccount == nidAccount);
        }

        public FinanceViewModel GetFinacialRecords(Guid nidUser,bool includeAll = false)
        {
            var result = new FinanceViewModel();
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
            var result = new IndexViewModel() { AllTasks = new List<Task>(), Schedules = new List<Schedule>(), Progresses = new List<Progress>() };
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

        public Routine GetRoutine(Guid nidRoutine)
        {
            return _dbRepository.Get<Routine>(p => p.NidRoutine == nidRoutine);
        }

        public List<RoutineProgress> GetRoutineProgresses(Guid nidRoutine)
        {
            return _dbRepository.GetList<RoutineProgress>(p => p.RoutineId == nidRoutine);
        }

        public RoutineViewModel GetRoutines(Guid nidUser, int Direction = 0)
        {
            var result = new RoutineViewModel() {  RoutineProgresses = new List<RoutineProgress>() };
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
            return result;
        }

        public Shield GetShield(Guid nidShield)
        {
            return _dbRepository.Get<Shield>(p => p.Id == nidShield);
        }

        public List<Shield> GetShields(Guid nidUser)
        {
            return _dbRepository.GetList<Shield>(p => p.UserId == nidUser);
        }

        public Task GetTask(Guid nidTask)
        {
            return _dbRepository.Get<Task>(p => p.NidTask == nidTask);
        }

        public Transaction GetTransaction(Guid nidTransaction)
        {
            return _dbRepository.Get<Transaction>(p => p.NidTransaction == nidTransaction);
        }

        public List<User> GetUsers()
        {
            return _dbRepository.GetList<User>();
        }

        public List<LendDetailViewModel> LendDetails(Guid nidAccount)
        {
            var lendTrs = _dbRepository.GetList<Transaction>(p => p.PayerAccount == nidAccount && p.TransactionType == 2)
                .GroupBy(q => q.RecieverAccount).Select(w => new { nidAcc = w.Key,amount = w.Sum(x => x.Amount)}).ToList();
            var lendBackTrs = _dbRepository.GetList<Transaction>(p => p.RecieverAccount == nidAccount && p.TransactionType == 3)
                .GroupBy(q => q.PayerAccount).Select(w => new { nidAcc = w.Key, amount = w.Sum(x => x.Amount) }).ToList();
            var result = new List<LendDetailViewModel>();
            lendTrs.ForEach(x => { result.Add(new LendDetailViewModel() {  NidAccount = x.nidAcc, Amount = x.amount }); });
            foreach (var lb in lendBackTrs)
            {
                if(lendTrs.Any(p => p.nidAcc == lb.nidAcc))
                {
                    if(lb.amount <= lendTrs.FirstOrDefault(p => p.nidAcc == lb.nidAcc).amount)
                    {
                        var oldAmount = result.FirstOrDefault(p => p.NidAccount == lb.nidAcc).Amount;
                        result.Remove(result.FirstOrDefault(p => p.NidAccount == lb.nidAcc));
                        result.Add(new LendDetailViewModel() {  NidAccount = lb.nidAcc, Amount = oldAmount - lb.amount});
                    }
                }
            }
            var result2 = new List<LendDetailViewModel>();
            foreach (var ln in result)
            {
                if (ln.Amount != 0)
                {
                    if(_dbRepository.Get<Account>(p => p.NidAccount == ln.NidAccount).IsActive)
                        result2.Add(new LendDetailViewModel() { NidAccount = ln.NidAccount, Amount = ln.Amount, AccountName = _dbRepository.Get<Account>(p => p.NidAccount == ln.NidAccount).Title });
                }
            }
            return result2;
        }

        public UserLoginDto LoginUser(string username, string password)
        {
            password = Helpers.Encryption.Sha256Hash(password.Trim());
            var user = _dbRepository.Get<User>(p => p.Username == username.Trim() && p.Password == password);
            if (user != null)
                return new UserLoginDto() {  IsSuccessful = true, IsAdmin = user.IsAdmin, NidUser = user.NidUser, Username = user.Username };
            else
                return new UserLoginDto() { IsSuccessful = false };
        }

        public bool PatchAccount(Account account)
        {
            account.LastModified = DateTime.Now;
            return _dbRepository.Update<Account>(account);
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

        public bool PatchRoutine(Routine routine)
        {
            return _dbRepository.Update<Routine>(routine);
        }

        public bool PatchShield(Shield shield)
        {
            shield.LastModified = DateTime.Now;
            shield.Password = Helpers.Encryption.RSAEncrypt(shield.Password);
            return _dbRepository.Update<Shield>(shield);
        }

        public bool PatchTask(Task task)
        {
            return _dbRepository.Update<Task>(task);
        }

        public bool PatchTransaction(Transaction transaction)
        {
            return _dbRepository.Update<Transaction>(transaction);
        }

        public bool PostAccount(Account account)
        {
            account.NidAccount = Guid.NewGuid();
            account.CreateDate = DateTime.Now;
            account.LastModified = DateTime.Now;
            return _dbRepository.Add<Account>(account);
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

        public bool PostRoutine(Routine routine)
        {
            routine.NidRoutine = Guid.NewGuid();
            routine.CreateDate = DateTime.Now;
            routine.Status = false;
            return _dbRepository.Add<Routine>(routine);
        }

        public bool PostRoutineProgress(RoutineProgress routineProgress)
        {
            routineProgress.NidRoutineProgress = Guid.NewGuid();
            routineProgress.CreateDate = DateTime.Now;
            return _dbRepository.Add<RoutineProgress>(routineProgress);
        }

        public bool PostSchedule(Schedule schedule)
        {
            schedule.NidSchedule = Guid.NewGuid();
            schedule.CreateDate = DateTime.Now;
            return _dbRepository.Add<Schedule>(schedule);
        }

        public bool PostShield(Shield shield)
        {
            shield.Id = Guid.NewGuid();
            shield.CreateDate = DateTime.Now;
            shield.Password = Helpers.Encryption.RSAEncrypt(shield.Password);
            return _dbRepository.Add<Shield>(shield);
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
            if (PerformTransaction(transaction))
            {
                transaction.NidTransaction = Guid.NewGuid();
                transaction.CreateDate = DateTime.Now;
                return _dbRepository.Add<Transaction>(transaction);
            }
            else
                return false;
        }

        public bool PostUser(User user)
        {
            user.NidUser = Guid.NewGuid();
            user.CreateDate = DateTime.Now;
            user.Password = Helpers.Encryption.Sha256Hash(user.Password);
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
        private bool PerformTransaction(Transaction Transaction)
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
            if(!includeAll)
                return _dbRepository.GetList<TransactionGroup>(p => p.UserId == nidUser && p.IsActive == true);
            else
                return _dbRepository.GetList<TransactionGroup>(p => p.UserId == nidUser);
        }
        public bool PostTransactionGroups(TransactionGroup transactionGroup)
        {
            transactionGroup.NidTransactionGroup = Guid.NewGuid();
            transactionGroup.CreateDate = DateTime.Now;
            return _dbRepository.Add<TransactionGroup>(transactionGroup);
        }
        public TransactionGroup GetTransactionGroup(Guid nidTransactionGroup)
        {
            return _dbRepository.Get<TransactionGroup>(p => p.NidTransactionGroup == nidTransactionGroup);
        }
        public bool DeleteTransactionGroup(Guid nidTransactionGroup)
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
        public EditTransactionViewModel GetEditTransaction(Guid nidTransaction,Guid nidUser)
        {
            var result = new EditTransactionViewModel();
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
            return result;
        }
    }
}