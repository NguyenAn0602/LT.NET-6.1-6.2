using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Bai6._2.BUS;
using Bai6._2.DTO;

namespace Bai6._2
{
    public partial class Form1 : Form
    {
        private HrmBUS hrmBUS = new HrmBUS();
        private MySqlDataAdapter chamCongAdapter;
        private DataTable dtChamCong;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.dgvNhanVien.SelectionChanged += dgvNhanVien_SelectionChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "HỆ THỐNG QUẢN LÝ NHÂN SỰ & TÍNH LƯƠNG (HRM & PAYROLL SYSTEM)";

            LoadDataNhanVien();
            LoadPhongBanCombo();
            txtThangNam.Text = "11/2026";
        }
        private void LoadDataNhanVien()
        {
            try
            {
                dgvNhanVien.DataSource = hrmBUS.GetListNhanVien();
                if (dgvNhanVien.Columns["LuongCoBan"] != null)
                {
                    dgvNhanVien.Columns["LuongCoBan"].DefaultCellStyle.Format = "N0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL HRMDB: " + ex.Message, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPhongBanCombo()
        {
            cboPhongBan.DataSource = hrmBUS.GetListPhongBan();
            cboPhongBan.DisplayMember = "TenPB";
            cboPhongBan.ValueMember = "MaPB";
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) || string.IsNullOrWhiteSpace(txtCCCD.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Họ tên và CCCD!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            NhanVienDTO nv = new NhanVienDTO
            {
                HoTen = txtHoTen.Text.Trim(),
                NgaySinh = dtpNgaySinh.Value,
                CCCD = txtCCCD.Text.Trim(),
                MaPB = Convert.ToInt32(cboPhongBan.SelectedValue),
                LuongCoBan = string.IsNullOrWhiteSpace(txtLuongCB.Text) ? 0 : Convert.ToDecimal(txtLuongCB.Text),
                HeSoLuong = string.IsNullOrWhiteSpace(txtHeSo.Text) ? 1.0 : Convert.ToDouble(txtHeSo.Text)
            };

            if (hrmBUS.InsertNhanVien(nv))
            {
                MessageBox.Show("Thêm nhân viên mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataNhanVien();
            }
            else
            {
                MessageBox.Show("Thêm nhân viên thất bại! (Kiểm tra lại trùng mã CCCD)", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (dgvNhanVien.CurrentRow == null) return;
            int maNV = Convert.ToInt32(dgvNhanVien.CurrentRow.Cells["MaNV"].Value);

            NhanVienDTO nv = new NhanVienDTO
            {
                MaNV = maNV,
                HoTen = txtHoTen.Text.Trim(),
                NgaySinh = dtpNgaySinh.Value,
                CCCD = txtCCCD.Text.Trim(),
                MaPB = Convert.ToInt32(cboPhongBan.SelectedValue),
                LuongCoBan = Convert.ToDecimal(txtLuongCB.Text),
                HeSoLuong = Convert.ToDouble(txtHeSo.Text)
            };

            if (hrmBUS.UpdateNhanVien(nv))
            {
                MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataNhanVien();
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (dgvNhanVien.CurrentRow == null) return;
            int maNV = Convert.ToInt32(dgvNhanVien.CurrentRow.Cells["MaNV"].Value);

            if (MessageBox.Show("Xác nhận xóa hồ sơ nhân viên này?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (hrmBUS.DeleteNhanVien(maNV))
                {
                    MessageBox.Show("Xóa nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDataNhanVien();
                }
            }
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            dgvNhanVien.DataSource = hrmBUS.SearchNhanVien(txtSearch.Text.Trim());
        }

        private void dgvNhanVien_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvNhanVien.CurrentRow != null && dgvNhanVien.CurrentRow.Cells["MaNV"].Value != null)
            {
                txtHoTen.Text = dgvNhanVien.CurrentRow.Cells["HoTen"].Value.ToString();
                txtCCCD.Text = dgvNhanVien.CurrentRow.Cells["CCCD"].Value.ToString();
                txtLuongCB.Text = dgvNhanVien.CurrentRow.Cells["LuongCoBan"].Value.ToString();
                txtHeSo.Text = dgvNhanVien.CurrentRow.Cells["HeSoLuong"].Value.ToString();
                if (dgvNhanVien.CurrentRow.Cells["NgaySinh"].Value != DBNull.Value)
                    dtpNgaySinh.Value = Convert.ToDateTime(dgvNhanVien.CurrentRow.Cells["NgaySinh"].Value);
                if (dgvNhanVien.CurrentRow.Cells["MaPB"].Value != DBNull.Value)
                    cboPhongBan.SelectedValue = dgvNhanVien.CurrentRow.Cells["MaPB"].Value;
            }
        }
        private void btnTaiChamCong_Click(object sender, EventArgs e)
        {
            string thangNam = txtThangNam.Text.Trim();
            dtChamCong = hrmBUS.GetBangChamCongByThang(thangNam, out chamCongAdapter);
            dgvChamCong.DataSource = dtChamCong;
            MessageBox.Show($"Đã nạp bảng chấm công ngắt kết nối cho tháng {thangNam}!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnLuuChamCong_Click(object sender, EventArgs e)
        {
            if (dtChamCong == null || chamCongAdapter == null) return;

            string error = "";
            bool ok = hrmBUS.SaveChamCongDisconnected(dtChamCong, chamCongAdapter, out error);
            if (ok)
            {
                MessageBox.Show("Lưu bảng chấm công ngắt kết nối thành công (Update CSDL)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnTaiChamCong_Click(sender, e); 
            }
            else
            {
                MessageBox.Show("Lưu bảng chấm công thất bại!\nChi tiết: " + error, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnChotLuong_Click(object sender, EventArgs e)
        {
            string thangNam = txtThangNam.Text.Trim();
            string error = "";

            bool ok = hrmBUS.ChotLuongTransaction(thangNam, out error);
            if (ok)
            {
                MessageBox.Show($"Chốt lương tháng {thangNam} thành công (Thực thi SqlTransaction)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dgvBangLuong.DataSource = hrmBUS.GetBangLuongByThang(thangNam);
                if (dgvBangLuong.Columns["ThucLinh"] != null)
                {
                    dgvBangLuong.Columns["ThucLinh"].DefaultCellStyle.Format = "N0";
                }
            }
            else
            {
                MessageBox.Show("Chốt lương thất bại!\nChi tiết: " + error, "Lỗi Transaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnXemBangLuong_Click(object sender, EventArgs e)
        {
            string thangNam = txtThangNam.Text.Trim();
            dgvBangLuong.DataSource = hrmBUS.GetBangLuongByThang(thangNam);
            if (dgvBangLuong.Columns["ThucLinh"] != null)
            {
                dgvBangLuong.Columns["ThucLinh"].DefaultCellStyle.Format = "N0";
            }
        }
        private void label2_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void txtSearch_TextChanged(object sender, EventArgs e) { }
        private void txtHoTen_TextChanged(object sender, EventArgs e) { }
        private void dtpNgaySinh_ValueChanged(object sender, EventArgs e) { }
        private void txtCCCD_TextChanged(object sender, EventArgs e) { }
        private void txtLuongCB_TextChanged(object sender, EventArgs e) { }
        private void cboPhongBan_SelectedIndexChanged(object sender, EventArgs e) { }
        private void dgvNhanVien_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void txtThangNam_TextChanged(object sender, EventArgs e) { }
        private void dgvChamCong_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dgvBangLuong_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}