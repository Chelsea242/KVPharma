using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

public sealed class FormLogin : Form
{
    private readonly AuthService _auth;
    private readonly GestionStockService _stock;
    private readonly GestionCatalogueService _catalogue;
    private readonly GestionTicketService _tickets;
    private readonly RapportService _rapports;
    private readonly GestionCaisseService _caisse;
    private readonly ParametresService _parametres;
    private readonly SauvegardeService _sauvegarde;
    private readonly GestionOperationsStockService _operationsStock;
    private readonly TextBox _identifiant = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F) };
    private readonly TextBox _motDePasse = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F), UseSystemPasswordChar = true };
    private readonly Label _message = new() { Dock = DockStyle.Fill, ForeColor = Color.Firebrick, TextAlign = ContentAlignment.MiddleLeft };
    private readonly CheckBox _afficherMotDePasse = new() { Text = "Afficher le mot de passe", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(82, 94, 106), AutoSize = false };

    public FormLogin(AuthService auth, GestionStockService stock, GestionCatalogueService catalogue, GestionTicketService tickets, RapportService rapports, GestionCaisseService caisse, ParametresService parametres, SauvegardeService sauvegarde, GestionOperationsStockService operationsStock)
    {
        _auth = auth; _stock = stock; _catalogue=catalogue;_tickets=tickets;_rapports=rapports;_caisse=caisse;_parametres=parametres;_sauvegarde=sauvegarde;_operationsStock=operationsStock;
        Text = "Pharma | Connexion"; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 510); MinimumSize = new Size(520, 510); MaximumSize = new Size(520, 510);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; BackColor = Color.FromArgb(244, 247, 250); Font = new Font("Segoe UI", 10F);
        ConstruireInterface();
    }

    private void ConstruireInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(54, 26, 54, 24) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var entete = new Panel { Dock = DockStyle.Fill };
        entete.Controls.Add(new Label { Text = "PHARMA", Dock = DockStyle.Top, Height = 54, Font = new Font("Segoe UI", 25F, FontStyle.Bold), ForeColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleCenter });
        entete.Controls.Add(new Label { Text = "Gestion officinale  •  ÉDITION PREMIUM", Dock = DockStyle.Top, Height = 28, ForeColor = UiTheme.Green, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter });
        root.Controls.Add(entete, 0, 0);

        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20), ColumnCount = 1, RowCount = 6, Margin = Padding.Empty };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        card.Controls.Add(new Label { Text = "Connectez-vous", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), ForeColor = UiTheme.Navy }, 0, 0);
        card.Controls.Add(new Label { Text = "Identifiant", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Color.FromArgb(82, 94, 106) }, 0, 1); card.Controls.Add(_identifiant, 0, 2);
        card.Controls.Add(new Label { Text = "Mot de passe", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Color.FromArgb(82, 94, 106) }, 0, 3); card.Controls.Add(_motDePasse, 0, 4);
        _afficherMotDePasse.CheckedChanged += (_, _) => _motDePasse.UseSystemPasswordChar = !_afficherMotDePasse.Checked;
        card.Controls.Add(_afficherMotDePasse, 0, 5);

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.Controls.Add(card, 0, 0);
        var login = UiTheme.Button("Se connecter"); login.Dock = DockStyle.Fill; login.Margin = new Padding(0, 10, 0, 0); login.Click += Login;
        content.Controls.Add(login, 0, 1); AcceptButton = login;
        root.Controls.Add(content, 0, 1); root.Controls.Add(_message, 0, 2); Controls.Add(root);
    }

    private void Login(object? sender, EventArgs e)
    {
        try
        {
            var user = _auth.Authentifier(_identifiant.Text, _motDePasse.Text);
            if (user is null)
            {
                _message.ForeColor = Color.Firebrick;
                _message.Text = "Identifiant ou mot de passe incorrect.";
                return;
            }
            if (user.Role == RoleUtilisateur.Administrateur)
            {
                try { _sauvegarde.ExecuterSauvegardeQuotidienne(user, DateTime.Now); } catch { /* Une sauvegarde manuelle reste disponible dans Administration. */ }
            }
            Hide(); using var main = new FormMain(user, _stock,_catalogue,_tickets,_rapports,_caisse,_parametres,_sauvegarde,_operationsStock); main.ShowDialog(this); Show(); _motDePasse.Clear(); _message.Text = string.Empty;
        }
        catch { _message.Text = "Connexion impossible. Vérifiez la base de données et réessayez."; }
    }
}
