using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class CategotiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toast;

        public CategotiesController(ApplicationDbContext context,IToastNotification toast)
        {
            _context = context;
            this.toast = toast;
        }

        // GET: Categoties
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categoties.ToListAsync());
        }

        // GET: Categoties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoty = await _context.Categoties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoty == null)
            {
                return NotFound();
            }

            return View(categoty);
        }

        // GET: Categoties/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categoties/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Categoty categoty)
        {
            if (ModelState.IsValid)
            {
                _context.Add(categoty);
                await _context.SaveChangesAsync();
                toast.AddSuccessToastMessage("Added successfully....");
                return RedirectToAction(nameof(Index));
            }
            return View(categoty);
        }

        // GET: Categoties/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoty = await _context.Categoties.FindAsync(id);
            if (categoty == null)
            {
                return NotFound();
            }
            return View(categoty);
        }

        // POST: Categoties/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Categoty categoty)
        {
            if (id != categoty.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoty);
                    await _context.SaveChangesAsync();
                    toast.AddSuccessToastMessage("Modified successfully....");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategotyExists(categoty.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoty);
        }

        // GET: Categoties/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoty = await _context.Categoties
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoty == null)
            {
                return NotFound();
            }

            return View(categoty);
        }

        // POST: Categoties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoty = await _context.Categoties.FindAsync(id);
            if (categoty != null)
            {
                _context.Categoties.Remove(categoty);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategotyExists(int id)
        {
            return _context.Categoties.Any(e => e.Id == id);
        }
    }
}
