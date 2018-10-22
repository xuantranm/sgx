using Models;
using MongoDB.Driver;
using System;

namespace Data
{
    public class MongoDBContext
    {
        public static string ConnectionString { get; set; }
        public static string DatabaseName { get; set; }
        public static bool IsSSL { get; set; }

        private IMongoDatabase _database { get; }

        public MongoDBContext()
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(ConnectionString));
                if (IsSSL)
                {
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 };
                }
                var mongoClient = new MongoClient(settings);
                _database = mongoClient.GetDatabase(DatabaseName);
            }
            catch (Exception ex)
            {
                throw new Exception("Can not access to db server.", ex);
            }
        }

        #region Location
        public IMongoCollection<Country> Countries
        {
            get
            {
                return _database.GetCollection<Country>("Countries");
            }
        }

        public IMongoCollection<City> Cities
        {
            get
            {
                return _database.GetCollection<City>("Cities");
            }
        }

        public IMongoCollection<District> Districts
        {
            get
            {
                return _database.GetCollection<District>("Districts");
            }
        }

        public IMongoCollection<Ward> Wards
        {
            get
            {
                return _database.GetCollection<Ward>("Wards");
            }
        }
        #endregion

        #region TimeKeeper
        public IMongoCollection<AttLog> X628CNMAttLogs
        {
            get
            {
                return _database.GetCollection<AttLog>("x628CNMAttLogs");
            }
        }

        public IMongoCollection<AttLog> X628CVPAttLogs
        {
            get
            {
                return _database.GetCollection<AttLog>("x628CVPAttLogs");
            }
        }

        public IMongoCollection<AttLog> X928CNMAttLogs
        {
            get
            {
                return _database.GetCollection<AttLog>("x928CNMAttLogs");
            }
        }

        public IMongoCollection<UserInfo> X628CNMUserInfos
        {
            get
            {
                return _database.GetCollection<UserInfo>("X628CNMUserInfos");
            }
        }

        public IMongoCollection<UserInfo> X628CVPUserInfos
        {
            get
            {
                return _database.GetCollection<UserInfo>("x628CVPUserInfos");
            }
        }

        public IMongoCollection<UserInfo> X928CNMUserInfos
        {
            get
            {
                return _database.GetCollection<UserInfo>("x928CNMUserInfos");
            }
        }

        public IMongoCollection<EmployeeWorkTimeLog> EmployeeWorkTimeLogs
        {
            get
            {
                return _database.GetCollection<EmployeeWorkTimeLog>("EmployeeWorkTimeLogs");
            }
        }

        public IMongoCollection<EmployeeWorkTimeMonthLog> EmployeeWorkTimeMonthLogs
        {
            get
            {
                return _database.GetCollection<EmployeeWorkTimeMonthLog>("EmployeeWorkTimeMonthLogs");
            }
        }
        #endregion

        #region HRs

        #region SALARIES

        /// <summary>
        /// Bang cai dat: mau so,...
        /// </summary>
        public IMongoCollection<SalarySetting> SalarySettings
        {
            get
            {
                return _database.GetCollection<SalarySetting>("SalarySettings");
            }
        }

        public IMongoCollection<SalarySaleKPI> SalarySaleKPIs
        {
            get
            {
                return _database.GetCollection<SalarySaleKPI>("SalarySaleKPIs");
            }
        }

        public IMongoCollection<SalaryLogisticData> SalaryLogisticDatas
        {
            get
            {
                return _database.GetCollection<SalaryLogisticData>("SalaryLogisticDatas");
            }
        }

        public IMongoCollection<SalaryCredit> SalaryCredits
        {
            get
            {
                return _database.GetCollection<SalaryCredit>("SalaryCredits");
            }
        }

        public IMongoCollection<SalaryMucLuongVung> SalaryMucLuongVungs
        {
            get
            {
                return _database.GetCollection<SalaryMucLuongVung>("SalaryMucLuongVungs");
            }
        }

        public IMongoCollection<SalaryFeeLaw> SalaryFeeLaws
        {
            get
            {
                return _database.GetCollection<SalaryFeeLaw>("SalaryFeeLaws");
            }
        }

        public IMongoCollection<SalaryThangBangLuong> SalaryThangBangLuongs
        {
            get
            {
                return _database.GetCollection<SalaryThangBangLuong>("SalaryThangBangLuongs");
            }
        }

        // No use. Use direct to SalaryThangBangLuong base ViTriCode of employee vs heso
        // Thang Bac Luong của nhân viên
        public IMongoCollection<SalaryThangBacLuongEmployee> SalaryThangBacLuongEmployees
        {
            get
            {
                return _database.GetCollection<SalaryThangBacLuongEmployee>("SalaryThangBacLuongEmployees");
            }
        }

        // Lương từng tháng của nhân viên
        public IMongoCollection<SalaryEmployeeMonth> SalaryEmployeeMonths
        {
            get
            {
                return _database.GetCollection<SalaryEmployeeMonth>("SalaryEmployeeMonths");
            }
        }

        // Danh sách phụ cấp - phúc lợi
        public IMongoCollection<SalaryPhuCapPhucLoi> SalaryPhuCapPhucLois
        {
            get
            {
                return _database.GetCollection<SalaryPhuCapPhucLoi>("SalaryPhuCapPhucLois");
            }
        }

        // Danh sách phụ cấp - phúc lợi theo từng vị trí
        public IMongoCollection<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois
        {
            get
            {
                return _database.GetCollection<SalaryThangBangPhuCapPhucLoi>("SalaryThangBangPhuCapPhucLois");
            }
        }

        public IMongoCollection<ChucDanhCongViec> ChucDanhCongViecs
        {
            get
            {
                return _database.GetCollection<ChucDanhCongViec>("ChucDanhCongViecs");
            }
        }
        #endregion

        #region KINH DOANH
        public IMongoCollection<ChucVuSale> ChucVuSales
        {
            get
            {
                return _database.GetCollection<ChucVuSale>("ChucVuSales");
            }
        }

        public IMongoCollection<SaleKPI> SaleKPIs
        {
            get
            {
                return _database.GetCollection<SaleKPI>("SaleKPIs");
            }
        }
        #endregion

        #region LOGISTICS
        public IMongoCollection<CityGiaoNhan> CityGiaoNhans
        {
            get
            {
                return _database.GetCollection<CityGiaoNhan>("CityGiaoNhans");
            }
        }

        public IMongoCollection<DonGiaChuyenXe> DonGiaChuyenXes
        {
            get
            {
                return _database.GetCollection<DonGiaChuyenXe>("DonGiaChuyenXes");
            }
        }

        public IMongoCollection<HoTroCongTacXa> HoTroCongTacXas
        {
            get
            {
                return _database.GetCollection<HoTroCongTacXa>("HoTroCongTacXas");
            }
        }
        #endregion

        public IMongoCollection<TrainningType> TrainningTypes
        {
            get
            {
                return _database.GetCollection<TrainningType>("TrainningTypes");
            }
        }

        public IMongoCollection<Trainning> Trainnings
        {
            get
            {
                return _database.GetCollection<Trainning>("Trainnings");
            }
        }

        public IMongoCollection<Bank> Banks
        {
            get
            {
                return _database.GetCollection<Bank>("Banks");
            }
        }

        public IMongoCollection<WorkTimeType> WorkTimeTypes
        {
            get
            {
                return _database.GetCollection<WorkTimeType>("WorkTimeTypes");
            }
        }

        public IMongoCollection<SalaryContentType> SalaryContentTypes
        {
            get
            {
                return _database.GetCollection<SalaryContentType>("SalaryContentTypes");
            }
        }

        public IMongoCollection<SalaryContent> SalaryContents
        {
            get
            {
                return _database.GetCollection<SalaryContent>("SalaryContents");
            }
        }

        public IMongoCollection<SalaryMonthlyType> SalaryMonthlyTypes
        {
            get
            {
                return _database.GetCollection<SalaryMonthlyType>("SalaryMonthlyTypes");
            }
        }

        public IMongoCollection<Employee> Employees
        {
            get
            {
                return _database.GetCollection<Employee>("Employees");
            }
        }

        // Store all change employee
        // Get newest by [UpdatedOn]
        // Check chance base current Employee vs newest history.
        public IMongoCollection<Employee> EmployeeHistories
        {
            get
            {
                return _database.GetCollection<Employee>("EmployeeHistories");
            }
        }

        public IMongoCollection<Leave> Leaves
        {
            get
            {
                return _database.GetCollection<Leave>("Leaves");
            }
        }

        // Quản lý tất cả ngày nghỉ còn lại
        public IMongoCollection<LeaveEmployee> LeaveEmployees
        {
            get
            {
                return _database.GetCollection<LeaveEmployee>("LeaveEmployees");
            }
        }

        public IMongoCollection<LeaveType> LeaveTypes
        {
            get
            {
                return _database.GetCollection<LeaveType>("LeaveTypes");
            }
        }

        #endregion

        #region Factories
        // Store date by date from FactoryProducts: for tracking
        public IMongoCollection<FactoryProduct> FactoryProductHistories
        {
            get
            {
                return _database.GetCollection<FactoryProduct>("FactoryProductHistories");
            }
        }

        public IMongoCollection<FactoryProduct> FactoryProducts
        {
            get
            {
                return _database.GetCollection<FactoryProduct>("FactoryProducts");
            }
        }

        public IMongoCollection<FactoryShift> FactoryShifts
        {
            get
            {
                return _database.GetCollection<FactoryShift>("FactoryShifts");
            }
        }

        public IMongoCollection<FactoryStage> FactoryStages
        {
            get
            {
                return _database.GetCollection<FactoryStage>("FactoryStages");
            }
        }

        public IMongoCollection<FactoryTruckType> FactoryTruckTypes
        {
            get
            {
                return _database.GetCollection<FactoryTruckType>("FactoryTruckTypes");
            }
        }

        public IMongoCollection<FactoryWork> FactoryWorks
        {
            get
            {
                return _database.GetCollection<FactoryWork>("FactoryWorks");
            }
        }

        public IMongoCollection<FactoryMotorVehicle> FactoryMotorVehicles
        {
            get
            {
                return _database.GetCollection<FactoryMotorVehicle>("FactoryMotorVehicles");
            }
        }

        public IMongoCollection<FactoryTonSX> FactoryTonSXs
        {
            get
            {
                return _database.GetCollection<FactoryTonSX>("FactoryTonSXs");
            }
        }

        public IMongoCollection<FactoryVanHanh> FactoryVanHanhs
        {
            get
            {
                return _database.GetCollection<FactoryVanHanh>("FactoryVanHanhs");
            }
        }

        public IMongoCollection<FactoryReportTonSX> FactoryReportTonSXs
        {
            get
            {
                return _database.GetCollection<FactoryReportTonSX>("FactoryReportTonSXs");
            }
        }

        public IMongoCollection<FactoryReportXCG> FactoryReportXCGs
        {
            get
            {
                return _database.GetCollection<FactoryReportXCG>("FactoryReportXCGs");
            }
        }

        public IMongoCollection<FactoryReportDG> FactoryReportDGs
        {
            get
            {
                return _database.GetCollection<FactoryReportDG>("FactoryReportDGs");
            }
        }

        public IMongoCollection<FactoryReportBocHang> FactoryReportBocHangs
        {
            get
            {
                return _database.GetCollection<FactoryReportBocHang>("FactoryReportBocHangs");
            }
        }

        public IMongoCollection<FactoryReportVanHanhMay> FactoryReportVanHanhMays
        {
            get
            {
                return _database.GetCollection<FactoryReportVanHanhMay>("FactoryReportVanHanhMays");
            }
        }

        public IMongoCollection<FactoryDanhGiaXCG> FactoryDanhGiaXCGs
        {
            get
            {
                return _database.GetCollection<FactoryDanhGiaXCG>("FactoryDanhGiaXCGs");
            }
        }

        public IMongoCollection<FactoryDinhMuc> FactoryDinhMucs
        {
            get
            {
                return _database.GetCollection<FactoryDinhMuc>("FactoryDinhMucs");
            }
        }

        public IMongoCollection<FactoryChiPhiXCG> FactoryChiPhiXCGs
        {
            get
            {
                return _database.GetCollection<FactoryChiPhiXCG>("FactoryChiPhiXCGs");
            }
        }

        public IMongoCollection<FactoryNhaThau> FactoryNhaThaus
        {
            get
            {
                return _database.GetCollection<FactoryNhaThau>("FactoryNhaThaus");
            }
        }

        #endregion

        #region SYSTEM
        public IMongoCollection<ScheduleEmail> ScheduleEmails
        {
            get
            {
                return _database.GetCollection<ScheduleEmail>("ScheduleEmails");
            }
        }

        public IMongoCollection<SystemReport> SystemReports
        {
            get
            {
                return _database.GetCollection<SystemReport>("SystemReports");
            }
        }
        
        public IMongoCollection<Error> Errors
        {
            get
            {
                return _database.GetCollection<Error>("Errors");
            }
        }

        // Use tracking error data,...
        public IMongoCollection<Miss> Misss
        {
            get
            {
                return _database.GetCollection<Miss>("Misss");
            }
        }

        public IMongoCollection<LogSystem> LogSystems
        {
            get
            {
                return _database.GetCollection<LogSystem>("LogSystems");
            }
        }

        public IMongoCollection<Location> Locations
        {
            get
            {
                return _database.GetCollection<Location>("Locations");
            }
        }

        public IMongoCollection<ActivitySys> Activities
        {
            get
            {
                return _database.GetCollection<ActivitySys>("Activities");
            }
        }

        public IMongoCollection<Language> Languages
        {
            get
            {
                return _database.GetCollection<Language>("Languages");
            }
        }

        public IMongoCollection<SEO> SEOs
        {
            get
            {
                return _database.GetCollection<SEO>("SEOs");
            }
        }

        public IMongoCollection<Content> Contents
        {
            get
            {
                return _database.GetCollection<Content>("Contents");
            }
        }

        public IMongoCollection<ContractType> ContractTypes
        {
            get
            {
                return _database.GetCollection<ContractType>("ContractTypes");
            }
        }

        public IMongoCollection<Text> Texts
        {
            get
            {
                return _database.GetCollection<Text>("Texts");
            }
        }

        public IMongoCollection<Setting> Settings
        {
            get
            {
                return _database.GetCollection<Setting>("Settings");
            }
        }

        public IMongoCollection<Report> Reports
        {
            get
            {
                return _database.GetCollection<Report>("Reports");
            }
        }

        public IMongoCollection<Company> Companies
        {
            get
            {
                return _database.GetCollection<Company>("Companies");
            }
        }

        public IMongoCollection<Department> Departments
        {
            get
            {
                return _database.GetCollection<Department>("Departments");
            }
        }

        public IMongoCollection<RoleUser> RoleUsers
        {
            get
            {
                return _database.GetCollection<RoleUser>("RoleUsers");
            }
        }

        public IMongoCollection<Role> Roles
        {
            get
            {
                return _database.GetCollection<Role>("Roles");
            }
        }

        public IMongoCollection<Image> Images
        {
            get
            {
                return _database.GetCollection<Image>("Images");
            }
        }

        public IMongoCollection<TrackingUser> TrackingUsers
        {
            get
            {
                return _database.GetCollection<TrackingUser>("TrackingUsers");
            }
        }

        public IMongoCollection<TrackingQuantity> TrackingQuantitys
        {
            get
            {
                return _database.GetCollection<TrackingQuantity>("TrackingQuantitys");
            }
        }

        public IMongoCollection<Title> Titles
        {
            get
            {
                return _database.GetCollection<Title>("Titles");
            }
        }

        public IMongoCollection<Notification> Notifications
        {
            get
            {
                return _database.GetCollection<Notification>("Notifications");
            }
        }

        // System
        public IMongoCollection<NotificationAction> NotificationActions
        {
            get
            {
                return _database.GetCollection<NotificationAction>("NotificationActions");
            }
        }
        #endregion

        #region Laws
        public IMongoCollection<BHYTHospital> BHYTHospitals
        {
            get
            {
                return _database.GetCollection<BHYTHospital>("BHYTHospitals");
            }
        }
        #endregion

        #region ERP

        public IMongoCollection<ProductGroup> ProductGroups
        {
            get
            {
                return _database.GetCollection<ProductGroup>("ProductGroups");
            }
        }

        public IMongoCollection<Unit> Units
        {
            get
            {
                return _database.GetCollection<Unit>("Units");
            }
        }

        public IMongoCollection<Product> Products
        {
            get
            {
                return _database.GetCollection<Product>("Products");
            }
        }

        public IMongoCollection<ProductLog> ProductLogs
        {
            get
            {
                return _database.GetCollection<ProductLog>("ProductLogs");
            }
        }

        public IMongoCollection<Request> Requests
        {
            get
            {
                return _database.GetCollection<Request>("Requests");
            }
        }
 
        public IMongoCollection<OrderDetail> OrderDetails
        {
            get
            {
                return _database.GetCollection<OrderDetail>("OrderDetails");
            }
        }

        public IMongoCollection<Supplier> Suppliers
        {
            get
            {
                return _database.GetCollection<Supplier>("Supplier");
            }
        }

        public IMongoCollection<Receive> Receives
        {
            get
            {
                return _database.GetCollection<Receive>("Receives");
            }
        }

        public IMongoCollection<Release> Releases
        {
            get
            {
                return _database.GetCollection<Release>("Releases");
            }
        }

        public IMongoCollection<Brand> Brands
        {
            get
            {
                return _database.GetCollection<Brand>("Brands");
            }
        }

        public IMongoCollection<Part> Parts
        {
            get
            {
                return _database.GetCollection<Part>("Parts");
            }
        }

        public IMongoCollection<Order> Orders
        {
            get
            {
                return _database.GetCollection<Order>("Orders");
            }
        }

        public IMongoCollection<Customer> Customers
        {
            get
            {
                return _database.GetCollection<Customer>("Customers");
            }
        }

        public IMongoCollection<Truck> Trucks
        {
            get
            {
                return _database.GetCollection<Truck>("Trucks");
            }
        }

        public IMongoCollection<TruckCheck> TruckChecks
        {
            get
            {
                return _database.GetCollection<TruckCheck>("TruckChecks");
            }
        }
        #endregion

        public IMongoCollection<ProductCategorySale> ProductCategorySales
        {
            get
            {
                return _database.GetCollection<ProductCategorySale>("ProductCategorySales");
            }
        }

        public IMongoCollection<ProductSale> ProductSales
        {
            get
            {
                return _database.GetCollection<ProductSale>("ProductSales");
            }
        }

        public IMongoCollection<JobCategory> JobCategories
        {
            get
            {
                return _database.GetCollection<JobCategory>("JobCategories");
            }
        }

        public IMongoCollection<Job> Jobs
        {
            get
            {
                return _database.GetCollection<Job>("Jobs");
            }
        }

        public IMongoCollection<NewsCategory> NewsCategories
        {
            get
            {
                return _database.GetCollection<NewsCategory>("NewsCategories");
            }
        }

        public IMongoCollection<News> News
        {
            get
            {
                return _database.GetCollection<News>("News");
            }
        }
    }
}
