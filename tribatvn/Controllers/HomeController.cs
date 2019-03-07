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

        public IActionResult Index()
        {
            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region SEO
            var entity = dbContext.SEOs.Find(m => m.Code.Equals("home") && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            if (entity != null)
            {
                SeoInit(entity);
            }
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
            #endregion

            #region News
            var categoryNews = dbContext.NewsCategories.Find(m => m.Language.Equals(cultureInfo.Name) && m.Code.Equals(1)).First().Alias;
            ViewData["CategoryNews"] = categoryNews;
            var news = dbContext.News.Find(m => m.Enable.Equals(true) && m.HomePage.Equals(true) && m.Language.Equals(cultureInfo.Name)).SortByDescending(m => m.ModifiedDate).Limit(3).ToList();
            #endregion

            #region Products
            var products = dbContext.ProductSales.Find(m => m.Enable.Equals(true) && m.HomePage.Equals(true) && m.Language.Equals(cultureInfo.Name)).SortByDescending(m => m.ModifiedDate).Limit(3).ToList();
            var exProducts = new List<ExProductSale>();
            foreach (var product in products)
            {
                var category = dbContext.ProductCategorySales.Find(m => m.Enable.Equals(true) && m.Code.Equals(product.Code)).FirstOrDefault();
                var categoryAlias = category != null ? category.Alias : string.Empty;
                var exProduct = new ExProductSale
                {
                    Product = product,
                    CategoryAlias = categoryAlias
                };
                exProducts.Add(exProduct);
            }
            #endregion

            #region Extend
            var datsach = dbContext.ProductCategorySales.Find(m => m.ParentCode.Equals(1) && m.Code.Equals(1) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            var bun = dbContext.ProductCategorySales.Find(m => m.ParentCode.Equals(2) && m.Code.Equals(5) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            var dichvu = dbContext.ProductCategorySales.Find(m => m.ParentCode.Equals(3) && m.Code.Equals(8) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            var linkBun = "/p/"+ bun.Alias;
            var linkDatSach = "/p/" + datsach.Alias;
            var linkDichVu = "/p/" + dichvu.Alias;
            #endregion


            var viewModel = new HomeViewModel
            {
                Menu = menuViewModel,
                News = news,
                Products = exProducts,
                LinkBun = linkBun,
                LinkDatSach = linkDatSach,
                LinkDichVu = linkDichVu
            };

            return View(viewModel);
        }

        #region Product, Services....
        [Route("/p/{category}")]
        public IActionResult Category(string category)
        {
            var entitypre = dbContext.ProductCategorySales.Find(m => m.Alias.Equals(category)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var entity = dbContext.ProductCategorySales.Find(m => m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();

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

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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
            var entitypre = dbContext.ProductSales.Find(m => m.Alias.Equals(product)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Disable item
            if (entitypre.Enable.Equals(false))
            {
                ViewData["Disable"] = entitypre.Name;
                if (cultureInfo.Name == "vi-VN")
                {
                    ViewData["Disable"] = "Dữ liệu đã xóa";
                }
                else
                {
                    ViewData["Disable"] = "Data had been deleted.";
                }
                entitypre.Robots = "noindex, nofollow";
            }
            #endregion

            #region Link to not found
            if (entitypre == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                
            }
            #endregion

            var entity = dbContext.ProductSales.Find(m => m.CategoryCode.Equals(entitypre.CategoryCode) && m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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
            var categoryLanguage = dbContext.ProductCategorySales.Find(m => m.Language.Equals(entity.Language) && m.Code.Equals(entity.CategoryCode)).FirstOrDefault();
            var categoryAlias = category;
            if (categoryLanguage != null)
            {
                categoryAlias = categoryLanguage.Alias;
            }
            ViewData["Category"] = categoryAlias;
            links.Add(new Link
            {
                Url = "/p/" + categoryAlias + "/" + entity.Alias,
                Language = entity.Language
            });
            if (categoryLanguage.Language == cultureInfo.Name)
            {
                breadcrumbs.Add(new Breadcrumb
                {
                    Name = categoryLanguage.Name,
                    Url = "/p/" + categoryAlias,
                });
            }
            #endregion

            #region Relations
            var relations = dbContext.ProductSales.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name) && !m.Code.Equals(entity.Code) && m.CategoryCode.Equals(entity.CategoryCode)).SortByDescending(m => m.CreatedDate)
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
            var categorypre = dbContext.NewsCategories.Find(m => m.Alias.Equals(news)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var category = dbContext.NewsCategories.Find(m => m.Code.Equals(categorypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            #region Link to not found
            if (category == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                
            }
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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
            var entitypre = dbContext.News.Find(m => m.Alias.Equals(detail)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var entity = dbContext.News.Find(m => m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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
            var categorypre = dbContext.JobCategories.Find(m => m.Alias.Equals(job)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var category = dbContext.JobCategories.Find(m => m.ParentCode.Equals(categorypre.ParentCode) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();
            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
            #endregion

            #region Link to not found
            if (category == null)
            {
                if (cultureInfo.Name == "vi-VN")
                {
                    return Redirect("/not-found/");
                }
                return Redirect("/khong-tim-thay/");
                
            }
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
            var parentCode = category.ParentCode;
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
            var entitypre = dbContext.Jobs.Find(m => m.Alias.Equals(name)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var entity = dbContext.Jobs.Find(m => m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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
            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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

            var viewModel = new ContentViewModel()
            {
                Menu = menuViewModel
            };

            return View(viewModel);
        }

        // Menu gioi thieu: gioi thieu, doi tac, co cau to chuc,....
        [Route("/a/{input}")]
        public IActionResult About(string input)
        {
            var entitypre = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Alias.Equals(input)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var entity = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
            #endregion

            var contents = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name)).ToList();
            var gioithieu = contents.Where(m => m.Code.Equals("about")).FirstOrDefault();
            var doitac = contents.Where(m => m.Code.Equals("doitac")).FirstOrDefault();
            var cocautochuc = contents.Where(m => m.Code.Equals("cocautochuc")).FirstOrDefault();
            var tamnhinsumang = contents.Where(m => m.Code.Equals("tamnhinsumang")).FirstOrDefault();
            var vanbanthamkhao = contents.Where(m => m.Code.Equals("vanbanthamkhao")).FirstOrDefault();
            var cosophaply = contents.Where(m => m.Code.Equals("cosophaply")).FirstOrDefault();
            var lichsuhinhthanh = contents.Where(m => m.Code.Equals("lichsuhinhthanh")).FirstOrDefault();
            var congnghexuly = contents.Where(m => m.Code.Equals("congnghexuly")).FirstOrDefault();
            var congsuatxuly = contents.Where(m => m.Code.Equals("congsuatxuly")).FirstOrDefault();

            #region Link Menu Sub
            var linkGioiThieu = gioithieu != null ? "/a/" + gioithieu.Alias : string.Empty;
            var textGioiThieu = gioithieu != null ? gioithieu.Name : string.Empty;
            var linkDoiTac = doitac != null ? "/a/" + doitac.Alias : string.Empty;
            var textDoiTac = doitac != null ? doitac.Name : string.Empty;
            var linkCoCauToChuc = cocautochuc != null ? "/a/" + cocautochuc.Alias : string.Empty;
            var textCoCauToChuc = cocautochuc != null ? cocautochuc.Name : string.Empty; ;
            var linkTamNhinSuMang = tamnhinsumang != null ? "/a/" + tamnhinsumang.Alias : string.Empty;
            var textTamNhinSuMang = tamnhinsumang != null ? tamnhinsumang.Name : string.Empty;
            var linkVanBanThamKhao = vanbanthamkhao != null ? "/a/" + vanbanthamkhao.Alias : string.Empty;
            var textVanBanThamKhao = vanbanthamkhao != null ? vanbanthamkhao.Name : string.Empty;
            var linkCoSoPhapLy = cosophaply != null ? "/a/" + cosophaply.Alias : string.Empty;
            var textCoSoPhapLy = cosophaply != null ? cosophaply.Name : string.Empty;
            var linkLichSuHinhThanh = lichsuhinhthanh != null ? "/a/" + lichsuhinhthanh.Alias : string.Empty;
            var textLichSuHinhThanh = lichsuhinhthanh != null ? lichsuhinhthanh.Name : string.Empty;
            var linkCongNgheXuLy = congnghexuly != null ? "/a/" + congnghexuly.Alias : string.Empty;
            var textCongNgheXuLy = congnghexuly != null ? congnghexuly.Name : string.Empty;
            var linkCongSuatXuLy = congsuatxuly != null ? "/a/" + congsuatxuly.Alias : string.Empty;
            var textCongSuatXuLy = congsuatxuly != null ? congsuatxuly.Name : string.Empty;
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

            #region SEO
            ViewData["Title"] = entity.Title;
            ViewData["KeyWords"] = entity.KeyWords;
            ViewData["Description"] = entity.Description;
            ViewData["MetaOwner"] = entity.MetaOwner;
            ViewData["Canonical"] = entity.Canonical;
            ViewData["OgUrl"] = entity.OgUrl;
            ViewData["OgTitle"] = entity.OgTitle;
            ViewData["OgDescription"] = entity.OgDescription;
            #endregion

            var viewModel = new ContentViewModel()
            {
                Entity = entity,
                Menu = menuViewModel,
                linkGioiThieu = linkGioiThieu,
                textGioiThieu = textGioiThieu,
                linkDoiTac = linkDoiTac,
                textDoiTac = textDoiTac,
                linkCoCauToChuc = linkCoCauToChuc,
                textCoCauToChuc = textCoCauToChuc,
                linkTamNhinSuMang = linkTamNhinSuMang,
                textTamNhinSuMang = textTamNhinSuMang,
                linkVanBanThamKhao = linkVanBanThamKhao,
                textVanBanThamKhao = textVanBanThamKhao,
                linkCoSoPhapLy = linkCoSoPhapLy,
                textCoSoPhapLy = textCoSoPhapLy,
                linkLichSuHinhThanh = linkLichSuHinhThanh,
                textLichSuHinhThanh = textLichSuHinhThanh,
                linkCongNgheXuLy = linkCongNgheXuLy,
                textCongNgheXuLy = textCongNgheXuLy,
                linkCongSuatXuLy = linkCongSuatXuLy,
                textCongSuatXuLy = textCongSuatXuLy,
            };

            return View(viewModel);
        }

        [Route("/kh/{input}")]
        public IActionResult Customer(string input)
        {
            var entitypre = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Alias.Equals(input)).FirstOrDefault();

            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            var entity = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Code.Equals(entitypre.Code) && m.Language.Equals(cultureInfo.Name)).FirstOrDefault();

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
            #endregion

            var contents = dbContext.Contents.Find(m => m.Enable.Equals(true) && m.Language.Equals(cultureInfo.Name)).ToList();
            var vitridialy = contents.Where(m => m.Code.Equals("khachhangvitridialy")).FirstOrDefault();
            var xulychatthai = contents.Where(m => m.Code.Equals("khachhangxulychatthai")).FirstOrDefault();

            #region Link Menu Sub
            var linkKhachHangViTriDiaLy = vitridialy != null ? "/kh/" + vitridialy.Alias : string.Empty;
            var textKhachHangViTriDiaLy = vitridialy != null ? vitridialy.Name : string.Empty;
            var linkKhachHangXuLyChatThai = xulychatthai != null ? "/kh/" + xulychatthai.Alias : string.Empty;
            var textKhachHangXuLyChatThai = xulychatthai != null ? xulychatthai.Name : string.Empty;
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

            #region SEO
            ViewData["Title"] = entity.Title;
            ViewData["KeyWords"] = entity.KeyWords;
            ViewData["Description"] = entity.Description;
            ViewData["MetaOwner"] = entity.MetaOwner;
            ViewData["Canonical"] = entity.Canonical;
            ViewData["OgUrl"] = entity.OgUrl;
            ViewData["OgTitle"] = entity.OgTitle;
            ViewData["OgDescription"] = entity.OgDescription;
            #endregion

            var viewModel = new ContentViewModel()
            {
                Entity = entity,
                Menu = menuViewModel,
                linkKhachHangViTriDiaLy = linkKhachHangViTriDiaLy,
                textKhachHangViTriDiaLy = textKhachHangViTriDiaLy,
                linkKhachHangXuLyChatThai = linkKhachHangXuLyChatThai,
                textKhachHangXuLyChatThai = textKhachHangXuLyChatThai
            };

            return View(viewModel);
        }

        [Route("/f/{faq}")]
        public IActionResult Faq(string faq)
        {
            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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

        [Route("/du-lieu-da-xoa/")]
        [Route("/data-removed/")]
        public IActionResult DataDeleted()
        {
            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
            #endregion

            return View();
        }

        [Route("/khong-tim-thay/")]
        public IActionResult PageNotFound()
        {
            #region Language
            CultureInfo cultureInfo = CultureDefine(string.Empty);
            ViewData["Language"] = cultureInfo.Name;
            ViewData["LanguageHtmlTag"] = cultureInfo.TwoLetterISOLanguageName;
            #endregion

            #region Menu
            MenuViewModel menuViewModel = Menu(cultureInfo);
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

            var viewModel = new ContentViewModel()
            {
                Menu = menuViewModel
            };

            return View(viewModel);
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

        private CultureInfo CultureDefine(string inputLang)
        {
            var languages = new List<string>() { "vi-VN", "en-US" };
            var cultureInfo = new CultureInfo(languages[0]); // Default
            var cookie = Get("language");
            if (!string.IsNullOrEmpty(inputLang))
            {
                if (inputLang != cookie)
                {
                    // Fix btn language.
                    inputLang = cookie;

                    // Uu tien link
                    //if (!languages.Contains(input))
                    //{
                    //    input = languages[0];
                    //}
                    //Set("language", input, 10);
                }
                cultureInfo = new CultureInfo(inputLang);
            }
            else
            {
                if (!string.IsNullOrEmpty(cookie))
                {
                    cultureInfo = new CultureInfo(cookie);
                }
                else
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

                    cultureInfo = new CultureInfo(countryIsoCode == "US" ? languages[1] : languages[0]);
                }
            }

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
            return string.IsNullOrEmpty(Request.Cookies[key]) ? string.Empty : Request.Cookies[key];
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

        private MenuViewModel Menu(CultureInfo cultureInfo)
        {
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
            return menuViewModel;
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
    }
}
