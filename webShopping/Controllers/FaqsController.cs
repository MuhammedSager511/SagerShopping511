using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class FaqsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public FaqsController(ApplicationDbContext context, IToastNotification toast, IAppLocalizer localizer)
        {
            _context = context;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var faqs = await _context.FaqItems.AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .ToListAsync();
            return View(faqs);
        }

        public IActionResult Create() => View(new FaqItem { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FaqItem faq)
        {
            faq.QuestionAr ??= "";
            faq.AnswerAr ??= "";

            if (ModelState.IsValid)
            {
                _context.Add(faq);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryAdded"]);
                return RedirectToAction(nameof(Index));
            }

            return View(faq);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var faq = await _context.FaqItems.FindAsync(id);
            return faq == null ? NotFound() : View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FaqItem faq)
        {
            if (id != faq.Id) return NotFound();

            var tracked = await _context.FaqItems.FindAsync(id);
            if (tracked == null) return NotFound();

            if (ModelState.IsValid)
            {
                tracked.QuestionEn = faq.QuestionEn;
                tracked.QuestionAr = faq.QuestionAr ?? "";
                tracked.AnswerEn = faq.AnswerEn;
                tracked.AnswerAr = faq.AnswerAr ?? "";
                tracked.SortOrder = faq.SortOrder;
                tracked.IsActive = faq.IsActive;
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            return View(faq);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var faq = await _context.FaqItems.FirstOrDefaultAsync(m => m.Id == id);
            return faq == null ? NotFound() : View(faq);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var faq = await _context.FaqItems.FindAsync(id);
            if (faq != null)
            {
                _context.FaqItems.Remove(faq);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
