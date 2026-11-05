using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Bai6._1.DTO;

namespace Bai6._1.DAL
{
    public class SalesDAL : DatabaseAccess
    {
        public DataTable GetListSanPham() => ExecuteQuery("SELECT * FROM SanPham");
        public DataTable SearchSanPham(string keyword)
        {
            string query = "SELECT * FROM SanPham WHERE MaSP LIKE @kw OR TenSP LIKE @kw";
            MySqlParameter[] p = { new MySqlParameter("@kw", "%" + keyword + "%") };
            return ExecuteQuery(query, p);
        }

        public DataTable GetListKhachHang() => ExecuteQuery("SELECT * FROM KhachHang");
        public DataTable GetListNhanVien() => ExecuteQuery("SELECT * FROM NhanVien");
        public decimal GetTongDoanhThuHienTai()
        {
            string query = "SELECT IFNULL(SUM(TongTien), 0) FROM HoaDon WHERE DATE(NgayLap) = CURDATE()";
            object result = ExecuteScalar(query);
            return Convert.ToDecimal(result);
        }

        public DataTable GetTop5SanPhamBanChay()
        {
            string query = @"SELECT sp.MaSP, sp.TenSP, SUM(ct.SoLuong) AS TongDaBan
                             FROM ChiTietHoaDon ct
                             JOIN SanPham sp ON ct.MaSP = sp.MaSP
                             GROUP BY sp.MaSP, sp.TenSP
                             ORDER BY TongDaBan DESC LIMIT 5";
            return ExecuteQuery(query);
        }
        public bool ThanhToanTransaction(int maNV, int? maKH, List<ChiTietHoaDonDTO> listChiTiet, out string errorMessage)
        {
            errorMessage = "";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction();
                try
                {
                    foreach (var ct in listChiTiet)
                    {
                        string qCheck = "SELECT SoLuongTon FROM SanPham WHERE MaSP = @MaSP";
                        MySqlCommand cmdCheck = new MySqlCommand(qCheck, conn, trans);
                        cmdCheck.Parameters.AddWithValue("@MaSP", ct.MaSP);

                        object res = cmdCheck.ExecuteScalar();
                        int tonHienTai = (res != null) ? Convert.ToInt32(res) : 0;
                        if (tonHienTai < ct.SoLuong)
                        {
                            trans.Rollback();
                            errorMessage = $"Sản phẩm [{ct.TenSP}] không đủ tồn kho! (Tồn hiện tại: {tonHienTai}, Cần bán: {ct.SoLuong})";
                            return false;
                        }
                    }
                    decimal tongTien = 0;
                    foreach (var item in listChiTiet) tongTien += item.ThanhTien;
                    string q1 = "INSERT INTO HoaDon (NgayLap, MaNV, MaKH, TongTien) VALUES (NOW(), @MaNV, @MaKH, @TongTien); SELECT LAST_INSERT_ID();";
                    MySqlCommand cmd1 = new MySqlCommand(q1, conn, trans);
                    cmd1.Parameters.AddWithValue("@MaNV", maNV);
                    cmd1.Parameters.AddWithValue("@MaKH", (object)maKH ?? DBNull.Value);
                    cmd1.Parameters.AddWithValue("@TongTien", tongTien);
                    int maHD = Convert.ToInt32(cmd1.ExecuteScalar());
                    foreach (var ct in listChiTiet)
                    {
                        string q2 = "INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia) VALUES (@MaHD, @MaSP, @SoLuong, @DonGia)";
                        MySqlCommand cmd2 = new MySqlCommand(q2, conn, trans);
                        cmd2.Parameters.AddWithValue("@MaHD", maHD);
                        cmd2.Parameters.AddWithValue("@MaSP", ct.MaSP);
                        cmd2.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
                        cmd2.Parameters.AddWithValue("@DonGia", ct.DonGia);
                        cmd2.ExecuteNonQuery();
                        string q3 = "UPDATE SanPham SET SoLuongTon = SoLuongTon - @SoLuong WHERE MaSP = @MaSP";
                        MySqlCommand cmd3 = new MySqlCommand(q3, conn, trans);
                        cmd3.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
                        cmd3.Parameters.AddWithValue("@MaSP", ct.MaSP);
                        cmd3.ExecuteNonQuery();
                    }
                    if (maKH.HasValue && maKH.Value > 1)
                    {
                        int diemCong = (int)(tongTien / 100000);
                        if (diemCong > 0)
                        {
                            string q4 = "UPDATE KhachHang SET DiemTichLuy = DiemTichLuy + @Diem WHERE MaKH = @MaKH";
                            MySqlCommand cmd4 = new MySqlCommand(q4, conn, trans);
                            cmd4.Parameters.AddWithValue("@Diem", diemCong);
                            cmd4.Parameters.AddWithValue("@MaKH", maKH.Value);
                            cmd4.ExecuteNonQuery();
                        }
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
    }
}