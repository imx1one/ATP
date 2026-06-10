using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Spreadsheet;
using MySqlConnector;


namespace ATP
{
    public partial class DriverForm : Form
    {
        private readonly string connStr = ATPConf.ConnectionString;
        public DriverForm()
        {
            InitializeComponent();
            buttonEdit.Enabled = false;
            comboBox1.Items.AddRange(new string[] { "Доступен", "В поездке", "Отдых" });
            comboBox1.SelectedIndex = 0;
            dtpExp.Value = DateTime.Now.AddYears(5);
            textPhone.Mask = "+7-900-000-00-00";
        }
        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("INSERT INTO drivers (full_name, license, license_expiry, phone, status) " +
                    "VALUES (@name, @license, @expiry, @phone, @status)", conn);
                cmd.Parameters.AddWithValue("@name", textName.Text.Trim());
                cmd.Parameters.AddWithValue("@license", textLic.Text.Trim());
                cmd.Parameters.AddWithValue("@expiry", dtpExp.Value.Date);
                cmd.Parameters.AddWithValue("@phone", textPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@status", comboBox1.Text);
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Водитель добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadDriversAsync();   
            }
            catch(MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("Водитель с таким номером уже существует!", "Дубликат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DriverForm_Load(object sender, EventArgs e)
        {
            await LoadDriversAsync();
        }
        private async Task LoadDriversAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "SELECT iddrivers as id, full_name AS 'ФИО', license AS 'Номер ВУ', License_expiry AS 'Срок ВУ', " +
                    "phone AS 'Телефон', status AS 'Статус' FROM drivers ORDER BY id", conn);
                using var adapter = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgvDrivers.DataSource = dt;
                dgvDrivers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvDrivers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                foreach (DataGridViewRow row in dgvDrivers.Rows){
                    if (row.IsNewRow) continue;
                    if (row.Cells[3].Value != null)
                    {
                        DateTime expiry = Convert.ToDateTime(row.Cells[3].Value);
                        DateTime now = DateTime.Now;
                        DateTime oneMonthAhead = now.AddMonths(1);
                        if (expiry < DateTime.Now)
                        {
                            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                            row.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                        }
                        else if (expiry <= oneMonthAhead)
                        {
                            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
                            row.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                        }
                        else
                        {
                            row.DefaultCellStyle.BackColor = SystemColors.Window;
                            row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvDrivers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvDrivers.Rows[e.RowIndex];
            textName.Text = row.Cells[1].Value?.ToString();
            textLic.Text = row.Cells[2].Value?.ToString();
            if (row.Cells[3].Value != null)
                dtpExp.Value = Convert.ToDateTime(row.Cells[3].Value);
            textPhone.Text = row.Cells[4].Value?.ToString();
            comboBox1.Text = row.Cells[5].Value?.ToString();
            buttonAdd.Enabled = false;
            buttonEdit.Enabled = true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textName.Clear();
            textLic.Clear();
            textPhone.Clear();
            dtpExp.Value = DateTime.Now.AddYears(5);
            comboBox1.SelectedIndex = 0;
            dgvDrivers.ClearSelection();
            buttonAdd.Enabled = true;
            buttonEdit.Enabled = false;
        }

        private async void buttonEdit_Click(object sender, EventArgs e)
        {
            if (dgvDrivers.CurrentRow == null || !ValidateInput()) return;
            int id = Convert.ToInt32(dgvDrivers.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("UPDATE drivers SET full_name=@name, license=@license, " +
                    "license_expiry=@expiry, phone=@phone, status=@status WHERE iddrivers=@id", conn);
                cmd.Parameters.AddWithValue("@name", textName.Text.Trim());
                cmd.Parameters.AddWithValue("@license", textLic.Text.Trim());
                cmd.Parameters.AddWithValue("@expiry", dtpExp.Value.Date);
                cmd.Parameters.AddWithValue("@phone", textPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@status", comboBox1.Text);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Данные обновлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadDriversAsync();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dgvDrivers.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку для удаления", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Удалить этого водителя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int id = Convert.ToInt32(dgvDrivers.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand("DELETE FROM drivers WHERE iddrivers = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show("Водитель удалён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadDriversAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("Нельзя удалить: водитель есть в журнале заправок!",
                    "Защита данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonRefresh_Click(object sender, EventArgs e)
        {
            await LoadDriversAsync();
        }
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(textName.Text))
            {
                MessageBox.Show("Введите ФИО водителя.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(textLic.Text))
            {
                MessageBox.Show("Введите номер водительского удостоверения.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (dtpExp.Value.Date < DateTime.Now.Date)
            {
                if (MessageBox.Show("Срок действия ВУ истёк! Всё равно сохранить?",
                    "Просроченное ВУ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return false;
            }

            return true;
        }
    }
}
