using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

/// <summary>Consultation des lots et alertes, avec administration du répertoire fournisseurs.</summary>
public sealed class UC_Stocks : UserControl
{
    private readonly GestionCatalogueService _catalogue; private readonly Utilisateur _user; private readonly GestionStockService _stock;
    private readonly DataGridView _lots=new(), _fournisseurs=new(), _alertes=new();
    private readonly GestionOperationsStockService _operations;
    public UC_Stocks(GestionCatalogueService catalogue, Utilisateur utilisateur, GestionStockService stock, GestionOperationsStockService operations)
    {
        _catalogue=catalogue;_user=utilisateur;_stock=stock;_operations=operations;Dock=DockStyle.Fill;BackColor=Color.FromArgb(244,247,250);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.Controls.Add(new Label{Text="Stocks et fournisseurs",Dock=DockStyle.Fill,Font=new Font("Segoe UI Semibold",19,FontStyle.Bold),ForeColor=UiTheme.Navy,TextAlign=ContentAlignment.MiddleLeft},0,0);
        var tabs=new TabControl{Dock=DockStyle.Fill};var lotsTab=new TabPage("Lots & alertes");var fournisseursTab=new TabPage("Fournisseurs");tabs.TabPages.Add(lotsTab);tabs.TabPages.Add(fournisseursTab);
        lotsTab.Controls.Add(ConstruireOngletLots());fournisseursTab.Controls.Add(ConstruireOngletFournisseurs());root.Controls.Add(tabs,0,1);Controls.Add(root);ChargerLots();ChargerAlertes();ChargerFournisseurs();
    }
    private Control ConstruireOngletLots()
    {
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=270,Panel1MinSize=130,Panel2MinSize=130};
        var top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(8)};top.RowStyles.Add(new RowStyle(SizeType.Absolute,36));top.RowStyles.Add(new RowStyle(SizeType.Absolute,42));top.RowStyles.Add(new RowStyle(SizeType.Percent,100));top.Controls.Add(new Label{Text="Lots — coûts visibles au responsable uniquement",Dock=DockStyle.Fill,ForeColor=UiTheme.Navy,Font=new Font("Segoe UI Semibold",11,FontStyle.Bold)},0,0);var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var reception=UiTheme.Button("＋ Réceptionner un lot");reception.Width=190;reception.Visible=_user.Role==RoleUtilisateur.Administrateur;reception.Click+=(_,_)=>ReceptionnerLot();toolbar.Controls.Add(reception);top.Controls.Add(toolbar,0,1);ConfigurerGrille(_lots);top.Controls.Add(_lots,0,2);split.Panel1.Controls.Add(top);
        var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(8)};bottom.RowStyles.Add(new RowStyle(SizeType.Absolute,36));bottom.RowStyles.Add(new RowStyle(SizeType.Percent,100));bottom.Controls.Add(new Label{Text="Alertes stock et péremption",Dock=DockStyle.Fill,ForeColor=UiTheme.Navy,Font=new Font("Segoe UI Semibold",11,FontStyle.Bold)},0,0);ConfigurerGrille(_alertes);bottom.Controls.Add(_alertes,0,1);split.Panel2.Controls.Add(bottom);return split;
    }
    private Control ConstruireOngletFournisseurs()
    {
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(10)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,FlowDirection=FlowDirection.LeftToRight};var add=UiTheme.Button("＋ Ajouter fournisseur");add.Width=190;add.Click+=(_,_)=>ModifierFournisseur(null);var edit=UiTheme.Button("Modifier");edit.Width=120;edit.Click+=(_,_)=>OuvrirModificationSelection();var archive=UiTheme.Button("Archiver");archive.Width=120;archive.Click+=(_,_)=>ArchiverFournisseur();toolbar.Controls.AddRange([add,edit,archive]);
        ConfigurerGrille(_fournisseurs);layout.Controls.Add(toolbar,0,0);layout.Controls.Add(_fournisseurs,0,1);
        var responsable=_user.Role==RoleUtilisateur.Administrateur;add.Visible=edit.Visible=archive.Visible=responsable;return layout;
    }
    private static void ConfigurerGrille(DataGridView grid){UiTheme.StyleGrid(grid);grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.AutoGenerateColumns=true;}
    private void ChargerLots()
    {
        try{var rows=_catalogue.Lots(_user).Select(x=>new{Produit=x.Designation,x.Reference,x.NumeroLot,Péremption=x.DatePeremption.ToString("dd/MM/yyyy"),x.QuantitePhysique,x.QuantiteVendable,Etat=x.IsBloque?"Bloqué":x.DatePeremption.Date<DateTime.Today?"Périmé":"Disponible",CoûtAchat=x.CoutAchat}).ToList();_lots.DataSource=rows;}catch(Exception ex){MessageBox.Show(ex.Message,"Chargement impossible",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    private void ChargerAlertes()=>_alertes.DataSource=_stock.RecupererAlertesStock().Select(x=>new{Type=x.Type,x.Message,x.ProduitId,x.LotId}).ToList();
    private void ChargerFournisseurs()=>_fournisseurs.DataSource=_catalogue.Fournisseurs(_user).Select(x=>new{x.Id,x.Nom,x.Telephone,x.Email,x.Adresse}).ToList();
    private void ReceptionnerLot()
    {
        if (_user.Role != RoleUtilisateur.Administrateur) return;
        using var dialog = new ReceptionLotDialog(_catalogue.Produits(RoleUtilisateur.Administrateur));
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try { _operations.ValiderReception(new Reception { UtilisateurId = _user.Id, ReferenceDocument = dialog.ReferenceDocument }, [(dialog.ProduitId, dialog.NumeroLot, dialog.DatePeremption, dialog.Quantite, dialog.CoutAchat)], _user); ChargerLots(); ChargerAlertes(); MessageBox.Show("Réception enregistrée et stock disponible à la vente.", "Réception", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Réception impossible", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private long? SelectionFournisseur()=>_fournisseurs.CurrentRow is null?null:Convert.ToInt64(_fournisseurs.CurrentRow.Cells["Id"].Value);
    private void OuvrirModificationSelection(){if(SelectionFournisseur() is not long id)return;ModifierFournisseur(_catalogue.Fournisseurs(_user).FirstOrDefault(x=>x.Id==id));}
    private void ModifierFournisseur(Fournisseur? f)
    {
        if(_user.Role!=RoleUtilisateur.Administrateur)return;
        if(f is null&&SelectionFournisseur() is long id)f=_catalogue.Fournisseurs(_user).FirstOrDefault(x=>x.Id==id);
        using var dlg=new FournisseurDialog(f);if(dlg.ShowDialog(FindForm())!=DialogResult.OK)return;
        try{if(f is null)_catalogue.AjouterFournisseur(dlg.Valeur,_user);else{dlg.Valeur.Id=f.Id;_catalogue.ModifierFournisseur(dlg.Valeur,_user);}ChargerFournisseurs();}catch(Exception ex){MessageBox.Show(ex.Message,"Opération impossible",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    private void ArchiverFournisseur(){if(_user.Role!=RoleUtilisateur.Administrateur||SelectionFournisseur() is not long id)return;if(MessageBox.Show("Archiver ce fournisseur ?","Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;try{_catalogue.ArchiverFournisseur(id,_user);ChargerFournisseurs();}catch(Exception ex){MessageBox.Show(ex.Message,"Opération impossible",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
}

internal sealed class ReceptionLotDialog : Form
{
    private readonly ComboBox _produit = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _lot = new(); private readonly TextBox _reference = new();
    private readonly DateTimePicker _peremption = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(1) };
    private readonly NumericUpDown _quantite = new() { Minimum = 1, Maximum = 100000, Value = 1 };
    private readonly NumericUpDown _cout = new() { Minimum = 0, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true };
    public long ProduitId => Convert.ToInt64(_produit.SelectedValue); public string NumeroLot => _lot.Text.Trim(); public DateTime DatePeremption => _peremption.Value.Date; public int Quantite => (int)_quantite.Value; public decimal CoutAchat => _cout.Value; public string? ReferenceDocument => string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim();
    public ReceptionLotDialog(IReadOnlyList<Produit> produits)
    {
        Text="Réceptionner un lot";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ClientSize=new Size(560,390);
        _produit.DataSource=produits.ToList();_produit.DisplayMember=nameof(Produit.Designation);_produit.ValueMember=nameof(Produit.Id);_produit.Dock=DockStyle.Fill;_lot.Dock=_reference.Dock=_peremption.Dock=_quantite.Dock=_cout.Dock=DockStyle.Fill;
        var l=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=2,RowCount=7};l.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(var i=0;i<6;i++)l.RowStyles.Add(new RowStyle(SizeType.Absolute,44));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));Ajouter(l,0,"Médicament *",_produit);Ajouter(l,1,"Numéro de lot *",_lot);Ajouter(l,2,"Péremption *",_peremption);Ajouter(l,3,"Quantité *",_quantite);Ajouter(l,4,"Coût d'achat",_cout);Ajouter(l,5,"Référence document",_reference);var ok=UiTheme.Button("Enregistrer la réception");ok.DialogResult=DialogResult.OK;ok.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(NumeroLot)||DatePeremption<DateTime.Today){DialogResult=DialogResult.None;MessageBox.Show("Le numéro de lot est obligatoire et la péremption doit être future.");}};l.Controls.Add(ok,1,6);Controls.Add(l);AcceptButton=ok;
    }
    private static void Ajouter(TableLayoutPanel l,int row,string label,Control control){l.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);l.Controls.Add(control,1,row);}
}

internal sealed class FournisseurDialog:Form
{
    private readonly TextBox _nom=new(),_telephone=new(),_email=new(),_adresse=new();
    public Fournisseur Valeur=>new(){Nom=_nom.Text.Trim(),Telephone=_telephone.Text.Trim(),Email=_email.Text.Trim(),Adresse=_adresse.Text.Trim()};
    public FournisseurDialog(Fournisseur? fournisseur){Text=fournisseur is null?"Nouveau fournisseur":"Modifier fournisseur";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ClientSize=new Size(480,330);var l=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=2,RowCount=5};l.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<4;i++)l.RowStyles.Add(new RowStyle(SizeType.Absolute,48));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));Control[] inputs=[_nom,_telephone,_email,_adresse];string[] labels=["Nom *","Téléphone","Email","Adresse"];for(int i=0;i<inputs.Length;i++){l.Controls.Add(new Label{Text=labels[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,i);inputs[i].Dock=DockStyle.Fill;l.Controls.Add(inputs[i],1,i);}if(fournisseur is not null){_nom.Text=fournisseur.Nom;_telephone.Text=fournisseur.Telephone;_email.Text=fournisseur.Email;_adresse.Text=fournisseur.Adresse;}var b=UiTheme.Button("Enregistrer");b.DialogResult=DialogResult.OK;b.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(_nom.Text)){DialogResult=DialogResult.None;MessageBox.Show("Le nom est requis.");}};l.Controls.Add(b,1,4);Controls.Add(l);AcceptButton=b;}
}
