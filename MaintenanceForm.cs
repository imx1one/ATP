using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySqlConnector;

namespace ATP
{
    public partial class MaintenanceForm : Form
    {
        private readonly string connStr = ATPConf.ConnectionString;
        private Dictionary<string, int> vehicleIds = new Dictionary<string, int>();

        public MaintenanceForm()
        {
            InitializeComponent();
            buttonEdit.Enabled = false;
            cmbServiceType.Items.AddRange(new string[] {
                "ТО-1 (5000 км)", "ТО-2 (10000 км)", "Замена масла",
                "Замена колодок", "Диагностика", "Текущий ремонт"
            });
            cmbServiceType.SelectedIndex = 0;
            dtpServiceDate.Value = DateTime.Now;
            dtpNextDue.Value = DateTime.Now.AddMonths(6);
            cmbServiceType.SelectedIndexChanged += (s, e) => CalculateNextDue();
            dtpServiceDate.ValueChanged += (s, e) => CalculateNextDue();
            buttonDelete.Visible = UserSession.isAdmin;
        }

        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput() || cmbVehicle.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля и выберите авто.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                int vid = vehicleIds[cmbVehicle.SelectedItem.ToString()];

                using var cmd = new MySqlCommand(
                    "INSERT INTO maintanance (vehicle_id, service_date, mileage_at_service, service_type, cost, next_due_date) " +
                    "VALUES (@vid, @sdate, @mileage, @stype, @cost, @nextdue)", conn);

                cmd.Parameters.AddWithValue("@vid", vid);
                cmd.Parameters.AddWithValue("@sdate", dtpServiceDate.Value.Date);
                cmd.Parameters.AddWithValue("@mileage", int.Parse(textMileage.Text));
                cmd.Parameters.AddWithValue("@stype", cmbServiceType.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@cost", decimal.Parse(textCost.Text));
                cmd.Parameters.AddWithValue("@nextdue", dtpNextDue.Value.Date);

                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Запись добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadMaintenanceAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void MaintenanceForm_Load(object sender, EventArgs e)
        {
            await LoadVehiclesAsync();
            await LoadMaintenanceAsync();
        }
        private async Task LoadVehiclesAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT id, plate_number FROM vehicles WHERE status != 'decommissioned' ORDER BY plate_number", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                vehicleIds.Clear();
                cmbVehicle.Items.Clear();
                while(await reader.ReadAsync())
                {
                    int id = reader.GetInt32("id");
                    string plate = reader.GetString("plate_number");
                    vehicleIds[plate] = id;
                    cmbVehicle.Items.Add(plate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авто: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task LoadMaintenanceAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "SELECT m.idmaintanance AS id, v.plate_number AS 'Автомобиль', m.service_date AS 'Дата ТО', " +
                    "m.mileage_at_service AS 'Пробег', m.service_type AS 'Тип работ', " +
                    "m.cost AS 'Стоимость', m.next_due_date AS 'След. ТО' " +
                    "FROM maintanance m JOIN vehicles v ON m.vehicles_id = v.id " +
                    "ORDER BY m.service_date DESC", conn);
                using var adapter = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgvMaintenance.DataSource = dt;
                dgvMaintenance.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvMaintenance.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                foreach (DataGridViewRow row in dgvMaintenance.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["След. ТО"].Value != null)
                    {
                        DateTime nextDue = Convert.ToDateTime(row.Cells["След. ТО"].Value);
                        if (nextDue < DateTime.Now)
                        {
                            row.DefaultCellStyle.BackColor = Color.LightCoral;
                            row.DefaultCellStyle.ForeColor = Color.White;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CalculateNextDue()
        {
            if (cmbServiceType.SelectedItem == null) return;
            string type = cmbServiceType.SelectedItem.ToString();
            DateTime date = dtpServiceDate.Value;

            int months = type switch
            {
                "ТО-1 (5000 км)" => 6,
                "ТО-2 (10000 км)" => 12,
                "Замена масла" => 6,
                "Замена колодок" => 12,
                "Диагностика" => 12,
                "Текущий ремонт" => 3,
                _ => 6
            };
            dtpNextDue.Value = date.AddMonths(months);
        }

        private void dgvMaintenance_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvMaintenance.Rows[e.RowIndex];

            string plate = row.Cells["Автомобиль"].Value?.ToString();
            if (cmbVehicle.Items.Contains(plate)) cmbVehicle.SelectedItem = plate;

            if (row.Cells["Дата ТО"].Value != null) dtpServiceDate.Value = Convert.ToDateTime(row.Cells["Дата ТО"].Value);
            textMileage.Text = row.Cells["Пробег"].Value?.ToString();

            string type = row.Cells["Тип работ"].Value?.ToString();
            if (cmbServiceType.Items.Contains(type)) cmbServiceType.SelectedItem = type;

            textCost.Text = row.Cells["Стоимость"].Value?.ToString();
            if (row.Cells["След. ТО"].Value != null) dtpNextDue.Value = Convert.ToDateTime(row.Cells["След. ТО"].Value);

            buttonAdd.Enabled = false;
            buttonEdit.Enabled = true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            cmbVehicle.SelectedIndex = -1;
            dtpServiceDate.Value = DateTime.Now;
            textMileage.Clear();
            cmbServiceType.SelectedIndex = 0;
            textCost.Clear();
            dtpNextDue.Value = DateTime.Now.AddMonths(6);
            dgvMaintenance.ClearSelection();
            buttonAdd.Enabled = true;
            buttonEdit.Enabled = false;
        }

        private async void buttonEdit_Click(object sender, EventArgs e)
        {
            if (dgvMaintenance.CurrentRow == null || !ValidateInput() || cmbVehicle.SelectedIndex == null)
            {
                MessageBox.Show("Выберите строку и заполните поля.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = Convert.ToInt32(dgvMaintenance.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                int vid = vehicleIds[cmbVehicle.SelectedItem.ToString()];
                using var cmd = new MySqlCommand(
                    "UPDATE maintanance SET vehicles_id=@vid, service_date=@sdate, mileage_at_service=@mileage, " +
                    "service_type=@stype, cost=@cost, next_due_date=@nextdue WHERE idmaintanance=@id", conn);
                cmd.Parameters.AddWithValue("@vid", vid);
                cmd.Parameters.AddWithValue("@sdate", dtpServiceDate.Value.Date);
                cmd.Parameters.AddWithValue("@mileage", int.Parse(textMileage.Text));
                cmd.Parameters.AddWithValue("@stype", cmbServiceType.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@cost", decimal.Parse(textCost.Text));
                cmd.Parameters.AddWithValue("@nextdue", dtpNextDue.Value.Date);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Данные обновлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadMaintenanceAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            if(dgvMaintenance.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Удалить эту запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int id = Convert.ToInt32(dgvMaintenance.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("DELETE FROM maintanance WHERE idmaintanance=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonRefresh_Click(object sender, EventArgs e)
        {
            await LoadMaintenanceAsync();
        }
        private bool ValidateInput()
        {
            if(!int.TryParse(textMileage.Text, out int m) || m < 0)
            {
                MessageBox.Show("Некорректный пробег.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            } 
            if(!decimal.TryParse(textCost.Text, out decimal c) || c < 0)
            {
                MessageBox.Show("Некорректная стоимость.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
    }
}
