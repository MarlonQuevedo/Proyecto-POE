using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CineApp
{
    public class MainForm : Form
    {
        ComboBox cboFunciones;
        Button btnAsientos;
        Button btnPeliculas;
        Button btnCliente;
        List<FuncionInfo> funciones = new List<FuncionInfo>();

        public MainForm()
        {
            Text = "Gestión de Cine";
            Width = 600;
            Height = 150;
            StartPosition = FormStartPosition.CenterScreen;

            cboFunciones = new ComboBox
            {
                Left = 20,
                Top = 20,
                Width = 540,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnAsientos = new Button
            {
                Left = 20,
                Top = 60,
                Width = 150,
                Height = 30,
                Text = "Ver asientos (Admin)"
            };
            btnAsientos.Click += BtnAsientos_Click;

            btnPeliculas = new Button
            {
                Left = 190,
                Top = 60,
                Width = 150,
                Height = 30,
                Text = "Gestionar Películas"
            };
            btnPeliculas.Click += BtnPeliculas_Click;

            btnCliente = new Button
            {
                Left = 360,
                Top = 60,
                Width = 200,
                Height = 30,
                Text = "Abrir Cliente (Ver Películas)"
            };
            btnCliente.Click += BtnCliente_Click;

            Controls.Add(cboFunciones);
            Controls.Add(btnAsientos);
            Controls.Add(btnPeliculas);
            Controls.Add(btnCliente);

            Load += MainForm_Load;
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            CargarFunciones();
        }

        void CargarFunciones()
        {
            funciones.Clear();

            try
            {
                using (var c = Db.NewConnection())
                {
                    c.Open();

                    string sql = @"
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

                cboFunciones.DataSource = null;
                cboFunciones.DataSource = funciones;
                cboFunciones.DisplayMember = nameof(FuncionInfo.Display);
                cboFunciones.ValueMember = nameof(FuncionInfo.FuncionId);

                btnAsientos.Enabled = funciones.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar funciones: " + ex.Message);
                cboFunciones.DataSource = null;
                btnAsientos.Enabled = false;
            }
        }

        void BtnAsientos_Click(object sender, EventArgs e)
        {
            if (cboFunciones.SelectedItem is FuncionInfo f)
            {
                // Admin solo mira asientos, no compra
                using (var frm = new SeatsForm(f.FuncionId, f.Display, false))
                {
                    frm.ShowDialog(this);
                }
            }
        }

        void BtnPeliculas_Click(object sender, EventArgs e)
        {
            using (var frm = new MoviesForm())
            {
                frm.ShowDialog(this);
            }

            // Al cerrar Gestión de Películas siempre refrescamos la lista
            CargarFunciones();
        }

        void BtnCliente_Click(object sender, EventArgs e)
        {
            using (var frm = new ClientForm())
            {
                frm.ShowDialog(this);
            }
        }
    }
}
