using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WeddingPlan.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Net;
using System.Xml.Linq;

namespace WeddingPlan.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
        public HomeController(MyContext context)
        {
            dbContext = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public IActionResult Register(User regUser)
        {
            if (ModelState.IsValid)
            {
                if (dbContext.Users.Any(u => u.Email == regUser.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }
                else
                {
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    regUser.Password = Hasher.HashPassword(regUser, regUser.Password);
                    dbContext.Add(regUser);
                    dbContext.SaveChanges();
                    HttpContext.Session.SetString("UserEmail", regUser.Email);
                    return RedirectToAction("Dashboard",new { userId = regUser.UserId });
                }
            }
            else
            {
                return View("Index");
            }
        }

        [HttpPost("login")]
        public IActionResult Login(LogUser logUser)
        {
            if (ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == logUser.LogEmail);
                if (userInDb == null)
                {
                    ModelState.AddModelError("LogEmail", "Invalid Email/Password");
                    return View("Index");
                }
                var hasher = new PasswordHasher<LogUser>();
                var result = hasher.VerifyHashedPassword(logUser, userInDb.Password, logUser.LogPassword);
                if(result == 0)
                {
                    ModelState.AddModelError("LogPassword", "Invalid Email/Password");
                    return View("Index");
                }
                HttpContext.Session.SetString("UserEmail", userInDb.Email);
                return RedirectToAction("Dashboard", new { userId = userInDb.UserId });
            }
            else
            {
                return View("Index");
            }
        }

        [HttpGet("dashboard/{userId}")]
        public IActionResult Dashboard(int userId)
        {
            User thisUser = dbContext.Users
            .Include(u => u.ManyWeddings)
            .ThenInclude(a => a.Wedding)
            .FirstOrDefault(u => u.UserId == userId);
            string userInSession = HttpContext.Session.GetString("UserEmail");
            if(thisUser.Email == userInSession)
            {
                ViewBag.username = thisUser.FirstName;
                ViewBag.userId = userId;
                List<Wedding> allWeddings = dbContext.Weddings
                .Include(w => w.Creator)
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .OrderBy(w => w.Date)
                .ToList();
                CheckExpiredWedding(allWeddings);
                ViewBag.joinedWeddings = thisUser.ManyWeddings.Select(w => w.Wedding).ToList();
                return View(allWeddings);
            }
            return View("Index");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View("Index");
        }

        [HttpGet("new/{userId}")]
        public IActionResult New(int userId)
        {
            User thisUser = dbContext.Users.FirstOrDefault(u => u.UserId == userId);
            string userInSession = HttpContext.Session.GetString("UserEmail");
            if(thisUser.Email == userInSession)
            {
                ViewBag.username = thisUser.FirstName;
                ViewBag.userId = userId;
                return View();
            }
            else
            {
                return View("Index");
            }           
        }

        [HttpPost("addWedding/{userId}")]
        public IActionResult AddWedding(int userId, Wedding oneWedding)
        {
            if (ModelState.IsValid)
            {
                Wedding newWedding = new Wedding{
                    Bride = oneWedding.Bride,
                    Bridegroom = oneWedding.Bridegroom,
                    Date = oneWedding.Date,
                    WeddingAddress = oneWedding.WeddingAddress,
                    UserId = userId
                };
                dbContext.Weddings.Add(newWedding);
                dbContext.SaveChanges();
                Wedding thisWedding = dbContext.Weddings
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .FirstOrDefault(w => w.WeddingId == newWedding.WeddingId);
                ViewBag.allGuests = thisWedding.ManyGuests.Select(w => w.User).ToList();
                ViewBag.userId = userId;

                var location = GetLocationFromGoogleApi(thisWedding.WeddingAddress);
                ViewBag.latitude = location.latitude; 
                ViewBag.longitude = location.longitude;

                return View("Show",newWedding);
            }
            else
            {
                User thisUser = dbContext.Users.FirstOrDefault(u => u.UserId == userId);
                ViewBag.username = thisUser.FirstName;
                ViewBag.userId = userId;
                return View("New");
            }
        }

        [HttpGet("{userId}/show/{weddingId}")]
        public IActionResult Show(int weddingId, int userId)
        {
            User thisUser = dbContext.Users.FirstOrDefault(u => u.UserId == userId);
            string userInSession = HttpContext.Session.GetString("UserEmail");
            if(thisUser.Email == userInSession)
            {
                Wedding thisWedding = dbContext.Weddings
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .FirstOrDefault(w => w.WeddingId == weddingId);
                ViewBag.allGuests = thisWedding.ManyGuests.Select(w => w.User).ToList();
                ViewBag.userId = userId;

                var location = GetLocationFromGoogleApi(thisWedding.WeddingAddress);
                ViewBag.latitude = location.latitude; 
                ViewBag.longitude = location.longitude;
                
                return View(thisWedding);
            }
            else
            {
                return View("Index");
            }
        }

        [HttpGet("{userId}/delete/{weddingId}")]
        public IActionResult Delete(int weddingId, int userId)
        {
            Wedding thisWedding = dbContext.Weddings.FirstOrDefault(w => w.WeddingId == weddingId);
            dbContext.Weddings.Remove(thisWedding);
            dbContext.SaveChanges();
            User thisUser = dbContext.Users
            .Include(u => u.ManyWeddings)
            .ThenInclude(a => a.Wedding)
            .FirstOrDefault(u => u.UserId == userId);
            ViewBag.username = thisUser.FirstName;
            ViewBag.userId = userId;
            List<Wedding> allWeddings = dbContext.Weddings
                .Include(w => w.Creator)
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .OrderBy(w => w.Date)
                .ToList();
            CheckExpiredWedding(allWeddings);
            ViewBag.joinedWeddings = thisUser.ManyWeddings.Select(w => w.Wedding).ToList();
            return View("Dashboard", allWeddings);
        }

        [HttpGet("{userId}/join/{weddingId}")]
        public IActionResult Join(int weddingId, int userId)
        {
            Association newJoin = new Association{
                UserId = userId,
                WeddingId = weddingId
            };
            dbContext.Associations.Add(newJoin);
            dbContext.SaveChanges();
            User thisUser = dbContext.Users
            .Include(u => u.ManyWeddings)
            .ThenInclude(a => a.Wedding)
            .FirstOrDefault(u => u.UserId == userId);
            ViewBag.username = thisUser.FirstName;
            ViewBag.userId = userId;
            List<Wedding> allWeddings = dbContext.Weddings
                .Include(w => w.Creator)
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .OrderBy(w => w.Date)
                .ToList();
            CheckExpiredWedding(allWeddings);
            ViewBag.joinedWeddings = thisUser.ManyWeddings.Select(w => w.Wedding).ToList();
            return View("Dashboard", allWeddings);
        }

        [HttpGet("{userId}/unjoin/{weddingId}")]
        public IActionResult UnJoin(int weddingId, int userId)
        {
            Association thisJoin = dbContext.Associations.FirstOrDefault(a => a.UserId == userId && a.WeddingId == weddingId);
            dbContext.Associations.Remove(thisJoin);
            dbContext.SaveChanges();
            User thisUser = dbContext.Users
            .Include(u => u.ManyWeddings)
            .ThenInclude(a => a.Wedding)
            .FirstOrDefault(u => u.UserId == userId);
            ViewBag.username = thisUser.FirstName;
            ViewBag.userId = userId;
            List<Wedding> allWeddings = dbContext.Weddings
                .Include(w => w.Creator)
                .Include(w => w.ManyGuests)
                .ThenInclude(a => a.User)
                .OrderBy(w => w.Date)
                .ToList();
            CheckExpiredWedding(allWeddings);
            ViewBag.joinedWeddings = thisUser.ManyWeddings.Select(w => w.Wedding).ToList();
            return View("Dashboard", allWeddings);
        }
        
        
        public void CheckExpiredWedding(List<Wedding> allWeddings)
        {
            foreach(var wedding in allWeddings)
            {
                DateTime datetime = Convert.ToDateTime(wedding.Date);
                if(datetime.Date < DateTime.Now.Date)
                {
                    dbContext.Weddings.Remove(wedding);
                    dbContext.SaveChanges();
                }
            }
        }

        private Location GetLocationFromGoogleApi(string address) {
            string requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}&key={REPLACE_WITH_YOUR_OWN_KEY}&sensor=false", Uri.EscapeDataString(address));

            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XElement result = xdoc.Element("GeocodeResponse").Element("result");
            XElement locationElement = result.Element("geometry").Element("location");
            XElement lat = locationElement.Element("lat");
            XElement lng = locationElement.Element("lng");

            return new Location {
                latitude = double.Parse(lat.Value),
                longitude = double.Parse(lng.Value)
            };
        }
    }

    public class Location {
        public double latitude {get; set;}
        public double longitude {get; set;}
    }
}
