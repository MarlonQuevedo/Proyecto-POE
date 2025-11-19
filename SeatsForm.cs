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
    FlowLayoutPanel panel;
    Button btnComprar;
    Button btnBack;
    Label lblInfo;
    DataGridView dgvAsientos;
    List<Button> seatButtons = new();
    HashSet<int> seleccionados = new();
    Panel bottomPanel;
    FlowLayoutPanel buttonsFlow;
    Label lblSelectedCount;
    bool allowPurchase = true;
        
        void SetButtonSelectedState(Button b, bool selected)
        {
            if (selected)
            {
                // Use a stronger blue and white text for clear selected state (matches admin)
                b.BackColor = Color.DodgerBlue;
                b.FlatAppearance.BorderSize = 2;
                b.FlatAppearance.BorderColor = Color.Black;
                b.ForeColor = Color.White;
            }
            else
            {
                b.BackColor = Color.LightGreen;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.Black;
                b.ForeColor = Color.Black;
            }
        }

        void ApplyRoundedRegion(Control c, int radius = 6)
        {
            try
            {
                var rect = new Rectangle(0, 0, c.Width, c.Height);
                using (var gp = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int d = radius * 2;
                    gp.AddArc(rect.X, rect.Y, d, d, 180, 90);
                    gp.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                    gp.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    gp.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                    gp.CloseAllFigures();
                    c.Region = new Region(gp);
                }
            }
            catch { }
        }

        void Inner_MouseEnter(object s, EventArgs e)
        {
            if (s is Button b && b.Enabled)
            {
                try
                {
                    // Slightly lighten the current color for hover
                    b.BackColor = System.Windows.Forms.ControlPaint.Light(b.BackColor);
                    b.Cursor = Cursors.Hand;
                }
                catch { }
            }
        }

        void Inner_MouseLeave(object s, EventArgs e)
        {
            if (s is Button b)
            {
                try
                {
                    // restore based on selection state
                    var id = (int)b.Tag;
                    SetButtonSelectedState(b, seleccionados.Contains(id));
                    b.Cursor = Cursors.Default;
                }
                catch { }
            }
        }

        void Inner_SizeChanged(object s, EventArgs e)
        {
            if (s is Control c) ApplyRoundedRegion(c, 6);
        }

        public SeatsForm(int id,string txt, bool allowPurchase = true)
        {
            funcionId=id; funcionTexto=txt;
            this.allowPurchase = allowPurchase;
            Text = "Asientos - " + txt;
            Width = 900; Height = 700;
            StartPosition=FormStartPosition.CenterParent;

            // Apply consistent UI theme
            UITheme.Apply(this);
            lblInfo = new Label { Left = 10, Top = 10, Width = 860, Text = txt, AutoSize = false, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
            lblInfo.Dock = DockStyle.Top;
            lblInfo.Font = new System.Drawing.Font(UITheme.AppFont.FontFamily, 14F, System.Drawing.FontStyle.Bold);
            // small separator to push seats lower so they don't overlap header
            var sep = new Panel { Height = 12, Dock = DockStyle.Top, BackColor = Color.Transparent };
            // Disable AutoScroll to remove the unnecessary scrollbar; layout uses docking and padding to keep buttons visible
            panel = new FlowLayoutPanel { Left = 10, Top = 80, Width = 860, Height = 560, AutoScroll = false, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            panel.Dock = DockStyle.Fill;
            dgvAsientos = new DataGridView { Left = 10, Top = 460, Width = 760, Height = 120, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom, ReadOnly = true, AutoGenerateColumns = true, Visible = false };
            // Buttons moved into a bottom panel to avoid overlap with the seat area
            bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.PanelBack }; 
            buttonsFlow = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 320, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10), WrapContents = false };
            // show Comprar button always (but enable only when purchases allowed) so it's visible in client view
            btnComprar = new Button { Width = 160, Height = 36, Text = "Comprar", Anchor = AnchorStyles.Right, Visible = true, Enabled = allowPurchase };
            btnComprar.FlatAppearance.MouseOverBackColor = UITheme.Accent;
            btnComprar.FlatAppearance.BorderColor = UITheme.Accent;
            btnComprar.ForeColor = Color.White;
            btnComprar.BackColor = UITheme.Accent;
            btnComprar.FlatStyle = FlatStyle.Flat;

            btnBack = new Button { Width = 120, Height = 36, Text = "Atrás", Anchor = AnchorStyles.Right };
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.BackColor = Color.White;
            btnBack.ForeColor = UITheme.ButtonFore;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            btnComprar.Click += BtnComprar_Click;

            // Add controls in an order that makes docking layout predictable
            Controls.Add(lblInfo);
            // ensure comprar is visible and enabled only when purchases are allowed
            btnComprar.Visible = true;
            btnComprar.Enabled = allowPurchase;
            // Add a small label showing how many seats are selected and the action buttons on the right
            lblSelectedCount = new Label { AutoSize = false, Text = "Seleccionados: 0", TextAlign = ContentAlignment.MiddleLeft, Width = 220, Height = 36, Margin = new Padding(12, 12, 12, 12), ForeColor = UITheme.ButtonFore };
            lblSelectedCount.Font = new System.Drawing.Font(UITheme.AppFont.FontFamily, 10F, System.Drawing.FontStyle.Regular);
            lblSelectedCount.Dock = DockStyle.Left;
            bottomPanel.Controls.Add(lblSelectedCount);
            // Add buttons into a right-to-left FlowLayout on the right so they never overlap and stay visible
            buttonsFlow.Controls.Add(btnBack);
            buttonsFlow.Controls.Add(btnComprar);
            bottomPanel.Controls.Add(buttonsFlow);
            // reserve padding inside panel so content doesn't get hidden behind bottomPanel
            // increase top padding so seats don't overlap header / title area (push seats further down)
            panel.Padding = new Padding(16, 60, 16, bottomPanel.Height + 12);
            Controls.Add(panel);
            Controls.Add(sep);
            Controls.Add(dgvAsientos);
            Controls.Add(bottomPanel);
            try { bottomPanel.BringToFront(); } catch { }
            Load+=SeatsForm_Load;
        }

    void SeatsForm_Load(object s,EventArgs e){Cargar();}

        void Cargar(){
            // Preserve user selections across UI refreshes
            panel.Controls.Clear(); seatButtons.Clear();
            try
            {
                var msg = $"SeatsForm: Cargar start - FuncionId={funcionId}\n";
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg);
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "run_diagnostics.txt"), msg);
            }
            catch { }
            // no manual positioning of btnBack here; layout is handled by bottom panel

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
                    try { var msg = "SeatsForm: no seats returned from DB - attempting to auto-generate seats\n"; File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg); File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "run_diagnostics.txt"), msg); } catch { }

                    // Attempt to determine SalaId/Capacidad and call sp_GenerarAsientosPorSala
                    try
                    {
                        using (var c2 = Db.NewConnection())
                        {
                            c2.Open();
                            int salaId = 0; int capacidad = 0;
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

                                try
                                {
                                    using (var gen = new SqlCommand("sp_GenerarAsientosPorSala", c2))
                                    {
                                        gen.CommandType = CommandType.StoredProcedure;
                                        gen.Parameters.Add(new SqlParameter("@SalaId", SqlDbType.Int) { Value = salaId });
                                        gen.Parameters.Add(new SqlParameter("@Filas", SqlDbType.Int) { Value = filas });
                                        gen.Parameters.Add(new SqlParameter("@Columnas", SqlDbType.Int) { Value = columnas });
                                        gen.ExecuteNonQuery();
                                    }
                                    try { var msg = $"SeatsForm: generated seats for SalaId={salaId} filas={filas} cols={columnas}\n"; File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg); File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "run_diagnostics.txt"), msg); } catch { }

                                    // Re-query asientos once
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
                                catch (SqlException ex)
                                {
                                    try { var msg = "SeatsForm: error generating seats: " + ex.Message + "\n"; File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg); File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "run_diagnostics.txt"), msg); } catch { }
                                }
                            }
                        }
                    }
                    catch { }

                    if (asientos.Count == 0)
                    {
                        var lbl = new Label { AutoSize = true, Text = "No hay asientos definidos para esta función.", ForeColor = Color.DarkRed, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleLeft };
                        panel.Controls.Add(lbl);
                        try { var msg = "SeatsForm: still no seats after generation attempt\n"; File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), msg); File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "run_diagnostics.txt"), msg); } catch { }
                        return;
                    }
                }

            // Try to arrange seats into rows by parsing the seat code (e.g. "A12" -> row 'A', column 12)
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;

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
                        // starts with digit, treat whole as column
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

            // If we successfully parsed rows (at least one non-empty rowKey) then group by row
            bool anyRowKeys = parsed.Exists(p => !string.IsNullOrEmpty(p.row));
            if (anyRowKeys)
            {
                var groups = new SortedDictionary<string, List<(SeatStatus st, int col, int order)>>();
                foreach (var p in parsed)
                {
                    var key = string.IsNullOrEmpty(p.row) ? "" : p.row;
                    if (!groups.ContainsKey(key)) groups[key] = new List<(SeatStatus, int, int)>();
                    groups[key].Add((p.st, p.col, p.order));
                }

                foreach (var g in groups)
                {
                    var rowPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true, Margin = new Padding(2) };
                    var items = g.Value;
                    items.Sort((a, b) =>
                    {
                        if (a.col != b.col) return a.col.CompareTo(b.col);
                        return a.order.CompareTo(b.order);
                    });
                    foreach (var it in items)
                    {
                        var st = it.st;
                        // outer panel to simulate a subtle shadow/border
                        var wrapper = new Panel { Width = UITheme.SeatButtonWidth, Height = UITheme.SeatButtonHeight, Margin = new Padding(6), BackColor = UITheme.Shadow };
                        var inner = new Button { Dock = DockStyle.Fill, Text = st.Asiento, Tag = st.AsientoId, Margin = new Padding(0) };
                        inner.UseVisualStyleBackColor = false;
                        inner.ForeColor = UITheme.ButtonFore;
                        inner.FlatStyle = FlatStyle.Flat;
                        inner.FlatAppearance.BorderSize = 1;
                        inner.FlatAppearance.BorderColor = Color.FromArgb(160,160,160);
                        inner.TabStop = false;
                        // make the inner button slightly smaller to show the shadow frame
                        inner.Padding = new Padding(4);
                        if (st.EstaOcupado)
                        {
                            inner.BackColor = Color.LightCoral; inner.Enabled = false;
                        }
                        else
                        {
                            inner.Click += Seat_Click;
                            SetButtonSelectedState(inner, seleccionados.Contains(st.AsientoId));
                            // hover and rounded effects
                            inner.MouseEnter += Inner_MouseEnter;
                            inner.MouseLeave += Inner_MouseLeave;
                        }
                        // always apply rounded region and size handler
                        inner.SizeChanged += Inner_SizeChanged;
                        ApplyRoundedRegion(inner, 6);
                        wrapper.Controls.Add(inner);
                        seatButtons.Add(inner);
                        rowPanel.Controls.Add(wrapper);
                    }
                    panel.Controls.Add(rowPanel);
                }
            }
            else
            {
                // Fallback: lay out seats in fixed-width rows (10 per row)
                int perRow = 10;
                var rowPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true, Margin = new Padding(2) };
                int count = 0;
                foreach (var st in asientos)
                {
                    var wrapper = new Panel { Width = UITheme.SeatButtonWidth, Height = UITheme.SeatButtonHeight, Margin = new Padding(6), BackColor = UITheme.Shadow };
                    var inner = new Button { Dock = DockStyle.Fill, Text = st.Asiento, Tag = st.AsientoId, Margin = new Padding(0) };
                    inner.UseVisualStyleBackColor = false;
                    inner.ForeColor = UITheme.ButtonFore;
                    inner.FlatStyle = FlatStyle.Flat;
                    inner.FlatAppearance.BorderSize = 1;
                    inner.FlatAppearance.BorderColor = Color.FromArgb(160,160,160);
                    inner.TabStop = false;
                    inner.Padding = new Padding(4);
                    if (st.EstaOcupado)
                    {
                        inner.BackColor = Color.LightCoral; inner.Enabled = false;
                    }
                    else
                    {
                        inner.Click += Seat_Click;
                        SetButtonSelectedState(inner, seleccionados.Contains(st.AsientoId));
                        // hover and rounded effects
                        inner.MouseEnter += Inner_MouseEnter;
                        inner.MouseLeave += Inner_MouseLeave;
                    }
                    inner.SizeChanged += Inner_SizeChanged;
                    ApplyRoundedRegion(inner, 6);
                    seatButtons.Add(inner);
                    wrapper.Controls.Add(inner);
                    rowPanel.Controls.Add(wrapper);
                    count++;
                    if (count % perRow == 0)
                    {
                        panel.Controls.Add(rowPanel);
                        rowPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
                    }
                }
                if (rowPanel.Controls.Count > 0) panel.Controls.Add(rowPanel);
            }

            // Make sure bottom panel stays visible and update selected count label
            try { lblSelectedCount.Text = $"Seleccionados: {seleccionados.Count}"; } catch { }
            try { bottomPanel.BringToFront(); } catch { }
        }

        void Seat_Click(object s,EventArgs e){
            var b=(Button)s;
            int id=(int)b.Tag;
            // Allow selection toggle in all modes (we want same behavior in client and admin)

            // Toggle selection
            if (seleccionados.Contains(id))
            {
                seleccionados.Remove(id);
                SetButtonSelectedState(b, false);
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), $"Seat_Click: deselected {id}\n"); } catch { }
                b.Refresh();
                try { panel.Invalidate(); panel.Update(); } catch { }
                try { lblSelectedCount.Text = $"Seleccionados: {seleccionados.Count}"; } catch { }
                try { bottomPanel.BringToFront(); } catch { }
            }
            else
            {
                seleccionados.Add(id);
                SetButtonSelectedState(b, true);
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run_diagnostics.txt"), $"Seat_Click: selected {id}\n"); } catch { }
                b.Refresh();
                try { panel.Invalidate(); panel.Update(); } catch { }
                try { lblSelectedCount.Text = $"Seleccionados: {seleccionados.Count}"; } catch { }
                try { bottomPanel.BringToFront(); } catch { }
            }
        }

        void BtnComprar_Click(object s,EventArgs e){
            if(seleccionados.Count==0){MessageBox.Show("Seleccione al menos un asiento.");return;}

            using(var c=Db.NewConnection()){
                c.Open();
                using(var t=c.BeginTransaction()){
                    try{
                        var ids=seleccionados.ToList();
                        var inClause=string.Join(",",ids);
                        var chk=$"SELECT COUNT(*) FROM AsientoOcupado WHERE FuncionId=@F AND AsientoId IN ({inClause})";
                        using(var cmd=new SqlCommand(chk,c,t)){
                            cmd.Parameters.Add(new SqlParameter("@F",SqlDbType.Int){Value=funcionId});
                            if((int)cmd.ExecuteScalar()>0){
                                t.Rollback();MessageBox.Show("Asiento ocupado.");Cargar();return;
                            }
                        }
                        int ventaId;
                        using(var cmd=new SqlCommand(
                          @"INSERT INTO Venta(FuncionId,ClienteId,Cantidad,Estado)
                            VALUES(@F,NULL,@C,'PAGADA');SELECT CAST(SCOPE_IDENTITY() AS INT);",
                          c,t)){
                            cmd.Parameters.Add(new SqlParameter("@F",SqlDbType.Int){Value=funcionId});
                            cmd.Parameters.Add(new SqlParameter("@C",SqlDbType.Int){Value=ids.Count});
                            ventaId=(int)cmd.ExecuteScalar();
                        }
                        foreach(var id in ids){
                            using(var cmd=new SqlCommand(
                              @"INSERT INTO AsientoOcupado(FuncionId,AsientoId,VentaId)
                                VALUES(@F,@A,@V);",c,t)){
                                cmd.Parameters.Add(new SqlParameter("@F",SqlDbType.Int){Value=funcionId});
                                cmd.Parameters.Add(new SqlParameter("@A",SqlDbType.Int){Value=id});
                                cmd.Parameters.Add(new SqlParameter("@V",SqlDbType.Int){Value=ventaId});
                                cmd.ExecuteNonQuery();
                            }
                        }
                        t.Commit();MessageBox.Show("Compra realizada.");Close();
                    } catch {t.Rollback();MessageBox.Show("Error.");}
                }
            }
        }
    }
}
