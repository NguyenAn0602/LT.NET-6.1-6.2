using System.Data;
using MySql.Data.MySqlClient;
using Bai6._2.DAL;
using Bai6._2.DTO;

namespace Bai6._2.BUS
{
    public class HrmBUS
    {
        private HrmDAL hrmDAL = new HrmDAL();

        public DataTable GetListNhanVien() => hrmDAL.GetListNhanVien();
        public DataTable GetListPhongBan() => hrmDAL.GetListPhongBan();
        public DataTable SearchNhanVien(string kw) => hrmDAL.SearchNhanVien(kw);

        public bool InsertNhanVien(NhanVienDTO nv) => hrmDAL.InsertNhanVien(nv);
        public bool UpdateNhanVien(NhanVienDTO nv) => hrmDAL.UpdateNhanVien(nv);
        public bool DeleteNhanVien(int maNV) => hrmDAL.DeleteNhanVien(maNV);

        public DataTable GetBangChamCongByThang(string thangNam, out MySqlDataAdapter adapter)
        {
            return hrmDAL.GetBangChamCongByThang(thangNam, out adapter);
        }

        public bool SaveChamCongDisconnected(DataTable dt, MySqlDataAdapter adapter, out string error)
        {
            return hrmDAL.SaveChamCongDisconnected(dt, adapter, out error);
        }

        public bool ChotLuongTransaction(string thangNam, out string errorMessage)
        {
            return hrmDAL.ChotLuongTransaction(thangNam, out errorMessage);
        }

        public DataTable GetBangLuongByThang(string thangNam) => hrmDAL.GetBangLuongByThang(thangNam);
    }
}