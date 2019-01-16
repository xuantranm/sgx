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
        public IMongoCollection<Department> Departments
        {
            get
            {
                return _database.GetCollection<Department>("Departments");
            }
        }


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
