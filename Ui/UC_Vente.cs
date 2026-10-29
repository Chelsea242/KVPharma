using Pharma.Bll;
using Pharma.Models;
using System.Drawing.Printing;

namespace Pharma.Ui;

/// Vue comptoir : recherche clavier/code-barres à gauche et panier à droite.
public sealed class UC_Vente : UserControl
{
    private readonly GestionStockService _stock;
    private readonly Utilisateur _utilisateur;
    private readonly GestionCatalogueService _catalogue;
    private readonly GestionTicketService _tickets;
    private readonly GestionCaisseService _caisse;
    private readonly SessionCaisse _session;
    private readonly TextBox _recherche = new() { Dock = DockStyle.Top, Height = 34, PlaceholderText = "Référence, désignation ou code-barres" };
    private readonly Button _ajouterPanier = UiTheme.Button("Ajouter au panier");
    private readonly DataGridView _resultats = new();
    private readonly DataGridView _panier = new();
    private readonly NumericUpDown _quantite = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 90 };
    private readonly NumericUpDown _prix = new() { Minimum = 0, Maximum = 100000000, DecimalPlaces = 0, Width = 120, ReadOnly = true, ThousandsSeparator = true };
    private readonly NumericUpDown _montantRecu = new() { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Width = 120 };
    private readonly ComboBox _modePaiement = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly Label _total = new() { Text = "0,00", AutoSize = true, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = UiTheme.Navy };
    private readonly Label _monnaie = new() { Text = "Monnaie à rendre : 0,00", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly List<LigneVente> _lignes = [];

    public UC_Vente(GestionStockService stock, GestionCatalogueService catalogue, GestionTicketService tickets, GestionCaisseService caisse, Utilisateur utilisateur)
    {
        _stock = stock; _catalogue=catalogue; _tickets=tickets; _caisse=caisse; _utilisateur = utilisateur; _session=_caisse.ObtenirOuOuvrir(_utilisateur.Id);
        ConstruireInterface();
        ChargerProduits();
    }

    private void ConstruireInterface()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.None, BackColor = Color.FromArgb(232, 237, 242), Padding = Padding.Empty };
        split.SizeChanged += (_, _) =>
        {
            var minimum = 300;
            var maximum = split.ClientSize.Width - split.SplitterWidth - 360;
            if (maximum >= minimum)
            {
                var desired = Math.Clamp((int)(split.ClientSize.Width * 0.46), minimum, maximum);
                if (split.SplitterDistance != desired) split.SplitterDistance = desired;
            }
        };
        split.Panel1.Padding = new Padding(0, 0, 8, 0); split.Panel2.Padding = new Padding(8, 0, 0, 0);

        var recherche = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        recherche.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); recherche.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); recherche.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        recherche.Controls.Add(Titre("Recherche produit"), 0, 0);
        var barreRecherche = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        barreRecherche.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); barreRecherche.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        _recherche.Dock = DockStyle.Fill; _recherche.Margin = new Padding(0, 3, 8, 3); _recherche.Font = new Font("Segoe UI", 11F);
        _ajouterPanier.Dock = DockStyle.Fill; _ajouterPanier.Margin = new Padding(0, 3, 0, 3); _ajouterPanier.Click += (_, _) => AjouterSelectionCourante();
        barreRecherche.Controls.Add(_recherche, 0, 0); barreRecherche.Controls.Add(_ajouterPanier, 1, 0);
        recherche.Controls.Add(barreRecherche, 0, 1); recherche.Controls.Add(_resultats, 0, 2);
        _recherche.KeyDown += RechercheKeyDown; _recherche.TextChanged += (_, _) => ChargerProduits();
        UiTheme.StyleGrid(_resultats); _resultats.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _resultats.CellDoubleClick += AjouterProduit;
        _resultats.SelectionChanged += (_, _) => ChargerPrixSelectionne();
        _panier.SelectionChanged += (_, _) => ChargerLigneSelectionnee();
        split.Panel1.Controls.Add(recherche);

        var caisse = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        caisse.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); caisse.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); caisse.RowStyles.Add(new RowStyle(SizeType.Absolute, 226));
        var entetePanier=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};entetePanier.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));entetePanier.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170));entetePanier.Controls.Add(Titre("Panier en cours"),0,0);var reimprimer=UiTheme.Button("Réimprimer ticket");reimprimer.Height=32;reimprimer.Click+=(_,_)=>ReimprimerTicket();entetePanier.Controls.Add(reimprimer,1,0);
        caisse.Controls.Add(entetePanier, 0, 0); caisse.Controls.Add(_panier, 0, 1); caisse.Controls.Add(ConstruirePiedDeCaisse(), 0, 2);
        UiTheme.StyleGrid(_panier); _panier.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        split.Panel2.Controls.Add(caisse); Controls.Add(split);
    }

    private Control ConstruirePiedDeCaisse()
    {
        var pied = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        pied.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); pied.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); pied.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var ligneOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
        ligneOptions.Controls.Add(new Label { Text = "Qté", AutoSize = true, Padding = new Padding(0, 9, 5, 0) }); ligneOptions.Controls.Add(_quantite);
        ligneOptions.Controls.Add(new Label { Text = "Prix", AutoSize = true, Padding = new Padding(15, 9, 5, 0) }); ligneOptions.Controls.Add(_prix);
        var modifier = UiTheme.Button("Appliquer quantité"); modifier.Width = 150; modifier.Height = 30; modifier.Margin = new Padding(12, 2, 0, 0); modifier.Click += (_, _) => ModifierQuantite(); ligneOptions.Controls.Add(modifier);
        var retirer = UiTheme.Button("Retirer ligne"); retirer.Width = 110; retirer.Height = 30; retirer.Margin = new Padding(8, 2, 0, 0); retirer.BackColor = Color.FromArgb(93, 105, 117); retirer.Click += (_, _) => RetirerLigne(); ligneOptions.Controls.Add(retirer);
        pied.Controls.Add(ligneOptions, 0, 0);

        var paiement = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        paiement.Controls.Add(new Label { Text = "Paiement", AutoSize = true, Padding = new Padding(0, 9, 5, 0) }); _modePaiement.Items.AddRange(Enum.GetNames<ModePaiement>()); _modePaiement.SelectedIndex = 0; _modePaiement.SelectedIndexChanged += (_, _) => ActualiserMonnaie(); paiement.Controls.Add(_modePaiement);
        paiement.Controls.Add(new Label { Text = "Reçu", AutoSize = true, Padding = new Padding(14, 9, 5, 0) }); paiement.Controls.Add(_montantRecu); _montantRecu.ValueChanged += (_, _) => ActualiserMonnaie(); pied.Controls.Add(paiement, 0, 1);

        var resume = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty, Padding = new Padding(0, 4, 0, 0) }; resume.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); resume.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42)); resume.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); resume.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        resume.Controls.Add(new Label { Text = "TOTAL GÉNÉRAL", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(85, 98, 110) }, 0, 0); resume.Controls.Add(_total, 1, 0); resume.Controls.Add(_monnaie, 0, 1);
        var encaisser = UiTheme.Button("ENCAISSER"); encaisser.Dock = DockStyle.Fill; encaisser.Margin = new Padding(8, 2, 0, 2); encaisser.BackColor = UiTheme.Green; encaisser.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold); encaisser.Click += (_, _) => Encaisser(); resume.Controls.Add(encaisser, 1, 1); pied.Controls.Add(resume, 0, 2);
        return pied;
    }

    private void RechercheKeyDown(object? sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { AjouterSelectionCourante(); e.SuppressKeyPress = true; } }
    private void AjouterSelectionCourante() { if (_resultats.Rows.Count == 0) { MessageBox.Show("Aucun produit vendable ne correspond à la recherche.", "Produit introuvable", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } var index = _resultats.CurrentRow?.Index ?? 0; AjouterProduit(this, new DataGridViewCellEventArgs(0, index)); }
    private void ChargerProduits() { _resultats.DataSource = _catalogue.RechercherPourVente(_recherche.Text).Select(x => new { x.Produit.Id, x.Produit.Reference, x.Produit.Designation, LotId = x.Lot.Id, x.Lot.NumeroLot, Peremption=x.Lot.DatePeremption.ToString("dd/MM/yyyy"), Disponible = x.Lot.QuantiteVendable, x.Produit.PrixVente }).ToList(); if (_resultats.Columns.Contains("Id")) _resultats.Columns["Id"].Visible=false; if (_resultats.Columns.Contains("LotId")) _resultats.Columns["LotId"].Visible=false; if (_resultats.Rows.Count > 0) { _resultats.Rows[0].Selected = true; _resultats.CurrentCell = _resultats.Rows[0].Cells["Reference"]; } ChargerPrixSelectionne(); }
    private void ChargerPrixSelectionne() { if (_resultats.CurrentRow is not null && _resultats.Columns.Contains("PrixVente")) _prix.Value=Math.Clamp(Convert.ToDecimal(_resultats.CurrentRow.Cells["PrixVente"].Value),_prix.Minimum,_prix.Maximum); }
    private void ChargerLigneSelectionnee() { if(_panier.CurrentRow is null)return;_quantite.Value=Math.Clamp(Convert.ToDecimal(_panier.CurrentRow.Cells["Quantite"].Value),_quantite.Minimum,_quantite.Maximum);_prix.Value=Math.Clamp(Convert.ToDecimal(_panier.CurrentRow.Cells["PrixVente"].Value),_prix.Minimum,_prix.Maximum); }

    private void AjouterProduit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return; var row = _resultats.Rows[e.RowIndex]; var produitId = Convert.ToInt64(row.Cells["Id"].Value); var lotId = Convert.ToInt64(row.Cells["LotId"].Value); var available=Convert.ToInt32(row.Cells["Disponible"].Value);
        var ligne = _lignes.FirstOrDefault(x => x.LotId == lotId); var nouvelleQuantite=(ligne?.Quantite ?? 0)+(int)_quantite.Value; if(nouvelleQuantite>available){MessageBox.Show($"Stock disponible sur ce lot : {available}.","Quantité indisponible",MessageBoxButtons.OK,MessageBoxIcon.Information);return;} var prix=Convert.ToDecimal(row.Cells["PrixVente"].Value); if (ligne is null) _lignes.Add(new LigneVente { ProduitId = produitId, LotId = lotId, Quantite = (int)_quantite.Value, PrixVente = prix }); else ligne.Quantite = nouvelleQuantite;
        ActualiserPanier();
    }

    private void ModifierQuantite()
    {
        if (_panier.CurrentRow is null) return;
        var lotId = Convert.ToInt64(_panier.CurrentRow.Cells["LotId"].Value);
        var ligne = _lignes.FirstOrDefault(x => x.LotId == lotId);
        if (ligne is null) return;
        var available = _stock.ObtenirLotsDisponibles(ligne.ProduitId).FirstOrDefault(x => x.Id == lotId)?.QuantiteVendable ?? 0;
        if (_quantite.Value > available) { MessageBox.Show($"Stock disponible sur ce lot : {available}.", "Quantité indisponible", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        ligne.Quantite = (int)_quantite.Value; ActualiserPanier();
    }

    private void RetirerLigne(){if(_panier.CurrentRow is null)return;var lotId=Convert.ToInt64(_panier.CurrentRow.Cells["LotId"].Value);_lignes.RemoveAll(x=>x.LotId==lotId);ActualiserPanier();}

    private void ActualiserPanier() { _panier.DataSource = _lignes.Select(x => new { x.ProduitId, x.LotId, x.Quantite, x.PrixVente, SousTotal = x.Quantite * x.PrixVente }).ToList(); var total = _lignes.Sum(x => x.Quantite * x.PrixVente); _total.Text = total.ToString("N2"); if (_montantRecu.Value == 0 || _modePaiement.SelectedItem?.ToString() != nameof(ModePaiement.Especes)) _montantRecu.Value = Math.Clamp(total, _montantRecu.Minimum, _montantRecu.Maximum); ActualiserMonnaie(); }
    private void ActualiserMonnaie() { var total = _lignes.Sum(x => x.Quantite * x.PrixVente); var especes = _modePaiement.SelectedItem?.ToString() == nameof(ModePaiement.Especes); var monnaie = especes ? _montantRecu.Value - total : 0m; _montantRecu.Enabled = especes; _monnaie.Text = especes ? $"Monnaie à rendre : {monnaie:N2}" : "Paiement électronique — aucune monnaie"; _monnaie.ForeColor = monnaie < 0 ? Color.Firebrick : UiTheme.Green; }

    private void Encaisser()
    {
        try { var total = _lignes.Sum(x => x.Quantite * x.PrixVente); if (_lignes.Count == 0) throw new InvalidOperationException("Le panier est vide. Recherchez un produit puis cliquez sur Ajouter au panier."); var mode = Enum.Parse<ModePaiement>(_modePaiement.SelectedItem?.ToString() ?? nameof(ModePaiement.Especes)); if (mode == ModePaiement.Especes && _montantRecu.Value < total) throw new InvalidOperationException($"Montant reçu insuffisant. Saisissez au moins {total:N2}."); var session = _caisse.ObtenirOuOuvrir(_utilisateur.Id); var vente = new Vente { UtilisateurId = _utilisateur.Id, SessionId = session.Id, ModePaiement = mode, Total = total }; _stock.ValiderVenteTransactionnelle(vente, _lignes); ImprimerTicket(vente.Id); _lignes.Clear(); ActualiserPanier(); ChargerProduits(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Encaissement impossible", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ImprimerTicket(long venteId)
    {
        try
        {
            ImprimerOuPrevisualiserTicket(venteId);
        }
        catch(Exception ex){MessageBox.Show($"La vente est enregistrée, mais le ticket n’a pas pu être préparé.\n\n{ex.Message}","Ticket",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }

    private void ReimprimerTicket()
    {
        using var dialog=new TicketNumberDialog();if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
        try{ImprimerOuPrevisualiserTicket(dialog.VenteId);}catch(Exception ex){MessageBox.Show(ex.Message,"Ticket indisponible",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }

    private void ImprimerOuPrevisualiserTicket(long venteId)
    {
            var ticket=_tickets.ObtenirTicket(venteId,_utilisateur);var lines=new List<string>{"PHARMACIE","Ticket de vente",$"N° V-{ticket.Id:000000}",$"Date : {ticket.Date:dd/MM/yyyy HH:mm}",$"Vendeur : {ticket.Vendeur}",new string('-',38)};
            lines.AddRange(ticket.Lignes.Select(x=>$"{x.Designation}\n  {x.Quantite} x {x.PrixUnitaire:N0} = {x.SousTotal:N0} FCFA"));lines.Add(new string('-',38));lines.Add($"TOTAL : {ticket.Total:N0} FCFA");lines.Add($"Paiement : {ticket.Paiement}");
            var text=string.Join(Environment.NewLine,lines);var doc=new PrintDocument();doc.DocumentName=$"Ticket V-{ticket.Id:000000}";doc.PrintPage+=(_,e)=>{using var font=new Font("Consolas",9);e.Graphics!.DrawString(text,font,Brushes.Black,new RectangleF(25,25,e.MarginBounds.Width,e.MarginBounds.Height));};
            using var preview=new PrintPreviewDialog{Document=doc,Width=800,Height=650};preview.ShowDialog(FindForm());
    }

    private static Label Titre(string text) => new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold), ForeColor = UiTheme.Navy };
}

internal sealed class TicketNumberDialog : Form
{
    private readonly TextBox _numero=new(){Dock=DockStyle.Fill,PlaceholderText="Ex. V-000123"};
    public long VenteId { get; private set; }
    public TicketNumberDialog()
    {
        Text="Réimprimer un ticket";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ClientSize=new Size(410,170);var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=3};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.Controls.Add(new Label{Text="Saisissez le numéro de vente",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);layout.Controls.Add(_numero,0,1);var ok=UiTheme.Button("Continuer");ok.DialogResult=DialogResult.OK;ok.Anchor=AnchorStyles.Right;ok.Click+=(_,_)=>{var digits=new string(_numero.Text.Where(char.IsDigit).ToArray());if(!long.TryParse(digits,out var id)||id<=0){DialogResult=DialogResult.None;MessageBox.Show("Saisissez un numéro valide, par exemple V-000123.","Numéro incorrect",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}VenteId=id;};layout.Controls.Add(ok,0,2);Controls.Add(layout);AcceptButton=ok;
    }
}
