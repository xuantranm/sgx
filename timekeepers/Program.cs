using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using timekeepers.Models;

namespace timekeepers
{
    class Program
    {
        static zkemkeeper.CZKEMClass TimeKeeperMachine = new zkemkeeper.CZKEMClass();

        static int iMachineNumber = 1;

        static void Main(string[] args)
        {
            Console.WriteLine("Get data from timekeeper...");

            #region Declare
            var model = ConfigurationSettings.AppSettings["Model"];
            var location = ConfigurationSettings.AppSettings["Location"];
            var ip = ConfigurationSettings.AppSettings["Ip"];
            var port = Convert.ToInt32(ConfigurationSettings.AppSettings["Port"]);

            var databaseConnection = ConfigurationSettings.AppSettings["Mongo_Connection"];
            var databaseName = ConfigurationSettings.AppSettings["Mongo_Database"];

            var isUser = Convert.ToInt32(ConfigurationSettings.AppSettings["Users"]) == 1 ? true : false;
            var isAttLogs = Convert.ToInt32(ConfigurationSettings.AppSettings["AttLogs"]) == 1 ? true : false;
            var mode = Convert.ToInt32(ConfigurationSettings.AppSettings["Mode"]) == 0 ? false : true;

            var debug = Convert.ToInt32(ConfigurationSettings.AppSettings["DeBug"]) == 1 ? true : false;
            #endregion

            if (isAttLogs)
            {
                if (TimeKeeper_Connect(ip, port))
                {
                    Console.WriteLine("Please wait. Reading logs  data...");
                    TimeKeeper_GetData(mode, model, location, ip, port, databaseConnection, databaseName);
                }
                else
                {
                    var message = "Fail in connecting devide";
                    Console.WriteLine(message);
                    WriteLog(model, location, ip, port, databaseConnection, databaseName, false, message);
                }
            }

            if (isUser)
            {
                if (TimeKeeper_Connect(ip, port))
                {
                    Console.WriteLine("Please wait. Reading user information data...");
                    TimeKeeper_GetUserInformation(model, location, ip, port, databaseConnection, databaseName);
                }
                else
                {
                    var message = "Fail in connecting devide";
                    Console.WriteLine(message);
                    WriteLog(model, location, ip, port, databaseConnection, databaseName, false, message);
                }
            }


            #region Clear logs. BE CAREFULL. Danger remove data finger. CHECK LATER
            //if (TimeKeeper_Connect(ip, port))
            //{
            //    TimeKeeper_ClearData();
            //}
            #endregion

            if (debug)
            {
                Console.WriteLine("Press ENTER to exist...");
                Console.ReadLine();
            }
        }

        static bool TimeKeeper_Connect(string ip, int port)
        {
            Console.WriteLine("Connecting with devide, please wait...");
            var IsConnected = TimeKeeperMachine.Connect_Net(ip, port);
            if (IsConnected == true)
            {
                Console.WriteLine("Connected.");
                TimeKeeperMachine.RegEvent(iMachineNumber, 65535);//Here you can register the realtime events that you want to be triggered(the parameters 65535 means registering all)
            }
            return IsConnected;
        }

        static void TimeKeeper_GetData(bool mode, string model, string location, string ip, int port, string databaseConnection, string databaseName)
        {
            #region Declare
            string sdwEnrollNumber = "";
            int idwVerifyMode = 0;
            int idwInOutMode = 0;
            int idwYear = 0;
            int idwMonth = 0;
            int idwDay = 0;
            int idwHour = 0;
            int idwMinute = 0;
            int idwSecond = 0;
            int idwWorkcode = 0;

            int idwErrorCode = 0;
            #endregion

            // Disable the device
            //TimeKeeperMachine.EnableDevice(iMachineNumber, false);//disable the device
            //if (!TimeKeeperMachine.ReadGeneralLogData(iMachineNumber))//read all the attendance records to the memory
            //{
            //    TimeKeeperMachine.GetLastError(ref idwErrorCode);
            //    var message = string.Empty;
            //    if (idwErrorCode != 0)
            //    {
            //        message = "Reading data from terminal failed, ErrorCode: " + idwErrorCode.ToString();
            //    }
            //    else
            //    {
            //        message = "No data from terminal returns!";
            //    }

            //    WriteLog(model, location, ip, port, databaseConnection, databaseName, false, message);

            //    TimeKeeperMachine.EnableDevice(iMachineNumber, true); //enable the device
            //    return;
            //}

            #region Get Data time
            var getDataDay = Convert.ToInt32(ConfigurationSettings.AppSettings["GetDataDay"]);
            var timeCrawled = DateTime.Now.AddDays(getDataDay);
            timeCrawled = new DateTime(timeCrawled.Year, timeCrawled.Month, timeCrawled.Day, 2, 0, 0);
            #endregion

            var list = new List<AttLog>();
            // Disable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, false);
            while (TimeKeeperMachine.SSR_GetGeneralLogData(iMachineNumber, out sdwEnrollNumber, out idwVerifyMode,
                       out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))//get records from the memory
            {
                var date = idwYear.ToString() + "-" + idwMonth.ToString() + "-" + idwDay.ToString() + " " + idwHour.ToString() + ":" + idwMinute.ToString() + ":" + idwSecond.ToString();
                var dateFinger = new DateTime(idwYear, idwMonth, idwDay, idwHour, idwMinute, idwSecond);
                if (!mode)
                {
                    if (dateFinger > timeCrawled)
                    {
                        list.Add(new AttLog
                        {
                            EnrollNumber = sdwEnrollNumber,
                            VerifyMode = idwVerifyMode.ToString(),
                            InOutMode = idwInOutMode.ToString(),
                            Date = dateFinger,
                            Workcode = idwWorkcode.ToString()
                        });
                        Console.WriteLine("Added to list "+ dateFinger + " , enrollNumber: " + sdwEnrollNumber);
                    }
                }
                else
                {
                    list.Add(new AttLog
                    {
                        EnrollNumber = sdwEnrollNumber,
                        VerifyMode = idwVerifyMode.ToString(),
                        InOutMode = idwInOutMode.ToString(),
                        Date = dateFinger,
                        Workcode = idwWorkcode.ToString()
                    });
                    Console.WriteLine("Added to list " + dateFinger + " , enrollNumber: " + sdwEnrollNumber);
                }
            }
            // Enable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, true);

            #region Store to db
            var client = new MongoClient(databaseConnection);
            var server = client.GetServer();
            var database = server.GetDatabase(databaseName);
            var collection = database.GetCollection<AttLog>(model + location + "AttLogs");
            var count = 0;
            if (!mode)
            {
                foreach (var item in list)
                {
                    // Check exist
                    var query = Query.And(
                                            Query<AttLog>.EQ(e => e.EnrollNumber, item.EnrollNumber),
                                            Query<AttLog>.EQ(e => e.Date, item.Date)
                                        );
                    var entity = collection.FindOne(query);
                    if (entity == null)
                    {
                        collection.Insert(item);
                        count++;
                        Console.WriteLine("Inserted to db: " + item.Date + " , enrollNumber: " + item.EnrollNumber);
                    }
                }
            }
            else
            {
                collection.RemoveAll();
                // Delete all. because get all
                foreach (var item in list)
                {
                    collection.Insert(item);
                    count++;
                    Console.WriteLine("Inserted to db: " + item.Date + " , enrollNumber: " + item.EnrollNumber);
                }
            }

            WriteLog(model, location, ip, port, databaseConnection, databaseName, true, "Completed get data, records: " + count);
            #endregion
        }

        static void TimeKeeper_GetUserInformation(string model, string location, string ip, int port, string databaseConnection, string databaseName)
        {
            string sdwEnrollNumber = string.Empty, sName = string.Empty, sPassword = string.Empty, sTmpData = string.Empty;
            int iPrivilege = 0, iTmpLength = 0, iFlag = 0, idwFingerIndex;
            bool bEnabled = false;

            ICollection<UserInfo> lstFPTemplates = new List<UserInfo>();

            // Disable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, false);

            TimeKeeperMachine.ReadAllUserID(iMachineNumber);
            TimeKeeperMachine.ReadAllTemplate(iMachineNumber);

            while (TimeKeeperMachine.SSR_GetAllUserInfo(iMachineNumber, out sdwEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))
            {
                for (idwFingerIndex = 0; idwFingerIndex < 10; idwFingerIndex++)
                {
                    if (TimeKeeperMachine.GetUserTmpExStr(iMachineNumber, sdwEnrollNumber, idwFingerIndex, out iFlag, out sTmpData, out iTmpLength))
                    {
                        UserInfo fpInfo = new UserInfo
                        {
                            MachineNumber = iMachineNumber,
                            EnrollNumber = sdwEnrollNumber,
                            Name = sName,
                            FingerIndex = idwFingerIndex,
                            TmpData = sTmpData,
                            Privelage = iPrivilege,
                            Password = sPassword,
                            Enabled = bEnabled,
                            iFlag = iFlag.ToString()
                        };

                        lstFPTemplates.Add(fpInfo);
                    }
                }
            }

            #region Store to db
            var client = new MongoClient(databaseConnection);
            var server = client.GetServer();
            var database = server.GetDatabase(databaseName);
            var collection = database.GetCollection<UserInfo>(model + location + "UserInfos");
            collection.Drop();
            foreach (var item in lstFPTemplates)
            {
                collection.Insert(item);
            }
            #endregion

            // Enable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, true);
        }

        static void TimeKeeper_ClearData(string ip, int port)
        {
            int idwErrorCode = 0;

            // Disable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, false);
            if (TimeKeeperMachine.ClearGLog(iMachineNumber))
            {
                TimeKeeperMachine.RefreshData(iMachineNumber);//the data in the device should be refreshed
                Console.WriteLine("All att Logs have been cleared from teiminal!", "Success");
            }
            else
            {
                TimeKeeperMachine.GetLastError(ref idwErrorCode);
                Console.WriteLine("Operation failed,ErrorCode=" + idwErrorCode.ToString(), "Error");
            }

            // Enable the device
            TimeKeeperMachine.EnableDevice(iMachineNumber, true);
        }

        static void WriteLog(string model, string location, string ip, int port, string connection, string databaseName, bool status, string message)
        {
            var client = new MongoClient(connection);
            var server = client.GetServer();
            var database = server.GetDatabase(databaseName);
            var collection = database.GetCollection<LogTimeKeeper>(model + location + "LogTimeKeepers");
            collection.Insert(new LogTimeKeeper
            {
                Date = DateTime.Now,
                Status = status,
                Message = message,
                Model = model,
                Location = location,
                Ip = ip,
                Port = port
            });

            var settings = database.GetCollection<Setting>("Settings");
            var query = Query<Setting>.EQ(e => e.Key, location.ToLower() + "-timekeeper-connection");
            var update = Update<Setting>.Set(e => e.Value, status.ToString());
            settings.Update(query, update);
        }
    }
}
