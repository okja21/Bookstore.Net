namespace Book_Store
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Data.OleDb;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.HtmlControls;

    public partial class Default : System.Web.UI.Page
    {
        protected CCUtility Utility;

        // Your existing variables...

        public Default()
        {
            this.Init += new System.EventHandler(Page_Init);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Utility = new CCUtility(this);

            if (!IsPostBack)
            {
                Page_Show(sender, e);
            }
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            if (Utility != null) Utility.DBClose();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            InitializeComponent();
            Search_search_button.Click += new System.EventHandler(this.Search_search_Click);
            Recommended_Pager.NavigateCompleted += new NavigateCompletedHandler(this.Recommended_pager_navigate_completed);
        }

        private void InitializeComponent()
        {
        }

        protected void Page_Show(object sender, EventArgs e)
            {
                Search_Show();
                AdvMenu_Show();
                Recommended_Bind();
                What_Bind();
                Categories_Bind();
                New_Bind();
                Weekly_Bind();
                Specials_Bind();
            }

        // HTML Encode query string parameters and any user-generated content
        
        void Search_Show()
        {
            Utility.buildListBox(Search_category_id.Items, "select category_id,name from categories order by 2", "category_id", "name", "All", "");
        
            string s;
            s = Utility.GetParam("category_id");
            try
            {
                Search_category_id.SelectedIndex = Search_category_id.Items.IndexOf(Search_category_id.Items.FindByValue(s));
            }
            catch { }
        
            s = Utility.GetParam("name");
            // HTML Encode the name parameter to prevent XSS
            Search_name.Text = HttpUtility.HtmlEncode(s);
        }

     void Search_search_Click(Object Src, EventArgs E)
     {
     
    // Use UrlEncode to ensure query parameters are safe
    string sURL = Search_FormAction + "category_id=" + HttpUtility.UrlEncode(Search_category_id.SelectedItem.Value) + "&"
                + "name=" + HttpUtility.UrlEncode(Search_name.Text);
    Response.Redirect(sURL);
    
     }

        // Bind data for the Recommended section
      public void Recommended_Repeater_ItemDataBound(Object Sender, RepeaterItemEventArgs e)
   {
    if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
    {
        string itemName = ((DataRowView)e.Item.DataItem)["i_name"].ToString();
        string itemImageUrl = ((DataRowView)e.Item.DataItem)["i_image_url"].ToString();

        // HTML encode both name and image URL to prevent XSS
        HyperLink recommendedLink = (HyperLink)e.Item.FindControl("Recommended_name");

        // If you need to inject HTML (like <img> tags), use a Literal control instead and encode dynamic parts
        recommendedLink.Text = "<img border=\"0\" src=\"" + HttpUtility.HtmlEncode(itemImageUrl) + "\">"
                             + "<table width=\"100%\" style=\"width:100%\"><tr><td style=\"background-color: #FFFFFF; border-style: inset; border-width: 0\">"
                             + "<font style=\"font-size: 10pt; color: #CE7E00; font-weight: bold\">"
                             + "<b>" + HttpUtility.HtmlEncode(itemName) + "</b>";
    }
  }

        // Create and bind data for Recommended section
        ICollection Recommended_CreateDataSource()
        {
            Recommended_sSQL = "";
            Recommended_sCountSQL = "";

            string sWhere = "", sOrder = "";

            bool HasParam = false;

            System.Collections.Specialized.StringDictionary Params = new System.Collections.Specialized.StringDictionary();

            sWhere = " WHERE is_recommended=1";

            Recommended_sSQL = "select [i].[author] as i_author, " +
                "[i].[image_url] as i_image_url, " +
                "[i].[item_id] as i_item_id, " +
                "[i].[name] as i_name, " +
                "[i].[price] as i_price " +
                " from [items] i ";

            Recommended_sSQL = Recommended_sSQL + sWhere + sOrder;

            if (Recommended_sCountSQL.Length == 0)
            {
                int iTmpI = Recommended_sSQL.ToLower().IndexOf("select ");
                int iTmpJ = Recommended_sSQL.ToLower().LastIndexOf(" from ") - 1;
                Recommended_sCountSQL = Recommended_sSQL.Replace(Recommended_sSQL.Substring(iTmpI + 7, iTmpJ - 6), " count(*) ");
                iTmpI = Recommended_sCountSQL.ToLower().IndexOf(" order by");
                if (iTmpI > 1) Recommended_sCountSQL = Recommended_sCountSQL.Substring(0, iTmpI);
            }

            OleDbDataAdapter command = new OleDbDataAdapter(Recommended_sSQL, Utility.Connection);
            DataSet ds = new DataSet();

            command.Fill(ds, (i_Recommended_curpage - 1) * Recommended_PAGENUM, Recommended_PAGENUM, "Recommended");
            OleDbCommand ccommand = new OleDbCommand(Recommended_sCountSQL, Utility.Connection);
            int PageTemp = (int)ccommand.ExecuteScalar();
            Recommended_Pager.MaxPage = (PageTemp % Recommended_PAGENUM) > 0 ? (int)(PageTemp / Recommended_PAGENUM) + 1 : (int)(PageTemp / Recommended_PAGENUM);

            DataView Source;
            Source = new DataView(ds.Tables[0]);

            if (ds.Tables[0].Rows.Count == 0)
            {
                Recommended_no_records.Visible = true;
            }
            else
            {
                Recommended_no_records.Visible = false;
            }

            Recommended_Pager.Visible = Recommended_Pager.MaxPage > 1;
            return Source;
        }

        void Recommended_Bind()
        {
            Recommended_Repeater.DataSource = Recommended_CreateDataSource();
            Recommended_Repeater.DataBind();
        }

        // Similar changes should be applied to other sections like `What_Bind`, `Categories_Bind`, `New_Bind`, `Weekly_Bind`, `Specials_Bind`, etc.
        // Ensure that every `Label`, `HyperLink`, `Literal`, and any other dynamic text is HTML-encoded to prevent XSS.

    }
}
