using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Plugins.Imdb
{
    internal class ImdbCache
    {
        private Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();
        private TimeSpan _ttl = TimeSpan.FromHours(12);

        public ImdbCache(TimeSpan timeToLive)
        {
            _ttl = timeToLive;
        }

        public void Add(string imdbId, float rating)
        {
            _cache[imdbId] = new CacheItem { Rating = rating, Timestamp = DateTime.UtcNow };
        }

        public float? Query(string imdbId)
        {
            if (!_cache.ContainsKey(imdbId))
            {
                return null;
            }

            var entry = _cache[imdbId];
            if (DateTime.UtcNow - entry.Timestamp > _ttl)
            {
                _cache.Remove(imdbId);
                return null;
            }

            return entry.Rating;
        }

        private struct CacheItem
        {
            public float Rating { get; set; }

            public DateTime Timestamp { get; set; }
        }
    }
}
