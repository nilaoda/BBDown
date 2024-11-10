using BBDown.Core.Fetcher;

namespace BBDown.Core;

public static class FetcherFactory
{
    /// <summary>
    /// 根据不同场景获取不同的Info解析器
    /// </summary>
    /// <param name="aidOri"></param>
    /// <returns>IFetcher</returns>
    public static IFetcher CreateFetcher(string aidOri, bool useIntlApi)
    {
        IFetcher fetcher = new NormalInfoFetcher();
        if (aidOri.StartsWith("cheese"))
        {
            fetcher = new CheeseInfoFetcher();
        }
        else if (aidOri.StartsWith("ep"))
        {
            fetcher = useIntlApi ? new IntlBangumiInfoFetcher() : new BangumiInfoFetcher();
        }
        else if (aidOri.StartsWith("mid"))
        {
            fetcher = new SpaceVideoFetcher();
        }
        else if (aidOri.StartsWith("listBizId"))
        {
            fetcher = new MediaListFetcher();
        }
        else if (aidOri.StartsWith("seriesBizId"))
        {
            fetcher = new SeriesListFetcher();
        }
        else if (aidOri.StartsWith("favId"))
        {
            fetcher = new FavListFetcher();
        }
        return fetcher;
    }
}