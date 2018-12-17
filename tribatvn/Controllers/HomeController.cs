using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Common.Utilities;
using Data;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Driver;
using ViewModels;

namespace tribatvn.Controllers
{
    public class HomeController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        // Use cookie
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(IConfiguration configuration, IHostingEnvironment env, ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor)
        {
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
            this._httpContextAccessor = httpContextAccessor;
        }

        #region Mau 1
        public IActionResult Index()
        {
            CultureInfo cultureInfo = CultureDefine();

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("home") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                SeoInit(entity);
            }
            #endregion

            #region cookie (see)
            ////read cookie from IHttpContextAccessor  
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];
            ////read cookie from Request object  
            //string cookieValueFromReq = Request.Cookies["Key"];
            //set the key value in Cookie  
            ////Set("language", languageCode, 10);

            ////Delete the cookie object  
            //Remove("Key");
            #endregion


            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Title,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region News
            var categoryNews = dbContext.NewsCategories.Find(m => m.Language.Equals(cultureInfo.Name) && m.Code.Equals(1)).First().Alias;
            ViewData["CategoryNews"] = categoryNews;
            var news = dbContext.News.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name)).SortByDescending(m => m.CreatedDate)
                .Limit(3).ToList();
            #endregion

            var viewModel = new HomeViewModel
            {
                Menu = menuViewModel,
                News = news
            };
            return View(viewModel);
        }

        private void SeoInit(SEO entity)
        {
            ViewData["Title"] = entity.Title;
            ViewData["KeyWords"] = entity.KeyWords;
            ViewData["Description"] = entity.Description;
            ViewData["MetaOwner"] = entity.MetaOwner;
            ViewData["Canonical"] = entity.Canonical;
            ViewData["OgUrl"] = entity.OgUrl;
            ViewData["OgTitle"] = entity.OgTitle;
            ViewData["OgDescription"] = entity.OgDescription;
            ViewData["Robots"] = Constants.Seo.indexFollow;
        }

        #region Product
        [Route("/p/{category}")]
        public IActionResult Category(string category)
        {
            var entity = dbContext.ProductCategorySales.Find(m => m.Alias.Equals(category)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Link to not found
            if (entity == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                //return RedirectToAction("NotFound", "Home");
            }
            #endregion

            #region Check language with cookie
            if (entity.Language != Get("language"))
            {
                // Set cookie again
                Set("language", entity.Language, 10);
            }

            cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (entity != null)
            {
                ViewData["Title"] = entity.Name;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
                ViewData["Robots"] = entity.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            ViewData["Category"] = category;
            var parentCode = entity.Code;
            var links = new List<Link>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Code.Equals(parentCode)).ToList())
            {
                links.Add(new Link
                {
                    Url = "/" + Constants.Link.Product + "/" + item.Alias,
                    Language = item.Language
                });
            }
            #endregion

            var entities = dbContext.ProductSales.Find(m => m.Enable.Equals(true) && m.CategoryCode.Equals(parentCode) && m.Language.Equals(cultureInfo.Name)).ToList();

            // Paging later, no more products now.
            var viewModel = new CategoryViewModel()
            {
                Entities = entities,
                Entity = entity,
                Links = links,
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        [Route("/p/{category}/{product}")]
        public IActionResult Product(string category, string product)
        {
            var entity = dbContext.ProductSales.Find(m => m.Alias.Equals(product)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Disable item
            if (entity.Enable.Equals(false))
            {
                ViewData["Disable"] = entity.Name;
                if (cultureInfo.Name == "vi-VN")
                {
                    ViewData["Disable"] = "Dữ liệu đã xóa";
                }
                else
                {
                    ViewData["Disable"] = "Data had been deleted.";
                }
                entity.Robots = "noindex, nofollow";
            }
            #endregion

            #region Link to not found
            if (entity == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                //return RedirectToAction("NotFound", "Home");
            }
            #endregion

            #region Check language with cookie
            if (entity.Language != Get("language"))
            {
                // Set cookie again
                Set("language", entity.Language, 10);
            }

            cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (entity != null)
            {
                ViewData["Title"] = entity.Name;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
                ViewData["Robots"] = entity.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            var breadcrumbs = new List<Breadcrumb>();
            var links = new List<Link>();
            foreach (var item in dbContext.ProductSales.Find(m => m.Code.Equals(entity.Code)).ToList())
            {
                var categoryLanguage = dbContext.ProductCategorySales.Find(m => m.Language.Equals(item.Language) && m.Code.Equals(item.CategoryCode)).FirstOrDefault();
                var categoryAlias = category;
                if (categoryLanguage != null)
                {
                    categoryAlias = categoryLanguage.Alias;
                }
                ViewData["Category"] = categoryAlias;

                links.Add(new Link
                {
                    Url = "/p/" + categoryAlias + "/" + item.Alias,
                    Language = item.Language
                });
                if (categoryLanguage.Language == cultureInfo.Name)
                {
                    breadcrumbs.Add(new Breadcrumb
                    {
                        Name = categoryLanguage.Name,
                        Url = "/p/" + categoryAlias,
                    });
                }
            }
            #endregion

            #region Relations
            var relations = dbContext.ProductSales.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name) && !m.Code.Equals(entity.Code)).SortByDescending(m => m.CreatedDate)
                .Limit(5).ToList();
            #endregion

            var viewModel = new ProductViewModel()
            {
                Entity = entity,
                Relations = relations,
                Breadcrumbs = breadcrumbs,
                Links = links,
                Menu = menuViewModel
            };
            return View(viewModel);
        }
        #endregion

        [Route("/n/{news}")]
        public IActionResult News(string news)
        {
            var category = dbContext.NewsCategories.Find(m => m.Alias.Equals(news)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Link to not found
            if (category == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                //return RedirectToAction("NotFound", "Home");
            }
            #endregion

            #region Check language with cookie
            if (category.Language != Get("language"))
            {
                // Set cookie again
                Set("language", category.Language, 10);
            }

            //cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (category != null)
            {
                ViewData["Title"] = category.SeoTitle;
                ViewData["KeyWords"] = category.KeyWords;
                ViewData["Description"] = category.Description;
                ViewData["MetaOwner"] = category.MetaOwner;
                ViewData["Canonical"] = category.Canonical;
                ViewData["OgUrl"] = category.OgUrl;
                ViewData["OgTitle"] = category.OgTitle;
                ViewData["OgDescription"] = category.OgDescription;
                ViewData["Robots"] = category.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            ViewData["Category"] = news;
            var parentCode = category.Code;
            var links = new List<Link>();
            foreach (var item in dbContext.NewsCategories.Find(m => m.Code.Equals(parentCode)).ToList())
            {
                links.Add(new Link
                {
                    Url = "/" + Constants.Link.News + "/" + item.Alias,
                    Language = item.Language
                });
            }
            #endregion

            var entities = dbContext.News.Find(m => m.Enable.Equals(true) && m.CategoryCode.Equals(parentCode) && m.Language.Equals(cultureInfo.Name)).ToList();

            // Paging later, no more products now.
            var viewModel = new NewsViewModel()
            {
                Entities = entities,
                Links = links,
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        [Route("/n/{category}/{detail}")]
        public IActionResult NewsDetail(string category, string detail)
        {
            var entity = dbContext.News.Find(m => m.Alias.Equals(detail)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Disable item
            if (entity.Enable.Equals(false))
            {
                ViewData["Disable"] = entity.Name;
                if (cultureInfo.Name == "vi-VN")
                {
                    ViewData["Disable"] = "Dữ liệu đã xóa";
                }
                else
                {
                    ViewData["Disable"] = "Data had been deleted.";
                }
                entity.Robots = "noindex, nofollow";
            }
            #endregion

            #region Link to not found
            if (entity == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
            }
            #endregion

            #region Check language with cookie
            if (entity.Language != Get("language"))
            {
                // Set cookie again
                Set("language", entity.Language, 10);
            }

            //cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (entity != null)
            {
                ViewData["Title"] = entity.SeoTitle;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
                ViewData["Robots"] = entity.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            var breadcrumbs = new List<Breadcrumb>();
            var links = new List<Link>();
            foreach (var item in dbContext.News.Find(m => m.Code.Equals(entity.Code)).ToList())
            {
                var categoryLanguage = dbContext.NewsCategories.Find(m => m.Language.Equals(item.Language) && m.Code.Equals(item.CategoryCode)).FirstOrDefault();
                var categoryAlias = category;
                if (categoryLanguage != null)
                {
                    categoryAlias = categoryLanguage.Alias;
                }
                links.Add(new Link
                {
                    Url = "/" + Constants.Link.News + "/" + categoryAlias + "/" + item.Alias,
                    Language = item.Language
                });
                if (categoryLanguage != null)
                {
                    if (categoryLanguage.Language == cultureInfo.Name)
                    {
                        breadcrumbs.Add(new Breadcrumb
                        {
                            Name = categoryLanguage.Name,
                            Url = "/" + Constants.Link.News + "/" + categoryAlias,
                        });
                        ViewData["Category"] = categoryAlias;
                    }
                }
            }
            #endregion

            #region Relations
            var relations = dbContext.News.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name) && !m.Code.Equals(entity.Code)).SortByDescending(m => m.CreatedDate)
                .Limit(3).ToList();
            #endregion

            // Paging later, no more products now.
            var viewModel = new NewsDetailViewModel()
            {
                Entity = entity,
                Relations = relations,
                Links = links,
                Breadcrumbs = breadcrumbs,
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        [Route("/j/{job}")]
        public IActionResult Job(string job)
        {
            var category = dbContext.JobCategories.Find(m => m.Alias.Equals(job)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Link to not found
            if (category == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                //return RedirectToAction("NotFound", "Home");
            }
            #endregion

            #region Check language with cookie
            if (category.Language != Get("language"))
            {
                // Set cookie again
                Set("language", category.Language, 10);
            }

            //cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (category != null)
            {
                ViewData["Title"] = category.SeoTitle;
                ViewData["KeyWords"] = category.KeyWords;
                ViewData["Description"] = category.Description;
                ViewData["MetaOwner"] = category.MetaOwner;
                ViewData["Canonical"] = category.Canonical;
                ViewData["OgUrl"] = category.OgUrl;
                ViewData["OgTitle"] = category.OgTitle;
                ViewData["OgDescription"] = category.OgDescription;
                ViewData["Robots"] = category.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            ViewData["Category"] = job;
            var parentCode = category.Code;
            var links = new List<Link>();
            foreach (var item in dbContext.NewsCategories.Find(m => m.Code.Equals(parentCode)).ToList())
            {
                links.Add(new Link
                {
                    Url = "/" + Constants.Link.Job + "/" + item.Alias,
                    Language = item.Language
                });
            }
            #endregion

            var entities = dbContext.Jobs.Find(m => m.Enable.Equals(true) && m.CategoryCode.Equals(parentCode) && m.Language.Equals(cultureInfo.Name)).ToList();

            // Paging later, no more products now.
            var viewModel = new JobViewModel()
            {
                Entities = entities,
                Links = links,
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        [Route("/j/{job}/{name}")]
        public IActionResult JobDetail(string job, string name)
        {
            var entity = dbContext.Jobs.Find(m => m.Alias.Equals(name)).FirstOrDefault();

            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion

            #region Disable item
            if (entity.Enable.Equals(false))
            {
                ViewData["Disable"] = entity.Name;
                if (cultureInfo.Name == "vi-VN")
                {
                    ViewData["Disable"] = "Dữ liệu đã xóa";
                }
                else
                {
                    ViewData["Disable"] = "Data had been deleted.";
                }
                entity.Robots = "noindex, nofollow";
            }
            #endregion

            #region Link to not found
            if (entity == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
            }
            #endregion

            #region Check language with cookie
            if (entity.Language != Get("language"))
            {
                // Set cookie again
                Set("language", entity.Language, 10);
            }

            //cultureInfo = new CultureInfo(entity.Language);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region SEO
            if (entity != null)
            {
                ViewData["Title"] = entity.SeoTitle;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
                ViewData["Robots"] = entity.Robots;
            }
            #endregion

            #region Breadcumbs and Link
            var breadcrumbs = new List<Breadcrumb>();
            var links = new List<Link>();
            foreach (var item in dbContext.Jobs.Find(m => m.Code.Equals(entity.Code)).ToList())
            {
                var categoryLanguage = dbContext.JobCategories.Find(m => m.Language.Equals(item.Language) && m.Code.Equals(item.CategoryCode)).FirstOrDefault();
                var categoryAlias = job;
                if (categoryLanguage != null)
                {
                    categoryAlias = categoryLanguage.Alias;
                }
                links.Add(new Link
                {
                    Url = "/" + Constants.Link.Job + "/" + categoryAlias + "/" + item.Alias,
                    Language = item.Language
                });
                if (categoryLanguage != null)
                {
                    if (categoryLanguage.Language == cultureInfo.Name)
                    {
                        breadcrumbs.Add(new Breadcrumb
                        {
                            Name = categoryLanguage.Name,
                            Url = "/" + Constants.Link.Job + "/" + categoryAlias,
                        });
                    }
                }
            }
            #endregion

            // Paging later, no more products now.
            var viewModel = new JobDetailViewModel()
            {
                Entity = entity,
                Links = links,
                Breadcrumbs = breadcrumbs,
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        [Route("/c/{contact}")]
        public IActionResult Contact(string contact)
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("contact") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                ViewData["Title"] = entity.Title;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
            }
            #endregion

            return View();
        }

        [Route("/a/{about}")]
        public IActionResult About(string about)
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Code.Equals("about") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                ViewData["Title"] = entity.Title;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
            }
            #endregion
            //ContentViewModel = new
            return View();
        }

        [Route("/f/{faq}")]
        public IActionResult Faq(string faq)
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("faq") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                ViewData["Title"] = entity.Title;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
            }
            #endregion
            return View();
        }
        #endregion

        #region Mau 2
        [Route("/2")]
        public IActionResult Index2()
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    try
                    {
                        var cityEntity = reader.City(ipAddress);
                        countryIsoCode = cityEntity.Country.IsoCode;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("home") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                ViewData["Title"] = entity.Title;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
                ViewData["Robots"] = Constants.Seo.indexFollow;
            }
            #endregion

            #region cookie
            ////read cookie from IHttpContextAccessor  
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];
            ////read cookie from Request object  
            //string cookieValueFromReq = Request.Cookies["Key"];
            //set the key value in Cookie  
            ////Set("language", languageCode, 10);

            ////Delete the cookie object  
            //Remove("Key");
            #endregion


            #region Menu : auto base categories product | news, about,...
            var menusProduct = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 1).ToList())
            {
                menusProduct.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusProcess = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 2).ToList())
            {
                menusProcess.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }
            var menusService = new List<Menu>();
            foreach (var item in dbContext.ProductCategorySales.Find(m => m.Language.Equals(cultureInfo.Name) && m.ParentCode == 3).ToList())
            {
                menusService.Add(new Menu
                {
                    Url = "/p/" + item.Alias,
                    Title = item.Name,
                    Description = item.Description,
                    Type = item.ParentCode,
                    Language = item.Language
                });
            }

            var menuContents = new List<Menu>();
            var contents = dbContext.Contents.Find(m => m.Language.Equals(cultureInfo.Name)).ToList();
            foreach (var content in contents)
            {
                menuContents.Add(new Menu
                {
                    Code = content.Code,
                    Title = content.Name,
                    Url = content.Alias,
                    Description = content.Description,
                    Type = 4,
                    Language = content.Language
                });
            }

            var menuViewModel = new MenuViewModel
            {
                MenusProduct = menusProduct,
                MenusProccess = menusProcess,
                MenusService = menusService,
                MenusContent = menuContents
            };
            #endregion

            #region 
            var categoryNews = dbContext.NewsCategories.Find(m => m.Language.Equals(cultureInfo.Name) && m.Code.Equals(1)).First().Alias;
            ViewData["CategoryNews"] = categoryNews;
            var news = dbContext.News.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name)).SortByDescending(m => m.CreatedDate)
                .Limit(3).ToList();
            #endregion


            var viewModel = new HomeViewModel
            {
                Menu = menuViewModel,
                News = news
            };
            return View(viewModel);
        }

        #endregion

        #region Mau 3
        [Route("/3")]
        public IActionResult Mau3()
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("home") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                ViewData["Title"] = entity.Title;
                ViewData["KeyWords"] = entity.KeyWords;
                ViewData["Description"] = entity.Description;
                ViewData["MetaOwner"] = entity.MetaOwner;
                ViewData["Canonical"] = entity.Canonical;
                ViewData["OgUrl"] = entity.OgUrl;
                ViewData["OgTitle"] = entity.OgTitle;
                ViewData["OgDescription"] = entity.OgDescription;
            }
            #endregion

            #region cookie
            ////read cookie from IHttpContextAccessor  
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];
            ////read cookie from Request object  
            //string cookieValueFromReq = Request.Cookies["Key"];
            //set the key value in Cookie  
            ////Set("language", languageCode, 10);

            ////Delete the cookie object  
            //Remove("Key");
            #endregion

            return View();
        }

        #endregion

        [Route("/du-lieu-da-xoa/")]
        [Route("/data-removed/")]
        public IActionResult DataDeleted()
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    var cityEntity = reader.City(ipAddress);
                    countryIsoCode = cityEntity.Country.IsoCode;
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }

            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private CultureInfo CultureDefine()
        {
            #region Geo
            var ipAddressCookie = Get("ipAddress");
            // Determine the IP Address of the request
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var countryIsoCode = string.Empty;
            if (ipAddress.ToString() != ipAddressCookie)
            {
                Set("ipAddress", ipAddress.ToString(), 10);
                var reader = new DatabaseReader(_hostingEnvironment.WebRootPath + "\\GeoLite2-City.mmdb");
                // Get the city from the IP Address
                if (ipAddress != null)
                {
                    try
                    {
                        var cityEntity = reader.City(ipAddress);
                        countryIsoCode = cityEntity.Country.IsoCode;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                }
            }
            #endregion

            #region Language
            var cultureInfo = new CultureInfo(countryIsoCode == "US" ? "en-US" : "vi-VN");
            // Nếu chưa chọn ngôn ngữ, mặc định theo location. VN là tiếng Việt.
            if (!string.IsNullOrEmpty(Get("language")))
            {
                cultureInfo = new CultureInfo(Get("language"));
            }
            #endregion
            return cultureInfo;
        }

        #region Cookie
        /// <summary>  
        /// Get the cookie  
        /// </summary>  
        /// <param name="key">Key </param>  
        /// <returns>string value</returns>  
        public string Get(string key)
        {
            //return Request.Cookies["Key"];
            return Request.Cookies[key];
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);
            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void Remove(string key)
        {
            Response.Cookies.Delete(key);
        }
        #endregion
    }
}
