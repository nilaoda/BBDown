namespace BBDown.Core
{
    public interface IFetcher
    {
        Task<Entity.VInfo> FetchAsync(string id);
    }
}
