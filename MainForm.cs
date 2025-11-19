using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace CineApp
{
    public class MainForm : Form
    {
        Button btnAsientos;
        Button btnPeliculas;
        Button btnCliente;
        List<FuncionInfo> funciones;

        public MainForm()
        {
            Text = "Gestión de Cine";
            Width = 600; Height = 150;
            StartPosition = FormStartPosition.CenterScreen;

            btnAsientos = new Button{Left=20,Top=20,Width=150,Height=30,Text="Ver asientos (Admin)"};
            btnAsientos.Click += BtnAsientos_Click;
            btnPeliculas = new Button{Left=190,Top=20,Width=150,Height=30,Text="Gestionar Películas"};
            btnPeliculas.Click += BtnPeliculas_Click;
            btnCliente = new Button{Left=360,Top=20,Width=200,Height=30,Text="Abrir Cliente (Ver Películas)"};
            btnCliente.Click += BtnCliente_Click;

            Controls.Add(btnAsientos);
            Controls.Add(btnPeliculas);
            Controls.Add(btnCliente);
            Load += MainForm_Load;
            // Apply consistent UI theme
            UITheme.Apply(this);
        }

        void MainForm_Load(object s, EventArgs e)
        {
            try
            {
                funciones = new List<FuncionInfo>();
                using (var c = Db.NewConnection())
                {
                    c.Open();
                    var sql = @"SELECT f.FuncionId,p.Titulo AS Pelicula,s.Nombre AS Sala,f.FechaHoraInicio
                            FROM Funcion f
                            JOIN Pelicula p ON p.PeliculaId=f.PeliculaId
                            JOIN Sala s ON s.SalaId=f.SalaId
                            ORDER BY f.FechaHoraInicio";

                    using (var cmd = new SqlCommand(sql, c))
                    {
                        // Instrumentation: log command and connection state before executing reader
                        try
                        {
                            var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                            var info = $"Pre-Execute: cmd={(cmd==null?"<null>":"OK")}; CommandTextLength={(cmd?.CommandText?.Length ?? 0)}; conn={(c==null?"<null>":c.State.ToString())}; connCSLen={(c?.ConnectionString?.Length ?? 0)}\n";
                            System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("s") + " - " + info);
                            // Also write a copy to the project root for easier discovery
                            try
                            {
                                var projRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"));
                                var alt = System.IO.Path.Combine(projRoot, "run_diagnostics.txt");
                                System.IO.File.AppendAllText(alt, DateTime.Now.ToString("s") + " - " + info);
                            }
                            catch { }
                        }
                        catch { }

                        try
                        {
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
                        catch (Exception ex2)
                        {
                            try
                            {
                                var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                                var extra = $"ExecuteReader failed: conn={(c==null?"<null>":c.State.ToString())}; connCS={(c?.ConnectionString??"<null>")}\nException:\n{ex2}\n";
                                System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("s") + " - " + extra + "\n");
                            }
                            catch { }
                            throw;
                        }
                    }
                }
                // functions loaded into memory; UI has no dropdown
            }
            catch (Exception ex)
            {
                // Log completo a archivo para análisis
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                    System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("s") + " - Error al cargar funciones:\n" + ex.ToString() + "\n\n");
                }
                catch { }

                // Mostrar información breve al usuario y pedir revisar el log
                MessageBox.Show("Error al cargar funciones: " + ex.Message + "\nRevise error_log.txt en el directorio de la aplicación para más detalles.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try { btnAsientos.Enabled = false; } catch { }
                try { btnPeliculas.Enabled = false; } catch { }
                try { btnCliente.Enabled = false; } catch { }
            }
        }

        void BtnAsientos_Click(object s, EventArgs e)
        {
            // Use the first loaded function (read-only) to open the seats view (admin mode => no purchase)
            if (funciones != null && funciones.Count > 0)
            {
                var f = funciones[0];
                using (var frm = new SeatsForm(f.FuncionId, f.Display, false)) { frm.ShowDialog(this); }
            }
        }

        void BtnPeliculas_Click(object s, EventArgs e)
        {
            using(var frm=new MoviesForm()){frm.ShowDialog(this);} 
        }

        void BtnCliente_Click(object s, EventArgs e)
        {
            using(var frm=new ClientForm()){frm.ShowDialog(this);} 
        }
    }
}
