using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Todolist.Models;
using Todolist.Services.Contracts;
using Todolist.ViewModels;

namespace Todolist.Controllers
{
    [Authorize]
    public class FinancialsController : Controller
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
        public FinancialsController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }
        //account section
        public ActionResult FinancialRecords(bool IncludeAll = false)
        {
            return View(_requestProcessor.GetFinacialRecords(UserId, IncludeAll));
        }
        public ActionResult SubmitAddAccount(string Title, decimal Amount, bool IsActive, bool IsBackup = false)
        {
            Account account = new Account() { Amount = Amount, IsActive = IsActive, LendAmount = 0, Title = Title, UserId = UserId, IsBackup = IsBackup };
            if (_requestProcessor.PostAccount(account))
                TempData["FinanceSuccess"] = $"{account.Title} created successfully";
            else
                TempData["FinanceError"] = $"an error occured while creating account!";
            return RedirectToAction("FinancialRecords", "Financials");
        }
        public ActionResult SubmitAddTransaction(byte TrType, Guid PayerAccount, Guid RecieverAccount, decimal Amount, Guid TransactionGroupId, string Reason = "")
        {
            Transaction tr = new Transaction() { Amount = Amount, TransactionType = TrType, PayerAccount = PayerAccount, RecieverAccount = RecieverAccount, TransactionReason = Reason, UserId = UserId, TransactionGroupId = TransactionGroupId };
            if (_requestProcessor.PostTransaction(tr))
                TempData["FinanceSuccess"] = $"transaction created successfully";
            else
                TempData["FinanceError"] = $"an error occured while creating transaction!";
            return RedirectToAction("FinancialRecords", "Financials");
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
            return RedirectToAction("FinancialRecords", "Financials");
        }
        public ActionResult SubmitDeleteTransaction(Guid NidTr)
        {
            if (_requestProcessor.DeleteTransaction(NidTr))
                TempData["FinanceSuccess"] = $"transaction deleted successfully";
            else
                TempData["FinanceError"] = $"an error occured while deleting transaction!";
            return RedirectToAction("FinancialRecords", "Financials");
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
            return RedirectToAction("FinancialRecords", "Financials");
        }
        public ActionResult SubmitDeleteAccount(Guid NidAccount)
        {
            if (_requestProcessor.DeleteAccount(NidAccount))
                TempData["FinanceSuccess"] = $"account deleted successfully";
            else
                TempData["FinanceError"] = $"an error occured while deleting account!";
            return RedirectToAction("FinancialRecords", "Financials");
        }
        public ActionResult LendDetail(Guid NidAccount)
        {
            var details = _requestProcessor.LendDetails(NidAccount);
            if (details.Any())
                return Json(new JsonResults() { HasValue = true, Html = Helpers.ViewHelper.RenderViewToString(this, "_LendDetailPartialView", details) });
            else
                return Json(new JsonResults() { HasValue = false });
        }
        //transaction group section
        public ActionResult TransactionGroups(bool IncludeAll = false)
        {
            return View(_requestProcessor.GetTransactionGroups(UserId, IncludeAll));
        }
        public ActionResult SubmitAddTransactionGroup(byte PaymentType, string Title)
        {
            TransactionGroup tr = new TransactionGroup() { UserId = UserId, IsActive = true, PaymentType = PaymentType, Title = Title };
            if (_requestProcessor.PostTransactionGroups(tr))
                TempData["TransactionGroupSuccess"] = $"transaction group created successfully";
            else
                TempData["TransactionGroupError"] = $"an error occured while creating transaction group!";
            return RedirectToAction("TransactionGroups", "Financials");
        }
        public ActionResult SubmitDeactiveTransactionGroup(Guid NidTr)
        {
            if (_requestProcessor.DeleteTransactionGroup(NidTr))
                TempData["TransactionGroupSuccess"] = $"transaction group deleted successfully";
            else
                TempData["TransactionGroupError"] = $"an error occured while deleting transaction group!";
            return RedirectToAction("TransactionGroups", "Financials");
        }
        public ActionResult GetTrGroupById(Guid NidTransactionGroup)
        {
            var tr = _requestProcessor.GetTransactionGroup(NidTransactionGroup);
            if (tr.NidTransactionGroup == Guid.Empty)
                return Json(new { HasValue = false });
            else
                return Json(new
                {
                    HasValue = true,
                    NidTr = tr.NidTransactionGroup.ToString(),
                    Title = tr.Title.ToString()
                });
        }

        //edit transaction
        public ActionResult EditTransaction(Guid TrId)
        {
            return View(_requestProcessor.GetEditTransaction(TrId, UserId));
        }
        public ActionResult SubmitEditTransaction2(Transaction transaction)
        {
            if (_requestProcessor.PatchTransaction(transaction))
                TempData["EditTrSuccess"] = $"transaction edited successfully";
            else
                TempData["EditTrError"] = $"an error occured while editing transaction!";
            return RedirectToAction("EditTransaction", "Financials", new { TrId = transaction.NidTransaction });
        }

        //financial report
        public ActionResult FinancialReport()
        {
            return View(_requestProcessor.GetFinancialReport(UserId));
        }
        [HttpPost]
        public ActionResult MonthlySpenceAndIncomeReport(int month)
        {
            return Json(new JsonResults()
            {
                HasValue = true,
                Html = Helpers.ViewHelper.RenderViewToString(this, "_MonthlyReportPartialView",
                new FinancialReportViewModel()
                {
                    CurrentMonth = month,
                    MonthlySpenceBarChart = _requestProcessor.MonthlySpenceBarCalc(month),
                    MonthlyIncomeBarChart = _requestProcessor.MonthlyIncomeBarCalc(month),
                    TotalCurrentMonthIncome = _requestProcessor.GetMonthIncomeAmounts(month),
                    TotalCurrentMonthSpence = _requestProcessor.GetMonthSpenceAmounts(month),
                    GroupMonthlySpenceBarChart = _requestProcessor.GroupMonthlySpenceBarCalc(month),
                    GroupMonthlyIncomeBarChart = _requestProcessor.GroupMonthlyIncomeBarCalc(month)
        })
            });
        }
        public ActionResult GroupTransations(Guid transactionGroupId)
        {
            return Json(new JsonResults()
            {
                HasValue = true,
                Html = Helpers.ViewHelper.RenderViewToString(this, "_GroupTransactionList",
                _requestProcessor.GetTransactionByGroupId(UserId,transactionGroupId))
            });
        }
        public ActionResult GroupDetailReport(Guid NidGroup)
        {
            return Json(new JsonResults()
            {
                HasValue = true,
                Html = Helpers.ViewHelper.RenderViewToString(this, "_GroupDetailReportPartialView",
                new FinancialReportViewModel()
                {
                    GroupSpenceBarChart = _requestProcessor.GroupSpenceBarCalc(NidGroup),
                    GroupIncomeBarChart = _requestProcessor.GroupIncomeBarCalc(NidGroup)
                })
            });
        }
    }
}