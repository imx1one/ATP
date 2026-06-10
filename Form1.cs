using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySqlConnector;
using ClosedXML.Excel;

namespace ATP
{
    public partial class Form1 : Form
    {
        private string connStr = "Server=localhost;Port=3307;Database=atp_db;Uid=root;Pwd=root;";
        public Form1()
        {
            InitializeComponent();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Form1_Resize(this, EventArgs.Empty);
                await LoadStatsAsync();
                await LoadChartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД:\n{ex.Message}", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (!UserSession.isAdmin)
            {
                справочникАвтомобилейToolStripMenuItem.Enabled = false;
                справочникВодителейToolStripMenuItem.Enabled = false;
                отчетыToolStripMenuItem.Enabled=false;

                справочникАвтомобилейToolStripMenuItem.ToolTipText = "Доступно только администратору";
                справочникВодителейToolStripMenuItem.ToolTipText = "Доступно только администратору";
                отчетыToolStripMenuItem.ToolTipText = "Доступно только администратору";
            }
        }
        private async Task LoadStatsAsync()
        {
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            label5.Text = await GetCountAsync(conn, "SELECT COUNT(*) FROM vehicles") + " ед.";
            label6.Text = await GetCountAsync(conn, "SELECT COUNT(*) FROM drivers") + " чел.";
            label7.Text = await GetCountAsync(conn, "SELECT COUNT(DISTINCT vehicles_id) FROM maintanance WHERE next_due_date < CURDATE()") + " ед.";
            decimal fuelCost = await GetFuelCostAsync(conn);
            label8.Text = fuelCost.ToString("F2") + " ₽";
        }

        private async Task<int> GetCountAsync(MySqlConnection conn, string query)
        {
            using var cmd = new MySqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        private async Task<decimal> GetFuelCostAsync(MySqlConnection conn)
        {
            string query = @"
                SELECT COALESCE(SUM(cost_total), 0) 
                FROM fuel_logs 
                WHERE MONTH(fuel_date) = MONTH(CURDATE()) 
                  AND YEAR(fuel_date) = YEAR(CURDATE())";

            using var cmd = new MySqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0;
        }
        private async Task LoadChartAsync()
        {
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            chart1.Series.Clear();
            var series = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Автопарк",
                ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column,
                Color = Color.FromArgb(79, 129, 189)
            };
            string query = @"
                SELECT status, COUNT(*) as count 
                FROM vehicles 
                GROUP BY status";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string status = reader.GetString("status");
                int count = reader.GetInt32("count");
                series.Points.AddXY(GetStatusName(status), count);
            }
            chart1.Series.Add(series);
            chart1.ChartAreas[0].AxisX.Title = "Статус";
            chart1.ChartAreas[0].AxisY.Title = "Количество";
            chart1.ChartAreas[0].AxisY.Minimum = 0;
        }
        private string GetStatusName(string status)
        {
            return status switch
            {
                "active" => "Исправны",
                "maintenance" => "В ремонте",
                "decommissioned" => "Списаны",
                _ => status
            };
        }
        private async Task<DataTable> GetReportDataAsync(string query)
        {
            var dt = new DataTable();
            try
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(query, conn);
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }
        private void SaveToExcel(DataTable dt, string title)
        {
            if(dt.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования отчета.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"{title}_{DateTime.Now:yyyy-MM-dd}.xlsx"
            };
            if(saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Отчёт");
                    worksheet.Cell(1, 1).InsertTable(dt);
                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Отчет успешно сохранен:\n{saveDialog.FileName}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void справочникАвтомобилейToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new VehicleForm().Show();
        }

        private void справочникВодителейToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DriverForm().Show();
        }

        private void журналТОToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new MaintenanceForm().Show();
        }

        private void журналЗаправокToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FuelForm().Show();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await LoadStatsAsync();
            await LoadChartAsync();
        }

        private async void отчетПоГСМToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string query = @"
                SELECT 
                    v.plate_number AS 'Гос. номер', 
                    v.brand AS 'Марка',
                    COUNT(f.idfuel_logs) AS 'Заправок', 
                    SUM(f.liters) AS 'Литров всего', 
                    SUM(f.cost_total) AS 'Сумма (руб)'
                FROM fuel_logs f
                JOIN vehicles v ON f.vehicles_id = v.id
                GROUP BY v.id, v.plate_number, v.brand
                ORDER BY SUM(f.cost_total) DESC";

            var data = await GetReportDataAsync(query);
            SaveToExcel(data, "Отчет_ГСМ");
        }

        private async void отчетПоТОToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string query = @"
                SELECT 
                    v.plate_number AS 'Машина', 
                    m.service_type AS 'Вид работ', 
                    m.service_date AS 'Дата', 
                    m.mileage_at_service AS 'Пробег',
                    m.cost AS 'Стоимость',
                    m.next_due_date AS 'След. ТО'
                FROM maintanance m
                JOIN vehicles v ON m.vehicles_id = v.id
                ORDER BY m.service_date DESC";

            var data = await GetReportDataAsync(query);
            SaveToExcel(data, "Отчет_ТО");
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized) return;
            CenterPanelBetweenControls();
        }
        private void CenterPanelBetweenControls()
        {
            if (panel1 == null || menuStrip1 == null) return;
            int topLimit = menuStrip1.Bottom;
            int bottomLimit = chart1 != null ? chart1.Top : this.ClientSize.Height;
            int availableHeight = bottomLimit - topLimit;
            int panelY = topLimit + (availableHeight - panel1.Height) / 2;
            int panelX = (this.ClientSize.Width - panel1.Width) / 2;
            panelY = Math.Max(topLimit, Math.Min(panelY, bottomLimit - panel1.Height));
            panelX = Math.Max(0, Math.Min(panelX, this.ClientSize.Width - panel1.Width));

            panel1.Location = new Point(panelX, panelY);
        }

        private void менюToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private async void отчетПоВУToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string query = @"
                SELECT 
                    d.full_name AS 'ФИО водителя',
                    d.license AS 'Номер ВУ',
                    d.license_expiry AS 'Действует до',
                    d.phone AS 'Телефон',
                    d.status AS 'Статус',
                    CASE 
                        WHEN d.license_expiry < CURDATE() THEN 'ПРОСРОЧЕНО'
                        WHEN d.license_expiry <= DATE_ADD(CURDATE(), INTERVAL 1 MONTH) THEN 'Истекает в течение месяца'
                        WHEN d.license_expiry <= DATE_ADD(CURDATE(), INTERVAL 3 MONTH) THEN 'Истекает в течение 3 месяцев'
                        ELSE 'Действует'
                    END AS 'Статус ВУ',
                    CASE 
                        WHEN d.license_expiry < CURDATE() THEN DATEDIFF(CURDATE(), d.license_expiry)
                        WHEN d.license_expiry > CURDATE() THEN DATEDIFF(d.license_expiry, CURDATE())
                        ELSE 0
                    END AS 'Дней (осталось/просрочено)'
                FROM drivers d
                WHERE d.license_expiry <= DATE_ADD(CURDATE(), INTERVAL 3 MONTH)
                   OR d.license_expiry < CURDATE()
                ORDER BY d.license_expiry ASC";

            var dt = await GetReportDataAsync(query);    

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Нет водителей с истекающими или просроченными правами.",
                               "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveToExcel(dt, "Отчет_ВУ.xlsx");
        }

        private void справкаToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new Reference().Show();
        }

        private void руководствоПользователяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Form2().Show();
        }
    }
}
