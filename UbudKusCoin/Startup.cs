using Coravel;
using GrpcService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UbudKusCoin.Sceduler;


namespace Main
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public static void ConfigureServices(IServiceCollection services)
        {


            services.AddScheduler();
            services.AddTransient<BlockJob>();

            services.AddGrpc();

            // add cors support
            services.AddCors(o => o.AddPolicy("AllowAll", builder =>
            {
                //builder.WithOrigins("localhost:4200", "YourCustomDomain");
                //builder.WithMethods("POST, OPTIONS");

                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();
            }


            // app.UseHttpsRedirection();

            app.UseRouting();
            // add support grpc call from web app, Must be added between UseRouting and UseEndpoints
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            //app.UseGrpcWeb(); // Must be added between UseRouting and UseEndpoints
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapGrpcService<BlockchainService>(); //.RequireHost("*:5008");
                //endpoints.MapGrpcService<BlockchainService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<BlockchainService>().RequireCors("AllowAll");

                // for grpcweb. is bellow code necessary?
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync(
                        "Communication with gRPC endpoints" +
                        " must be made through a gRPC client.");
                });

            });
        }
    }
}
