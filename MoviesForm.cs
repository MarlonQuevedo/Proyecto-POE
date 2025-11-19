using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CineApp
{
    public class MoviesForm : Form
    {
        DataGridView dgv;
        Button btnNuevo;
        Button btnEditar;
        Button btnBack;

        public MoviesForm()
        {
            Text = "Gestionar Películas";
            Width = 700; Height = 500; StartPosition = FormStartPosition.CenterParent;
            dgv = new DataGridView { ReadOnly = true, AutoGenerateColumns = false, Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeliculaId", DataPropertyName = "PeliculaId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Titulo", DataPropertyName = "Titulo", HeaderText = "Título", Width = 350 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", DataPropertyName = "Precio", HeaderText = "Precio", Width = 100 });

            btnNuevo = new Button { Width = 120, Height = 36, Text = "Nuevo" };
            btnEditar = new Button { Width = 120, Height = 36, Text = "Editar" };
            btnBack = new Button { Width = 120, Height = 36, Text = "Atrás" };
            btnNuevo.Click += BtnNuevo_Click;
            btnEditar.Click += BtnEditar_Click;
            btnBack.Click += (s,e) => { DialogResult = DialogResult.Cancel; };
            // Bottom panel for actions
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.PanelBack };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10), WrapContents = false, AutoSize = true };
            actions.Controls.Add(btnNuevo); actions.Controls.Add(btnEditar); actions.Controls.Add(btnBack);
            bottom.Controls.Add(actions);

            Controls.Add(dgv);
            Controls.Add(bottom);
            Load += MoviesForm_Load;
            // Apply consistent UI theme
            UITheme.Apply(this);
        }

        void MoviesForm_Load(object s, EventArgs e)
        {
            LoadMovies();
        }

        void LoadMovies()
        {
            var list = new List<dynamic>();
            using (var c = Db.NewConnection())
            {
                c.Open();
                var hasPrecioColumn = Db.ColumnExists("Pelicula", "Precio");
                string sql;
                if (hasPrecioColumn)
                {
                    // If Pelicula.Precio exists, prefer it; otherwise fall back to MIN(Funcion.Precio)
                    sql = @"
SELECT p.PeliculaId, p.Titulo, ISNULL(p.Precio, ISNULL(MIN(f.Precio), 0)) AS Precio
FROM Pelicula p
LEFT JOIN Funcion f ON f.PeliculaId = p.PeliculaId
GROUP BY p.PeliculaId, p.Titulo, p.Precio
ORDER BY p.Titulo";
                }
                else
                {
                    // Legacy: no Precio column on Pelicula, use MIN(Funcion.Precio)
                    sql = @"
SELECT p.PeliculaId, p.Titulo, ISNULL(MIN(f.Precio), 0) AS Precio
FROM Pelicula p
LEFT JOIN Funcion f ON f.PeliculaId = p.PeliculaId
GROUP BY p.PeliculaId, p.Titulo
ORDER BY p.Titulo";
                }

                using (var cmd = new SqlCommand(sql, c))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new { PeliculaId = rdr.GetInt32(0), Titulo = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1), Precio = rdr.GetDecimal(2) });
                    }
                }
            }
            dgv.DataSource = list;
            // Format Precio column as currency if present
            try
            {
                if (dgv.Columns.Contains("Precio"))
                {
                    dgv.Columns["Precio"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["Precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgv.Columns["Precio"].Width = 120;
                }
            }
            catch { }
        }

        void BtnNuevo_Click(object s, EventArgs e)
        {
            using (var f = new MovieEditForm()) { if (f.ShowDialog(this) == DialogResult.OK) LoadMovies(); }
        }

        void BtnEditar_Click(object s, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            var id = (int)dgv.CurrentRow.Cells["PeliculaId"].Value;
            using (var f = new MovieEditForm(id)) { if (f.ShowDialog(this) == DialogResult.OK) LoadMovies(); }
        }
    }

    class MovieEditForm : Form
    {
        TextBox txtTitulo;
        NumericUpDown nudDuracion;
        TextBox txtClasificacion;
        DateTimePicker dtpFechaEstreno;
        CheckBox chkActiva;
        TextBox txtSinopsis;
        NumericUpDown nudPrecio;
        Label lblPrecio;
        Button btnOk;
        Button btnCancel;
        int? id;

        public MovieEditForm(int? id = null)
        {
            this.id = id;
            Text = id == null ? "Nueva Película" : "Editar Película";
            Width = 520; Height = 420; StartPosition = FormStartPosition.CenterParent;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Titulo
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Duracion
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Clasificacion
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // FechaEstreno
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Activa + Precio
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Sinopsis
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons

            var l1 = new Label { Text = "Título", Anchor = AnchorStyles.Left, AutoSize = true };
            txtTitulo = new TextBox { Dock = DockStyle.Fill };

            var l2 = new Label { Text = "Duración (min)", Anchor = AnchorStyles.Left, AutoSize = true };
            nudDuracion = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 90, Dock = DockStyle.Left, Width = 120 };

            var l3 = new Label { Text = "Clasificación", Anchor = AnchorStyles.Left, AutoSize = true };
            txtClasificacion = new TextBox { Dock = DockStyle.Left, Width = 160 };

            var l4 = new Label { Text = "Fecha estreno", Anchor = AnchorStyles.Left, AutoSize = true };
            dtpFechaEstreno = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140 };

            chkActiva = new CheckBox { Text = "Activa", Checked = true, AutoSize = true };

            lblPrecio = new Label { Text = "Precio", Anchor = AnchorStyles.Left, AutoSize = true };
            nudPrecio = new NumericUpDown { DecimalPlaces = 2, Maximum = 10000, Dock = DockStyle.Left, Width = 120 };

            var lSin = new Label { Text = "Sinopsis", Anchor = AnchorStyles.Left, AutoSize = true };
            txtSinopsis = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Height = 140, MaxLength = 500 };

            btnOk = new Button { Text = "OK", Width = 100, Height = 34 };
            btnCancel = new Button { Text = "Cancelar", Width = 100, Height = 34 };
            btnOk.Click += BtnOk_Click; btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
            buttons.Controls.Add(btnOk); buttons.Controls.Add(btnCancel);

            layout.Controls.Add(l1, 0, 0); layout.Controls.Add(txtTitulo, 1, 0);
            layout.Controls.Add(l2, 0, 1); layout.Controls.Add(nudDuracion, 1, 1);
            layout.Controls.Add(l3, 0, 2); layout.Controls.Add(txtClasificacion, 1, 2);
            layout.Controls.Add(l4, 0, 3); layout.Controls.Add(dtpFechaEstreno, 1, 3);

            // Activa + Precio in one row
            var panelActPrecio = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            panelActPrecio.Controls.Add(chkActiva);
            panelActPrecio.Controls.Add(lblPrecio);
            panelActPrecio.Controls.Add(nudPrecio);
            layout.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 4); layout.Controls.Add(panelActPrecio, 1, 4);

            layout.Controls.Add(lSin, 0, 5); layout.Controls.Add(txtSinopsis, 1, 5);
            layout.Controls.Add(buttons, 1, 6);

            Controls.Add(layout);
            Load += MovieEditForm_Load;
        }

        void MovieEditForm_Load(object s, EventArgs e)
        {
            var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
            lblPrecio.Visible = true; nudPrecio.Visible = true;

            if (id.HasValue)
            {
                using (var c = Db.NewConnection())
                {
                    c.Open();
                    // select columns that exist in the Pelicula schema
                    using (var cmd = new SqlCommand("SELECT Titulo, DuracionMin, Clasificacion, FechaEstreno, Activa, Sinopsis" + (hasPrecio ? ", ISNULL(Precio,0)" : "") + " FROM Pelicula WHERE PeliculaId=@Id", c))
                    {
                        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                txtTitulo.Text = rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0);
                                nudDuracion.Value = rdr.IsDBNull(1) ? nudDuracion.Minimum : rdr.GetInt32(1);
                                txtClasificacion.Text = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);
                                dtpFechaEstreno.Value = rdr.IsDBNull(3) ? DateTime.Today : rdr.GetDateTime(3);
                                chkActiva.Checked = rdr.IsDBNull(4) ? true : rdr.GetBoolean(4);
                                txtSinopsis.Text = rdr.IsDBNull(5) ? string.Empty : rdr.GetString(5);
                                if (hasPrecio) nudPrecio.Value = rdr.IsDBNull(6) ? 0m : rdr.GetDecimal(6);
                            }
                        }
                    }
                }
            }
            else
            {
                // defaults for new movie
                nudDuracion.Value = 90;
                dtpFechaEstreno.Value = DateTime.Today;
                chkActiva.Checked = true;
                nudPrecio.Value = 0m;
            }
        }

        void BtnOk_Click(object s, EventArgs e)
        {
            // Validate input
            var title = (txtTitulo.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show(this, "El título no puede estar vacío.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (nudDuracion.Value <= 0)
            {
                MessageBox.Show(this, "La duración debe ser mayor que cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var c = Db.NewConnection())
                {
                    c.Open();
                    var hasPrecio = Db.ColumnExists("Pelicula", "Precio");

                    // If the DB doesn't have Pelicula.Precio but the user entered a non-zero price, offer to create the column
                    if (!hasPrecio && nudPrecio.Value != 0m)
                    {
                        var res = MessageBox.Show(this, "La columna 'Pelicula.Precio' no existe en la base de datos. ¿Desea crearla para poder almacenar precios por película?", "Crear columna Precio", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            try
                            {
                                using (var cmdAlter = new SqlCommand("ALTER TABLE Pelicula ADD Precio DECIMAL(8,2) NOT NULL DEFAULT(0)", c))
                                {
                                    cmdAlter.ExecuteNonQuery();
                                }
                                hasPrecio = true; // now we can save the price
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(this, "No se pudo crear la columna Precio: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                hasPrecio = false;
                            }
                        }
                    }

                    int rowsAffected = 0;
                    if (id.HasValue)
                    {
                        // UPDATE Pelicula SET Titulo, DuracionMin, Clasificacion, FechaEstreno, Activa, Sinopsis [, Precio]
                        string updateSql = hasPrecio ? "UPDATE Pelicula SET Titulo=@T, DuracionMin=@D, Clasificacion=@C, FechaEstreno=@F, Activa=@A, Sinopsis=@S, Precio=@P WHERE PeliculaId=@Id"
                                                     : "UPDATE Pelicula SET Titulo=@T, DuracionMin=@D, Clasificacion=@C, FechaEstreno=@F, Activa=@A, Sinopsis=@S WHERE PeliculaId=@Id";
                        using (var cmd = new SqlCommand(updateSql, c))
                        {
                            cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = title });
                            cmd.Parameters.Add(new SqlParameter("@D", SqlDbType.Int) { Value = (int)nudDuracion.Value });
                            cmd.Parameters.Add(new SqlParameter("@C", SqlDbType.NVarChar, 20) { Value = (object)txtClasificacion.Text ?? DBNull.Value });
                            cmd.Parameters.Add(new SqlParameter("@F", SqlDbType.Date) { Value = dtpFechaEstreno.Value.Date });
                            cmd.Parameters.Add(new SqlParameter("@A", SqlDbType.Bit) { Value = chkActiva.Checked });
                            cmd.Parameters.Add(new SqlParameter("@S", SqlDbType.NVarChar, 500) { Value = (object)txtSinopsis.Text ?? DBNull.Value });
                            if (hasPrecio)
                            {
                                var p = new SqlParameter("@P", SqlDbType.Decimal) { Value = nudPrecio.Value };
                                p.Precision = 8; p.Scale = 2;
                                cmd.Parameters.Add(p);
                            }
                            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                            rowsAffected = cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // INSERT INTO Pelicula (Titulo, DuracionMin, Clasificacion, FechaEstreno, Activa, Sinopsis [, Precio]) VALUES (...)
                        string insertSql = hasPrecio ? "INSERT INTO Pelicula(Titulo,DuracionMin,Clasificacion,FechaEstreno,Activa,Sinopsis,Precio) VALUES(@T,@D,@C,@F,@A,@S,@P)"
                                                    : "INSERT INTO Pelicula(Titulo,DuracionMin,Clasificacion,FechaEstreno,Activa,Sinopsis) VALUES(@T,@D,@C,@F,@A,@S)";
                        using (var cmd = new SqlCommand(insertSql, c))
                        {
                            cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = title });
                            cmd.Parameters.Add(new SqlParameter("@D", SqlDbType.Int) { Value = (int)nudDuracion.Value });
                            cmd.Parameters.Add(new SqlParameter("@C", SqlDbType.NVarChar, 20) { Value = string.IsNullOrWhiteSpace(txtClasificacion.Text) ? (object)DBNull.Value : txtClasificacion.Text });
                            cmd.Parameters.Add(new SqlParameter("@F", SqlDbType.Date) { Value = dtpFechaEstreno.Value.Date });
                            cmd.Parameters.Add(new SqlParameter("@A", SqlDbType.Bit) { Value = chkActiva.Checked });
                            cmd.Parameters.Add(new SqlParameter("@S", SqlDbType.NVarChar, 500) { Value = string.IsNullOrWhiteSpace(txtSinopsis.Text) ? (object)DBNull.Value : txtSinopsis.Text });
                            if (hasPrecio)
                            {
                                var p = new SqlParameter("@P", SqlDbType.Decimal) { Value = nudPrecio.Value };
                                p.Precision = 8; p.Scale = 2;
                                cmd.Parameters.Add(p);
                            }
                            rowsAffected = cmd.ExecuteNonQuery();
                        }
                    }

                    // Log
                    try { File.AppendAllText("run_diagnostics.txt", $"[{DateTime.Now}] Movie save rowsAffected={rowsAffected}, Title={title}\n"); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("run_diagnostics.txt", $"[{DateTime.Now}] Movie save error: {ex}\n"); } catch { }
                MessageBox.Show(this, "Error al guardar la película: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
