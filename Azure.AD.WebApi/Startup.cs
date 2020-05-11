using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AD.WebApi.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Azure.AD.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IWebHostEnvironment Environment { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new Appsettings(Environment.ContentRootPath));

            services.AddAuthentication(AzureADDefaults.JwtBearerAuthenticationScheme)
               .AddAzureADBearer(options => Configuration.Bind("AzureAd", options));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入Bearer {token}（注意两者之间是一个空格）\"",
                    Type = SecuritySchemeType.OAuth2,
                    In = ParameterLocation.Header,//jwt默认存放Authorization信息的位置(请求头中)
                    Flows = new OpenApiOAuthFlows()
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://login.chinacloudapi.cn/{ Appsettings.app(new string[] { "AzureAD", "TenantId" })}/oauth2/authorize")

                        }
                    }
                });
                // 在header中添加token，传递到后台
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //根据版本名称倒序 遍历展示
                var ApiName = Appsettings.app(new string[] { "Startup", "ApiName" });
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{ApiName} v1");

                c.OAuthClientId(Appsettings.app(new string[] { "Swagger", "ClientId" }));
                //c.OAuthClientSecret(Appsettings.app(new string[] { "Swagger", "ClientSecret" }));
                c.OAuthRealm(Appsettings.app(new string[] { "AzureAD", "ClientId" }));
                c.OAuthAppName("My API V1");
                c.OAuthScopeSeparator(" ");
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "resource", Appsettings.app(new string[] { "AzureAD", "ClientId" }) } });
            });
            #endregion

            // open authentication middleware
            app.UseAuthentication();


            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
