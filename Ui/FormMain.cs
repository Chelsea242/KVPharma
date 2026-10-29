using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

public sealed class FormMain : Form
{
    private readonly Utilisateur _utilisateur;
    private readonly GestionStockService _stockService;
    private readonly GestionCatalogueService _catalogueService;
    private readonly GestionTicketService _tickets;
    private readonly RapportService _rapports;
    private readonly GestionCaisseService _caisse;
    private readonly ParametresService _parametres;
    private readonly SauvegardeService _sauvegarde;
    private readonly GestionOperationsStockService _operationsStock;
    private readonly Panel _conteneurVue = new();
    private readonly Label _titreVue = new();
    private readonly FlowLayoutPanel _menuNavigation = new();
    private readonly Button _btnStocks = new(), _btnRapports = new(), _btnAdmin = new(), _btnProduits = new(), _btnVentes = new(), _btnDashboard = new();
    private Button? _boutonActif;

    private const int LargeurMenu = 238;
    private static readonly Color Navy = Color.FromArgb(23, 48, 75);
    private static readonly Color BleuClair = Color.FromArgb(48, 91, 132);
    private static readonly Color Fond = Color.FromArgb(244, 247, 250);
    private static readonly Color Vert = Color.FromArgb(46, 117, 98);

    public FormMain(Utilisateur utilisateur, GestionStockService stockService, GestionCatalogueService catalogueService, GestionTicketService tickets, RapportService rapports, GestionCaisseService caisse, ParametresService parametres, SauvegardeService sauvegarde, GestionOperationsStockService operationsStock)
    {
        _utilisateur = utilisateur ?? throw new ArgumentNullException(nameof(utilisateur));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _catalogueService = catalogueService ?? throw new ArgumentNullException(nameof(catalogueService));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _rapports = rapports ?? throw new ArgumentNullException(nameof(rapports));
        _caisse=caisse ?? throw new ArgumentNullException(nameof(caisse)); _parametres=parametres ?? throw new ArgumentNullException(nameof(parametres)); _sauvegarde=sauvegarde ?? throw new ArgumentNullException(nameof(sauvegarde)); _operationsStock=operationsStock ?? throw new ArgumentNullException(nameof(operationsStock));
        Text = "Pharma | Gestion officinale — Édition Premium";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1366, 768);
        WindowState = FormWindowState.Maximized;
        BackColor = Fond;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;
        ConstruireInterface();
        AppliquerDroitsUtilisateur(_utilisateur.Role.ToString());
        Naviguer(CreerDashboard(), "Tableau de bord", _btnDashboard);
    }

    private void ConstruireInterface()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LargeurMenu));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(ConstruireMenu(), 0, 0);

        var zone = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Fond, Margin = Padding.Empty };
        zone.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        zone.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 0, 26, 0), Margin = Padding.Empty };
        _titreVue.Dock = DockStyle.Left; _titreVue.AutoSize = false; _titreVue.Width = 520; _titreVue.Dock = DockStyle.Fill;
        _titreVue.TextAlign = ContentAlignment.MiddleLeft; _titreVue.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold); _titreVue.ForeColor = Navy;
        var who = new Label { Dock = DockStyle.Right, AutoSize = false, Width = 260, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(95, 108, 121), Text = $"{_utilisateur.Identifiant}   ·   {_utilisateur.Role}" };
        var accent = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Vert };
        header.Controls.Add(who); header.Controls.Add(_titreVue); header.Controls.Add(accent);
        _conteneurVue.Dock = DockStyle.Fill; _conteneurVue.BackColor = Fond; _conteneurVue.Padding = new Padding(22); _conteneurVue.Margin = Padding.Empty;
        zone.Controls.Add(header, 0, 0); zone.Controls.Add(_conteneurVue, 0, 1);
        shell.Controls.Add(zone, 1, 0);
        Controls.Add(shell);
    }

    private Control ConstruireMenu()
    {
        var menu = new Panel { Dock = DockStyle.Fill, BackColor = Navy, Padding = new Padding(14, 18, 14, 16), Margin = Padding.Empty };
        var brand = new Panel { Dock = DockStyle.Top, Height = 95 };
        brand.Controls.Add(new Label { Dock = DockStyle.Top, Height = 43, Text = "PHARMA", ForeColor = Color.White, Font = new Font("Segoe UI", 21F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        brand.Controls.Add(new Label { Dock = DockStyle.Top, Height = 26, Text = "Gestion officinale", ForeColor = Color.FromArgb(187, 204, 218), Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft });
        brand.Controls.Add(new Label { Dock = DockStyle.Top, Height = 20, Text = "ÉDITION PREMIUM  •  2026", ForeColor = Color.FromArgb(137, 190, 177), Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        menu.Controls.Add(brand);

        _menuNavigation.Dock = DockStyle.Top; _menuNavigation.AutoSize = true; _menuNavigation.FlowDirection = FlowDirection.TopDown; _menuNavigation.WrapContents = false; _menuNavigation.Margin = Padding.Empty; _menuNavigation.Padding = new Padding(0, 18, 0, 0);
        ConfigurerBouton(_btnDashboard, "Tableau de bord", () => Naviguer(CreerDashboard(), "Tableau de bord", _btnDashboard));
        ConfigurerBouton(_btnVentes, "Ventes et caisse", () => Naviguer(CreerVente(), "Ventes et caisse", _btnVentes));
        ConfigurerBouton(_btnProduits, "Produits", () => Naviguer(CreerProduits(), "Produits", _btnProduits));
        ConfigurerBouton(_btnStocks, "Stocks / fournisseurs", () => Naviguer(CreerStocks(), "Stocks / fournisseurs", _btnStocks));
        ConfigurerBouton(_btnRapports, "Rapports", () => Naviguer(CreerRapports(), "Rapports", _btnRapports));
        ConfigurerBouton(_btnAdmin, "Administration", () => Naviguer(new UC_Administration(_utilisateur,_caisse,_parametres,_sauvegarde), "Administration", _btnAdmin));
        _menuNavigation.Controls.AddRange([_btnDashboard, _btnVentes, _btnProduits, _btnStocks, _btnRapports, _btnAdmin]);
        menu.Controls.Add(_menuNavigation);
        menu.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Pharma  ·  SQLite", ForeColor = Color.FromArgb(160, 181, 199), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5F) });
        return menu;
    }

    private static void ConfigurerBouton(Button button, string text, Action click)
    {
        button.Text = text; button.Width = LargeurMenu - 28; button.Height = 47; button.Margin = new Padding(0, 3, 0, 3);
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = BleuClair;
        button.BackColor = Navy; button.ForeColor = Color.FromArgb(238, 243, 247); button.Font = new Font("Segoe UI", 10F);
        button.TextAlign = ContentAlignment.MiddleLeft; button.Padding = new Padding(14, 0, 6, 0); button.Cursor = Cursors.Hand;
        button.Click += (_, _) => click();
    }

    public void AppliquerDroitsUtilisateur(string role)
    {
        var admin = string.Equals(role, nameof(RoleUtilisateur.Administrateur), StringComparison.OrdinalIgnoreCase);
        _btnStocks.Visible = _btnStocks.Enabled = admin;
        _btnRapports.Visible = _btnRapports.Enabled = admin;
        _btnAdmin.Visible = _btnAdmin.Enabled = admin;
        if (!admin && ReferenceEquals(_boutonActif, _btnStocks) || !admin && ReferenceEquals(_boutonActif, _btnRapports))
            Naviguer(CreerDashboard(), "Tableau de bord", _btnDashboard);
    }

    public void AppliquerDroits(string role) => AppliquerDroitsUtilisateur(role);

    private void Naviguer(Control vue, string titre, Button bouton)
    {
        var ancienne = _conteneurVue.Controls.Count > 0 ? _conteneurVue.Controls[0] : null;
        _conteneurVue.SuspendLayout();
        _conteneurVue.Controls.Clear();
        ancienne?.Dispose();
        vue.Dock = DockStyle.Fill;
        _conteneurVue.Controls.Add(vue);
        _conteneurVue.ResumeLayout(true);
        _titreVue.Text = titre;
        if (_boutonActif is not null) { _boutonActif.BackColor = Navy; _boutonActif.Font = new Font("Segoe UI", 10F); }
        _boutonActif = bouton; bouton.BackColor = Vert; bouton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
    }

    private UC_Dashboard CreerDashboard()
    {
        var dashboard = new UC_Dashboard(_stockService, _utilisateur);
        dashboard.VenteDemandee += (_, _) => Naviguer(CreerVente(), "Ventes et caisse", _btnVentes);
        dashboard.ProduitsDemandes += (_, _) => Naviguer(CreerProduits(), "Produits", _btnProduits);
        return dashboard;
    }

    private UC_Produits CreerProduits() => new(_catalogueService, _utilisateur);
    private Control CreerStocks() => new UC_Stocks(_catalogueService, _utilisateur, _stockService, _operationsStock);
    private Control CreerRapports() => new UC_Rapports(_rapports, _utilisateur);
    private UC_Vente CreerVente() => new(_stockService,_catalogueService,_tickets,_caisse,_utilisateur);

}
