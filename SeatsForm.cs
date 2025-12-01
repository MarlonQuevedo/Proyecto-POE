using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CineApp
{
    public class SeatsForm : Form
    {
        int funcionId;
        string funcionTexto;

        FlowLayoutPanel panelSeats;
        Panel bottomPanel;
        Button btnComprar;
        Button btnBack;
        Label lblInfo;

        List<Button> seatButtons = new();
        HashSet<int> seleccionados = new();
        bool allowPurchase = true;

        public SeatsForm(int id, string txt, bool allowPurchase = true)
        {
            funcionId = id;
            funcionTexto = txt;
            this.allowPurchase = allowPurchase;

            Text = "Asientos - " + txt;
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(600, 400);

            // Label superior
            lblInfo = new Label
            {
                Text = txt,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // Panel central de asientos
            panelSeats = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };

            // Panel inferior con botones
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            btnComprar = new Button
            {
                Text = "Comprar",
                Width = 120,
                Height = 30,
                Left = 10,
                Top = 10,
                Visible = allowPurchase
            };
            btnComprar.Click += BtnComprar_Click;

            btnBack = new Button
            {
                Text = "Atrás",
                Width = 120,
                Height = 30,
                Top = 10
            };
            btnBack.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBack.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            bottomPanel.Controls.Add(btnComprar);
            bottomPanel.Controls.Add(btnBack);
            bottomPanel.Resize += (s, e) =>
            {
                btnBack.Left = bottomPanel.ClientSize.Width - btnBack.Width - 10;
            };

            Controls.Add(panelSeats);
            Controls.Add(bottomPanel);
            Controls.Add(lblInfo);

            Load += SeatsForm_Load;
        }

        void SeatsForm_Load(object s, EventArgs e)
        {
            Cargar();
        }

        void Cargar()
        {
            panelSeats.Controls.Clear();
            seatButtons.Clear();
            seleccionados.Clear();

            try
            {
                var msg = $"SeatsForm: Cargar start - FuncionId={funcionId}\n";
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg);
            }
            catch { }

            List<SeatStatus> asientos = new List<SeatStatus>();

            using (var c = Db.NewConnection())
            {
                c.Open();
                var sql = @"
SELECT a.AsientoId,a.Codigo AS Asiento,
CASE WHEN ao.AsientoId IS NULL THEN 0 ELSE 1 END AS Ocupado
FROM Asiento a
JOIN Sala s ON s.SalaId=a.SalaId
JOIN Funcion f ON f.SalaId=s.SalaId
LEFT JOIN AsientoOcupado ao ON ao.AsientoId=a.AsientoId AND ao.FuncionId=f.FuncionId
WHERE f.FuncionId=@FuncionId
ORDER BY a.Codigo";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.Parameters.Add(new SqlParameter("@FuncionId", SqlDbType.Int) { Value = funcionId });
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            asientos.Add(new SeatStatus
                            {
                                AsientoId = rdr.GetInt32(0),
                                Asiento = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                                Ocupado = rdr.GetInt32(2)
                            });
                        }
                    }
                }
            }

            if (asientos.Count == 0)
            {
                try
                {
                    var msg = "SeatsForm: no seats returned from DB - attempting to auto-generate seats\n";
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg);
                }
                catch { }

                try
                {
                    using (var c2 = Db.NewConnection())
                    {
                        c2.Open();
                        int salaId = 0;
                        int capacidad = 0;

                        using (var cmd2 = new SqlCommand("SELECT SalaId FROM Funcion WHERE FuncionId=@F", c2))
                        {
                            cmd2.Parameters.Add(new SqlParameter("@F", SqlDbType.Int) { Value = funcionId });
                            var obj = cmd2.ExecuteScalar();
                            if (obj != null && obj != DBNull.Value) salaId = Convert.ToInt32(obj);
                        }

                        if (salaId > 0)
                        {
                            using (var cmd3 = new SqlCommand("SELECT Capacidad FROM Sala WHERE SalaId=@S", c2))
                            {
                                cmd3.Parameters.Add(new SqlParameter("@S", SqlDbType.Int) { Value = salaId });
                                var obj2 = cmd3.ExecuteScalar();
                                if (obj2 != null && obj2 != DBNull.Value) capacidad = Convert.ToInt32(obj2);
                            }

                            if (capacidad <= 0) capacidad = 50;
                            int columnas = 10;
                            int filas = (int)Math.Ceiling((double)capacidad / columnas);

                            using (var gen = new SqlCommand("sp_GenerarAsientosPorSala", c2))
                            {
                                gen.CommandType = CommandType.StoredProcedure;
                                gen.Parameters.Add(new SqlParameter("@SalaId", SqlDbType.Int) { Value = salaId });
                                gen.Parameters.Add(new SqlParameter("@Filas", SqlDbType.Int) { Value = filas });
                                gen.Parameters.Add(new SqlParameter("@Columnas", SqlDbType.Int) { Value = columnas });
                                gen.ExecuteNonQuery();
                            }

                            asientos.Clear();
                            using (var cmd = new SqlCommand(@"
SELECT a.AsientoId,a.Codigo AS Asiento,
CASE WHEN ao.AsientoId IS NULL THEN 0 ELSE 1 END AS Ocupado
FROM Asiento a
JOIN Sala s ON s.SalaId=a.SalaId
JOIN Funcion f ON f.SalaId=s.SalaId
LEFT JOIN AsientoOcupado ao ON ao.AsientoId=a.AsientoId AND ao.FuncionId=f.FuncionId
WHERE f.FuncionId=@FuncionId
ORDER BY a.Codigo", c2))
                            {
                                cmd.Parameters.Add(new SqlParameter("@FuncionId", SqlDbType.Int) { Value = funcionId });
                                using (var rdr = cmd.ExecuteReader())
                                {
                                    while (rdr.Read())
                                    {
                                        asientos.Add(new SeatStatus
                                        {
                                            AsientoId = rdr.GetInt32(0),
                                            Asiento = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                                            Ocupado = rdr.GetInt32(2)
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                if (asientos.Count == 0)
                {
                    var lbl = new Label
                    {
                        AutoSize = true,
                        Text = "No hay asientos definidos para esta función.",
                        ForeColor = Color.DarkRed,
                        Dock = DockStyle.Top,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    panelSeats.Controls.Add(lbl);
                    return;
                }
            }

            panelSeats.FlowDirection = FlowDirection.TopDown;
            panelSeats.WrapContents = false;

            var parsed = new List<(SeatStatus st, string row, int col, int order)>();
            int ord = 0;

            foreach (var st in asientos)
            {
                var code = (st.Asiento ?? string.Empty).Trim();
                string rowKey = string.Empty;
                int col = 0;

                if (!string.IsNullOrEmpty(code))
                {
                    int i = 0;
                    while (i < code.Length && !char.IsDigit(code[i])) i++;

                    if (i == 0)
                    {
                        rowKey = "";
                        int.TryParse(code, out col);
                    }
                    else if (i >= code.Length)
                    {
                        rowKey = code;
                        col = 0;
                    }
                    else
                    {
                        rowKey = code.Substring(0, i);
                        var num = code.Substring(i);
                        if (!int.TryParse(num, out col)) col = 0;
                    }
                }

                parsed.Add((st, rowKey, col, ord++));
            }

            bool anyRowKeys = parsed.Exists(p => !string.IsNullOrEmpty(p.row));

            if (anyRowKeys)
            {
                var groups = new SortedDictionary<string, List<(SeatStatus st, int col, int order)>>();

                foreach (var p in parsed)
                {
                    var key = string.IsNullOrEmpty(p.row) ? "" : p.row;
                    if (!groups.ContainsKey(key))
                        groups[key] = new List<(SeatStatus, int, int)>();
                    groups[key].Add((p.st, p.col, p.order));
                }

                foreach (var g in groups)
                {
                    var rowPanel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.LeftToRight,
                        AutoSize = true,
                        WrapContents = true,
                        Margin = new Padding(2)
                    };

                    var items = g.Value;
                    items.Sort((a, b) =>
                    {
                        if (a.col != b.col) return a.col.CompareTo(b.col);
                        return a.order.CompareTo(b.order);
                    });

                    foreach (var it in items)
                    {
                        var st = it.st;
                        var b = CrearBotonAsiento(st);
                        rowPanel.Controls.Add(b);
                    }

                    panelSeats.Controls.Add(rowPanel);
                }
            }
            else
            {
                int perRow = 10;
                var rowPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    WrapContents = true,
                    Margin = new Padding(2)
                };
                int count = 0;

                foreach (var st in asientos)
                {
                    var b = CrearBotonAsiento(st);
                    rowPanel.Controls.Add(b);
                    count++;
                    if (count % perRow == 0)
                    {
                        panelSeats.Controls.Add(rowPanel);
                        rowPanel = new FlowLayoutPanel
                        {
                            FlowDirection = FlowDirection.LeftToRight,
                            AutoSize = true,
                            WrapContents = true,
                            Margin = new Padding(2)
                        };
                    }
                }

                if (rowPanel.Controls.Count > 0)
                    panelSeats.Controls.Add(rowPanel);
            }
        }

        Button CrearBotonAsiento(SeatStatus st)
        {
            var b = new Button
            {
                Width = 60,
                Height = 44,
                Text = st.Asiento,
                Tag = st.AsientoId,
                Margin = new Padding(6),
                UseVisualStyleBackColor = false,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderSize = 1;

            if (st.EstaOcupado)
            {
                b.BackColor = Color.LightCoral;
                b.Enabled = false;
            }
            else
            {
                b.BackColor = Color.LightGreen;
                if (allowPurchase)
                    b.Click += Seat_Click;
            }

            seatButtons.Add(b);
            return b;
        }

        void Seat_Click(object s, EventArgs e)
        {
            var b = (Button)s;
            int id = (int)b.Tag;

            if (seleccionados.Contains(id))
            {
                seleccionados.Remove(id);
                b.BackColor = Color.LightGreen;
            }
            else
            {
                seleccionados.Add(id);
                b.BackColor = Color.LightSkyBlue;
            }
        }

        void BtnComprar_Click(object s, EventArgs e)
        {
            if (!allowPurchase) return;

            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un asiento.");
                return;
            }

            using (var c = Db.NewConnection())
            {
                c.Open();
                using (var t = c.BeginTransaction())
                {
                    try
                    {
                        var ids = seleccionados.ToList();
                        var inClause = string.Join(",", ids);
                        var chk = $"SELECT COUNT(*) FROM AsientoOcupado WHERE FuncionId=@F AND AsientoId IN ({inClause})";

                        using (var cmd = new SqlCommand(chk, c, t))
                        {
                            cmd.Parameters.Add(new SqlParameter("@F", SqlDbType.Int) { Value = funcionId });
                            if ((int)cmd.ExecuteScalar() > 0)
                            {
                                t.Rollback();
                                MessageBox.Show("Alguno de los asientos seleccionados ya fue ocupado. Actualizando vista.");
                                Cargar();
                                return;
                            }
                        }

                        int ventaId;
                        using (var cmd = new SqlCommand(
                            @"INSERT INTO Venta(FuncionId,ClienteId,Cantidad,Estado)
                              VALUES(@F,NULL,@C,'PAGADA');
                              SELECT CAST(SCOPE_IDENTITY() AS INT);", c, t))
                        {
                            cmd.Parameters.Add(new SqlParameter("@F", SqlDbType.Int) { Value = funcionId });
                            cmd.Parameters.Add(new SqlParameter("@C", SqlDbType.Int) { Value = ids.Count });
                            ventaId = (int)cmd.ExecuteScalar();
                        }

                        foreach (var id in ids)
                        {
                            using (var cmd = new SqlCommand(
                                @"INSERT INTO AsientoOcupado(FuncionId,AsientoId,VentaId)
                                  VALUES(@F,@A,@V);", c, t))
                            {
                                cmd.Parameters.Add(new SqlParameter("@F", SqlDbType.Int) { Value = funcionId });
                                cmd.Parameters.Add(new SqlParameter("@A", SqlDbType.Int) { Value = id });
                                cmd.Parameters.Add(new SqlParameter("@V", SqlDbType.Int) { Value = ventaId });
                                cmd.ExecuteNonQuery();
                            }
                        }

                        t.Commit();
                        MessageBox.Show("Compra realizada.");
                        Close();
                    }
                    catch
                    {
                        t.Rollback();
                        MessageBox.Show("Error al realizar la compra.");
                    }
                }
            }
        }
    }
}
