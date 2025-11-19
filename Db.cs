using System;
using System.Configuration;
using System.Linq;
using System.Data.SqlClient;

namespace CineApp
{
	public static class Db
	{
		public static SqlConnection NewConnection()
		{
			var entry = ConfigurationManager.ConnectionStrings["CineDb"] ?? ConfigurationManager.ConnectionStrings["Cine"];
			if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
			{
				string available;
				try
				{
					var settings = ConfigurationManager.ConnectionStrings;
					if (settings != null && settings.Count > 0)
						available = string.Join(", ", settings.Cast<System.Configuration.ConnectionStringSettings>().Select(c => c.Name));
					else
						available = "<ninguna>";
				}
				catch
				{
					available = "<error al leer connectionStrings>";
				}
				throw new InvalidOperationException($"No se encontró la cadena de conexión 'CineDb' ni 'Cine'. Cadenas disponibles: {available}. Actualice App.config para incluir una con nombre 'CineDb' o 'Cine'.");
			}
			var cs = entry.ConnectionString;
			return new SqlConnection(cs);
		}

		public static bool ColumnExists(string tableName, string columnName)
		{
			try
			{
				using (var c = NewConnection())
				{
					c.Open();
					using (var cmd = new SqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME=@c", c))
					{
						cmd.Parameters.Add(new SqlParameter("@t", System.Data.SqlDbType.NVarChar) { Value = tableName });
						cmd.Parameters.Add(new SqlParameter("@c", System.Data.SqlDbType.NVarChar) { Value = columnName });
						var o = cmd.ExecuteScalar();
						return o != null;
					}
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
