using System.Configuration;

namespace ACAM.Manager.Infrastructure.Data
{
    public static class DbContext
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}
