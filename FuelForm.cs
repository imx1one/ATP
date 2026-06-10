using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ATP
{
    public partial class FuelForm : Form
    {
        private readonly string connStr = ATPConf.ConnectionString;
        private Dictionary<string, int> vehicleIds = new Dictionary<string, int>();
        private Dictionary<string, int> driverIds = new Dictionary<string, int>();
        public FuelForm()
        {
            InitializeComponent();
            buttonEdit.Enabled = false;
            dtpFuelDate.Value = DateTime.Now;
            buttonDelete.Visible = UserSession.isAdmin;
        }

        private async void FuelForm_Load(object sender, EventArgs e)
        {
            await LoadVehiclesAsync();
            await LoadDriversAsync();
            await LoadFuelLogsAsync();
        }
        private async Task LoadVehiclesAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT id, plate_number FROM vehicles WHERE status != 'decommisioned' ORDER BY plate_number", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                vehicleIds.Clear(); cmbVehicle.Items.Clear();
                while(await reader.ReadAsync())
                {
                    vehicleIds[reader.GetString("plate_number")] = reader.GetInt32("id");
                    cmbVehicle.Items.Add(reader.GetString("plate_number"));
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авто: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task LoadDriversAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT iddrivers, full_name FROM drivers ORDER BY full_name", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                driverIds.Clear(); cmbDriver.Items.Clear();
                while (await reader.ReadAsync())
                {
                    driverIds[reader.GetString("full_name")] = reader.GetInt32("iddrivers");
                    cmbDriver.Items.Add(reader.GetString("full_name"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки водителя: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task LoadFuelLogsAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "SELECT f.idfuel_logs AS id, v.plate_number AS 'Авто', d.full_name AS 'Водитель', " +
                    "f.fuel_date AS 'Дата', f.liters AS 'Литры', f.cost_total AS 'Сумма', f.odometer AS 'Одометр' " +
                    "FROM fuel_logs f " +
                    "JOIN vehicles v ON f.vehicles_id = v.id " +
                    "JOIN drivers d ON f.drivers_id = d.iddrivers " +
                    "ORDER BY f.fuel_date DESC", conn);
                using var adapter = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgvFuel.DataSource = dt;
                dgvFuel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvFuel.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvFuel_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex < 0) return;
            var row = dgvFuel.Rows[e.RowIndex];
            if (cmbVehicle.Items.Contains(row.Cells["Авто"].Value?.ToString())) cmbVehicle.SelectedItem = row.Cells["Авто"].Value;
            if (cmbDriver.Items.Contains(row.Cells["Водитель"].Value?.ToString())) cmbDriver.SelectedItem = row.Cells["Водитель"].Value;
            if (row.Cells["Дата"].Value != null) dtpFuelDate.Value = Convert.ToDateTime(row.Cells["Дата"].Value);
            textLiters.Text = row.Cells["Литры"].Value?.ToString();
            textCost.Text = row.Cells["Сумма"].Value?.ToString();
            textOdometer.Text = row.Cells["Одометр"].Value?.ToString();
            buttonAdd.Enabled = false;
            buttonEdit.Enabled = true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            cmbVehicle.SelectedIndex = -1;
            cmbDriver.SelectedIndex = -1;
            dtpFuelDate.Value = DateTime.Now;
            textLiters.Clear(); textCost.Clear(); textOdometer.Clear();
            dgvFuel.ClearSelection();
            buttonAdd.Enabled = true;
            buttonEdit.Enabled = false;
        }

        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput() || cmbVehicle.SelectedItem == null || cmbDriver.SelectedItem == null)
            {
                MessageBox.Show("Выберите авто, водителя и заполните поля.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                if (!vehicleIds.TryGetValue(cmbVehicle.SelectedItem.ToString(), out int vid) ||
                    !driverIds.TryGetValue(cmbDriver.SelectedItem.ToString(), out int did))
                    throw new Exception("Не найдены ID выбранных объектов.");

                using var cmd = new MySqlCommand(
                    "INSERT INTO fuel_logs (vehicles_id, drivers_id, fuel_date, liters, cost_total, odometer) " +
                    "VALUES (@vid, @did, @date, @liters, @cost, @odo)", conn);
                cmd.Parameters.AddWithValue("@vid", vid);
                cmd.Parameters.AddWithValue("@did", did);
                cmd.Parameters.AddWithValue("@date", dtpFuelDate.Value.Date);
                cmd.Parameters.AddWithValue("@liters", decimal.Parse(textLiters.Text));
                cmd.Parameters.AddWithValue("@cost", decimal.Parse(textCost.Text));
                cmd.Parameters.AddWithValue("@odo", int.Parse(textOdometer.Text));
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Заправка добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadFuelLogsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonEdit_Click(object sender, EventArgs e)
        {
            if (dgvFuel.CurrentRow == null || !ValidateInput() || cmbVehicle.SelectedItem == null || cmbDriver.SelectedItem == null)
            {
                MessageBox.Show("Выберите строку и заполните поля.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = Convert.ToInt32(dgvFuel.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                if (!vehicleIds.TryGetValue(cmbVehicle.SelectedItem.ToString(), out int vid) ||
                    !driverIds.TryGetValue(cmbDriver.SelectedItem.ToString(), out int did))
                    throw new Exception("Не найдены ID выбранных объектов.");

                using var cmd = new MySqlCommand(
                    "UPDATE fuel_logs SET vehicles_id=@vid, drivers_id=@did, fuel_date=@date, liters=@liters, cost_total=@cost, odometer=@odo WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@vid", vid);
                cmd.Parameters.AddWithValue("@did", did);
                cmd.Parameters.AddWithValue("@date", dtpFuelDate.Value.Date);
                cmd.Parameters.AddWithValue("@liters", decimal.Parse(textLiters.Text));
                cmd.Parameters.AddWithValue("@cost", decimal.Parse(textCost.Text));
                cmd.Parameters.AddWithValue("@odo", int.Parse(textOdometer.Text));
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Данные обновлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadFuelLogsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dgvFuel.CurrentRow == null) { MessageBox.Show("Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            int id = Convert.ToInt32(dgvFuel.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("DELETE FROM fuel_logs WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Запись удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadFuelLogsAsync();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonRefresh_Click(object sender, EventArgs e)
        {
            await LoadFuelLogsAsync();
        }
        private bool ValidateInput()
        {
            if (!decimal.TryParse(textLiters.Text, out decimal liters) || liters <= 0)
            { MessageBox.Show("Литры должны быть больше 0.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (!decimal.TryParse(textCost.Text, out decimal cost) || cost <= 0)
            { MessageBox.Show("Сумма должна быть больше 0.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (!int.TryParse(textOdometer.Text, out int odo) || odo <= 0)
            { MessageBox.Show("Одометр должен быть положительным.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            return true;
        }
    }
}
