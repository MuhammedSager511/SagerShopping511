using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webShopping.Models;
using webShopping.Data;
using Microsoft.AspNetCore.Authorization;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hosting;

        public ProductsController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IHostingEnvironment hosting)
        {
            _context = context;
            this.hosting = hosting;
        }

        // GET: FileDetails
        public async Task<IActionResult> Index()
        {
            var Products = await _context.Products.Include(f => f.categoty).ToListAsync();
            return View(Products);
        }
       

        // GET: FileDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileDetails = await _context.Products
                .Include(f => f.categoty)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fileDetails == null)
            {
                return NotFound();
            }

            return View(fileDetails);
        }

        // GET: FileDetails/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name");
            return View();
        }

        // POST: FileDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]



        public async Task<IActionResult> Create(Product fileDetails)
        {
            if (!ModelState.IsValid)
            {
                if (fileDetails.File != null)
                {
                    string fileFolder = Path.Combine(hosting.WebRootPath, "files");
                    string fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(fileDetails.File.FileName);
                    string filePath = Path.Combine(fileFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileDetails.File.CopyToAsync(stream);
                    }

                    fileDetails.Path = fileName;

                    // Rotate the image 90 degrees clockwise

                }

                string[] extension = fileDetails.Path.Split('.');
                fileDetails.FileType = extension[1];
                _context.Add(fileDetails);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            //ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
            return View(fileDetails);
        }

        // GET: FileDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileDetails = await _context.Products.FindAsync(id);
            if (fileDetails == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
            return View(fileDetails);
        }

        // POST: FileDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product fileDetails)
        {
            if (id != fileDetails.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                if (fileDetails.File != null)
                {
                    string fileFolder = Path.Combine(hosting.WebRootPath, "files");
                    string fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(fileDetails.File.FileName);
                    string filePath = Path.Combine(fileFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileDetails.File.CopyToAsync(stream);
                    }

                    // Update the fileDetails with the new file path
                    fileDetails.Path = fileName;
                }

                // Update the entity in the database
                _context.Update(fileDetails);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
            return View(fileDetails);
        }

        //public async Task<IActionResult> Edit(int id, FileDetails fileDetails)
        //{
        //    if (id != fileDetails.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        if (fileDetails.File != null)
        //        {
        //            string fileFolder = Path.Combine(hosting.WebRootPath, "Products");
        //            string fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(fileDetails.File.FileName);
        //            string filePath = Path.Combine(fileFolder, fileName);


        //            using (var stream = new Productstream(filePath, FileMode.Create))
        //            {
        //                await fileDetails.File.CopyToAsync(stream);
        //            }

        //            fileDetails.Path = fileName;

        //            // Rotate the image 90 degrees clockwise
        //            using (var image = Image.FromFile(filePath))
        //            {
        //                image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        //                image.Save(filePath, ImageFormat.Jpeg); // You can change the format if needed
        //            }
        //        }
        //        _context.Update(fileDetails);
        //       await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
        //    return View(fileDetails);
        //}

        // GET: FileDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileDetails = await _context.Products
                .Include(f => f.categoty)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fileDetails == null)
            {
                return NotFound();
            }

            return View(fileDetails);
        }

        // POST: FileDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fileDetails = await _context.Products.FindAsync(id);
            if (fileDetails != null)
            {
                // حذف الصورة من مجلد "Products"
                if (!string.IsNullOrEmpty(fileDetails.Path))
                {
                    string filePath = Path.Combine(hosting.WebRootPath, "Products", fileDetails.Path);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // حذف العنصر من قاعدة البيانات
                _context.Products.Remove(fileDetails);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool FileDetailsExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
