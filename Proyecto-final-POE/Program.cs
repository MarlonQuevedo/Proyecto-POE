using CineApp;
using System;
using System.Windows.Forms;

namespace CineApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) =>
            {
                try
                {
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                    System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("s") + " - UI ThreadException:\n" + e.Exception + "\n\n");
                }
                catch { }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                    System.IO.File.AppendAllText(
                        logPath,
                        DateTime.Now.ToString("s") + " - UnhandledException:\n" +
                        (ex != null ? ex.ToString() : e.ExceptionObject.ToString()) + "\n\n"
                    );
                }
                catch { }
            };

            using (var roleForm = new RoleSelectionForm())
            {
                var dr = roleForm.ShowDialog();
                if (dr != DialogResult.OK) return;

                bool ok = TestDatabase(out string info);
                if (!ok)
                {
                    var ask = MessageBox.Show(
                        "Error al conectar/consultar la base de datos:\n" + info + "\n\n¿Desea continuar sin conexión?",
                        "Error BD",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (ask == DialogResult.No) return;
                }
                else
                {
                    MessageBox.Show(info, "Comprobación BD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (roleForm.SelectedRole == RoleSelectionForm.Role.Admin)
                {
                    Application.Run(new MainForm());
                }
                else if (roleForm.SelectedRole == RoleSelectionForm.Role.Client)
                {
                    Application.Run(new ClientForm());
                }
            }
        }

        static bool TestDatabase(out string info)
        {
            try
            {
                using (var c = Db.NewConnection())
                {
                    c.Open();

                    try
                    {
                        var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                        var infoLog = $"TestDatabase open: conn={(c == null ? "<null>" : c.State.ToString())}; connCSLen={(c?.ConnectionString?.Length ?? 0)}\n";
                        System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("s") + " - " + infoLog);

                        try
                        {
                            var projRoot = System.IO.Path.GetFullPath(
                                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"));
                            var alt = System.IO.Path.Combine(projRoot, "run_diagnostics.txt");
                            System.IO.File.AppendAllText(alt, DateTime.Now.ToString("s") + " - " + infoLog);
                        }
                        catch { }
                    }
                    catch { }

                    info = $"Conexión OK. Estado de conexión: {c.State}";
                    return true;
                }
            }
            catch (Exception ex)
            {
                info = ex.ToString();
                return false;
            }
        }
    }
}
