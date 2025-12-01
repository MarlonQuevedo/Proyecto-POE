using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace CineApp
{
    public class ClientForm : Form
    {
        DataGridView dgvFunciones;
        Button btnVerAsientos;
        Button btnBack;

        List<FuncionInfo> funciones;

        public ClientForm()
        {
            Text = "Cliente - Cartelera";
            Width = 700;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            dgvFunciones = new DataGridView
            {
                Left = 10,
                Top = 10,
                Width = 660,
                Height = 380,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            dgvFunciones.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FuncionId",
                DataPropertyName = "FuncionId",
                Visible = false
            });
            dgvFunciones.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Pelicula",
                DataPropertyName = "Pelicula",
                HeaderText = "Película",
                Width = 350
            });
            dgvFunciones.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Sala",
                DataPropertyName = "Sala",
                HeaderText = "Sala",
                Width = 120
            });
            dgvFunciones.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FechaHoraInicio",
                DataPropertyName = "FechaHoraInicio",
                HeaderText = "Inicio",
                Width = 150
            });

            btnVerAsientos = new Button
            {
                Left = 10,
                Top = 400,
                Width = 150,
                Text = "Ver asientos"
            };
            btnVerAsientos.Click += BtnVerAsientos_Click;

            btnBack = new Button
            {
                Left = 170,
                Top = 400,
                Width = 100,
                Text = "Atrás"
            };
            btnBack.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            Controls.Add(dgvFunciones);
            Controls.Add(btnVerAsientos);
            Controls.Add(btnBack);

            Load += ClientForm_Load;
        }

        void ClientForm_Load(object s, EventArgs e)
        {
            LoadFunciones();
        }

        void LoadFunciones()
        {
            funciones = new List<FuncionInfo>();

            using (var c = Db.NewConnection())
            {
                c.Open();
                var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
                var sql = hasPrecio
                    ? @"
                        SELECT f.FuncionId,
                               p.Titulo AS Pelicula,
                               s.Nombre AS Sala,
                               f.FechaHoraInicio,
                               ISNULL(p.Precio,0) AS Precio
                        FROM Funcion f
                        JOIN Pelicula p ON p.PeliculaId = f.PeliculaId
                        JOIN Sala s ON s.SalaId = f.SalaId
                        WHERE p.Activa = 1
                        ORDER BY f.FechaHoraInicio"
                    : @"
                        SELECT f.FuncionId,
                               p.Titulo AS Pelicula,
                               s.Nombre AS Sala,
                               f.FechaHoraInicio
                        FROM Funcion f
                        JOIN Pelicula p ON p.PeliculaId = f.PeliculaId
                        JOIN Sala s ON s.SalaId = f.SalaId
                        WHERE p.Activa = 1
                        ORDER BY f.FechaHoraInicio";

                using (var cmd = new SqlCommand(sql, c))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        funciones.Add(new FuncionInfo
                        {
                            FuncionId = rdr.GetInt32(0),
                            Pelicula = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                            Sala = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                            FechaHoraInicio = rdr.GetDateTime(3)
                        });
                    }
                }
            }

            DataTable sinFun = Db.ObtenerPeliculasSinFuncion();
            foreach (DataRow row in sinFun.Rows)
            {
                funciones.Add(new FuncionInfo
                {
                    FuncionId = 0,
                    Pelicula = row["Titulo"].ToString(),
                    Sala = "(Sin función)",
                    FechaHoraInicio = DateTime.MinValue
                });
            }

            dgvFunciones.DataSource = funciones
                .Select(f => new
                {
                    f.FuncionId,
                    f.Pelicula,
                    f.Sala,
                    FechaHoraInicio = (f.FechaHoraInicio == DateTime.MinValue)
                        ? ""
                        : f.FechaHoraInicio.ToString("g")
                })
                .ToList();
        }

        void BtnVerAsientos_Click(object s, EventArgs e)
        {
            if (dgvFunciones.CurrentRow == null) return;

            var funcionIdObj = dgvFunciones.CurrentRow.Cells["FuncionId"].Value;
            if (funcionIdObj == null || funcionIdObj == DBNull.Value)
                return;

            int funcionId = (int)funcionIdObj;

            if (funcionId == 0)
            {
                MessageBox.Show(
                    "Esta película aún no tiene una función programada en ninguna sala.",
                    "Sin función",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var pelicula = dgvFunciones.CurrentRow.Cells["Pelicula"].Value?.ToString() ?? "";
            var sala = dgvFunciones.CurrentRow.Cells["Sala"].Value?.ToString() ?? "";
            var inicio = dgvFunciones.CurrentRow.Cells["FechaHoraInicio"].Value?.ToString() ?? "";
            var display = pelicula + " - " + sala + " - " + inicio;

            using (var frm = new SeatsForm(funcionId, display, true))
            {
                frm.ShowDialog(this);
            }
        }
    }
}
