using System.Web;
using System.Web.Security;

namespace StudentAccountingSystem
{
    public class CustomRoleProvider : RoleProvider
    {
        public override string[] GetRolesForUser(string username)
        {
            var cookie = HttpContext.Current.Request.Cookies[
                FormsAuthentication.FormsCookieName];
            if (cookie == null) return new string[0];

            var ticket = FormsAuthentication.Decrypt(cookie.Value);
            if (ticket == null || ticket.Expired) return new string[0];

            return new[] { ticket.UserData };
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            var roles = GetRolesForUser(username);
            foreach (var r in roles)
                if (r == roleName) return true;
            return false;
        }

        // The rest are not used — leave them throwing NotImplementedException
        public override string ApplicationName { get; set; }
        public override void AddUsersToRoles(string[] u, string[] r) { }
        public override void CreateRole(string r) { }
        public override bool DeleteRole(string r, bool t) { return false; }
        public override string[] FindUsersInRole(string r, string u) { return new string[0]; }
        public override string[] GetAllRoles() { return new string[0]; }
        public override string[] GetUsersInRole(string r) { return new string[0]; }
        public override void RemoveUsersFromRoles(string[] u, string[] r) { }
        public override bool RoleExists(string r) { return false; }
    }
}