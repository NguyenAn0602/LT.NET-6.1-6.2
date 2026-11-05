using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Bai6._1.BUS;
using Bai6._1.DTO;

namespace Bai6._1
{
    public partial class Form1 : Form
    {
        private SalesBUS salesBUS = new SalesBUS();
        private List<ChiTietHoaDonDTO> listGioHang = new List<ChiTietHoaDonDTO>();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "HỆ THỐNG QUẢN LÝ BÁN HÀNG & SIÊU THỊ MINI (RETAIL SALES SYSTEM)";

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                dgvSanPham.DataSource = salesBUS.GetListSanPham();
                if (dgvSanPham.Columns["GiaBan"] != null)
                {
                    dgvSanPham.Columns["GiaBan"].DefaultCellStyle.Format = "N0";
                }
                cboNhanVien.DataSource = salesBUS.GetListNhanVien();
                cboNhanVien.DisplayMember = "TenNV";
                cboNhanVien.ValueMember = "MaNV";
                cboKhachHang.DataSource = salesBUS.GetListKhachHang();
                cboKhachHang.DisplayMember = "TenKH";
                cboKhachHang.ValueMember = "MaKH";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL SalesDB: " + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string kw = txtSearch.Text.Trim();
            dgvSanPham.DataSource = salesBUS.SearchSanPham(kw);
        }
        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (dgvSanPham.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn 1 sản phẩm từ danh mục bên trái!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maSP = dgvSanPham.CurrentRow.Cells["MaSP"].Value.ToString();
            string tenSP = dgvSanPham.CurrentRow.Cells["TenSP"].Value.ToString();
            decimal donGia = Convert.ToDecimal(dgvSanPham.CurrentRow.Cells["GiaBan"].Value);
            int soLuong = (int)nmSoLuong.Value;

            var item = listGioHang.Find(x => x.MaSP == maSP);
            if (item != null)
            {
                item.SoLuong += soLuong;
            }
            else
            {
                listGioHang.Add(new ChiTietHoaDonDTO
                {
                    MaSP = maSP,
                    TenSP = tenSP,
                    SoLuong = soLuong,
                    DonGia = donGia
                });
            }

            BindGioHangGrid();
        }

        private void BindGioHangGrid()
        {
            dgvGioHang.DataSource = null;
            dgvGioHang.DataSource = listGioHang;

            decimal tongTien = 0;
            foreach (var item in listGioHang) tongTien += item.ThanhTien;
            lblTongTien.Text = $"Tổng tiền: {tongTien:N0} VNĐ";
        }
        private void btnThanhToan_Click(object sender, EventArgs e)
        {
            if (cboNhanVien.SelectedValue == null) return;

            int maNV = Convert.ToInt32(cboNhanVien.SelectedValue);
            int? maKH = cboKhachHang.SelectedValue != null ? Convert.ToInt32(cboKhachHang.SelectedValue) : (int?)null;

            string error = "";
            bool ok = salesBUS.ThanhToan(maNV, maKH, listGioHang, out error);

            if (ok)
            {
                MessageBox.Show("Thanh toán thành công!\nHóa đơn đã lưu, tự động trừ tồn kho và cộng điểm tích lũy cho khách hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                listGioHang.Clear();
                BindGioHangGrid();
                LoadData();
            }
            else
            {
                MessageBox.Show("Thanh toán thất bại (Rollback Transaction)!\nChi tiết: " + error, "Lỗi Transaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnThongKe_Click(object sender, EventArgs e)
        {
            decimal doanhThu = salesBUS.GetTongDoanhThuHienTai();
            DataTable dtTop5 = salesBUS.GetTop5SanPhamBanChay();

            string msg = $"[BÁO CÁO DOANH THU HÔM NAY]\nTổng doanh thu: {doanhThu:N0} VNĐ\n\n[TOP 5 SẢN PHẨM BÁN CHẠY NHẤT]:\n";
            foreach (DataRow r in dtTop5.Rows)
            {
                msg += $"- {r["TenSP"]}: Đã bán {r["TongDaBan"]} sản phẩm\n";
            }

            MessageBox.Show(msg, "Thống kê bán hàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void label4_Click(object sender, EventArgs e) { }
        private void txtSearch_TextChanged(object sender, EventArgs e) { }
        private void dgvSanPham_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void nmSoLuong_ValueChanged(object sender, EventArgs e) { }
        private void cboNhanVien_SelectedIndexChanged(object sender, EventArgs e) { }
        private void cboKhachHang_SelectedIndexChanged(object sender, EventArgs e) { }
        private void dgvGioHang_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}