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

        #region KE HOACH TONG HOP
        public IMongoCollection<KhoNguyenLieu> KhoNguyenLieus
        {
            get
            {
                return _database.GetCollection<KhoNguyenLieu>("KhoNguyenLieus");
            }
        }

        public IMongoCollection<KhoThanhPham> KhoThanhPhams
        {
            get
            {
                return _database.GetCollection<KhoThanhPham>("KhoThanhPhams");
            }
        }

        public IMongoCollection<KhoBun> KhoBuns
        {
            get
            {
                return _database.GetCollection<KhoBun>("KhoBuns");
            }
        }

        public IMongoCollection<KhoXuLy> KhoXuLys
        {
            get
            {
                return _database.GetCollection<KhoXuLy>("KhoXuLys");
            }
        }

        public IMongoCollection<TiepNhanXuLy> TiepNhanXuLys
        {
            get
            {
                return _database.GetCollection<TiepNhanXuLy>("TiepNhanXuLys");
            }
        }

        public IMongoCollection<TrangThai> TrangThais
        {
            get
            {
                return _database.GetCollection<TrangThai>("TrangThais");
            }
        }

        public IMongoCollection<HoChua> HoChuas
        {
            get
            {
                return _database.GetCollection<HoChua>("HoChuas");
            }
        }
        #endregion

        #region Location
        public IMongoCollection<Ip> Ips
        {
            get
            {
                return _database.GetCollection<Ip>("Ips");
            }
        }


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

        public IMongoCollection<NgachLuong> NgachLuongs
        {
            get
            {
                return _database.GetCollection<NgachLuong>("NgachLuongs");
            }
        }

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

        public IMongoCollection<SaleKPIEmployee> SaleKPIEmployees
        {
            get
            {
                return _database.GetCollection<SaleKPIEmployee>("SaleKPIEmployees");
            }
        }

        public IMongoCollection<LogisticGiaChuyenXe> LogisticGiaChuyenXes
        {
            get
            {
                return _database.GetCollection<LogisticGiaChuyenXe>("LogisticGiaChuyenXes");
            }
        }

        public IMongoCollection<LogisticEmployeeCong> LogisticEmployeeCongs
        {
            get
            {
                return _database.GetCollection<LogisticEmployeeCong>("LogisticEmployeeCongs");
            }
        }

        public IMongoCollection<CreditEmployee> CreditEmployees
        {
            get
            {
                return _database.GetCollection<CreditEmployee>("CreditEmployees");
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
        public IMongoCollection<ChucVuKinhDoanh> ChucVuKinhDoanhs
        {
            get
            {
                return _database.GetCollection<ChucVuKinhDoanh>("ChucVuKinhDoanhs");
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
        public IMongoCollection<LogisticsLocation> LogisticsLocations
        {
            get
            {
                return _database.GetCollection<LogisticsLocation>("LogisticsLocations");
            }
        }

        public IMongoCollection<LogisticsLoaiXe> LogisticsLoaiXes
        {
            get
            {
                return _database.GetCollection<LogisticsLoaiXe>("LogisticsLoaiXes");
            }
        }

        public IMongoCollection<LogisticGiaBun> LogisticGiaBuns
        {
            get
            {
                return _database.GetCollection<LogisticGiaBun>("LogisticGiaBuns");
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

        public IMongoCollection<EmployeeCong> EmployeeCongs
        {
            get
            {
                return _database.GetCollection<EmployeeCong>("EmployeeCongs");
            }
        }

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

        public IMongoCollection<CongTyChiNhanh> CongTyChiNhanhs
        {
            get
            {
                return _database.GetCollection<CongTyChiNhanh>("CongTyChiNhanhs");
            }
        }

        public IMongoCollection<KhoiChucNang> KhoiChucNangs
        {
            get
            {
                return _database.GetCollection<KhoiChucNang>("KhoiChucNangs");
            }
        }

        public IMongoCollection<PhongBan> PhongBans
        {
            get
            {
                return _database.GetCollection<PhongBan>("PhongBans");
            }
        }

        public IMongoCollection<BoPhan> BoPhans
        {
            get
            {
                return _database.GetCollection<BoPhan>("BoPhans");
            }
        }

        public IMongoCollection<ChucVu> ChucVus
        {
            get
            {
                return _database.GetCollection<ChucVu>("ChucVus");
            }
        }

        public IMongoCollection<EmailGroup> EmailGroups
        {
            get
            {
                return _database.GetCollection<EmailGroup>("EmailGroups");
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

        public IMongoCollection<LeaveEmployeeHistory> LeaveEmployeeHistories
        {
            get
            {
                return _database.GetCollection<LeaveEmployeeHistory>("LeaveEmployeeHistories");
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

        #region FACTORIES
        // Store date by date from FactoryProducts: for tracking
        public IMongoCollection<FactoryProduct> FactoryProductHistories
        {
            get
            {
                return _database.GetCollection<FactoryProduct>("FactoryProductHistories");
            }
        }

        /// <summary>
        /// Thanh pham, ban thanh pham, nguyen lieu,...
        /// + Xem so luong hien tai
        /// </summary>
        public IMongoCollection<FactoryProduct> FactoryProducts
        {
            get
            {
                return _database.GetCollection<FactoryProduct>("FactoryProducts");
            }
        }

        /// <summary>
        /// Mang cong viec: 
        /// + Main: false - Cong viec khac
        /// + Main: true - Cong viec chinh
        /// </summary>
        public IMongoCollection<FactoryCongViec> FactoryCongViecs
        {
            get
            {
                return _database.GetCollection<FactoryCongViec>("FactoryCongViecs");
            }
        }

        public IMongoCollection<FactoryProductDinhMuc> FactoryProductDinhMucs
        {
            get
            {
                return _database.GetCollection<FactoryProductDinhMuc>("FactoryProductDinhMucs");
            }
        }

        public IMongoCollection<FactoryProductDinhMucTangCa> FactoryProductDinhMucTangCas
        {
            get
            {
                return _database.GetCollection<FactoryProductDinhMucTangCa>("FactoryProductDinhMucTangCas");
            }
        }

        public IMongoCollection<FactoryProductDinhMucTiLe> FactoryProductDinhMucTiLes
        {
            get
            {
                return _database.GetCollection<FactoryProductDinhMucTiLe>("FactoryProductDinhMucTiLes");
            }
        }

        public IMongoCollection<FactoryProductDonGiaM3> FactoryProductDonGiaM3s
        {
            get
            {
                return _database.GetCollection<FactoryProductDonGiaM3>("FactoryProductDonGiaM3s");
            }
        }

        public IMongoCollection<FactoryProductCongTheoThang> FactoryProductCongTheoThangs
        {
            get
            {
                return _database.GetCollection<FactoryProductCongTheoThang>("FactoryProductCongTheoThangs");
            }
        }

        public IMongoCollection<FactoryProductCongTheoNgay> FactoryProductCongTheoNgays
        {
            get
            {
                return _database.GetCollection<FactoryProductCongTheoNgay>("FactoryProductCongTheoNgays");
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

        // Use temp. Remove later
        public IMongoCollection<ProductV100> ProductV100s
        {
            get
            {
                return _database.GetCollection<ProductV100>("ProductV100s");
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

        public IMongoCollection<Holiday> Holidays
        {
            get
            {
                return _database.GetCollection<Holiday>("Holidays");
            }
        }
    }
}
