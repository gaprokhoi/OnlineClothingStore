using System.Collections.Generic;
using System.Web;

namespace ClothingStoreWebApp.Helpers
{
    public static class SessionRoleHelper
    {
        private const string RolesKey = "UserRoles";

        // Lưu danh sách roles vào session
        public static void SetRoles(this HttpSessionStateBase session, List<string> roles)
        {
            session[RolesKey] = roles;
        }

        // Lấy danh sách roles từ session
        public static List<string> GetRoles(this HttpSessionStateBase session)
        {
            return session[RolesKey] as List<string> ?? new List<string>();
        }

        // Kiểm tra user có role cụ thể hay không
        public static bool HasRole(this HttpSessionStateBase session, string role)
        {
            return session.GetRoles().Contains(role);
        }

        // Kiểm tra user có là Admin không
        public static bool IsAdmin(this HttpSessionStateBase session)
        {
            return session.HasRole("Admin");
        }

        // Kiểm tra user có là Customer không
        public static bool IsCustomer(this HttpSessionStateBase session)
        {
            return session.HasRole("Customer");
        }
    }
}
