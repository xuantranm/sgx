
namespace Common.Enums
{
    public enum ESalary
    {
        VP = 1,
        NM = 2,
        SX = 3,
        Law =4
    }
    public enum EModeDirect
    {
        Content = 1,
        Category = 2
    }

    public enum EVanHanhStatus
    {
        DangXuLy = 1,
        HoanThanh = 2,
        ChinhSua = 3,
        ChotBaoCao = 4,
        SuaBaoCao = 5
    }

    public enum EPosition
    {
        Normal = 1,
        Feature = 2,
        Top = 3,
        Sticky = 4
    }

    public enum EModeData
    {
        Input = 1,
        File = 2,
        Merge = 3
    }

    public enum EImageSize
    {
        Desktop = 1,
        Tablet = 2,
        Mobile = 3,
        Thumb = 4,
        Icon = 5,
        Avatar = 6,
        x300x300 = 7,
        x600x400 = 8,
        x720x480 = 9, // GG | FB
        x1110x576 = 10,
        x1200x800 = 11,
        x1600x1050 = 12,
        x1980x600 = 13,
        x1600x375 = 14,
        Logo = 15, // size get in setting
        Cover = 16
    }

    public enum ERightType
    {
        User = 1,
        ChucVu = 2,
        Location = 3
    }

    public enum ECategory
    {
        Ca = 1,
        CongDoan = 2,
        MangCongViec = 3,
        XeCoGioivsMayMoc = 4,
        PhanLoaiXe = 5,
        Role = 6,
        NVLvsBTPvsTP = 7,
        CaLamViec = 8,
        ChungLoaiXe = 9,
        NhaThau = 10,
        DinhMucVanHanh = 11,
        Company = 12,
        KhoiChucNang = 13,
        PhongBan = 14,
        BoPhan = 15,
        ChucVu = 16,
        Contract = 17,
        TimeWork = 18,
        Bank = 19,
        Hospital = 20,
        Gender = 21,
        Probation = 22,
        SalaryBase = 23,
        Holiday = 24,
        LeaveType = 25
    }

    public enum EPropertyValue
    {
        String = 1,
        Interger = 2,
        Double = 3,
        Decimal = 4,
        Bool = 5
    }
    public enum ENotification
    {
        None = 0,
        System = 1,
        Hr = 2,
        ExpireDocument = 3,
        TaskBHXH = 4,
        Company = 5
    }

    public enum EData
    {
        System = 1,
        Hcns = 2,
        Salary = 3,
        Setting = 4,
        Content = 5,
        Property = 6
    }

    public enum EValueType
    {
        String = 1,
        Interger = 2,
        Double = 3,
        Decimal = 4,
        Bool = 5,
        Date = 6
    }

    public enum EText
    {
        System = 1,
        Error = 2
    }

    public enum ESalaryType
    {
        VP = 1,
        NM = 2,
        SX = 3
    }

    public enum EBun
    {
        DAC = 1,
        DAT = 2        
    }

    public enum ECustomer
    {
        Supplier = 1,
        Client = 2,
        Bun = 3,
        ChuDauTu = 4,
        ThiCong = 5,
        GiamSat = 6,
    }

    public enum ETrangThai
    {
        Kho = 1
    }

    public enum ETiepNhanXuLy
    {
        Nhap = 1,
        Xuat = 2
    }

    public enum EKho
    {
        NguyenVatLieu = 1,
        ThanhPham = 2,
        Bun = 3,
        HangTraVe = 4,
        TramCan = 5, // Tram Can
        DuAnCong = 6 // Tram Can
    }

    public enum EFile
    {
        Image = 1,
        Document = 2
    }

    public enum EProductStatus
    {
        New = 1
    }

    public enum ECreditStatus
    {
        New = 0,
        Approved = 1,
        Cancel = 2,
        Process = 3,
        Completed = 4
    }

    public enum ECredit
    {
        UngLuong = 1,
        Vay = 2
    }

    public enum EProductType
    {
        TP = 1,
        BTP = 2,
        NVL = 3
    }

    public enum EMode
    {
        TrongGio = 1,
        NgoaiGio = 2
    }

    public enum EDinhMuc
    {
        DongGoi = 1,
        BocVac = 2,
        CongViecKhac = 3
    }

    public enum Phase { Years, Months, Days, Done }

    public enum EEmailGroup
    {
        New = 1,
        Leave = 2,
    }

    public enum EEmailStatus
    {
        Send = 0,
        Ok = 1,
        Fail = 2,
        Resend = 3,
        Schedule = 4,
        ScheduleASAP = 5
    }

    public enum EPCPL
    {
        PC = 1,
        PL = 2
    }

    public enum EKhoiLamViec
    {
        VP = 1,
        NM = 2,
        SX = 3
    }

    public enum StatusLeave
    {
        New = 0,
        Accept = 1,
        Cancel = 2,
        Pending = 3
    }

    public enum EStatusWork
    {
        XacNhanCong = 0,
        DuCong = 1,
        DaGuiXacNhan = 2,
        DongY = 3,
        TuChoi = 4,
        Wait = 5
    }

    public enum EStatusLeave
    {
        New = 0,
        Accept = 1,
        Cancel = 2,
        Pending = 3
    }

    public enum ETangCa
    {
        None = 0,
        CanXacNhan = 1,
        GuiXacNhan = 2,
        DongY = 3,
        TuChoi = 4
    }

    public enum EOvertime
    {
        None = 0,
        Create = 1,
        Ok = 2,
        Cancel = 3,
        Secutity = 4,
        Signed = 5
    }

    public enum EDateType
    {
        Normal = 1,
        Sunday = 2,
        PublicHoliday = 3
    }

    public enum ETimeWork
    {
        None = 0,
        Normal = 1,
        LeavePhep = 3,
        LeaveHuongLuong = 4,
        LeaveKhongHuongLuong = 5,
        Other = 7,
        Wait = 8,
        Sunday = 20,
        Holiday = 60
    }

    public enum ETexts
    {
        None = 1,
        ErrorSystem = 2
        // up to 10
    }

    public enum EGroupPolicy
    {
        User = 1,
        Title = 2
    }

    public enum ERoleControl
    {
        User = 1,
        Group = 2,
        ChucVu = 3
    }

    public enum ERights
    {
        None = 0,
        View = 1,
        Add = 2,
        Edit = 3,
        Disable = 4,
        Delete = 5,
        History = 6,
        Boss = 7,
        BossLv2 = 8,
        BossLv3 = 9
    }

    public enum EStatus
    {
        // implement later
    }

    public enum ELogType
    {
        Other = 0,
        Performance = 1
    }

    public enum EMonths
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    public enum EDownTimeMessageType
    {
        DownTimeWarningMessage = 1,
        DownTimeErrorMessage = 2
    }

    public enum EAlertType
    {
        /// <summary>
        /// remove all flag states from database completly
        /// </summary>
        Remove = -1,

        /// <summary>
        /// set flag to none
        /// </summary>
        None = 0,

        /// <summary>
        /// new flag
        /// </summary>
        New = 1,

        /// <summary>
        /// updated flag
        /// </summary>
        Update = 2
    }
}
