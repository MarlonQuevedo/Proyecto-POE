using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CineApp
{
    public class RoleSelectionForm : Form
    {
        public enum Role { None, Admin, Client }
        public Role SelectedRole { get; private set; } = Role.None;

        public RoleSelectionForm()
        {
            Text = "Seleccionar rol";
            Width = 640; Height = 220; StartPosition = FormStartPosition.CenterScreen;

            // Three-row layout: header, flexible spacer, actions (so actions sit centered vertically)
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lbl = new Label { Dock = DockStyle.Fill, Text = "Seleccione cómo desea usar la aplicación:", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
            lbl.Font = new Font(UITheme.AppFont.FontFamily, 13F, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(40, 40, 40);
            lbl.Margin = new Padding(6, 8, 6, 18);

            // actions will be placed inside a centered middle column so the buttons stay centered regardless of form width
            var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Anchor = AnchorStyles.None, BackColor = Color.Transparent };
            var btnAdmin = new Button { Width = 220, Height = 48, Text = "Cine (Administrador)" };
            var btnClient = new Button { Width = 180, Height = 48, Text = "Cliente" };
            var btnCancel = new Button { Width = 110, Height = 42, Text = "Salir" };

            // tidy margins for even spacing
            btnAdmin.Margin = new Padding(8);
            btnClient.Margin = new Padding(8);
            btnCancel.Margin = new Padding(8);

            // small helper to style buttons consistently
            void StyleButton(Button b, int radius = 10)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
                b.BackColor = UITheme.ButtonBack;
                b.ForeColor = UITheme.ButtonFore;
                b.Font = new Font(UITheme.AppFont.FontFamily, 10.5F, FontStyle.Regular);
                b.Cursor = Cursors.Hand;
                ApplyRoundedRegion(b, radius);
                b.MouseEnter += (s, e) => { b.BackColor = UITheme.Accent; b.ForeColor = Color.White; };
                b.MouseLeave += (s, e) => { b.BackColor = UITheme.ButtonBack; b.ForeColor = UITheme.ButtonFore; };
            }

            // rounded region helper
            void ApplyRoundedRegion(Control c, int radius)
            {
                var rect = new RectangleF(0, 0, c.Width, c.Height);
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                float d = radius * 2f;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
                path.CloseFigure();
                c.Region = new Region(path);
                c.SizeChanged += (s, e) => {
                    try { c.Region.Dispose(); } catch { }
                    var p = new System.Drawing.Drawing2D.GraphicsPath();
                    p.AddArc(0, 0, d, d, 180, 90);
                    p.AddArc(c.Width - d, 0, d, d, 270, 90);
                    p.AddArc(c.Width - d, c.Height - d, d, d, 0, 90);
                    p.AddArc(0, c.Height - d, d, d, 90, 90);
                    p.CloseFigure();
                    c.Region = new Region(p);
                };
            }

            // style buttons
            StyleButton(btnAdmin, 12);
            StyleButton(btnClient, 12);
            StyleButton(btnCancel, 10);

            btnAdmin.Click += (s, e) => { SelectedRole = Role.Admin; DialogResult = DialogResult.OK; Close(); };
            btnClient.Click += (s, e) => { SelectedRole = Role.Client; DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { SelectedRole = Role.None; DialogResult = DialogResult.Cancel; Close(); };

            actions.Controls.Add(btnAdmin);
            actions.Controls.Add(btnClient);
            actions.Controls.Add(btnCancel);

            // create a middle-row table to center the actions horizontally
            var bottomRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // put actions in the center column
            bottomRow.Controls.Add(actions, 1, 0);

            layout.Controls.Add(lbl, 0, 0);
            // place bottomRow in the last row so actions sit centered horizontally and vertically
            layout.Controls.Add(bottomRow, 0, 2);
            Controls.Add(layout);
            // Apply global theme for consistent look
            UITheme.Apply(this);
        }
    }
}
