namespace Book_Store
{
//
// Filename: MembersGrid.cs
//

    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Data;
    using System.Data.OleDb;
    using System.Drawing;
    using System.Web;
    using System.Web.SessionState;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.HtmlControls;

    public partial class MembersGrid : System.Web.UI.Page
    {

        protected CCUtility Utility;

        protected string Members_sSQL;
        protected string Members_sCountSQL;
        protected int Members_CountPage;
        protected int i_Members_curpage = 1;

        protected string Search_FormAction = "MembersGrid.aspx?";
        protected string Members_FormAction = "MembersRecord.aspx?";

        protected String[] Members_member_level_lov =
            "1;Member;2;Administrator".Split(new Char[] { ';' });

        public MembersGrid()
        {
            this.Init += new System.EventHandler(Page_Init);
        }

        public void ValidateNumeric(object source, ServerValidateEventArgs args)
        {
            try
            {
                Decimal.Parse(args.Value);
                args.IsValid = true;
            }
            catch
            {
                args.IsValid = false;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Utility = new CCUtility(this);

            // Security check
            Utility.CheckSecurity(2);

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
            Members_insert.Click += new System.EventHandler(this.Members_insert_Click);
            Members_Pager.NavigateCompleted +=
                new NavigateCompletedHandler(this.Members_pager_navigate_completed);
        }

        private void InitializeComponent()
        {
        }

        protected void Page_Show(object sender, EventArgs e)
        {
            Search_Show();
            Members_Bind();
        }

        // =========================
        // SEARCH FORM
        // =========================

        void Search_Show()
        {
            string nameParam = Utility.GetParam("name");

            if (nameParam == null)
                nameParam = "";

            // Encode before rendering
            Search_name.Text = HttpUtility.HtmlEncode(nameParam);
        }

        void Search_search_Click(Object Src, EventArgs E)
        {
            string encodedName = HttpUtility.UrlEncode(Search_name.Text);

            string sURL = Search_FormAction + "name=" + encodedName + "&";

            Response.Redirect(sURL);
        }

        const int Members_PAGENUM = 20;

        public void Members_Repeater_ItemDataBound(Object Sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item ||
                e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView row = (DataRowView)e.Item.DataItem;

                Label first = (Label)e.Item.FindControl("Members_name");
                Label last = (Label)e.Item.FindControl("Members_last_name");
                Label level = (Label)e.Item.FindControl("Members_member_level");

                if (first != null)
                    first.Text = HttpUtility.HtmlEncode(row["m_first_name"].ToString());

                if (last != null)
                    last.Text = HttpUtility.HtmlEncode(row["m_last_name"].ToString());

                if (level != null)
                    level.Text = HttpUtility.HtmlEncode(
                        CCUtility.GetValFromLOV(
                            row["m_member_level"].ToString(),
                            Members_member_level_lov
                        )
                    );
            }
        }

ICollection Members_CreateDataSource()
{
    string nameParam = Utility.GetParam("name") ?? "";
    string sOrder = " ORDER BY m.member_login ASC";
    string sWhere = "";

    Members_sSQL = "";
    Members_sCountSQL = "";

    bool hasParam = !string.IsNullOrEmpty(nameParam);

    if (hasParam)
    {
        sWhere = " WHERE (m.member_login LIKE ? OR m.first_name LIKE ? OR m.last_name LIKE ?)";
    }

    Members_sSQL =
        "SELECT m.first_name AS m_first_name, " +
        "m.last_name AS m_last_name, " +
        "m.member_id AS m_member_id, " +
        "m.member_level AS m_member_level, " +
        "m.member_login AS m_member_login " +
        "FROM members m " +
        sWhere + sOrder;

    Members_sCountSQL =
        "SELECT COUNT(*) FROM members m " + sWhere;

    DataSet ds = new DataSet();

    using (OleDbDataAdapter adapter = new OleDbDataAdapter(Members_sSQL, Utility.Connection))
    {
        if (hasParam)
        {
            string likeParam = "%" + nameParam + "%";

            adapter.SelectCommand.Parameters.AddWithValue("@p1", likeParam);
            adapter.SelectCommand.Parameters.AddWithValue("@p2", likeParam);
            adapter.SelectCommand.Parameters.AddWithValue("@p3", likeParam);
        }

        adapter.Fill(
            ds,
            (i_Members_curpage - 1) * Members_PAGENUM,
            Members_PAGENUM,
            "Members"
        );
    }

    // --- Stored XSS protection ---
    foreach (DataRow row in ds.Tables["Members"].Rows)
    {
        row["m_first_name"] = Microsoft.Security.Application.Encoder.HtmlEncode(row["m_first_name"]?.ToString() ?? "");
        row["m_last_name"] = Microsoft.Security.Application.Encoder.HtmlEncode(row["m_last_name"]?.ToString() ?? "");
        row["m_member_login"] = Microsoft.Security.Application.Encoder.HtmlEncode(row["m_member_login"]?.ToString() ?? "");
    }

    int totalRecords = 0;

    using (OleDbCommand cmd = new OleDbCommand(Members_sCountSQL, Utility.Connection))
    {
        if (hasParam)
        {
            string likeParam = "%" + nameParam + "%";

            cmd.Parameters.AddWithValue("@p1", likeParam);
            cmd.Parameters.AddWithValue("@p2", likeParam);
            cmd.Parameters.AddWithValue("@p3", likeParam);
        }

        totalRecords = Convert.ToInt32(cmd.ExecuteScalar());
    }

    Members_Pager.MaxPage =
        (totalRecords % Members_PAGENUM) > 0
        ? (totalRecords / Members_PAGENUM) + 1
        : (totalRecords / Members_PAGENUM);

    bool AllowScroller = Members_Pager.MaxPage > 1;

    DataView source = new DataView(ds.Tables["Members"]);
    return source;
}

        protected void Members_pager_navigate_completed(Object Src, int CurrPage)
        {
            i_Members_curpage = CurrPage;
            Members_Bind();
        }

        void Members_Bind()
        {
            Members_Repeater.DataSource = Members_CreateDataSource();
            Members_Repeater.DataBind();
        }

        void Members_insert_Click(Object Src, EventArgs E)
        {
            string encodedName = HttpUtility.UrlEncode(Utility.GetParam("name"));

            string sURL = Members_FormAction + "name=" + encodedName + "&";

            Response.Redirect(sURL);
        }

        protected void Members_SortChange(Object Src, EventArgs E)
        {
            if (ViewState["SortColumn"] == null ||
                ViewState["SortColumn"].ToString() !=
                ((LinkButton)Src).CommandArgument)
            {
                ViewState["SortColumn"] = ((LinkButton)Src).CommandArgument;
                ViewState["SortDir"] = "ASC";
            }
            else
            {
                ViewState["SortDir"] =
                    ViewState["SortDir"].ToString() == "ASC"
                    ? "DESC"
                    : "ASC";
            }

            Members_Bind();
        }

    }
}
