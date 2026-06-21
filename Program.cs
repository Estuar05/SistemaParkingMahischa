namespace SistemaParkingMahischa
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            try
            {
                Data.DatabaseInitializer.EnsureCreated();
                using var loginForm = new Forms.LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK && loginForm.AuthenticatedUser is { } user)
                {
                    if (user.MustChangePassword)
                    {
                        using var changePassword = new Forms.ChangePasswordForm(user);
                        if (changePassword.ShowDialog() != DialogResult.OK)
                        {
                            return; // No definió una nueva contraseña: no se permite el ingreso.
                        }
                    }

                    Services.BackupService.TryAutoBackup();
                    Application.Run(new Forms.MainForm(user));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo iniciar SistemaParkingMahischa.\n\nDetalle: {ex.Message}",
                    "Error de inicio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
