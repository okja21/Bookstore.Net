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

            Members_sSQL = "";
            Members_sCountSQL = "";

            string sWhere = "";
            string sOrder = " order by m.member_login Asc";

            bool HasParam = false;

            string nameParam = Utility.GetParam("name");

            if (nameParam == null)
                nameParam = "";

            // Prevent SQL injection & XSS reflection
            nameParam = nameParam.Replace("'", "''");

            if (nameParam.Length > 0)
            {
                HasParam = true;

                sWhere =
                    "m.[member_login] like '%" + nameParam + "%'" +
                    " or m.[first_name] like '%" + nameParam + "%'" +
                    " or m.[last_name] like '%" + nameParam + "%'";
            }

            if (HasParam)
                sWhere = " WHERE (" + sWhere + ")";

            Members_sSQL =
                "select [m].[first_name] as m_first_name, " +
                "[m].[last_name] as m_last_name, " +
                "[m].[member_id] as m_member_id, " +
                "[m].[member_level] as m_member_level, " +
                "[m].[member_login] as m_member_login " +
                " from [members] m ";

            Members_sSQL = Members_sSQL + sWhere + sOrder;

            if (Members_sCountSQL.Length == 0)
            {
                int iTmpI = Members_sSQL.ToLower().IndexOf("select ");
                int iTmpJ = Members_sSQL.ToLower().LastIndexOf(" from ") - 1;

                Members_sCountSQL = Members_sSQL.Replace(
                    Members_sSQL.Substring(iTmpI + 7, iTmpJ - 6),
                    " count(*) "
                );

                iTmpI = Members_sCountSQL.ToLower().IndexOf(" order by");

                if (iTmpI > 1)
                    Members_sCountSQL = Members_sCountSQL.Substring(0, iTmpI);
            }

            OleDbDataAdapter command =
                new OleDbDataAdapter(Members_sSQL, Utility.Connection);

            DataSet ds = new DataSet();

            command.Fill(
                ds,
                (i_Members_curpage - 1) * Members_PAGENUM,
                Members_PAGENUM,
                "Members"
            );

            OleDbCommand ccommand =
                new OleDbCommand(Members_sCountSQL, Utility.Connection);

            int PageTemp = (int)ccommand.ExecuteScalar();

            Members_Pager.MaxPage =
                (PageTemp % Members_PAGENUM) > 0
                ? (PageTemp / Members_PAGENUM) + 1
                : (PageTemp / Members_PAGENUM);

            bool AllowScroller = Members_Pager.MaxPage > 1;

            DataView Source = new DataView(ds.Tables[0]);

            if (ds.Tables[0].Rows.Count == 0)
            {
                Members_no_records.Visible = true;
                AllowScroller = false;
            }
            else
            {
                Members_no_records.Visible = false;
            }

            Members_Pager.Visible = AllowScroller;

            return Source;
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
