using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todolist.Models;
using Todolist.Models.Dto;
using Todolist.ViewModels;
using static Todolist.Models.TradeModels;

namespace Todolist.Services.Contracts
{
    public interface IRequestProcessor
    {
        List<User> GetUsers();
        UserLoginDto LoginUser(string username,string password);
        bool PostUser(User user);
        bool DeleteUser(Guid nidUser);

        GoalViewModel GetGoals(Guid nidUser);
        GoalPageViewModel GetGoal(Guid nidGoal);
        bool PostGoal(Goal goal);
        bool PostTask(Models.Task task);
        bool DeleteTask(Guid nidTask);
        bool DeleteProgress(Guid nidProgress);
        bool DoneTask(Guid nidTask);
        bool UndoTask(Guid nidTask);
        Models.Task GetTask(Guid nidTask);
        bool PatchTask(Models.Task task);
        Goal GetGoalWithoutDependancy(Guid nidGoal);
        bool PatchGoal(Goal goal);
        bool DeleteGoal(Guid nidGoal);
        IndexViewModel GetIndex(Guid nidUser, int Direction = 0);
        bool PostSchedule(Schedule schedule);
        bool PostProgress(Progress progress);
        bool PatchProgress(Progress progress);
        bool DeleteSchedule(Guid nidSchedule);

        List<NoteGroup> GetNoteGroups(Guid nidUser);
        bool PostNoteGroup(NoteGroup noteGroup);
        bool PatchNoteGroup(NoteGroup noteGroup);
        bool DeleteNoteGroup(Guid nidNoteGroup);
        List<Note> GetNotes(Guid nidGroup);
        NotesViewModel GetNoteGroup(Guid nidNoteGroup);

        Note GetNote(Guid nidNote);
        bool PostNote(Note note);
        bool PatchNote(Note note);
        bool DeleteNote(Guid nidNote);

        FinanceViewModel GetFinacialRecords(Guid nidUser, bool includeAll = false);
        Account GetAccount(Guid nidAccount);
        bool PostAccount(Account account);
        bool PatchAccount(Account account);
        bool DeleteAccount(Guid nidAccount);

        Transaction GetTransaction(Guid nidTransaction);
        bool PostTransaction(Transaction transaction);
        bool PatchTransaction(Transaction transaction);
        bool DeleteTransaction(Guid nidTransaction);

        Shield GetShield(Guid nidShield);
        bool PostShield(Shield shield);
        bool PatchShield(Shield shield);
        bool DeleteShield(Guid nidShield);
        List<Shield> GetShields(Guid nidUser);
        bool ConvertShields();

        RoutineViewModel GetRoutines(Guid nidUser, int Direction = 0);
        Routine GetRoutine(Guid nidRoutine);
        bool PostRoutine(Routine routine);
        bool PatchRoutine(Routine routine);
        bool DeleteRoutine(Guid nidRoutine);

        List<RoutineProgress> GetRoutineProgresses(Guid nidRoutine);
        bool PostRoutineProgress(RoutineProgress routineProgress);
        bool DeleteRoutineProgress(Guid nidRoutineProgress);
        List<LendDetailViewModel> LendDetails(Guid nidAccount);
        List<TransactionGroup> GetTransactionGroups(Guid nidUser, bool includeAll = false);
        bool PostTransactionGroups(TransactionGroup transactionGroup);
        TransactionGroup GetTransactionGroup(Guid nidTransactionGroup);
        bool DeleteTransactionGroup(Guid nidTransactionGroup);
        EditTransactionViewModel GetEditTransaction(Guid nidTransaction, Guid nidUser);
        FinancialReportViewModel GetFinancialReport(Guid nidUser);
        Tuple<string, string, decimal> MonthlySpenceBarCalc(int month);
        Tuple<string, string, decimal> MonthlyIncomeBarCalc(int month);
        Tuple<string, string, decimal> GroupSpenceBarCalc(Guid NidGroup);
        Tuple<string, string, decimal> GroupIncomeBarCalc(Guid NidGroup);
        TradeDashboardViewModel GetTradeDashboard(Symbol symbol, Timeframe timeframe);
        List<MarketDataCredential> GetMarketDataCredentials(Symbol symbol, Timeframe timeframe);
        bool PostMarketDataCredential(MarketDataCredential credential);
        bool DeleteMarketDataCredential(Guid nidCredential);
    }
}
