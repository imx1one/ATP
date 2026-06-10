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
    public partial class VehicleForm : Form
    {
        private readonly string connStr = ATPConf.ConnectionString;
        public VehicleForm()
        {
            InitializeComponent();
            buttonEdit.Enabled = false;
            comboBox1.Items.AddRange(new string[] { "Исправен", "В работе", "Списан" });
            comboBox1.SelectedIndex = 0;
        }

        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "INSERT INTO vehicles (plate_number, brand, model, year, mileage, status) VALUES (@plate, @brand, @model, @year, @mileage, @status)", conn);
                cmd.Parameters.AddWithValue("@plate", textPlate.Text.Trim());
                cmd.Parameters.AddWithValue("@brand", textBrand.Text.Trim());
                cmd.Parameters.AddWithValue("@model", textModel.Text.Trim());
                cmd.Parameters.AddWithValue("@year", int.Parse(textYear.Text));
                cmd.Parameters.AddWithValue("@mileage", int.Parse(textMileage.Text));
                cmd.Parameters.AddWithValue("@status", comboBox1.Text);

                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Автомобиль добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadVehiclesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void VehicleForm_Load(object sender, EventArgs e)
        {
            await LoadVehiclesAsync();
        }
        private async Task LoadVehiclesAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "SELECT id, plate_number AS 'Гос. номер', brand AS 'Марка', model AS 'Модель', " +
                    "year AS 'Год', mileage AS 'Пробег', status AS 'Статус' FROM vehicles ORDER BY id DESC", conn);
                using var adapter = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgvVehicles.DataSource = dt;
                dgvVehicles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvVehicles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvVehicles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvVehicles.Rows[e.RowIndex];
            textPlate.Text = row.Cells["Гос. номер"].Value?.ToString();
            textBrand.Text = row.Cells["Марка"].Value?.ToString();
            textModel.Text = row.Cells["Модель"].Value?.ToString();
            textYear.Text = row.Cells["Год"].Value?.ToString();
            textMileage.Text = row.Cells["Пробег"].Value?.ToString();
            comboBox1.Text = row.Cells["Статус"].Value?.ToString();
            buttonAdd.Enabled = false;
            buttonEdit.Enabled = true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textPlate.Clear();
            textBrand.Clear();
            textModel.Clear();
            textYear.Clear();
            textMileage.Clear();
            comboBox1.SelectedIndex = 0;
            dgvVehicles.ClearSelection();
            buttonAdd.Enabled = true;
            buttonEdit.Enabled = false;
        }

        private async void buttonEdit_Click(object sender, EventArgs e)
        {
            if (dgvVehicles.CurrentRow == null || !ValidateInput()) return;
            int id = Convert.ToInt32(dgvVehicles.CurrentRow.Cells["id"].Value);

            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(
                    "UPDATE vehicles SET plate_number=@plate, brand=@brand, model=@model, year=@year, mileage=@mileage, status=@status WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@plate", textPlate.Text.Trim());
                cmd.Parameters.AddWithValue("@brand", textBrand.Text.Trim());
                cmd.Parameters.AddWithValue("@model", textModel.Text.Trim());
                cmd.Parameters.AddWithValue("@year", int.Parse(textYear.Text));
                cmd.Parameters.AddWithValue("@mileage", int.Parse(textMileage.Text));
                cmd.Parameters.AddWithValue("@status", comboBox1.Text);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("Данные обновлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadVehiclesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dgvVehicles.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Удалить этот автомобиль?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            int id = Convert.ToInt32(dgvVehicles.CurrentRow.Cells["id"].Value);
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("DELETE FROM vehicles WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show("Автомобиль удалён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonClear_Click(null, null);
                await LoadVehiclesAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                MessageBox.Show("Нельзя удалить: автомобиль используется в журналах ТО или заправок.", "Защита данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonRefresh_Click(object sender, EventArgs e)
        {
            await LoadVehiclesAsync();
        }
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(textPlate.Text) || string.IsNullOrWhiteSpace(textBrand.Text))
            {
                MessageBox.Show("Заполните гос. номер и марку.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!int.TryParse(textYear.Text, out int year) || year < 1900 || year > DateTime.Now.Year + 1)
            {
                MessageBox.Show("Некорректный год выпуска.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!int.TryParse(textMileage.Text, out int mileage) || mileage < 0)
            {
                MessageBox.Show("Некорректный пробег.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
    }
}
