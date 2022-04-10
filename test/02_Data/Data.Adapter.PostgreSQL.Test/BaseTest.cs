using System;
using System.Threading.Tasks;
using Data.Common.Test;
using Data.Common.Test.Domain.Article;
using Data.Common.Test.Domain.Category;
using Data.Common.Test.Infrastructure;
using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mkh.Data.Abstractions;
using Xunit.Abstractions;

namespace Data.Adapter.PostgreSQL.Test;

public class BaseTest
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IDbContext _dbContext;
    protected readonly IArticleRepository _articleRepository;
    protected readonly ICategoryRepository _categoryRepository;

    public BaseTest(ITestOutputHelper output)
    {
        var connString = "server=localhost;uid=mkh;password=mkh;database=mkh;port=5433;commandtimeout=1024;";
        var services = new ServiceCollection();
        //日志
        services.AddLogging(builder =>
        {
            builder.AddXunit(output, new LoggingConfig
            {
                LogLevel = LogLevel.Trace
            });
        });

        //自定义账户信息解析器
        services.AddSingleton<IAccountResolver, CustomAccountResolver>();

        services
            .AddMkhDb<BlogDbContext>(options =>
            {
                //开启日志
                options.Log = true;
            })
            .UsePostgreSQL(connString)
            .AddRepositoriesFromAssembly(typeof(BlogDbContext).Assembly)
            .AddRepositoriesFromAssembly(this.GetType().Assembly)
            //开启代码优先
            .AddCodeFirst(options =>
            {
                options.CreateDatabase = true;
                options.UpdateColumn = true;

                options.BeforeCreateDatabase = ctx =>
                {
                    ctx.Logger.Write("BeforeCreateDatabase", "数据库创建前事件");
                };
                options.AfterCreateDatabase = ctx =>
                {
                    ctx.Logger.Write("AfterCreateDatabase", "数据库创建后事件");
                };
                options.BeforeCreateTable = (ctx, entityDescriptor) =>
                {
                    ctx.Logger.Write("BeforeCreateTable", "表创建前事件，表名称：" + entityDescriptor.TableName);
                };
                options.AfterCreateTable = (ctx, entityDescriptor) =>
                {
                    ctx.Logger.Write("AfterCreateTable", "表创建后事件，表名称：" + entityDescriptor.TableName);
                };
            })
            .Build();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetService<BlogDbContext>();
        _articleRepository = _serviceProvider.GetService<IArticleRepository>();
        _categoryRepository = _serviceProvider.GetService<ICategoryRepository>();
    }

    protected async Task ClearTable()
    {
        await _articleRepository.Execute("truncate Article;");
        await _articleRepository.Execute("truncate MyCategory;");
    }
}
