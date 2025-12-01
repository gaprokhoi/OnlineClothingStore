using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ClothingStoreWebApp.Startup))]
namespace ClothingStoreWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
