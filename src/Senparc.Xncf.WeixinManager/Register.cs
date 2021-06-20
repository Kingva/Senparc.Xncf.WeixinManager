﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.NeuChar;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.WeixinManager.Models;
using Senparc.Xncf.WeixinManager.Services;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager
{
    public partial class Register : XncfRegisterBase, IXncfRegister //注册 XNCF 基础模块接口（必须）
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.WeixinManager";

        public override string Uid => "EB84CB21-AC22-406E-0001-000000000001";


        public override string Version => "0.6.1-beta1";


        public override string MenuName => "微信管理";


        public override string Icon => "fa fa-weixin";


        public override string Description => @"NCF 模块：盛派官方发布的微信管理后台
使用此插件可以在 NCF 中快速集成微信公众号、小程序的部分基础管理功能，欢迎大家一起扩展！
微信 SDK 基于 Senparc.Weixin SDK 开发。";

        public override IList<Type> Functions => new Type[] { };


        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            //services.AddScoped<PostModel>(ServiceProvider =>
            //{
            //    //根据条件生成不同的PostModel


            //});


            return base.AddXncfModule(services, configuration);//如果重写此方法，必须调用基类方法
        }

        public async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await base.MigrateDatabaseAsync(serviceProvider);
        }

        public async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO:可以在基础模块里给出选项是否删除

            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            WeixinSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as WeixinSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            //按照删除顺序排序
            var types = new[] { typeof(UserTag_WeixinUser), typeof(UserTag), typeof(WeixinUser), typeof(MpAccount) };
            types.ToList().AddRange(dropTableKeys);
            types = types.Distinct().ToArray();
            //指定需要删除的数据实体
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, types);

            #endregion

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        private List<MpAccount> _allMpAccounts = null;

        private List<MpAccount> GetAllMpAccounts(IServiceProvider serviceProvider)
        {
            try
            {
                if (_allMpAccounts == null)
                {
                    var mpAccountService = serviceProvider.GetService<ServiceBase<MpAccount>>();
                    _allMpAccounts = mpAccountService.GetFullList(z => z.AppId != null && z.AppId.Length > 0, z => z.Id, OrderingType.Ascending);
                }
                return _allMpAccounts;
            }
            catch
            {
                return new List<MpAccount>();
            }
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            //注册微信
            Senparc.Weixin.WeixinRegister.UseSenparcWeixin(null, null, senparcSetting: null);

            try
            {
                //未安装数据库表的情况下可能会出错，因此需要try
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var allMpAccount = GetAllMpAccounts(scope.ServiceProvider);

                    //批量自动注册公众号
                    foreach (var mpAccount in allMpAccount)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await AccessTokenContainer.RegisterAsync(mpAccount.AppId, mpAccount.AppSecret, $"{mpAccount.Name}-{mpAccount.Id}");
                        });

                        //TODO：更多执行过程中的动态注册
                    }
                }
            }
            catch
            {
            }

            Console.WriteLine("UseSwaggerUI");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.DocumentTitle = "Senparc Weixin SDK Demo API";
                c.InjectJavascript("/lib/jquery/dist/jquery.min.js");
                c.InjectJavascript("/js/swagger.js");
                //c.InjectJavascript("/js/tongji.js");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

                foreach (var neucharApiDocAssembly in WeixinApiService.WeixinApiAssemblyCollection)
                {
                    //TODO:真实的动态版本号
                    var verion = WeixinApiService.WeixinApiAssemblyVersions[neucharApiDocAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
                    var docName = WeixinApiService.GetDocName(neucharApiDocAssembly.Key);

                    Console.WriteLine($"\tAdd {docName}");

                    c.SwaggerEndpoint($"/swagger/{docName}/swagger.json", $"{neucharApiDocAssembly.Key} v{verion}");
                }

                //OAuth 暂时不使用   —— Jeffrey Su   2021.06.20
                ////OAuth     https://www.cnblogs.com/miskis/p/10083985.html
                //c.OAuthClientId("e65ea785b96b442a919965ccf857aba3");//客服端名称
                //c.OAuthAppName("微信 API Swagger 文档 "); // 描述
            });


            return base.UseXncfModule(app, registerService);
        }

    }
    #endregion

    class RemoveVerbsFilter : IDocumentFilter
    {
        //public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        //{
        //    foreach (PathItem path in swaggerDoc.paths.Values)
        //    {
        //        path.delete = null;
        //        //path.get = null; // leaving GET in
        //        path.head = null;
        //        path.options = null;
        //        path.patch = null;
        //        path.post = null;
        //        path.put = null;
        //    }
        //}

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            //每次切换定义，都需要经过比较长的时间才到达这里

            return;
            string platformType;
            var title = swaggerDoc.Info.Title;

            if (title.Contains(PlatformType.WeChat_OfficialAccount.ToString()))
            {
                platformType = PlatformType.WeChat_OfficialAccount.ToString();
            }
            else if (title.Contains(PlatformType.WeChat_Work.ToString()))
            {
                platformType = PlatformType.WeChat_Work.ToString();
            }
            else if (title.Contains(PlatformType.WeChat_Open.ToString()))
            {
                platformType = PlatformType.WeChat_Open.ToString();
            }
            else if (title.Contains(PlatformType.WeChat_MiniProgram.ToString()))
            {
                platformType = PlatformType.WeChat_MiniProgram.ToString();
            }
            else
            {
                throw new NotImplementedException($"未提供的 PlatformType 类型，Title：{title}");
            }

            var pathList = swaggerDoc.Paths.Keys;

            foreach (var path in pathList)
            {
                if (!path.Contains(platformType))
                {
                    //移除非当前模块的API对象
                    swaggerDoc.Paths.Remove(path);
                }
            }

            //SwaggerOperationAttribute
            //移除Schema对象
            //var toRemoveSchema = context.SchemaRepository.Schemas.Where(z => !z.Key.Contains(platformType)).ToList();//结果为全部删除，仅测试
            //foreach (var schema in toRemoveSchema)
            //{
            //    context.SchemaRepository.Schemas.Remove(schema.Key);
            //}
        }
    }

    //public class AuthResponsesOperationFilter : IOperationFilter
    //{
    //    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    //    {
    //        //获取是否添加登录特性
    //        var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
    //         .Union(context.MethodInfo.GetCustomAttributes(true))
    //         .OfType<AuthorizeAttribute>().Any();

    //        if (authAttributes)
    //        {
    //            operation.Responses.Add("401", new OpenApiResponse { Description = "暂无访问权限" });
    //            operation.Responses.Add("403", new OpenApiResponse { Description = "禁止访问" });
    //            operation.Security = new List<OpenApiSecurityRequirement>
    //            {
    //                new OpenApiSecurityRequirement { { new OpenApiSecurityScheme() {  Name= "oauth2" }, new[] { "swagger_api" } }}
    //            };
    //        }
    //    }
    //}

}
