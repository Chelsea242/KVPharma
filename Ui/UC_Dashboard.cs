using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

public sealed class UC_Dashboard : UserControl
{
    public event EventHandler? VenteDemandee;
    public event EventHandler? ProduitsDemandes;

    public UC_Dashboard(GestionStockService stock, Utilisateur? utilisateur = null)
    {
        Dock = DockStyle.Fill; BackColor = Color.FromArgb(244, 247, 250); Padding = new Padding(2);
        var alertes = stock.RecupererAlertesStock();
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));

        var titre = new Label { Text = "Vue d'ensemble  ·  ÉDITION PREMIUM", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold), ForeColor = UiTheme.Navy };
        layout.Controls.Add(titre, 0, 0);

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 8, 0, 14) };
        for (var i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cards.Controls.Add(CreerTuile("CHIFFRE D'AFFAIRES", $"{stock.ObtenirChiffreAffairesDuJour():N0} FCFA", "Aujourd'hui", UiTheme.Navy, Color.FromArgb(59, 112, 157)), 0, 0);
        var rupture = alertes.Count(x => x.Type == "Rupture"); var faible = alertes.Count(x => x.Type == "SousSeuil");
        cards.Controls.Add(CreerTuile("RUPTURES / STOCKS FAIBLES", $"{rupture} / {faible}", "Ruptures / seuil minimum atteint", UiTheme.Navy, Color.FromArgb(204, 139, 51)), 1, 0);
        cards.Controls.Add(CreerTuile("LOTS À SURVEILLER", alertes.Count(x => x.Type == "Peremption").ToString(), "Péremption sous 90 jours", UiTheme.Navy, Color.FromArgb(204, 139, 51)), 2, 0);
        layout.Controls.Add(cards, 0, 1);

        var quick = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = utilisateur?.Role == RoleUtilisateur.Administrateur ? 3 : 2, RowCount = 1, BackColor = Color.White, Padding = new Padding(20, 14, 20, 14), Margin = new Padding(0, 4, 0, 0) };
        for (var i=0;i<quick.ColumnCount;i++) quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, i==0 ? 54 : 23));
        quick.Controls.Add(new Label { Text = "Prêt à servir ?\nOuvrez la caisse pour démarrer une vente.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(76, 89, 102) }, 0, 0);
        var vente = UiTheme.Button("Nouvelle vente  →"); vente.Dock = DockStyle.Fill; vente.Margin = new Padding(8); vente.BackColor = UiTheme.Green; vente.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold); vente.Click += (_, e) => VenteDemandee?.Invoke(this, e); quick.Controls.Add(vente, 1, 0);
        if (utilisateur?.Role == RoleUtilisateur.Administrateur) { var ajouter = UiTheme.Button("＋  Ajouter produit"); ajouter.Dock=DockStyle.Fill; ajouter.Margin=new Padding(8); ajouter.Click+=(_,e)=>ProduitsDemandes?.Invoke(this,e); quick.Controls.Add(ajouter,2,0); }
        layout.Controls.Add(quick, 0, 2);
        Controls.Add(layout);
    }

    private static Control CreerTuile(string titre, string valeur, string detail, Color texte, Color bande)
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(226, 232, 238), Padding = new Padding(0, 0, 8, 0), Margin = new Padding(0, 0, 14, 0) };
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 2, RowCount = 3, Padding = new Padding(16, 12, 14, 12), Margin = Padding.Empty };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 5)); card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        var stripe = new Panel { Dock = DockStyle.Fill, BackColor = bande, Margin = new Padding(0, 0, 12, 0) };
        card.Controls.Add(stripe, 0, 0); card.SetRowSpan(stripe, 3);
        card.Controls.Add(new Label { Text = titre, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(97, 109, 121), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) }, 1, 0);
        card.Controls.Add(new Label { Text = valeur, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = texte, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), AutoEllipsis = true }, 1, 1);
        card.Controls.Add(new Label { Text = detail, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9F) }, 1, 2);
        outer.Controls.Add(card);
        return outer;
    }
}
