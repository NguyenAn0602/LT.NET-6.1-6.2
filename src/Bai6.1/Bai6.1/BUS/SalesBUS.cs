using System.Collections.Generic;
using System.Data;
using Bai6._1.DAL;
using Bai6._1.DTO;

namespace Bai6._1.BUS
{
    public class SalesBUS
    {
        private SalesDAL salesDAL = new SalesDAL();

        public DataTable GetListSanPham() => salesDAL.GetListSanPham();
        public DataTable SearchSanPham(string kw) => salesDAL.SearchSanPham(kw);
        public DataTable GetListKhachHang() => salesDAL.GetListKhachHang();
        public DataTable GetListNhanVien() => salesDAL.GetListNhanVien();
        public decimal GetTongDoanhThuHienTai() => salesDAL.GetTongDoanhThuHienTai();
        public DataTable GetTop5SanPhamBanChay() => salesDAL.GetTop5SanPhamBanChay();

        public bool ThanhToan(int maNV, int? maKH, List<ChiTietHoaDonDTO> listChiTiet, out string errorMessage)
        {
            if (listChiTiet == null || listChiTiet.Count == 0)
            {
                errorMessage = "Giỏ hàng hiện tại đang trống!";
                return false;
            }
            return salesDAL.ThanhToanTransaction(maNV, maKH, listChiTiet, out errorMessage);
        }
    }
}