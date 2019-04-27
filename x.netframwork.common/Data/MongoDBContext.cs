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

        #region Tribatvn
        public IMongoCollection<ProductSale> ProductSales
        {
            get
            {
                return _database.GetCollection<ProductSale>("ProductSales");
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
        public IMongoCollection<WorkTimeType> WorkTimeTypes
        {
            get
            {
                return _database.GetCollection<WorkTimeType>("WorkTimeTypes");
            }
        }

        public IMongoCollection<EmployeeWorkTime> EmployeeWorkTimes
        {
            get
            {
                return _database.GetCollection<EmployeeWorkTime>("EmployeeWorkTimes");
            }
        }

        public IMongoCollection<EmployeeManager> EmployeeManagers
        {
            get
            {
                return _database.GetCollection<EmployeeManager>("EmployeeManagers");
            }
        }

        public IMongoCollection<LeaveType> LeaveTypes
        {
            get
            {
                return _database.GetCollection<LeaveType>("LeaveTypes");
            }
        }

        public IMongoCollection<Leave> Leaves
        {
            get
            {
                return _database.GetCollection<Leave>("Leaves");
            }
        }

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

        public IMongoCollection<Employee> Employees
        {
            get
            {
                return _database.GetCollection<Employee>("Employees");
            }
        }

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
        #endregion

        #region SYSTEM
        public IMongoCollection<Setting> Settings
        {
            get
            {
                return _database.GetCollection<Setting>("Settings");
            }
        }

        public IMongoCollection<ScheduleEmail> ScheduleEmails
        {
            get
            {
                return _database.GetCollection<ScheduleEmail>("ScheduleEmails");
            }
        }
        #endregion

        #region FACTORIES
        public IMongoCollection<FactoryProduct> FactoryProducts
        {
            get
            {
                return _database.GetCollection<FactoryProduct>("FactoryProducts");
            }
        }

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
        #endregion

        #region SALARIES
        public IMongoCollection<EmployeeCong> EmployeeCongs
        {
            get
            {
                return _database.GetCollection<EmployeeCong>("EmployeeCongs");
            }
        }

        public IMongoCollection<SalaryEmployeeMonth> SalaryEmployeeMonths
        {
            get
            {
                return _database.GetCollection<SalaryEmployeeMonth>("SalaryEmployeeMonths");
            }
        }

        #endregion

        public IMongoCollection<CreditEmployee> CreditEmployees
        {
            get
            {
                return _database.GetCollection<CreditEmployee>("CreditEmployees");
            }
        }

        public IMongoCollection<Role> Roles
        {
            get
            {
                return _database.GetCollection<Role>("Roles");
            }
        }

        public IMongoCollection<RoleUser> RoleUsers
        {
            get
            {
                return _database.GetCollection<RoleUser>("RoleUsers");
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
