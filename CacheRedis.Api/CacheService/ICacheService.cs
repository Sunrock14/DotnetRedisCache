namespace CacheRedis.Api.CacheService;

public interface ICacheService
{
    /// <summary>  
    /// Cache'te veri varsa onu döndürür, yoksa veritabanından çekip cache'e kaydeder  
    /// </summary>  
    /// <typeparam name="T">Dönüş veri tipi</typeparam>  
    /// <param name="key">Cache anahtarı</param>  
    /// <param name="getData">Veri olmadığında çalışacak fonksiyon</param>  
    /// <param name="slidingExpiration">Son erişimden itibaren geçerlilik süresi. Eğer verilen süre içerisinde tekrar istek atılmaz ise maksimum yaşam süresinden önce silinir</param>  
    /// <param name="absoluteExpiration">Maksimum yaşam süresi</param>  
    /// <returns>Cache'ten veya veritabanından alınan veri</returns> 
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getData, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);
    /// <summary>
    /// Cache'ten veri çeker
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T> GetAsync<T>(string key);
    /// <summary>
    /// Cache verileri yazar
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="slidingExpiration"></param>
    /// <param name="absoluteExpiration"></param>
    /// <returns></returns>
    Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);
    /// <summary>
    /// Cache'ten veri siler
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task RemoveAsync(string key);
    /// <summary>
    /// Index Key oluşturmak için yardımcı method
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    string GenerateCacheKey(params string[] parts);
}


