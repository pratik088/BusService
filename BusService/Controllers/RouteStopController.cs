using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BusService.Models;
using Microsoft.AspNetCore.Http;

namespace BusService.Controllers
{
    public class RouteStopController : Controller
    {
        private readonly BusServiceContext _context;

        public RouteStopController(BusServiceContext context)
        {
            _context = context;
        }

        // GET: RouteStop
        public async Task<IActionResult> Index(string BusRouteCode)
        {
            //URL Check
            if(BusRouteCode != null)
            {
                Response.Cookies.Append("BusRouteCode",BusRouteCode);
                HttpContext.Session.SetString("BusRouteCode", BusRouteCode);
            }
            //QueryString Check
            else if (Request.Query["BusRouteCode"].Any())
            {
                Response.Cookies.Append("BusRouteCode", Request.Query["BusRouteCode"]);
                HttpContext.Session.SetString("BusRouteCode", Request.Query["BusRouteCode"]);
                BusRouteCode = Request.Query["BusRouteCode"];
            }
            else if(Request.Cookies["BusRouteCode"] != null)
            {
                BusRouteCode = Request.Cookies["BusRouteCode"].ToString();
            }
            else if(HttpContext.Session.GetString("BusRouteCode") != null)
            {
                BusRouteCode = HttpContext.Session.GetString("BusRouteCode");
            }
            else
            {
                TempData["Message"] = "Please Select a Route";
                return RedirectToAction("Index", "BusRoute");
            }

            var busRoute = _context.BusRoute.Where(a => a.BusRouteCode == BusRouteCode).FirstOrDefault();
            ViewData["BusRouteCode"] = busRoute.BusRouteCode;
            ViewData["BusRouteName"] = busRoute.RouteName;
            // Query to order by offset minutesx
            var busServiceContext = _context.RouteStop.Include(r => r.BusRouteCodeNavigation).Include(r => r.BusStopNumberNavigation)
                .Where(a => a.BusRouteCode == BusRouteCode).OrderBy(m => m.OffsetMinutes);
            return View(await busServiceContext.ToListAsync());
        }

        // GET: RouteStop/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop
                .Include(r => r.BusRouteCodeNavigation)
                .Include(r => r.BusStopNumberNavigation)
                .FirstOrDefaultAsync(m => m.RouteStopId == id);
            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // GET: RouteStop/Create
        public IActionResult Create()
        {
            string busRCode = String.Empty;
         if (Request.Query["BusRouteCode"].Any())
            {
                Response.Cookies.Append("BusRouteCode", Request.Query["BusRouteCode"]);
                HttpContext.Session.SetString("BusRouteCode", Request.Query["BusRouteCode"]);
                busRCode = Request.Query["BusRouteCode"];
            }
            else
            {
                TempData["BusRouteCode"] = "Please Select a Route";
                return RedirectToAction("Index", "BusRoute");
            }

            var busRoute = _context.BusRoute.Where(a => a.BusRouteCode == busRCode).FirstOrDefault();
            ViewData["BusRCode"] = busRoute.BusRouteCode;
            ViewData["BusRName"] = busRoute.RouteName;

            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode");
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(a => a.Location), "BusStopNumber", "Location");
            return View();
        }

        // POST: RouteStop/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RouteStopId,BusRouteCode,BusStopNumber,OffsetMinutes")] RouteStop routeStop)
        {
            //The offsetMinutes must be zero or more
            if (routeStop.OffsetMinutes < 0) {
                ModelState.AddModelError("", "OffsetMinutes must be 0 or greater than 0");
            }

            string busRCode = String.Empty;
            if (Request.Query["BusRouteCode"].Any())
            {
                Response.Cookies.Append("BusRouteCode", Request.Query["BusRouteCode"]);
                HttpContext.Session.SetString("BusRouteCode", Request.Query["BusRouteCode"]);
                busRCode = Request.Query["BusRouteCode"];
            }
            else
            {
                TempData["BusRouteCode"] = "Please Select a Route";
                return RedirectToAction("Index", "BusRoute");
            }

            var Test = busRCode; 

            var busRoute = _context.BusRoute.Where(a => a.BusRouteCode == busRCode).FirstOrDefault();
            ViewData["BusRCode"] = busRoute.BusRouteCode;
            ViewData["BusRName"] = busRoute.RouteName;
            routeStop.BusRouteCode = busRCode;
            
            //Check for offset 0 
            if(routeStop.OffsetMinutes == 0)
            {
               var isZeroExists =  _context.RouteStop.Where(a => a.OffsetMinutes == 0 && a.BusRouteCode == routeStop.BusRouteCode);
                if (isZeroExists.Any())
                {
                    ModelState.AddModelError("", "There is already record for offset minute 0");
                }
            }
            //iii.	There cannot be a duplicate route/stop combination already on file.
            var isDuplicate = _context.RouteStop.Where(a => a.BusRouteCode == routeStop.BusRouteCode && a.BusStopNumber == routeStop.BusStopNumber);
            if (isDuplicate.Any())
            {
                ModelState.AddModelError("", "Duplicate Record");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(routeStop);
                    await _context.SaveChangesAsync();
                    //If the record passes these criteria and is successfully added to the database, return to the routeStop listing with a message saying so
                    TempData["message"] = "New Route Stop Added";
                    return RedirectToAction(nameof(Index));
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("",ex.GetBaseException().Message);
                }
            }
            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(a => a.Location), "BusStopNumber", "Location", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // GET: RouteStop/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            string busRCode = String.Empty;
            if (Request.Cookies["BusRouteCode"] != null)
            {
                busRCode = Request.Cookies["BusRouteCode"];
            }
            else if (HttpContext.Session.GetString("BusRouteCode") != null) { }

            var busRoute = _context.BusRoute.Where(a => a.BusRouteCode == busRCode).FirstOrDefault();
            ViewData["BusRCode"] = busRoute.BusRouteCode;
            ViewData["BusRName"] = busRoute.RouteName;

            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop.FindAsync(id);
            if (routeStop == null)
            {
                return NotFound();
            }
            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(a => a.Location), "BusStopNumber", "Location", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // POST: RouteStop/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RouteStopId,BusRouteCode,BusStopNumber,OffsetMinutes")] RouteStop routeStop)
        {
            string busRCode = String.Empty;
            if (Request.Cookies["BusRouteCode"] != null)
            {
                busRCode = Request.Cookies["BusRouteCode"];
            }
            else if (HttpContext.Session.GetString("BusRouteCode") != null) { }
            var busRoute = _context.BusRoute.Where(a => a.BusRouteCode == busRCode).FirstOrDefault();
            ViewData["BusRCode"] = busRoute.BusRouteCode;
            ViewData["BusRName"] = busRoute.RouteName;
            routeStop.BusRouteCode = busRCode;

            //The offsetMinutes must be zero or more
            if (routeStop.OffsetMinutes < 0)
            {
                ModelState.AddModelError("", "OffsetMinutes must be 0 or greater than 0");
            }
            //Check for offset 0 
            if (routeStop.OffsetMinutes == 0)
            {
                var isZeroExists = _context.RouteStop.Where(a => a.OffsetMinutes == 0 && a.BusRouteCode == routeStop.BusRouteCode);
                if (isZeroExists.Any())
                {
                    ModelState.AddModelError("", "There is already record for offset minute 0");
                }
            }
           

            if (id != routeStop.RouteStopId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                try
                {
                    _context.Update(routeStop);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RouteStopExists(routeStop.RouteStopId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["message"] = "Sucessfully Edited";
                return RedirectToAction(nameof(Index));
            }
            ViewData["BusRouteCode"] = new SelectList(_context.BusRoute, "BusRouteCode", "BusRouteCode", routeStop.BusRouteCode);
            ViewData["BusStopNumber"] = new SelectList(_context.BusStop.OrderBy(a => a.Location), "BusStopNumber", "Location", routeStop.BusStopNumber);
            return View(routeStop);
        }

        // GET: RouteStop/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeStop = await _context.RouteStop
                .Include(r => r.BusRouteCodeNavigation)
                .Include(r => r.BusStopNumberNavigation)
                .FirstOrDefaultAsync(m => m.RouteStopId == id);
            if (routeStop == null)
            {
                return NotFound();
            }

            return View(routeStop);
        }

        // POST: RouteStop/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routeStop = await _context.RouteStop.FindAsync(id);
            _context.RouteStop.Remove(routeStop);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RouteStopExists(int id)
        {
            return _context.RouteStop.Any(e => e.RouteStopId == id);
        }
    }
}
