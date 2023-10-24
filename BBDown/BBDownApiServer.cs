using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using BBDown.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
namespace BBDown;

public class BBDownApiServer
{
    private WebApplication? app;
    private List<DownloadTask> runningTasks = [];
    private List<DownloadTask> finishedTasks = [];

    public void SetUpServer()
    {
        if (app is not null) return;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.ConfigureHttpJsonOptions((options) =>
        {
            options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.SerializerOptions.TypeInfoResolver, AppJsonSerializerContext.Default);
        });
        app = builder.Build();
        var taskStatusApi = app.MapGroup("/get-tasks");
        taskStatusApi.MapGet("/", handler: () => Results.Json(new DownloadTaskCollection(runningTasks, finishedTasks), AppJsonSerializerContext.Default.DownloadTaskCollection));
        taskStatusApi.MapGet("/running", handler: () => Results.Json(runningTasks, AppJsonSerializerContext.Default.ListDownloadTask));
        taskStatusApi.MapGet("/finished", handler: () => Results.Json(finishedTasks, AppJsonSerializerContext.Default.ListDownloadTask));
        taskStatusApi.MapGet("/{id}", (string id) =>
        {
            var task = finishedTasks.FirstOrDefault(a => a.Aid == id);
            var rtask = runningTasks.FirstOrDefault(a => a.Aid == id);
            if (rtask is not null) task = rtask;
            if (task is null)
            {
                return Results.NotFound();
            }
            else
            {
                return Results.Json(task, AppJsonSerializerContext.Default.DownloadTask);
            }
        });
        app.MapPost("/add-task", (MyOptionBindingResult<MyOption> bindingResult) =>
        {
            if (!bindingResult.IsValid)
            {
                //var exception = bindingResult.Exception;
                return Results.BadRequest("输入有误");
            }
            var req = bindingResult.Result;
            AddDownloadTask(req);
            return Results.Ok();
        });
        var finishedRemovalApi = app.MapGroup("remove-finished");
        finishedRemovalApi.MapGet("/", () => { finishedTasks.RemoveAll(t => true); return Results.Ok(); });
        finishedRemovalApi.MapGet("/failed", () => { finishedTasks.RemoveAll(t => !t.IsSuccessful); return Results.Ok(); });
        finishedRemovalApi.MapGet("/{id}", (string id) => { finishedTasks.RemoveAll(t => t.Aid == id); return Results.Ok(); });
    }

    public void Run(string url)
    {
        if (app is null) return;
        bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            && uriResult.Scheme == Uri.UriSchemeHttp;
        if (!result)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{url}不是合法的http URL，url示例：http://0.0.0.0:5000");
            Console.WriteLine("如果您需要https，请额外配置反向代理");
            Console.ResetColor();
            Console.WriteLine();
            Thread.Sleep(1);
            Environment.Exit(1);
        }
        app.Run(url);
    }

    private async Task AddDownloadTask(MyOption option)
    {
        var aid = await BBDownUtil.GetAvIdAsync(option.Url);
        if (runningTasks.Any(task => task.Aid == aid)) return;
        var task = new DownloadTask(aid, option.Url, DateTimeOffset.Now.ToUnixTimeSeconds());
        runningTasks.Add(task);
        try
        {
            var (encodingPriority, dfnPriority, firstEncoding, downloadDanmaku, input, savePathFormat, lang, aidOri, delay) = Program.SetUpWork(option);
            var (fetchedAid, vInfo, apiType) = await Program.GetVideoInfo(option, aidOri, input);
            task.Title = vInfo.Title;
            task.Pic = vInfo.Pic;
            task.VideoPubTime = vInfo.PubTime;
            await Program.DownloadPage(option, vInfo, encodingPriority, dfnPriority, firstEncoding, downloadDanmaku,
                        input, savePathFormat, lang, fetchedAid, delay, apiType, task);
            task.IsSuccessful = true;
        }
        catch (Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{aid}下载失败");
            var msg = Config.DEBUG_LOG ? e.ToString() : e.Message;
            Console.Write($"{msg}{Environment.NewLine}请尝试升级到最新版本后重试!");
            Console.ResetColor();
            Console.WriteLine();
        }
        task.TaskFinishTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (task.IsSuccessful)
        {
            task.Progress = 1f;
            task.DownloadSpeed = (double)(task.TotalDownloadedBytes / (task.TaskFinishTime - task.TaskCreateTime));
        }
        runningTasks.Remove(task);
        finishedTasks.Add(task);
    }
}

public record DownloadTask(string Aid, string Url, long TaskCreateTime)
{
    [JsonInclude]
    public string? Title = null;
    [JsonInclude]
    public string? Pic = null;
    [JsonInclude]
    public long? VideoPubTime = null;
    [JsonInclude]
    public long? TaskFinishTime = null;
    [JsonInclude]
    public double Progress = 0f;
    [JsonInclude]
    public double DownloadSpeed = 0f;
    [JsonInclude]
    public double TotalDownloadedBytes = 0f;
    [JsonInclude]
    public bool IsSuccessful = false;
};
public record DownloadTaskCollection(List<DownloadTask> Running, List<DownloadTask> Finished);

record struct MyOptionBindingResult<T>(T? Result, Exception? Exception)
{
    public bool IsValid => Exception is null;

    public static async ValueTask<MyOptionBindingResult<MyOption>> BindAsync(HttpContext httpContext)
    {
        try
        {
            var item = await httpContext.Request.ReadFromJsonAsync(SourceGenerationContext.Default.MyOption);

            if (item is null) return new(default, new NoNullAllowedException());

            return new(item, null);
        }
        catch (Exception ex)
        {
            return new(default, ex);
        }
    }
}

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(DownloadTask))]
[JsonSerializable(typeof(List<DownloadTask>))]
[JsonSerializable(typeof(DownloadTaskCollection))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{

}

[JsonSerializable(typeof(MyOption))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
