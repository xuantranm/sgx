using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace tribatvn.Controllers
{
    [Route("api/[controller]")]
    public class SystemController : Controller
    {
        MongoDBContext dbContext = new MongoDBContext();
        IHostingEnvironment _hostingEnvironment;

        private readonly ILogger _logger;

        public IConfiguration Configuration { get; }

        public SystemController(IConfiguration configuration, IHostingEnvironment env, ILogger<SystemController> logger)
        {
            Configuration = configuration;
            _hostingEnvironment = env;
            _logger = logger;
        }

        #region Init Data
        // Call an initialization - api/system/init
        [HttpGet("{setting}")]
        public string Get(string setting)
        {
            if (setting == "init")
            {
                _logger.LogInformation(LoggingEvents.GenerateItems, "Generate first data");

                InitLanguages();

                InitSeos();

                InitTexts();

                InitDepartments();

                InitCategories();

                InitProducts();

                InitContents();

                InitNewsCategory();

                InitNews();

                InitJobcategory();

                InitJobs();

                return Constants.String_Y;
            }

            return Constants.String_Y;
        }

        public void InitLanguages()
        {
            dbContext.Languages.DeleteMany(new BsonDocument());
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "zh",
                Title = "Chinese",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "en",
                Name = "en-US",
                Title = "English"
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "fr",
                Title = "French",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "ko",
                Title = "Korean",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "hi",
                Title = "Hindi",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "es",
                Title = "Spanish",
                Enable = false
            });
            dbContext.Languages.InsertOne(new Language()
            {
                Code = "vi",
                Name = "vi-VN",
                Title = "Vietnamese"
            });
        }

        public void InitDepartments()
        {
            dbContext.Departments.DeleteMany(new BsonDocument());
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 1,
                Name = "IT"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 2,
                Name = "Hành chính nhân sự"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 3,
                Name = "Kế Toán"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 4,
                Name = "Kinh Doanh"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 5,
                Name = "Vật Tư"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 6,
                Name = "Nhà Máy"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 7,
                Name = "Kho"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 8,
                Name = "Dự Án"
            });
            dbContext.Departments.InsertOne(new Department()
            {
                Code = 9,
                Name = "Kế Hoạch"
            });

            // Fix alias
            foreach (var item in dbContext.Departments.Find(m => true).ToList())
            {
                var filter = Builders<Department>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<Department>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));
                dbContext.Departments.UpdateOne(filter, update);
            }
        }

        public void InitCategories()
        {
            dbContext.ProductCategorySales.DeleteMany(new BsonDocument());

            #region Product
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 1,
                ParentCode = 1,
                ParentName = "Sản phẩm",
                ParentAlias = "san-pham",
                Name = "Đất sạch",
                Alias = "dat-sach",
                Description = "Đất sạch dinh dưỡng tốt cho cây xanh, cây ăn trái, rau sạch,... Tribat",
                KeyWords = "đất sạch, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 2,
                ParentCode = 1,
                ParentName = "Sản phẩm",
                ParentAlias = "san-pham",
                Name = "Gạch không nung",
                Alias = "gach-khong-nung",
                Description = "Gạch không nung có khả năng cách âm, cách nhiệt, chống thấm khá tốt, điều này phù hợp với kết cấu của từng viên gạch cũng như cấp phối vữa bê tông",
                KeyWords = "gạch xây, gạch lát vỉa hè , tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 2,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 3,
                ParentCode = 1,
                ParentName = "Sản phẩm",
                ParentAlias = "san-pham",
                Name = "Phân bón",
                Alias = "phan-bon",
                Description = "Phân bón: thông tin đang cập nhật",
                KeyWords = "phân bón, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 3,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 4,
                ParentCode = 1,
                ParentName = "Sản phẩm",
                ParentAlias = "san-pham",
                Name = "Sản phẩm khác",
                Alias = "san-pham-khac",
                Description = "Sản phẩm khác: thông tin đang cập nhật",
                KeyWords = "Sản phẩm khác, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 4,
                Language = Constants.Languages.Vietnamese
            });

            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 1,
                ParentCode = 1,
                ParentName = "Product",
                ParentAlias = "product",
                Name = "Clean soil",
                Alias = "clean-soil",
                Description = "Clean soil is good for plants, fruit trees, clean vegetables,... Tribat",
                KeyWords = "clean soil, nutrient soil, trees, fruit trees, clean vegetables, tribat, green saigon",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 2,
                ParentCode = 1,
                ParentName = "Product",
                ParentAlias = "product",
                Name = "Adobe bricks",
                Alias = "adobe-bricks",
                Description = "Adobe bricks: updating information,... Tribat",
                KeyWords = "Adobe bricks, tribat, green saigon",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 2,
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 3,
                ParentCode = 1,
                ParentName = "Product",
                ParentAlias = "product",
                Name = "Fertilizer",
                Alias = "fertilizer",
                Description = "Fertilizer: updating information,... Tribat",
                KeyWords = "Fertilizer, tribat, green saigon",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 3,
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 4,
                ParentCode = 1,
                ParentName = "Product",
                ParentAlias = "product",
                Name = "Others",
                Alias = "others",
                Description = "Others: updating information,... Tribat",
                KeyWords = "Others, tribat, green saigon",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 4,
                Language = Constants.Languages.English
            });
            #endregion

            #region Xu ly - tai che
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 5,
                ParentCode = 2,
                ParentName = "Xử lý - Tái chế",
                ParentAlias = "xu-ly-tai-che",
                Name = "Bùn",
                Alias = "bun",
                Description = "Bùn: thông tin đang cập nhật",
                KeyWords = "bùn, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 6,
                ParentCode = 2,
                ParentName = "Xử lý - Tái chế",
                ParentAlias = "xu-ly-tai-che",
                Name = "Lục bình",
                Alias = "luc-binh",
                Description = "Lục bình: thông tin đang cập nhật",
                KeyWords = "lục bình, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 2,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 7,
                ParentCode = 2,
                ParentName = "Xử lý - Tái chế",
                ParentAlias = "xu-ly-tai-che",
                Name = "Tái chế nhựa",
                Alias = "tai-che-nhua",
                Description = "Bùn: thông tin đang cập nhật",
                KeyWords = "bùn, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 3,
                Language = Constants.Languages.Vietnamese
            });

            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 5,
                ParentCode = 2,
                ParentName = "Processing - Recycling",
                ParentAlias = "processing-recycling",
                Name = "Mud",
                Alias = "mud",
                Description = "Mud: updating information",
                KeyWords = "mud, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 6,
                ParentCode = 2,
                ParentName = "Processing - Recycling",
                ParentAlias = "processing-recycling",
                Name = "Hyacinth",
                Alias = "hyacinth",
                Description = "Hyacinth: updating information",
                KeyWords = "hyacinth, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 2,
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 7,
                ParentCode = 2,
                ParentName = "Processing - Recycling",
                ParentAlias = "processing-recycling",
                Name = "Plastic recycling",
                Alias = "plastic-recycling",
                Description = "Plastic recycling: updating information",
                KeyWords = "plastic recycling, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Order = 3,
                Language = Constants.Languages.English
            });
            #endregion

            #region Dich vu
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 8,
                ParentCode = 3,
                ParentName = "Dịch vụ",
                ParentAlias = "dich-vu",
                Name = "Nạo vét",
                Alias = "nao-vet",
                Order = 1,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 9,
                ParentCode = 3,
                ParentName = "Dịch vụ",
                ParentAlias = "dich-vu",
                Name = "Xử lý chất thải không nguy hại",
                Alias = "xu-ly-chat-thai-khong-nguy-hai",
                Order = 2,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 10,
                ParentCode = 3,
                ParentName = "Dịch vụ",
                ParentAlias = "dich-vu",
                Name = "Vận chuyển chất thải",
                Alias = "van-chuyen-chat-thai",
                Order = 3,
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 8,
                ParentCode = 3,
                ParentName = "Service",
                ParentAlias = "service",
                Name = "Dredging",
                Alias = "dredging",
                Order = 1,
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 9,
                ParentCode = 3,
                ParentName = "Service",
                ParentAlias = "service",
                Name = "Non-hazardous waste disposal",
                Alias = "Non-hazardous-waste-disposal",
                Order = 2,
                Language = Constants.Languages.English
            });
            dbContext.ProductCategorySales.InsertOne(new ProductCategorySale()
            {
                Code = 10,
                ParentCode = 3,
                ParentName = "Service",
                ParentAlias = "service",
                Name = "Transportation of waste",
                Alias = "transportation-of-waste",
                Order = 3,
                Language = Constants.Languages.English
            });
            #endregion
        }

        public void InitProducts()
        {
            dbContext.ProductSales.DeleteMany(new BsonDocument());

            #region Product VI
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Đất Việt 50, 20 dm3",
                Alias = "dat-viet-50-20dm3",
                Price = 0,
                Description = "a/ Đặc tính sản phẩm",
                Content = "a/ Đặc tính sản phẩm",
                KeyWords = "đất sạch, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Đất trồng thuốc lá",
                Alias = "dat-trong-thuoc-la",
                Price = 0,
                Description = "Đất trồng thuốc lá",
                Content = "Đất trồng thuốc lá",
                KeyWords = "đất trồng thuốc lá, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "Đất trồng cây: 40dm3",
                Alias = "dat-trong-cay-40dm3",
                Price = 0,
                Description = "Đất trồng cây: 40dm3",
                Content = "Đất trồng cây: 40dm3",
                KeyWords = "đất trồng cây: 40dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/3/",
                        FileName = "1449045882dattrongcay.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 4,
                CategoryCode = 1,
                Name = "Đất trồng mai: 20dm3",
                Alias = "dat-trong-mai-20dm3",
                Price = 0,
                Description = "Đất trồng mai: 20dm3",
                Content = "Đất trồng mai: 20dm3",
                KeyWords = "đất trồng mai: 20dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/4/",
                        FileName = "1449045939dattrongmai.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 5,
                CategoryCode = 1,
                Name = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                Alias = "dat-trong-rau-20dm3-10dm3-5dm3",
                Price = 0,
                Description = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                Content = "Đất trồng rau: 20dm3; 10dm3; 5dm3",
                KeyWords = "đất trồng rau: 20dm3; 10dm3; 5dm3, đất dinh dưỡng, cây xanh, cây ăn trái, rau sạch, tribat, saigon xanh",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/5/",
                        FileName = "1449045982dattrongrau.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region Product EN
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Vietnamese land 50, 20 dm3",
                Alias = "vietnamese-land-50-20dm3",
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Cultivation of tobacco",
                Alias = "cultivation-of-tobacco",
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "Woodland: 40dm3",
                Alias = "woodland-40dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/3/",
                        FileName = "1449045882dattrongcay.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 4,
                CategoryCode = 1,
                Name = "Land for planting apricots: 20dm3",
                Alias = "land-for-planting-apricots-20dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/4/",
                        FileName = "1449045939dattrongmai.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            dbContext.ProductSales.InsertOne(new ProductSale()
            {
                Code = 5,
                CategoryCode = 1,
                Name = "Land for growing vegetables: 20dm3; 10dm3; 5dm3",
                Alias = "land-for-growing-vegetables-20dm3-10dm3-5dm3",
                Images = new List<Image>()
                {
                    new Image{
                        Path = "images/p/5/",
                        FileName = "1449045982dattrongrau.jpg",
                        Order = 1,
                        Main = true
                    }
                },
                Language = Constants.Languages.English
            });
            #endregion
        }

        public void InitSeos()
        {
            dbContext.SEOs.DeleteMany(new BsonDocument());
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "home",
                Title = "Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Đất sạch, xử lý - tái chế bùn thải",
                OgTitle = "Công ty TNHH CNSH SÀI GÒN XANH",
                OgDescription = "Đất sạch, xử lý - tái chế bùn thải",
                Name = "Trang chủ",
                Alias = "trang-chu",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "home",
                Title = "GREEN SAIGON BIOTECH CO., LTD",
                Description = "GREEN SAIGON BIOTECH CO., LTD",
                Name = "Home",
                Alias = "home",
                Language = Constants.Languages.English
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "about",
                Title = "Thông tin về Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Thông tin về Công ty TNHH CNSH SÀI GÒN XANH",
                Name = "Giới thiệu",
                Alias = "gioi-thieu",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "about",
                Title = "About GREEN SAIGON BIOTECH CO., LTD",
                Description = "About GREEN SAIGON BIOTECH CO., LTD",
                Name = "About",
                Alias = "about",
                Language = Constants.Languages.English
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "news",
                Title = "Tin tức về Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Tin tức về Công ty TNHH CNSH SÀI GÒN XANH",
                Name = "Tin tức",
                Alias = "tin-tuc",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "news",
                Title = "News GREEN SAIGON BIOTECH CO., LTD",
                Description = "News GREEN SAIGON BIOTECH CO., LTD",
                Name = "News",
                Alias = "news",
                Language = Constants.Languages.English
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "news",
                Title = "Tin tức về Công ty TNHH CNSH SÀI GÒN XANH",
                Description = "Tin tức về Công ty TNHH CNSH SÀI GÒN XANH",
                Name = "Tin tức",
                Alias = "tin-tuc",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.SEOs.InsertOne(new SEO()
            {
                Code = "news",
                Title = "News GREEN SAIGON BIOTECH CO., LTD",
                Description = "News GREEN SAIGON BIOTECH CO., LTD",
                Name = "News",
                Alias = "news",
                Language = Constants.Languages.English
            });
        }

        public void InitContents()
        {
            dbContext.Contents.DeleteMany(new BsonDocument());
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "about",
                Title = "Giới thiệu",
                Alias = "gioi-thieu",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "about",
                Title = "About",
                Alias = "about",
                Language = Constants.Languages.English
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "job",
                Title = "Tuyển dụng",
                Alias = "tuyen-dung",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "job",
                Name = "Job",
                Alias = "job",
                Language = Constants.Languages.English
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "contact",
                Title = "Liên hệ",
                Alias = "lien-he",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "contact",
                Title = "Contact",
                Alias = "contact",
                Language = Constants.Languages.English
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "news",
                Title = "Tin tức",
                Alias = "tin-tuc",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "news",
                Title = "News",
                Alias = "news",
                Language = Constants.Languages.English
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "faq",
                Title = "Hỏi đáp",
                Alias = "hoi-dap",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "faq",
                Title = "FAQ",
                Alias = "faq",
                Language = Constants.Languages.English
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "customer",
                Title = "Khách hàng",
                Alias = "khach-hang",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Contents.InsertOne(new Content()
            {
                Code = "customer",
                Title = "Customer",
                Alias = "customer",
                Language = Constants.Languages.English
            });
        }

        public void InitTexts()
        {
            // System Id from 0 -> 500
            // Web client Id from > 500 -> 10000
            // ERP Id from > 10000
            //var filter = new BsonDocument();
            //var filter = Builders<User>.Filter.Eq(x => x.A, "1");
            //filter = filter & (Builders<User>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));

            var filter = Builders<Text>.Filter.Gt(x => x.Code, 500);
            filter = filter & (Builders<Text>.Filter.Lt(x => x.Code, 1000));
            dbContext.Texts.DeleteMany(filter);

            #region 501
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 501,
                Content = "Sản phẩm",
                ContentPlainText = "Sản phẩm",
                ToolTip = "Sản phẩm",
                Seo = "Sản phẩm",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 502
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 502,
                Content = "Đất sạch",
                ContentPlainText = "Đất sạch",
                ToolTip = "Đất sạch",
                Seo = "Đất sạch",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 503
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 503,
                Content = "Gạch không nung",
                ContentPlainText = "Gạch không nung",
                ToolTip = "Gạch không nung",
                Seo = "Gạch không nung",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 504
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 504,
                Content = "Phân bón",
                ContentPlainText = "Phân bón",
                ToolTip = "Phân bón",
                Seo = "Phân bón",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 505
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 505,
                Content = "Sản phẩm khác",
                ContentPlainText = "Sản phẩm khác",
                ToolTip = "Sản phẩm khác",
                Seo = "Sản phẩm khác",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 506
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 506,
                Content = "Xử lý - tái chế",
                ContentPlainText = "Xử lý - tái chế",
                ToolTip = "Xử lý - tái chế",
                Seo = "Xử lý - tái chế",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 507
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 507,
                Content = "Bùn",
                ContentPlainText = "Bùn",
                ToolTip = "Bùn",
                Seo = "Bùn",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 508
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 508,
                Content = "Lục bình",
                ContentPlainText = "Lục bình",
                ToolTip = "Lục bình",
                Seo = "Lục bình",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 509
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 509,
                Content = "Tái chế nhựa",
                ContentPlainText = "Tái chế nhựa",
                ToolTip = "Tái chế nhựa",
                Seo = "Tái chế nhựa",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 510
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 510,
                Content = "Dịch vụ",
                ContentPlainText = "Dịch vụ",
                ToolTip = "Dịch vụ",
                Seo = "Dịch vụ",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 511
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 511,
                Content = "Tin tức",
                ContentPlainText = "Tin tức",
                ToolTip = "Tin tức",
                Seo = "Tin tức",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 512
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 512,
                Content = "Liên hệ",
                ContentPlainText = "Liên hệ",
                ToolTip = "Liên hệ",
                Seo = "Liên hệ",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 513
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 513,
                Content = "Giới thiệu",
                ContentPlainText = "Giới thiệu",
                ToolTip = "Giới thiệu",
                Seo = "Giới thiệu",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 514
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 514,
                Content = "Giao diện",
                ContentPlainText = "Giao diện",
                ToolTip = "Giao diện",
                Seo = "Giao diện",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            #endregion

            #region 515
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 515,
                Content = "Mô tả ngắn...",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 515,
                Content = "Short description...",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 516
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 516,
                Content = "Mô tả ngắn...",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 516,
                Content = "Short description...",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 517
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 517,
                Content = "Mô tả ngắn...",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 517,
                Content = "Short description...",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 518
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 518,
                Content = "Mô tả ngắn...",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 518,
                Content = "Short description...",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 519
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 519,
                Content = "Mô tả ngắn...",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 519,
                Content = "Short description...",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 520
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 520,
                Content = "Nhà máy Xử lý bùn thải Sài Gòn Xanh.",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 520,
                Content = "Green Sai Gon Sludge Disposal Plant.",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 521
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 521,
                Content = "Công ty TNHH CNSH SÀI GÒN XANH",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 521,
                Content = "GREEN SAIGON BIOTECH CO., LTD.",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 522
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 522,
                Content = "127 Nguyễn Trọng Tuyển - P.15 - Q.Phú Nhuận - Tp HCM",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 522,
                Content = "127 Nguyen Trong Tuyen - Ward 15 - Phu Nhuan District - HCMC",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 523
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 523,
                Content = "Điện thoại:",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 523,
                Content = "Contact",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 524
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 524,
                Content = "Điều hướng",
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = 524,
                Content = "Direct",
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 525
            int code = 525;
            var vi = "Trang chủ";
            var en = "Home";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 526
            code = 526;
            vi = "Tin tức";
            en = "News";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 527
            code = 527;
            vi = "Công ty";
            en = "Company";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 528
            code = 528;
            vi = "Đối tác";
            en = "Partner";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 529
            code = 529;
            vi = "Nhà đầu tư";
            en = "Investors";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 530
            code = 530;
            vi = "Giới thiệu 1, 2, 3";
            en = "Introduce 1, 2, 3";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 531
            code = 531;
            vi = "Giới thiệu";
            en = "Introduce";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 532
            code = 532;
            vi = "Mô tả ngắn, mô tả ngắn, mô tả ngắn...";
            en = "Short description, short description, short description...";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 533
            code = 533;
            vi = "Chức năng";
            en = "Function";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 534
            code = 534;
            vi = "Tất cả";
            en = "All";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 535
            code = 535;
            vi = "Mô tả ngắn ...";
            en = "Short description...";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 536
            code = 536;
            vi = "Đất sạch -phân bón";
            en = "Clean soil - fertilizer";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 537
            code = 537;
            vi = "CÔNG NGHỆ XỬ LÝ BÙN";
            en = "Sludge treatment technology";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 538
            code = 538;
            vi = "Nạo vét";
            en = "Dredging";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 539
            code = 539;
            vi = "Xử lý chất thải không nguy hại";
            en = "Non-hazardous waste disposal";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 540
            code = 540;
            vi = "Vận chuyển chất thải";
            en = "Transportation of waste";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 541
            code = 541;
            vi = "Hệ thống đang cập nhật dữ liệu.Vui lòng quay lại sau.Cảm ơn...";
            en = "The system is updating data. Please come back later. Thanks ...";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 542
            code = 542;
            vi = "Không tìm thấy kết quả";
            en = "Not found data.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 543
            code = 543;
            vi = "Trở lại";
            en = "Back";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 544
            code = 544;
            vi = "Chia sẻ";
            en = "Share";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 545
            code = 545;
            vi = "Hoạt động";
            en = "Productivity";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 546
            code = 546;
            vi = "Công nghệ xử lý bùn";
            en = "Sludge treatment technology";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 547
            code = 547;
            vi = "Giới thiệu ngắn công nghệ";
            en = "Short description technology";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 548
            code = 548;
            vi = "Giải pháp tách nước nhằm chứa đựng các chất bùn thải và tách nước giữ lại chất rắn bên trong với giá thành thấp.";
            en = "Water separation solution to contain sludge and water retaining solids at low cost.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 549
            code = 549;
            vi = "Bài viết thiết kế tuyệt vời.";
            en = "Great design articles, daily.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 550
            code = 550;
            vi = "Đăng ký và nhận bản tin hàng tuần";
            en = "Subscribe and get our weekly newsletter in your inbox";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 551
            code = 551;
            vi = "Chúng tôi sẽ không bao giờ chia sẻ địa chỉ email của bạn";
            en = "We will never share your email address";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 552
            code = 552;
            vi = "Theo dõi ngay";
            en = "Subscribe now";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 553
            code = 553;
            vi = "Làm việc với chúng tôi";
            en = "Work with us";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 554
            code = 554;
            vi = "Nghề nghiệp tại Tribat";
            en = "Careers at Tribat";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 555
            code = 555;
            vi = "Công Ty TNHH Công Nghệ Sinh Học Sài Gòn Xanh được thành lập từ những năm đầu của thế kỷ 21, kết hợp từ ý tưởng, khả năng sáng tạo của những kỹ sư từ trên ghế giảng đường đại học...";
            en = "Green Saigon Biotechnology Co., Ltd was established in the beginning of the 21st century, combining with the idea, creative creation of the skills from the chairs of the learning...";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 556
            code = 556;
            vi = "Bạn không thấy công việc của mình?";
            en = "Didn't see your job?";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 557
            code = 557;
            vi = "Chúng tôi luôn tìm kiếm các tài năng để tham gia nhóm của chúng tôi";
            en = "We're always on the hunt for talented to join our team";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            //Benefits & Incentives
            #region 558
            code = 558;
            vi = "Lợi ích & Ưu đãi";
            en = "Benefits & Incentives";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 559
            code = 559;
            vi = "Môi trường làm việc";
            en = "Inclusive Environment";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 560
            code = 560;
            vi = "Trang thiết bị làm việc đầy đủ, nhân viên được phát huy năng lực và được hưởng xứng đáng với kết quả họ làm ra.";
            en = "A self-contained unit of a discourse in writing dealing with a particular point or idea. A paragraph consists of one or more sentences.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 561
            code = 561;
            vi = "Đọc thêm qui định công ty";
            en = "Read our diversity policy";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 562
            code = 562;
            vi = "Cơ hội từ xa";
            en = "Remote Opportunities";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 563
            code = 563;
            vi = "Một đoạn văn, từ đoạn văn Hy Lạp là một đơn vị khép kín của một bài diễn văn bằng văn bản đối phó với một điểm hoặc ý tưởng cụ thể.";
            en = "A paragraph, from the Greek paragraphos is a self-contained unit of a discourse in writing dealing with a particular point or idea.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 564
            code = 564;
            vi = "Lương cạnh tranh";
            en = "Competetive Salary";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 565
            code = 565;
            vi = "Một đoạn văn, từ đoạn văn Hy Lạp là một đơn vị khép kín của một bài diễn văn bằng văn bản đối phó với một điểm hoặc ý tưởng cụ thể.";
            en = "A paragraph, from the Greek paragraphos is a self-contained unit of a discourse in writing dealing with a particular point or idea.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 566
            code = 566;
            vi = "Lợi ích chăm sóc sức khỏe";
            en = "Healthcare Benefits";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 567
            code = 567;
            vi = "Một đơn vị khép kín của một bài diễn văn bằng văn bản đối phó với một điểm hoặc ý tưởng cụ thể. Một đoạn bao gồm một hoặc nhiều câu.";
            en = "A self-contained unit of a discourse in writing dealing with a particular point or idea. A paragraph consists of one or more sentences.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 568
            code = 568;
            vi = "Đọc chính sách chăm sóc sức khỏe";
            en = "Read our healthcare policy";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 569
            code = 569;
            vi = "Nộp đơn ngay";
            en = "Apply now";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 570
            code = 570;
            vi = "Bạn thấy công việc thích hợp?";
            en = "Sound like the job for you?";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 571
            code = 571;
            vi = "Tribat là một Cơ hội Sử dụng Cơ hội Bình đẳng và Cấm Phân biệt đối xử và Quấy rối Bất kỳ Loại nào: Wingman cam kết nguyên tắc cơ hội việc làm bình đẳng cho tất cả nhân viên và cung cấp cho nhân viên một môi trường làm việc không bị phân biệt đối xử và quấy rối. Tất cả các quyết định về việc làm tại Wingman đều dựa trên nhu cầu kinh doanh, yêu cầu công việc và trình độ cá nhân, không tính đến chủng tộc, màu da, tôn giáo hay tín ngưỡng, nguồn gốc quốc gia, xã hội hoặc dân tộc, giới tính (kể cả mang thai), tuổi tác, khuyết tật về thể chất, tinh thần hoặc cảm giác. Wingman sẽ không chịu đựng sự phân biệt đối xử hoặc quấy rối dựa trên bất kỳ đặc điểm nào trong số này. Wingman khuyến khích người nộp đơn ở mọi lứa tuổi.";
            en = "Tribat is an Equal Opportunity Employer and Prohibits Discrimination and Harassment of Any Kind: Wingman is committed to the principle of equal employment opportunity for all employees and to providing employees with a work environment free of discrimination and harassment. All employment decisions at Wingman are based on business needs, job requirements and individual qualifications, without regard to race, color, religion or belief, national, social or ethnic origin, sex (including pregnancy), age, physical, mental or sensory disability. Wingman will not tolerate discrimination or harassment based on any of these characteristics. Wingman encourages applicants of all ages.";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 572
            code = 572;
            vi = "Bộ phận";
            en = "Department";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 573
            code = 573;
            vi = "Nơi làm việc";
            en = "Location";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 574
            code = 574;
            vi = "Thời gian";
            en = "Basis";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 575
            code = 575;
            vi = "Danh mục";
            en = "Categories";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 576
            code = 576;
            en = "Related Products";
            vi = "Sản phẩm liên quan";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            #region 577
            code = 577;
            en = "You might also enjoy";
            vi = "Tin liên quan";
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = vi,
                Type = "home",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Texts.InsertOne(new Text()
            {
                Code = code,
                Content = en,
                Type = "home",
                Language = Constants.Languages.English
            });
            #endregion

            //#region int
            //code = int;
            //vi = "";
            //en = "";
            //dbContext.Texts.InsertOne(new Text()
            //{
            //    Code = code,
            //    Content = vi,
            //    Type = "home",
            //    Language = Constants.Languages.Vietnamese
            //});
            //dbContext.Texts.InsertOne(new Text()
            //{
            //    Code = code,
            //    Content = en,
            //    Type = "home",
            //    Language = Constants.Languages.English
            //});
            //#endregion

        }

        public void InitJobcategory()
        {
            dbContext.JobCategories.DeleteMany(new BsonDocument());

            #region JobCategories

            dbContext.JobCategories.InsertOne(new JobCategory()
            {
                Code = 0,
                Name = "Tuyển dụng",
                Alias = "tuyen-dung",
                SeoTitle = "Tìm việc làm tại Công Ty TNHH Công Nghệ Sinh Học Sài Gòn Xanh",
                Description = "Tìm việc làm tại Công Ty TNHH Công Nghệ Sinh Học Sài Gòn Xanh tại Hồ Chí Minh",
                KeyWords = "Tribat, Sài gòn xanh, Tin tức, Báo, Việt Nam, Hà Nội, Hồ Chí Minh, Đà Nẵng, Tin nội bộ, Đời sống, Phóng sự, Pháp luật, Thế giới, Khám phá, Thị trường, Chứng khoán, Kinh tế, Bất động sản, Giáo dục, Tuyển sinh, Teen, Thể thao, Ngoại hạng, Champion, La liga, Công nghệ, điện thoại, Oto, Xe Máy, Giải trí, Showbiz, Sao Việt, Âm nhạc, VPOP, KPOP, Phim ảnh, Điện ảnh, Đẹp, Thời trang, Làm đẹp, Người Đẹp, Tình yêu, Du lịch, Ẩm thực, Sách, Cười",
                OgTitle = "Tìm việc làm tại Công Ty TNHH Công Nghệ Sinh Học Sài Gòn Xanh tại Hồ Chí Minh",
                OgDescription = "Cập nhật việc làm tạ Tribat, Đời sống - Xã hội, Kinh tế, Thế giới, Thể thao, Giải trí, Công nghệ và nhiều lĩnh vực khác…",
                Language = Constants.Languages.Vietnamese
            });

            dbContext.JobCategories.InsertOne(new JobCategory()
            {
                Code = 1,
                Name = "Recruitment",
                Alias = "recruitment",
                SeoTitle = "Jobs, recruitment, apply now",
                Description = "Get the latest and hottest news on Tribal, Life - Society, Economy, World, Sports, Entertainment, Technology and more ...",
                KeyWords = "Tribat, Green Sai Gon, News, Newspaper, Vietnam, Hanoi, Ho Chi Minh, Da Nang, Internal News, Life, Report, Law, World, Discover, Market, Securities Real Estate, Education, Admission, Teen, Sports, Premier, Champion, La liga, Technology, Phone, Oto, Motorcycle, Entertainment, Showbiz, Sao Viet, Music, VPOP, KPOP , Movies, Movies, Beautiful, Fashion, Beauty, Beautiful, Love, Travel, Food, Books, Laugh",
                OgTitle = "Tribat.vn - News 24h, images awesome",
                OgDescription = "Get the latest and hottest news on Tribal, Life - Society, Economy, World, Sports, Entertainment, Technology and more ...",
                Language = Constants.Languages.English
            });

            #endregion
        }

        public void InitJobs()
        {
            dbContext.Jobs.DeleteMany(new BsonDocument());

            #region Jobs
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 1,
                Name = "Nhân Viên Kinh Doanh (Xử Lý Bùn Thải)",
                Alias = "nhan-vien-kinh-doanh-xu-ly-bun-thai",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 2,
                Name = "Nhân Viên Thiết Kế XDCB",
                Alias = "nhan-vien-thiet-ke-xdcb",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 3,
                Name = "Nhân Viên Kiểm Soát Chất Lượng Sản Phẩm",
                Alias = "nhan-vien-kiem-soat-chat-luong-san-pham",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 4,
                Name = "Nhân Viên Thống Kê Xử Lý Bùn",
                Alias = "nhan-vien-thong-ke-xu-ly-bun",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 5,
                Name = "Chuyên Viên Kiểm Toán Nội Bộ",
                Alias = "chuyen-vien-kiem-toan-noi-bo",
                Language = Constants.Languages.Vietnamese
            });

            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 1,
                Name = "Sales Staff (Waste Mud Treatment)",
                Alias = "sales-staff-waste-mud-treatment",
                Language = Constants.Languages.English
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 2,
                Name = "Construction Design Staff",
                Alias = "construction-design-staff",
                Language = Constants.Languages.English
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 3,
                Name = "Quality Control Supervisor",
                Alias = "quantity-control-supervisor",
                Language = Constants.Languages.English
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 4,
                Name = "Mud Processor Statistics Officer",
                Alias = "mud-processor-statistics-officer",
                Language = Constants.Languages.English
            });
            dbContext.Jobs.InsertOne(new Job()
            {
                Code = 5,
                Name = "Internal Auditor",
                Alias = "internal-auditor",
                Language = Constants.Languages.English
            });
            #endregion
        }

        public void InitNewsCategory()
        {
            dbContext.NewsCategories.DeleteMany(new BsonDocument());

            #region NewsCategories

            dbContext.NewsCategories.InsertOne(new NewsCategory()
            {
                Code = 1,
                Name = "Tin tức",
                Alias = "tin-tuc",
                SeoTitle = "Tribat.vn - Tin tức 24h, hình ảnh ấn tượng",
                Description = "Cập nhật tin tức mới và nóng nhất về Tribat, Đời sống - Xã hội, Kinh tế, Thế giới, Thể thao, Giải trí, Công nghệ và nhiều lĩnh vực khác…",
                KeyWords = "Tribat, Sài gòn xanh, Tin tức, Báo, Việt Nam, Hà Nội, Hồ Chí Minh, Đà Nẵng, Tin nội bộ, Đời sống, Phóng sự, Pháp luật, Thế giới, Khám phá, Thị trường, Chứng khoán, Kinh tế, Bất động sản, Giáo dục, Tuyển sinh, Teen, Thể thao, Ngoại hạng, Champion, La liga, Công nghệ, điện thoại, Oto, Xe Máy, Giải trí, Showbiz, Sao Việt, Âm nhạc, VPOP, KPOP, Phim ảnh, Điện ảnh, Đẹp, Thời trang, Làm đẹp, Người Đẹp, Tình yêu, Du lịch, Ẩm thực, Sách, Cười",
                OgTitle = "Tribat.vn - Tin tức 24h, hình ảnh ấn tượng",
                OgDescription = "Cập nhật tin tức mới và nóng nhất về Tribat, Đời sống - Xã hội, Kinh tế, Thế giới, Thể thao, Giải trí, Công nghệ và nhiều lĩnh vực khác…",
                Language = Constants.Languages.Vietnamese
            });

            dbContext.NewsCategories.InsertOne(new NewsCategory()
            {
                Code = 1,
                Name = "News",
                Alias = "news",
                SeoTitle = "Tribat.vn - News 24h, images awesome",
                Description = "Get the latest and hottest news on Tribal, Life - Society, Economy, World, Sports, Entertainment, Technology and more ...",
                KeyWords = "Tribat, Green Sai Gon, News, Newspaper, Vietnam, Hanoi, Ho Chi Minh, Da Nang, Internal News, Life, Report, Law, World, Discover, Market, Securities Real Estate, Education, Admission, Teen, Sports, Premier, Champion, La liga, Technology, Phone, Oto, Motorcycle, Entertainment, Showbiz, Sao Viet, Music, VPOP, KPOP , Movies, Movies, Beautiful, Fashion, Beauty, Beautiful, Love, Travel, Food, Books, Laugh",
                OgTitle = "Tribat.vn - News 24h, images awesome",
                OgDescription = "Get the latest and hottest news on Tribal, Life - Society, Economy, World, Sports, Entertainment, Technology and more ...",
                Language = Constants.Languages.English
            });

            #endregion
        }

        public void InitNews()
        {
            dbContext.News.DeleteMany(new BsonDocument());

            #region News
            dbContext.News.InsertOne(new News()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Xử lý bùn thải tại TP.HCM: Đề xuất giá thấp, duyệt giá cao",
                KeyWords = "san lấp mặt bằng, xử lý nước thải, dự án chống ngập,Tribat, Sài gòn xanh, xử lý rác",
                OgTitle = "Xử lý bùn thải tại TP.HCM: Đề xuất giá thấp, duyệt giá cao",
                Source = "TTO",
                SourceLink = "https://tuoitre.vn/xu-ly-bun-thai-tai-tphcm-de-xuat-gia-thap-duyet-gia-cao-20170929081959974.htm",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.News.InsertOne(new News()
            {
                Code = 1,
                CategoryCode = 1,
                Name = "Sludge treatment in Ho Chi Minh City: low price proposal, high price",
                KeyWords = "leveling, sewage treatment, flood protection projects, Tribat, Sai Gon green, waste treatment",
                OgTitle = "Sludge treatment in Ho Chi Minh City: low price proposal, high price",
                Source = "Tribat-Translate",
                SourceLink = "",
                Language = Constants.Languages.English
            });

            dbContext.News.InsertOne(new News()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Chậm xử lý bùn thải, công ty thoát nước bị phê bình",
                KeyWords = "phê bình, khu xử lý bùn Đa Phước, dự án xử lý bùn thải,Công ty TNHH CNSH SÀI GÒN XANH",
                OgTitle = "Chậm xử lý bùn thải, công ty thoát nước bị phê bình",
                Source = "TTO",
                SourceLink = "https://tuoitre.vn/cham-xu-ly-bun-thai-cong-ty-thoat-nuoc-bi-phe-binh-578248.htm",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.News.InsertOne(new News()
            {
                Code = 2,
                CategoryCode = 1,
                Name = "Slow disposal of sludge, wastewater company is criticized",
                KeyWords = "Da Phuoc Sludge Treatment Plant, Sludge Treatment Plant, Green Sai Gon Biotech Company Limited",
                OgTitle = "Slow disposal of sludge, wastewater company is criticized",
                Source = "Tribat-Translate",
                SourceLink = "",
                Language = Constants.Languages.English
            });

            dbContext.News.InsertOne(new News()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "TP.HCM: Gần 3000 tấn bùn thải mỗi ngày chưa được xử lý",
                KeyWords = "",
                OgTitle = "TP.HCM: Gần 3000 tấn bùn thải mỗi ngày chưa được xử lý",
                Source = "TTO",
                SourceLink = "https://tuoitre.vn/tphcm-gan-3000-tan-bun-thai-moi-ngay-chua-duoc-xu-ly-199217.htm",
                Language = Constants.Languages.Vietnamese
            });
            dbContext.News.InsertOne(new News()
            {
                Code = 3,
                CategoryCode = 1,
                Name = "HCMC: Nearly 3,000 tons of sludge are not treated each day",
                KeyWords = "",
                OgTitle = "HCMC: Nearly 3,000 tons of sludge are not treated each day",
                Source = "Tribat-Translate",
                SourceLink = "",
                Language = Constants.Languages.English
            });
            #endregion

            // Fix alias
            foreach (var item in dbContext.News.Find(m => true).ToList())
            {
                var filter = Builders<News>.Filter.Eq(m => m.Id, item.Id);
                var update = Builders<News>.Update
                    .Set(m => m.Alias, Utility.AliasConvert(item.Name));
                dbContext.News.UpdateOne(filter, update);
            }
        }
        #endregion

        #region Cookies
        // Use
        ////read cookie from IHttpContextAccessor  
        //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];

        ////read cookie from Request object  
        //string cookieValueFromReq = Request.Cookies["Key"];

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