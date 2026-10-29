using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

/// <summary>Catalogue filtrable, avec toutes les opérations d'écriture réservées au responsable.</summary>
public sealed class UC_Produits : UserControl
{
    private readonly GestionCatalogueService _catalogue;
    private readonly Utilisateur _utilisateur;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new() { Dock = DockStyle.Fill, PlaceholderText = "Référence ou désignation…" };
    private readonly ComboBox _categorie = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _disponibilite = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _archives = new() { Text = "Inclure les produits archivés", AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _compteur = new() { AutoSize = true, ForeColor = Color.FromArgb(92, 104, 116), Anchor = AnchorStyles.Right };

    public UC_Produits(GestionCatalogueService catalogue, Utilisateur utilisateur)
    {
        _catalogue = catalogue; _utilisateur = utilisateur;
        Dock = DockStyle.Fill; BackColor = Color.FromArgb(244, 247, 250); Font = new Font("Segoe UI", 9.5F);
        ConstruireInterface(); ChargerCategories(); Rafraichir();
    }

    private bool EstResponsable => _utilisateur.Role == RoleUtilisateur.Administrateur;

    private void ConstruireInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = "Catalogue des produits", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold), ForeColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);

        var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 2, BackColor = Color.White, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 2, 0, 10) };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 195)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        filters.Controls.Add(_search, 0, 0); filters.Controls.Add(new Label { Text = "Catégorie", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0); filters.Controls.Add(_categorie, 2, 0);
        filters.Controls.Add(new Label { Text = "Disponibilité", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 3, 0); filters.Controls.Add(_disponibilite, 4, 0); filters.Controls.Add(_archives, 5, 0);
        var ajouter = UiTheme.Button("＋  Ajouter"); ajouter.Dock = DockStyle.Fill; ajouter.Height = 36; ajouter.Click += (_, _) => Ajouter(); filters.Controls.Add(ajouter, 6, 0);
        _disponibilite.Items.AddRange(["Tous", "En stock", "Rupture", "Sous le seuil"]); _disponibilite.SelectedIndex = 0;
        _categorie.Items.Add("Toutes"); _categorie.SelectedIndex = 0;
        filters.Controls.Add(_compteur, 0, 1); filters.SetColumnSpan(_compteur, 7);
        if (!EstResponsable) { _archives.Visible = false; ajouter.Visible = false; }
        _search.TextChanged += (_, _) => Rafraichir(); _categorie.SelectedIndexChanged += (_, _) => Rafraichir(); _disponibilite.SelectedIndexChanged += (_, _) => Rafraichir(); _archives.CheckedChanged += (_, _) => Rafraichir();

        var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = Color.White, Margin = Padding.Empty };
        UiTheme.StyleGrid(_grid); _grid.AutoGenerateColumns = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        AjouterColonne("Reference", "Référence", 14); AjouterColonne("CodeBarres", "Code-barres", 13); AjouterColonne("Designation", "Désignation", 27); AjouterColonne("Categorie", "Catégorie", 14); AjouterColonne("StockVendable", "Stock vendable", 12); AjouterColonne("SeuilStockMinimum", "Seuil", 8); AjouterColonne("PrixVente", "Prix de vente", 12, "N0"); AjouterColonne("Statut", "Statut", 12);
        card.Controls.Add(_grid);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        if (EstResponsable)
        {
            var supprimer = BoutonAction("Supprimer définitivement", Color.FromArgb(103, 113, 124)); supprimer.Click += (_, _) => Supprimer();
            var archiver = BoutonAction("Archiver", Color.FromArgb(65, 91, 111)); archiver.Click += (_, _) => Archiver();
            var modifier = BoutonAction("Modifier", UiTheme.Navy); modifier.Click += (_, _) => Modifier();
            actions.Controls.AddRange([supprimer, archiver, modifier]);
        }
        card.Controls.Add(actions); root.Controls.Add(filters, 0, 1); root.Controls.Add(card, 0, 2); Controls.Add(root);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && EstResponsable) Modifier(); };
    }

    private void AjouterColonne(string prop, string title, float fill, string? format = null)
    {
        var col = new DataGridViewTextBoxColumn { Name=prop, DataPropertyName = prop, HeaderText = title, FillWeight = fill, SortMode = DataGridViewColumnSortMode.Automatic };
        if (format is not null) col.DefaultCellStyle.Format = format;
        _grid.Columns.Add(col);
    }
    private static Button BoutonAction(string text, Color color) => new() { Text = text, Width = 160, Height = 34, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(6, 0, 0, 0), Cursor = Cursors.Hand };

    private void ChargerCategories()
    {
        var selected = _categorie.SelectedItem?.ToString(); _categorie.Items.Clear(); _categorie.Items.Add("Toutes");
        foreach (var item in _catalogue.Categories()) _categorie.Items.Add(item);
        _categorie.SelectedItem = selected is not null && _categorie.Items.Contains(selected) ? selected : "Toutes";
    }

    private void Rafraichir()
    {
        try
        {
            var categorie = _categorie.SelectedItem?.ToString(); if (categorie == "Toutes") categorie = null;
            var rows = _catalogue.RechercherProduits(_search.Text, categorie, EstResponsable && _archives.Checked, _utilisateur.Role)
                .Where(x => _disponibilite.SelectedIndex switch { 1 => x.StockVendable > 0, 2 => x.StockVendable == 0, 3 => x.StockVendable > 0 && x.StockVendable <= x.Produit.SeuilStockMinimum, _ => true })
                .Select(x => new { x.Produit.Reference, x.Produit.CodeBarres, x.Produit.Designation, x.Produit.Categorie, x.StockVendable, x.Produit.SeuilStockMinimum, x.Produit.PrixVente, Statut = x.Produit.IsArchive ? "Archivé" : x.StockVendable == 0 ? "Rupture" : x.StockVendable <= x.Produit.SeuilStockMinimum ? "Stock faible" : "Disponible", x.Produit.Id }).ToList();
            _grid.DataSource = rows; _compteur.Text = $"{rows.Count} produit(s) affiché(s)";
            if (!EstResponsable && _grid.Columns.Contains("PrixVente")) _grid.Columns["PrixVente"].Visible = true;
            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
            if (_grid.Columns.Contains("Statut")) _grid.Columns["Statut"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        catch (Exception ex) { AfficherErreur(ex); }
    }

    private Produit? SelectionnerProduit() => _grid.CurrentRow?.DataBoundItem is not null ? _catalogue.Produits(_utilisateur.Role, true).FirstOrDefault(p => p.Id == Convert.ToInt64(_grid.CurrentRow.Cells["Id"].Value)) : null;

    private void Ajouter() { using var dialog = new ProduitDialog(); if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return; Executer(() => _catalogue.AjouterProduit(dialog.Produit, _utilisateur)); }
    private void Modifier() { var p = SelectionnerProduit(); if (p is null) return; using var dialog = new ProduitDialog(p); if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return; Executer(() => _catalogue.ModifierProduit(dialog.Produit, _utilisateur)); }
    private void Archiver()
    {
        var p = SelectionnerProduit(); if (p is null) return;
        if (MessageBox.Show($"Archiver « {p.Designation} » ? Son historique sera conservé.", "Confirmer l’archivage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Executer(() => _catalogue.ArchiverProduit(p.Id, _utilisateur));
    }
    private void Supprimer()
    {
        var p = SelectionnerProduit(); if (p is null) return;
        if (MessageBox.Show($"Supprimer définitivement « {p.Designation} » ? Cette action est réservée aux produits sans lot ni historique.", "Confirmer la suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { if (!_catalogue.SupprimerProduitSansHistorique(p.Id, _utilisateur)) MessageBox.Show("Suppression refusée : le produit est introuvable ou possède un lot/historique. Archivez-le pour conserver la traçabilité.", "Suppression impossible", MessageBoxButtons.OK, MessageBoxIcon.Information); Rafraichir(); }
        catch (Exception ex) { AfficherErreur(ex); }
    }
    private void Executer(Action action) { try { action(); ChargerCategories(); Rafraichir(); } catch (Exception ex) { AfficherErreur(ex); } }
    private static void AfficherErreur(Exception ex) => MessageBox.Show(ex.Message, "Opération impossible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}

internal sealed class ProduitDialog : Form
{
    private readonly TextBox _reference = new(), _codeBarres=new(), _designation = new(), _categorie = new();
    private readonly NumericUpDown _seuil = new() { Minimum=0,Maximum=1000000,DecimalPlaces=0,ThousandsSeparator=true };
    private readonly NumericUpDown _prix = new() { Minimum=0,Maximum=100000000,DecimalPlaces=0,ThousandsSeparator=true };
    private readonly Produit? _existant;
    public Produit Produit => new() { Id=_existant?.Id ?? 0,Reference=_reference.Text.Trim(),CodeBarres=_codeBarres.Text.Trim(),Designation=_designation.Text.Trim(),Categorie=_categorie.Text.Trim(),SeuilStockMinimum=(int)_seuil.Value,PrixVente=_prix.Value,IsArchive=_existant?.IsArchive ?? false };
    public ProduitDialog(Produit? produit = null)
    {
        _existant = produit; Text = produit is null ? "Nouveau produit" : "Modifier le produit"; StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MinimizeBox=false; MaximizeBox=false; ShowInTaskbar=false; ClientSize=new Size(520,400); BackColor=Color.White; Font=new Font("Segoe UI",10F);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=2,RowCount=7 }; layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        for(var i=0;i<6;i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        AjouterChamp(layout,0,"Référence *",_reference); AjouterChamp(layout,1,"Code-barres",_codeBarres); AjouterChamp(layout,2,"Désignation *",_designation); AjouterChamp(layout,3,"Catégorie",_categorie); AjouterChamp(layout,4,"Seuil minimum",_seuil); AjouterChamp(layout,5,"Prix de vente",_prix);
        if(produit is not null){_reference.Text=produit.Reference;_codeBarres.Text=produit.CodeBarres;_designation.Text=produit.Designation;_categorie.Text=produit.Categorie;_seuil.Value=produit.SeuilStockMinimum;_prix.Value=produit.PrixVente;}
        var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false}; var ok=UiTheme.Button("Enregistrer");ok.Width=130;ok.DialogResult=DialogResult.OK;ok.Click+=(_,e)=>{if(string.IsNullOrWhiteSpace(_reference.Text)||string.IsNullOrWhiteSpace(_designation.Text)){DialogResult=DialogResult.None;MessageBox.Show("La référence et la désignation sont obligatoires.","Vérification",MessageBoxButtons.OK,MessageBoxIcon.Information);}};var cancel=new Button{Text="Annuler",Width=100,Height=38,DialogResult=DialogResult.Cancel};footer.Controls.Add(ok);footer.Controls.Add(cancel);layout.Controls.Add(footer,0,5);layout.SetColumnSpan(footer,2);Controls.Add(layout);AcceptButton=ok;CancelButton=cancel;
    }
    private static void AjouterChamp(TableLayoutPanel panel,int row,string titre,Control champ){panel.Controls.Add(new Label{Text=titre,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=UiTheme.Navy},0,row);champ.Dock=DockStyle.Fill;panel.Controls.Add(champ,1,row);}
}
