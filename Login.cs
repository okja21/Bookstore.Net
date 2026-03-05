namespace Book_Store
{
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
    using System.Security.Cryptography;
    using System.Text;

    public partial class Login : System.Web.UI.Page
    {
        protected CCUtility Utility;

        protected System.Web.UI.HtmlControls.HtmlInputHidden Login_querystring;
        protected System.Web.UI.HtmlControls.HtmlInputHidden Login_ret_page;
        protected string Login_FormAction = "ShoppingCart.aspx?";

        public Login()
        {
            this.Init += new System.EventHandler(Page_Init);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Utility = new CCUtility(this);

            if (Session["UserID"] != null && Int16.Parse(Session["UserID"].ToString()) > 0)
                Login_logged = true;

            if (!IsPostBack)
                Page_Show(sender, e);
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            if (Utility != null) Utility.DBClose();
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            InitializeComponent();
            Login_login.Click += new System.EventHandler(this.Login_login_Click);
        }

        private void InitializeComponent() { }

        protected void Page_Show(object sender, EventArgs e)
        {
            Login_Show();
        }

        protected bool Login_logged = false;

        void Login_Show()
        {
            if (Login_logged)
            {
                Login_login.Text = "Logout";
                Login_trpassword.Visible = false;
                Login_trname.Visible = false;
                Login_labelname.Visible = true;

                // Ensure we HTML encode the member name to prevent XSS
                string memberName = Utility.DlookupSafe("members", "member_login", "member_id", Session["UserID"]);
                Login_labelname.Text = HttpUtility.HtmlEncode(memberName) + "&nbsp;&nbsp;&nbsp;";
            }
            else
            {
                Login_login.Text = "Login";
                Login_trpassword.Visible = true;
                Login_trname.Visible = true;
                Login_labelname.Visible = false;
            }
        }

        void Login_login_Click(Object Src, EventArgs E)
        {
            if (Login_logged)
            {
                // Logout
                Login_logged = false;
                Session["UserID"] = 0;
                Session["UserRights"] = 0;
                Login_Show();
            }
            else
            {
                string loginName = Login_name.Text;
                string password = Login_password.Text; // plaintext input

                try
                {
                    int iPassed = Convert.ToInt32(
                        Utility.DlookupSafe("members", "count(*)", "member_login", loginName)
                    );

                    if (iPassed > 0)
                    {
                        // Fetch hashed password from DB
                        string dbPasswordHash = Utility.DlookupSafe("members", "member_password", "member_login", loginName);

                        // Compute hash of entered password
                        string passwordHash = CCUtility.ComputeHash(password);

                        // Compare hashes
                        if (dbPasswordHash != passwordHash)
                            iPassed = 0;

                        // Clear sensitive variables
                        dbPasswordHash = null;
                        passwordHash = null;
                    }

                    if (iPassed > 0)
                    {
                        Login_message.Visible = false;

                        Session["UserID"] = Convert.ToInt32(Utility.DlookupSafe("members", "member_id", "member_login", loginName));
                        Session["UserRights"] = Convert.ToInt32(Utility.DlookupSafe("members", "member_level", "member_login", loginName));

                        string sQueryString = Utility.GetParam("querystring");
                        string sPage = Utility.GetParam("ret_page");
                        if (!sPage.Equals(Request.ServerVariables["SCRIPT_NAME"]) && sPage.Length > 0)
                            Response.Redirect(sPage + "?" + sQueryString);
                        else
                            Response.Redirect(Login_FormAction);

                        Login_logged = true;
                    }
                    else
                    {
                        Login_message.Visible = true;
                    }
                }
                finally
                {
                    // Clear plaintext password immediately
                    password = null;
                }
            }
        }
    }

    //===============================
    public partial class CCUtility
    {
        public string DlookupSafe(string table, string field, string whereColumn, object whereValue)
        {
            // Whitelist validation
            var allowedTables = new[] { "members" };
            var allowedFields = new[] { "member_id", "member_login", "member_password", "member_level", "count(*)" };

            if (!allowedTables.Contains(table) || !allowedFields.Contains(field) || !allowedFields.Contains(whereColumn))
                throw new ArgumentException("Invalid table or column name");

            string sSQL = $"SELECT {field} FROM {table} WHERE {whereColumn} = ?";

            using (OleDbCommand command = new OleDbCommand(sSQL, Connection))
            {
                command.Parameters.AddWithValue("@value", whereValue);

                using (OleDbDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                        return reader[0]?.ToString() ?? "";
                    return "";
                }
            }
        }

        // Computes SHA256 hash of a string
        public static string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
