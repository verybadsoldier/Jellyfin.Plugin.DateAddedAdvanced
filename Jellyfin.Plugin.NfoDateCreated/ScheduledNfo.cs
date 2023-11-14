using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.Libraries;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.NfoCreateDate
{
    public class ScheduledNfo : IScheduledTask
    {
        private readonly ILogger<ScheduledNfo> _logger;

        private readonly ILibraryManager _libraryManager;

        public ScheduledNfo(ILogger<ScheduledNfo> logger, ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _logger.LogInformation("Retention task scheduled");
            _libraryManager = libraryManager;
        }

        public string Name => "Update NFO DateCreated";

        public string Category => "NFO DateCreated";

        public string Description => "Update NFO DateCreated";

        public string Key => "NFODateCreatedTask";

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var itesm = _libraryManager.GetItemList(new InternalItemsQuery());

            foreach (var item in itesm.OfType<Folder>())
            {
               var f = item.Id;
            }

            /*
            // Is retDays 0.. If So Exit...
            if (!int.TryParse(KodiSyncQueuePlugin.Instance.Configuration.RetDays, out var retDays) || retDays == 0)
            {
                _logger.LogInformation("Retention deletion not possible if retention days is set to zero!");
                return Task.CompletedTask;
            }

            // Check Database
            var dt = DateTime.UtcNow.AddDays(-retDays);
            var dtl = (long)dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            KodiSyncQueuePlugin.Instance.DbRepo.DeleteOldData(dtl);
            */
            return Task.CompletedTask;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromMinutes(1).Ticks
                }
            };
        }
    }
}
