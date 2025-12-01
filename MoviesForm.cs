using System;
using System.Data;
using System.Windows.Forms;

namespace CineApp
{
    public class MoviesForm : Form
    {
        TextBox txtTitle;
        TextBox txtGenre;
        TextBox txtDuration;
        ComboBox cboSala;
        DateTimePicker dtpInicio;
        DataGridView dgvMovies;
        Button btnSave;
        Button btnEliminar;
        Button btnBack;

        int? idPeliculaSeleccionada = null;

        public MoviesForm()
        {
            Text = "Gestión de Películas";
            Width = 900;
            Height = 550;
            StartPosition = FormStartPosition.CenterParent;

            InicializarControles();
            Load += MoviesForm_Load;
        }

        void InicializarControles()
        {
            Label lblTitle = new Label { Left = 30, Top = 40, Text = "Título:", AutoSize = true };
            txtTitle = new TextBox { Left = 150, Top = 35, Width = 200 };

            Label lblGenre = new Label { Left = 30, Top = 80, Text = "Clasificación:", AutoSize = true };
            txtGenre = new TextBox { Left = 150, Top = 75, Width = 200 };

            Label lblDuration = new Label { Left = 30, Top = 120, Text = "Duración (min):", AutoSize = true };
            txtDuration = new TextBox { Left = 150, Top = 115, Width = 200 };

            Label lblSala = new Label { Left = 30, Top = 160, Text = "Sala:", AutoSize = true };
            cboSala = new ComboBox
            {
                Left = 150,
                Top = 155,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblInicio = new Label { Left = 30, Top = 200, Text = "Fecha y hora función:", AutoSize = true };
            dtpInicio = new DateTimePicker
            {
                Left = 150,
                Top = 195,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm"
            };

            btnSave = new Button { Left = 30, Top = 240, Width = 100, Text = "Guardar" };
            btnEliminar = new Button { Left = 140, Top = 240, Width = 100, Text = "Eliminar", Enabled = false };
            btnBack = new Button { Left = 250, Top = 240, Width = 100, Text = "Volver" };

            btnSave.Click += BtnSave_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnBack.Click += (s, e) => Close();

            dgvMovies = new DataGridView
            {
                Left = 380,
                Top = 30,
                Width = 480,
                Height = 450,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvMovies.CellClick += DgvMovies_CellClick;

            Controls.Add(lblTitle);
            Controls.Add(txtTitle);
            Controls.Add(lblGenre);
            Controls.Add(txtGenre);
            Controls.Add(lblDuration);
            Controls.Add(txtDuration);
            Controls.Add(lblSala);
            Controls.Add(cboSala);
            Controls.Add(lblInicio);
            Controls.Add(dtpInicio);
            Controls.Add(btnSave);
            Controls.Add(btnEliminar);
            Controls.Add(btnBack);
            Controls.Add(dgvMovies);
        }

        void MoviesForm_Load(object sender, EventArgs e)
        {
            CargarSalas();
            CargarDatos();
        }

        void CargarSalas()
        {
            var salas = Db.ObtenerSalas();
            cboSala.DataSource = salas;
            cboSala.DisplayMember = "Nombre";
            cboSala.ValueMember = "SalaId";
        }

        void CargarDatos()
        {
            var dt = Db.ObtenerPeliculas();

            DataRow nueva = dt.NewRow();
            nueva["PeliculaId"] = DBNull.Value;
            nueva["Titulo"] = "Ingresar nueva ...";
            nueva["Genero"] = "";
            nueva["DuracionMin"] = DBNull.Value;
            dt.Rows.Add(nueva);

            dgvMovies.DataSource = dt;
            dgvMovies.Columns["PeliculaId"].HeaderText = "PeliculaId";
            dgvMovies.Columns["Titulo"].HeaderText = "Título";
            dgvMovies.Columns["Genero"].HeaderText = "Género";
            dgvMovies.Columns["DuracionMin"].HeaderText = "DuraciónMin";
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text) ||
                string.IsNullOrWhiteSpace(txtGenre.Text) ||
                string.IsNullOrWhiteSpace(txtDuration.Text))
            {
                MessageBox.Show("Completa título, clasificación y duración.");
                return;
            }

            if (!int.TryParse(txtDuration.Text, out int duracion) || duracion <= 0)
            {
                MessageBox.Show("La duración debe ser un número entero mayor que cero.");
                return;
            }

            if (cboSala.SelectedValue == null)
            {
                MessageBox.Show("Selecciona una sala.");
                return;
            }

            int salaId = Convert.ToInt32(cboSala.SelectedValue);
            DateTime inicio = dtpInicio.Value;

            // NUEVA PELÍCULA
            if (idPeliculaSeleccionada == null)
            {
                if (Db.HayChoqueDeFuncion(salaId, inicio, duracion))
                {
                    MessageBox.Show("Ya existe una función en esa sala cuyo horario se cruza con esta película.");
                    return;
                }

                int nuevaId = Db.GuardarPelicula(txtTitle.Text.Trim(), txtGenre.Text.Trim(), duracion);
                if (nuevaId > 0)
                {
                    Db.CrearFuncionParaNuevaPelicula(nuevaId, salaId, inicio);
                    MessageBox.Show("Película y función creadas.");
                }
            }
            else // EDITAR PELÍCULA Y SU FUNCIÓN
            {
                int peliculaId = idPeliculaSeleccionada.Value;

                if (Db.TryObtenerFuncionDePelicula(peliculaId, out int funcionId, out _, out _))
                {
                    if (Db.HayChoqueDeFuncionExcluyendo(salaId, inicio, duracion, funcionId))
                    {
                        MessageBox.Show("No se puede actualizar la función porque se cruza con otra en la misma sala.");
                        return;
                    }

                    Db.ActualizarPelicula(peliculaId, txtTitle.Text.Trim(), txtGenre.Text.Trim(), duracion);

                    if (!Db.ActualizarHorarioFuncion(funcionId, salaId, inicio, duracion))
                    {
                        MessageBox.Show("No se pudo actualizar la función.");
                        return;
                    }

                    MessageBox.Show("Película y función actualizadas.");
                }
                else
                {
                    if (Db.HayChoqueDeFuncion(salaId, inicio, duracion))
                    {
                        MessageBox.Show("Ya existe una función en esa sala cuyo horario se cruza con esta película.");
                        return;
                    }

                    Db.ActualizarPelicula(peliculaId, txtTitle.Text.Trim(), txtGenre.Text.Trim(), duracion);
                    Db.CrearFuncionParaNuevaPelicula(peliculaId, salaId, inicio);
                    MessageBox.Show("Película actualizada y función creada.");
                }
            }

            Limpiar();
            CargarDatos();
        }

        void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (idPeliculaSeleccionada == null)
            {
                MessageBox.Show("Selecciona una película para eliminar.");
                return;
            }

            if (MessageBox.Show("¿Seguro que deseas eliminar esta película?",
                    "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Db.EliminarPelicula(idPeliculaSeleccionada.Value);
                MessageBox.Show("Película eliminada (borrado lógico).");
                Limpiar();
                CargarDatos();
            }
        }

        void DgvMovies_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvMovies.Rows[e.RowIndex];

            if (row.Cells["PeliculaId"].Value == DBNull.Value)
            {
                Limpiar();
                return;
            }

            try
            {
                idPeliculaSeleccionada = Convert.ToInt32(row.Cells["PeliculaId"].Value);
                txtTitle.Text = row.Cells["Titulo"].Value?.ToString() ?? "";
                txtGenre.Text = row.Cells["Genero"].Value?.ToString() ?? "";
                txtDuration.Text = row.Cells["DuracionMin"].Value?.ToString() ?? "";

                // Cargar sala y hora de la función asociada (si existe)
                if (idPeliculaSeleccionada.HasValue &&
                    Db.TryObtenerFuncionDePelicula(idPeliculaSeleccionada.Value, out _, out int salaId, out DateTime inicio))
                {
                    if (cboSala.Items.Count > 0)
                    {
                        try { cboSala.SelectedValue = salaId; }
                        catch { }
                    }
                    dtpInicio.Value = inicio;
                }
                else
                {
                    if (cboSala.Items.Count > 0)
                        cboSala.SelectedIndex = 0;
                    dtpInicio.Value = DateTime.Now;
                }

                btnEliminar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al seleccionar: " + ex.Message);
            }
        }

        void Limpiar()
        {
            txtTitle.Clear();
            txtGenre.Clear();
            txtDuration.Clear();
            idPeliculaSeleccionada = null;
            btnEliminar.Enabled = false;

            if (cboSala.Items.Count > 0)
                cboSala.SelectedIndex = 0;

            dtpInicio.Value = DateTime.Now;
        }
    }
}
