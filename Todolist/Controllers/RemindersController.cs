using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Todolist.Models;
using Todolist.Services.Contracts;

namespace Todolist.Controllers
{
    [Authorize]
    public class RemindersController : Controller
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
        public RemindersController(IRequestProcessor requestProcessor)
        {
            _requestProcessor = requestProcessor;
        }

        //notes section
        public ActionResult NoteGroups()
        {
            return View(_requestProcessor.GetNoteGroups(UserId));
        }
        public ActionResult AddNoteGroup()
        {
            return View();
        }
        public ActionResult SubmitAddNoteGroup(NoteGroup group)
        {
            group.UserId = UserId;
            if (_requestProcessor.PostNoteGroup(group))
                TempData["GroupSuccess"] = $"{group.Title} created successfully";
            else
                TempData["GroupError"] = $"an error occured while creating group!";
            return RedirectToAction("NoteGroups", "Reminders");
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
            return RedirectToAction("NoteGroup", "Reminders", new { NidGroup = group.NidGroup });
        }
        public ActionResult SubmitDeleteNoteGroup(Guid NidGroup)
        {
            if (_requestProcessor.DeleteNoteGroup(NidGroup))
                TempData["GroupSuccess"] = $"group deleted successfully";
            else
                TempData["GroupError"] = $"an error occured while deleting group!";
            return RedirectToAction("NoteGroups", "Reminders");
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
            return RedirectToAction("NoteGroup", "Reminders", new { NidGroup = note.GroupId });
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
            return RedirectToAction("NoteGroup", "Reminders", new { NidGroup = note.GroupId });
        }
        public ActionResult SubmitDeleteNote(Guid NidNote)
        {
            var note = _requestProcessor.GetNote(NidNote);
            if (_requestProcessor.DeleteNote(NidNote))
                TempData["NoteSuccess"] = $"note deleted successfully";
            else
                TempData["NoteError"] = $"an error occured while deleting note!";
            return RedirectToAction("NoteGroup", "Reminders", new { NidGroup = note.GroupId });
        }

        //shield section
        public ActionResult Shields(bool IncludeAll = false)
        {
            return View(_requestProcessor.GetShields(UserId));
        }
        public ActionResult AddShield()
        {
            return View();
        }
        public ActionResult SubmitAddShield(Shield shield)
        {
            shield.UserId = UserId;
            if (_requestProcessor.PostShield(shield))
                TempData["ShieldSuccess"] = $"{shield.Title} created successfully";
            else
                TempData["ShieldError"] = $"an error occured while creating Shield!";
            return RedirectToAction("Shields", "Reminders");
        }
        public ActionResult SubmitEditShield(Shield shield)
        {
            if (_requestProcessor.PatchShield(shield))
                TempData["ShieldSuccess"] = $"{shield.Title} edited successfully";
            else
                TempData["ShieldError"] = $"an error occured while editing Shield!";
            return RedirectToAction("Shields", "Reminders");
        }
        public ActionResult SubmitDeleteShield(Guid NidShield)
        {
            if (_requestProcessor.DeleteShield(NidShield))
                TempData["ShieldSuccess"] = $"Shield deleted successfully";
            else
                TempData["ShieldError"] = $"an error occured while deleting Shield!";
            return RedirectToAction("Shields", "Reminders");
        }
        public ActionResult EditShield(Guid NidShield, string masterPassword = "")
        {
            var shield = _requestProcessor.GetShield(NidShield);
            var decrypt = Helpers.Encryption.RSADecrypt(shield.Password, masterPassword);
            if (!string.IsNullOrEmpty(decrypt))
            {
                shield.Password = decrypt;
                return View(shield);
            }
            else
            {
                TempData["ShieldError"] = $"wrong master password";
                return RedirectToAction("Shields", "Reminders");
            }
        }
        public ActionResult ShieldDetail(Guid NidShield, string masterPassword = "")
        {
            var shield = _requestProcessor.GetShield(NidShield);
            var decrypt = Helpers.Encryption.RSADecrypt(shield.Password, masterPassword);
            if (!string.IsNullOrEmpty(decrypt))
            {
                shield.Password = decrypt;
                return View(shield);
            }
            else
            {
                TempData["ShieldError"] = $"wrong master password";
                return RedirectToAction("Shields", "Reminders");
            }
        }
        public ActionResult ConvertShields()
        {
            if (_requestProcessor.ConvertShields())
                TempData["ShieldSuccess"] = $"Shield converted successfully";
            else
                TempData["ShieldError"] = $"an error occured while converting Shield!";
            return RedirectToAction("Shields", "Reminders");
        }
    }
}