using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.Sqlite;
using Senparc.Ncf.XncfBase;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Tests
{
    [TestClass]
    public class BaseTest
    {
        public IServiceCollection ServiceCollection { get; }
        public static IServiceProvider ServiceProvider { get; set; }
        public static IConfiguration Configuration { get; set; }

        protected static IRegisterService registerService;
        protected static SenparcSetting _senparcSetting;

        protected static Mock<Microsoft.Extensions.Hosting.IHostEnvironment/*IHostingEnvironment*/> _env;

        public BaseTest()
        {
            _env = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment/*IHostingEnvironment*/>();
            _env.Setup(z => z.ContentRootPath).Returns(() => Path.GetFullPath("..\\..\\..\\"));

            ServiceCollection = new ServiceCollection();
        }

        public void Init()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RegisterServiceCollection();
            RegisterServiceStart();
            Console.WriteLine("��� TaseTest ��ʼ��");
        }

        /// <summary>
        /// ע�� IServiceCollection �� MemoryCache
        /// </summary>
        public static void RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            serviceCollection.AddDatabase<SqliteMemoryDatabaseConfiguration>();//ʹ�� SQLServer���ݿ�


            SiteConfig.WebRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

            Senparc.Ncf.Core.Register.TryRegisterMiniCore(services => { });
            serviceCollection.AddSenparcGlobalServices(config);

            serviceCollection.AddMemoryCache();//ʹ���ڴ滺��
            serviceCollection.AddRouting();
            var builder = serviceCollection.AddRazorPages();
            builder.AddNcfAreas(_env.Object);

            //�Զ�����ע��ɨ��
            serviceCollection.ScanAssamblesForAutoDI();
            //�Ѿ���������г����Զ�ɨ���ί�У�����ִ��ɨ�裨���룩
            AssembleScanHelper.RunScan();


            var result = serviceCollection.StartEngine(Configuration);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// ע�� RegisterService.Start()
        /// </summary>
        public static void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //ע��
            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(_env.Object, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            IApplicationBuilder app = new ApplicationBuilder(ServiceProvider);
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });

            Console.WriteLine("Senparc.Ncf.XncfBase.Register.UseXncfModules");
            //XncfModules�����룩
            Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService);
        }
    }
}
