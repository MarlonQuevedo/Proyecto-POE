using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CineApp
{
    public static class Db
    {
        private static string connectionString =
            "Data Source=MSI\\SQLEXPRESS;Initial Catalog=Cine;Integrated Security=True";

        public static SqlConnection NewConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Películas activas para el grid de gestión
        public static DataTable ObtenerPeliculas()
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    string query =
                        "SELECT PeliculaId, Titulo, Clasificacion AS Genero, DuracionMin " +
                        "FROM Pelicula WHERE Activa = 1";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
            }
            return dt;
        }

        // Inserta película y devuelve su ID
        public static int GuardarPelicula(string titulo, string clasificacion, int duracion)
        {
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO Pelicula
                            (Titulo, Clasificacion, DuracionMin, FechaEstreno, Activa, Sinopsis)
                        VALUES
                            (@Titulo, @Clasificacion, @Duracion, GETDATE(), 1, 'Sin descripción');
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Titulo", titulo);
                        cmd.Parameters.AddWithValue("@Clasificacion", clasificacion);
                        cmd.Parameters.AddWithValue("@Duracion", duracion);
                        object result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
                return 0;
            }
        }

        // Actualiza solo los datos de la película
        public static void ActualizarPelicula(int id, string titulo, string clasificacion, int duracion)
        {
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    string query = @"
                        UPDATE Pelicula
                        SET Titulo = @Titulo,
                            Clasificacion = @Clasificacion,
                            DuracionMin = @Duracion
                        WHERE PeliculaId = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Titulo", titulo);
                        cmd.Parameters.AddWithValue("@Clasificacion", clasificacion);
                        cmd.Parameters.AddWithValue("@Duracion", duracion);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message);
            }
        }

        // Borrado lógico de película
        public static void EliminarPelicula(int id)
        {
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_EliminarPelicula", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PeliculaId", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message);
            }
        }

        public static bool ColumnExists(string tableName, string columnName)
        {
            using (var c = NewConnection())
            {
                c.Open();
                string sql = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @Table AND COLUMN_NAME = @Column";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("@Table", tableName);
                    cmd.Parameters.AddWithValue("@Column", columnName);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static DataTable ObtenerSalas()
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    string query =
                        "SELECT SalaId, Nombre FROM Sala WHERE Habilitada = 1 ORDER BY Nombre";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar salas: " + ex.Message);
            }
            return dt;
        }

        public static DataTable ObtenerPeliculasSinFuncion()
        {
            DataTable dt = new DataTable();
            try
            {
                using (var c = NewConnection())
                {
                    c.Open();
                    string sql = @"
                        SELECT p.PeliculaId, p.Titulo
                        FROM Pelicula p
                        WHERE p.Activa = 1
                          AND NOT EXISTS (
                              SELECT 1 FROM Funcion f
                              WHERE f.PeliculaId = p.PeliculaId
                          )";
                    using (var da = new SqlDataAdapter(sql, c))
                    {
                        da.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar películas sin función: " + ex.Message);
            }
            return dt;
        }

        // Chequea cruce de horarios para nueva función
        public static bool HayChoqueDeFuncion(int salaId, DateTime fechaHoraInicio, int duracionMin)
        {
            using (var c = NewConnection())
            {
                c.Open();
                string sql = @"
DECLARE @Inicio datetime2(0) = @InicioParam;
DECLARE @Fin    datetime2(0) = DATEADD(MINUTE, @Duracion, @InicioParam);

SELECT COUNT(*)
FROM Funcion f
JOIN Pelicula p ON p.PeliculaId = f.PeliculaId
WHERE f.SalaId = @SalaId
  AND p.Activa = 1
  AND (@Inicio < DATEADD(MINUTE, p.DuracionMin, f.FechaHoraInicio)
       AND f.FechaHoraInicio < @Fin);";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("@SalaId", salaId);
                    cmd.Parameters.AddWithValue("@InicioParam", fechaHoraInicio);
                    cmd.Parameters.AddWithValue("@Duracion", duracionMin);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Igual que el anterior pero excluyendo una función concreta (para edición)
        public static bool HayChoqueDeFuncionExcluyendo(int salaId, DateTime fechaHoraInicio, int duracionMin, int funcionIdExcluir)
        {
            using (var c = NewConnection())
            {
                c.Open();
                string sql = @"
DECLARE @Inicio datetime2(0) = @InicioParam;
DECLARE @Fin    datetime2(0) = DATEADD(MINUTE, @Duracion, @InicioParam);

SELECT COUNT(*)
FROM Funcion f
JOIN Pelicula p ON p.PeliculaId = f.PeliculaId
WHERE f.SalaId = @SalaId
  AND p.Activa = 1
  AND f.FuncionId <> @FuncionId
  AND (@Inicio < DATEADD(MINUTE, p.DuracionMin, f.FechaHoraInicio)
       AND f.FechaHoraInicio < @Fin);";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("@SalaId", salaId);
                    cmd.Parameters.AddWithValue("@InicioParam", fechaHoraInicio);
                    cmd.Parameters.AddWithValue("@Duracion", duracionMin);
                    cmd.Parameters.AddWithValue("@FuncionId", funcionIdExcluir);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Crea función para una película ya existente
        public static void CrearFuncionParaNuevaPelicula(
            int peliculaId,
            int salaId,
            DateTime fechaHoraInicio,
            decimal precio = 5.00m,
            string idioma = "Doblada",
            string formato = "2D")
        {
            try
            {
                using (SqlConnection conn = NewConnection())
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO Funcion
                            (PeliculaId, SalaId, FechaHoraInicio, Precio, Idioma, Formato)
                        VALUES
                            (@PeliculaId, @SalaId, @Inicio, @Precio, @Idioma, @Formato);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PeliculaId", peliculaId);
                        cmd.Parameters.AddWithValue("@SalaId", salaId);
                        cmd.Parameters.AddWithValue("@Inicio", fechaHoraInicio);
                        cmd.Parameters.AddWithValue("@Precio", precio);
                        cmd.Parameters.AddWithValue("@Idioma", idioma);
                        cmd.Parameters.AddWithValue("@Formato", formato);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al crear función: " + ex.Message);
            }
        }

        // Obtiene la función asociada a una película (si existe)
        public static bool TryObtenerFuncionDePelicula(int peliculaId, out int funcionId, out int salaId, out DateTime fechaHoraInicio)
        {
            funcionId = 0;
            salaId = 0;
            fechaHoraInicio = DateTime.MinValue;

            using (var c = NewConnection())
            {
                c.Open();
                string sql = @"
                    SELECT TOP 1 FuncionId, SalaId, FechaHoraInicio
                    FROM Funcion
                    WHERE PeliculaId = @PeliculaId
                    ORDER BY FechaHoraInicio";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("@PeliculaId", peliculaId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            funcionId = rdr.GetInt32(0);
                            salaId = rdr.GetInt32(1);
                            fechaHoraInicio = rdr.GetDateTime(2);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Actualiza sala y hora de una función concreta
        public static bool ActualizarHorarioFuncion(int funcionId, int salaIdNuevo, DateTime inicioNuevo, int duracionMin)
        {
            using (var c = NewConnection())
            {
                c.Open();

                if (HayChoqueDeFuncionExcluyendo(salaIdNuevo, inicioNuevo, duracionMin, funcionId))
                    return false;

                string sql = @"
                    UPDATE Funcion
                    SET SalaId = @SalaId,
                        FechaHoraInicio = @Inicio
                    WHERE FuncionId = @FuncionId";

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("@SalaId", salaIdNuevo);
                    cmd.Parameters.AddWithValue("@Inicio", inicioNuevo);
                    cmd.Parameters.AddWithValue("@FuncionId", funcionId);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }
    }
}
