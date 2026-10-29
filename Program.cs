using Pharma.Bll;
using Pharma.Dal;
using Pharma.Ui;

namespace Pharma;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            var database = new DatabaseInitializer();
            database.Initialize();
            var stock = new GestionStockService(database.ConnectionString);
            var auth = new AuthService(database.ConnectionString);
            var catalogue = new GestionCatalogueService(database.ConnectionString);
            var tickets = new GestionTicketService(database.ConnectionString);
            var rapports = new RapportService(database.ConnectionString);
            var caisse = new GestionCaisseService(database.ConnectionString);
            var parametres = new ParametresService(database.ConnectionString);
            var sauvegarde = new SauvegardeService(database.ConnectionString);
            var operationsStock = new GestionOperationsStockService(database.ConnectionString);

            // Diagnostic local : vérifie le compte initial sans afficher ni exporter le hash.
            if (args.Contains("--verifier-compte-initial", StringComparer.OrdinalIgnoreCase))
            {
                var resultat = auth.Authentifier("admin", "admin") is not null
                    ? "Compte initial présent : connexion admin/admin acceptée."
                    : "Connexion admin/admin refusée. Le compte admin de cette base existe déjà avec un autre mot de passe, ou doit être recréé.";
                var diagnosticPath = Path.Combine(DatabaseInitializer.ResolveDataDirectory(), "diagnostic-connexion.txt");
                File.WriteAllText(diagnosticPath, $"{resultat}\r\nBase SQLite : {database.DatabasePath}");
                return;
            }

            if (args.Contains("--acceptance-tests", StringComparer.OrdinalIgnoreCase))
            {
                Environment.ExitCode = Pharma.Tests.AcceptanceRunner.Run();
                return;
            }

            Application.Run(new FormLogin(auth, stock, catalogue, tickets, rapports, caisse, parametres, sauvegarde, operationsStock));
        }
        catch (Exception exception)
        {
            var logPath = Path.Combine(DatabaseInitializer.ResolveDataDirectory(), "pharma-startup-error.log");
            File.WriteAllText(logPath, $"{DateTime.Now:O}\r\n{exception}");
            MessageBox.Show($"Le démarrage de Pharma a échoué.\r\n\r\nLe détail est disponible dans :\r\n{logPath}\r\n\r\n{exception.Message}", "Erreur de démarrage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
