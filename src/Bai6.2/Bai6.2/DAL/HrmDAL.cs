using System;
using System.Data;
using MySql.Data.MySqlClient;
using Bai6._2.DTO;

namespace Bai6._2.DAL
{
    public class HrmDAL : DatabaseAccess
    {
        // 1. Quản lý Nhân sự & Phòng ban
        public DataTable GetListNhanVien()
        {
            string query = @"SELECT nv.MaNV, nv.HoTen, nv.NgaySinh, nv.CCCD, nv.LuongCoBan, nv.HeSoLuong, pb.TenPB, nv.MaPB
                             FROM NhanVien nv LEFT JOIN PhongBan pb ON nv.MaPB = pb.MaPB";
            return ExecuteQuery(query);
        }

        public DataTable GetListPhongBan() => ExecuteQuery("SELECT * FROM PhongBan");

        public DataTable SearchNhanVien(string kw)
        {
            string query = @"SELECT nv.MaNV, nv.HoTen, nv.NgaySinh, nv.CCCD, nv.LuongCoBan, nv.HeSoLuong, pb.TenPB, nv.MaPB
                             FROM NhanVien nv LEFT JOIN PhongBan pb ON nv.MaPB = pb.MaPB
                             WHERE nv.HoTen LIKE @kw OR nv.CCCD LIKE @kw OR pb.TenPB LIKE @kw";
            MySqlParameter[] p = { new MySqlParameter("@kw", "%" + kw + "%") };
            return ExecuteQuery(query, p);
        }

        public bool InsertNhanVien(NhanVienDTO nv)
        {
            string query = @"INSERT INTO NhanVien (HoTen, NgaySinh, CCCD, MaPB, LuongCoBan, HeSoLuong)
                             VALUES (@HoTen, @NgaySinh, @CCCD, @MaPB, @LuongCoBan, @HeSoLuong)";
            MySqlParameter[] p = {
                new MySqlParameter("@HoTen", nv.HoTen),
                new MySqlParameter("@NgaySinh", nv.NgaySinh),
                new MySqlParameter("@CCCD", nv.CCCD),
                new MySqlParameter("@MaPB", nv.MaPB),
                new MySqlParameter("@LuongCoBan", nv.LuongCoBan),
                new MySqlParameter("@HeSoLuong", nv.HeSoLuong)
            };
            return ExecuteNonQuery(query, p) > 0;
        }

        public bool UpdateNhanVien(NhanVienDTO nv)
        {
            string query = @"UPDATE NhanVien 
                             SET HoTen = @HoTen, NgaySinh = @NgaySinh, CCCD = @CCCD, MaPB = @MaPB, LuongCoBan = @LuongCoBan, HeSoLuong = @HeSoLuong
                             WHERE MaNV = @MaNV";
            MySqlParameter[] p = {
                new MySqlParameter("@HoTen", nv.HoTen),
                new MySqlParameter("@NgaySinh", nv.NgaySinh),
                new MySqlParameter("@CCCD", nv.CCCD),
                new MySqlParameter("@MaPB", nv.MaPB),
                new MySqlParameter("@LuongCoBan", nv.LuongCoBan),
                new MySqlParameter("@HeSoLuong", nv.HeSoLuong),
                new MySqlParameter("@MaNV", nv.MaNV)
            };
            return ExecuteNonQuery(query, p) > 0;
        }

        public bool DeleteNhanVien(int maNV)
        {
            string query = "DELETE FROM NhanVien WHERE MaNV = @MaNV";
            MySqlParameter[] p = { new MySqlParameter("@MaNV", maNV) };
            return ExecuteNonQuery(query, p) > 0;
        }

        // 2. CHẤM CÔNG NGẮT KẾT NỐI (DISCONNECTED MODEL)
        public DataTable GetBangChamCongByThang(string thangNam, out MySqlDataAdapter adapter)
        {
            // 1. Tự động nạp bản ghi chấm công cho nhân viên chưa có trong tháng
            string initQuery = @"INSERT IGNORE INTO BangChamCong (MaNV, ThangNam, SoNgayCong, SoGioOT)
                         SELECT MaNV, @ThangNam, 0, 0 FROM NhanVien";
            MySqlParameter[] pInit = { new MySqlParameter("@ThangNam", thangNam) };
            ExecuteNonQuery(initQuery, pInit);

            // 2. Truy vấn lấy dữ liệu bảng chấm công
            string query = @"SELECT cc.MaCong, cc.MaNV, nv.HoTen, cc.SoNgayCong, cc.SoGioOT
                     FROM BangChamCong cc 
                     JOIN NhanVien nv ON cc.MaNV = nv.MaNV
                     WHERE cc.ThangNam = @ThangNam";

            MySqlConnection conn = new MySqlConnection(connectionString);
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ThangNam", thangNam);

            adapter = new MySqlDataAdapter(cmd);

            // 3. Cấu hình thủ công UpdateCommand cho adapter để cập nhật đúng bảng BangChamCong
            string updateQuery = @"UPDATE BangChamCong 
                           SET SoNgayCong = @SoNgayCong, SoGioOT = @SoGioOT 
                           WHERE MaCong = @MaCong";

            MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
            updateCmd.Parameters.Add("@SoNgayCong", MySqlDbType.Float, 0, "SoNgayCong");
            updateCmd.Parameters.Add("@SoGioOT", MySqlDbType.Float, 0, "SoGioOT");
            updateCmd.Parameters.Add("@MaCong", MySqlDbType.Int32, 0, "MaCong");

            adapter.UpdateCommand = updateCmd;

            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public bool SaveChamCongDisconnected(DataTable dtChamCong, MySqlDataAdapter adapter, out string error)
        {
            error = "";
            try
            {
                adapter.Update(dtChamCong);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // 3. TÍNH LƯƠNG & CHỐT LƯƠNG THÁNG (MYSQLTRANSACTION)
        public bool ChotLuongTransaction(string thangNam, out string errorMessage)
        {
            errorMessage = "";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // Lấy danh sách chấm công kèm thông tin lương cơ bản
                    string queryData = @"SELECT cc.MaNV, nv.LuongCoBan, nv.HeSoLuong, cc.SoNgayCong, cc.SoGioOT
                                         FROM BangChamCong cc JOIN NhanVien nv ON cc.MaNV = nv.MaNV
                                         WHERE cc.ThangNam = @ThangNam";
                    MySqlCommand cmdData = new MySqlCommand(queryData, conn, trans);
                    cmdData.Parameters.AddWithValue("@ThangNam", thangNam);

                    MySqlDataAdapter da = new MySqlDataAdapter(cmdData);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        trans.Rollback();
                        errorMessage = $"Chưa có dữ liệu chấm công cho tháng {thangNam}!";
                        return false;
                    }

                    // Duyệt từng nhân viên để tính Thực Lĩnh và chèn vào BangLuong
                    foreach (DataRow row in dt.Rows)
                    {
                        int maNV = Convert.ToInt32(row["MaNV"]);
                        decimal luongCB = Convert.ToDecimal(row["LuongCoBan"]);
                        double heSo = Convert.ToDouble(row["HeSoLuong"]);
                        double ngayCong = Convert.ToDouble(row["SoNgayCong"]);
                        double gioOT = Convert.ToDouble(row["SoGioOT"]);

                        // Công thức: ThucLinh = (LuongCoBan * HeSoLuong / 22) * SoNgayCong + (SoGioOT * 50,000)
                        decimal thucLinh = (decimal)((double)luongCB * heSo / 22.0) * (decimal)ngayCong + (decimal)(gioOT * 50000.0);

                        string qInsert = @"INSERT INTO BangLuong (MaNV, ThangNam, ThucLinh) 
                                           VALUES (@MaNV, @ThangNam, @ThucLinh)
                                           ON DUPLICATE KEY UPDATE ThucLinh = @ThucLinh";
                        MySqlCommand cmdInsert = new MySqlCommand(qInsert, conn, trans);
                        cmdInsert.Parameters.AddWithValue("@MaNV", maNV);
                        cmdInsert.Parameters.AddWithValue("@ThangNam", thangNam);
                        cmdInsert.Parameters.AddWithValue("@ThucLinh", thucLinh);
                        cmdInsert.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = ex.Message;
                    return false;
                }
            }
        }

        public DataTable GetBangLuongByThang(string thangNam)
        {
            string query = @"SELECT l.MaLuong, l.MaNV, nv.HoTen, pb.TenPB, l.ThucLinh
                             FROM BangLuong l 
                             JOIN NhanVien nv ON l.MaNV = nv.MaNV
                             LEFT JOIN PhongBan pb ON nv.MaPB = pb.MaPB
                             WHERE l.ThangNam = @ThangNam";
            MySqlParameter[] p = { new MySqlParameter("@ThangNam", thangNam) };
            return ExecuteQuery(query, p);
        }
    }
}