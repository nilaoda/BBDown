using BBDown;

internal class ServeRequestOptions : MyOption
{

    /// <summary>
    /// 任务完成回调Http请求地址
    /// </summary>
    public string? CallBackWebHook { get; set; }

}