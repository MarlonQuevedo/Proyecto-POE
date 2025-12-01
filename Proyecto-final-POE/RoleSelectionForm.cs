using System;
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
            Width = 360; Height = 160; StartPosition = FormStartPosition.CenterScreen;
            var lbl = new Label { Left = 10, Top = 10, Width = 320, Text = "Seleccione cómo desea usar la aplicación:" };
            var btnAdmin = new Button { Left = 20, Top = 40, Width = 140, Text = "Cine (Administrador)" };
            var btnClient = new Button { Left = 180, Top = 40, Width = 140, Text = "Cliente" };
            var btnCancel = new Button { Left = 120, Top = 80, Width = 100, Text = "Salir" };

            btnAdmin.Click += (s, e) => { SelectedRole = Role.Admin; DialogResult = DialogResult.OK; Close(); };
            btnClient.Click += (s, e) => { SelectedRole = Role.Client; DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { SelectedRole = Role.None; DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lbl);
            Controls.Add(btnAdmin);
            Controls.Add(btnClient);
            Controls.Add(btnCancel);
        }
    }
}
