using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CineApp
{
    public static class UITheme
    {
        public static readonly Font AppFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Color WindowBack = Color.FromArgb(250, 250, 250);
        public static readonly Color PanelBack = Color.FromArgb(245, 245, 245);
        public static readonly Color Accent = Color.FromArgb(30, 144, 255); // DodgerBlue
        public static readonly Color ButtonBack = Color.White;
        public static readonly Color ButtonFore = Color.FromArgb(30, 30, 30);
    public static readonly Color Shadow = Color.FromArgb(220, 220, 220);
    public const int SeatButtonWidth = 68;
    public const int SeatButtonHeight = 50;

        public static void Apply(Form f)
        {
            if (f == null) return;
            try
            {
                f.SuspendLayout();
                f.Font = AppFont;
                f.BackColor = WindowBack;
                // Walk direct child controls and apply sensible defaults
                foreach (Control c in f.Controls.Cast<Control>())
                {
                    ApplyControl(c);
                }
            }
            catch { }
            finally { try { f.ResumeLayout(); } catch { } }
        }

        static void ApplyControl(Control c)
        {
            if (c == null) return;
            try
            {
                // Common properties
                c.Font = AppFont;
                if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
                {
                    c.BackColor = PanelBack;
                }
                else
                {
                    // Keep label/other backgrounds as-is where appropriate
                }

                if (c is Button b)
                {
                    b.BackColor = ButtonBack;
                    b.ForeColor = ButtonFore;
                    b.FlatStyle = FlatStyle.Flat;
                    b.Height = Math.Max(30, b.Height);
                    b.FlatAppearance.BorderSize = 1;
                    b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                }

                if (c is DataGridView dgv)
                {
                    dgv.BackgroundColor = Color.White;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = PanelBack;
                    dgv.ColumnHeadersDefaultCellStyle.Font = AppFont;
                }

                // Recurse into children
                foreach (Control child in c.Controls.Cast<Control>()) ApplyControl(child);
            }
            catch { }
        }
    }
}
