using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kyrodan.HiDrive;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using SyncService.Options;
using SyncService.Services.Account;
using SyncService.Services.Folder;

namespace SyncService.Services.Sync
{
    public class HiDriveSyncService : IHostedService
    {
        private readonly ConcurrentDictionary<Guid, HiDriveSyncTask> _hiDriveSyncTasks = new ConcurrentDictionary<Guid, HiDriveSyncTask>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public HiDriveSyncService(FolderConfigurationService folderConfigurationService, IHiDriveClient hiDriveClient,  AccountService accountService)
        {
            var allConfigs = folderConfigurationService.GetAllConfigs();
            foreach (var folderConfiguration in allConfigs.Where(configuration => !string.IsNullOrEmpty(configuration.Schedule)))
            {
                _hiDriveSyncTasks.TryAdd(folderConfiguration.Id, new HiDriveSyncTask(folderConfiguration, hiDriveClient, accountService));
            }

            folderConfigurationService.ConfigurationUpdated.Subscribe(configuration =>
            {
                if (_hiDriveSyncTasks.TryGetValue(configuration.Id, out var hiDriveSyncTask))
                {
                    Log.Information("Folder {folder} updated!", configuration);
                    hiDriveSyncTask.Update(configuration);
                }
                else
                {
                    Log.Information("Folder {folder} added!", configuration);
                    hiDriveSyncTask = new HiDriveSyncTask(configuration, hiDriveClient,
                        accountService);
                    if (_hiDriveSyncTasks.TryAdd(configuration.Id, hiDriveSyncTask))
                    {
                        hiDriveSyncTask.Activate(_cts.Token);
                        //_tasks.TryAdd(configuration.Id, Task.Factory.StartNew(() => hiDriveSyncTask.Start(_cts.Token), TaskCreationOptions.LongRunning));
                    }
                }
            });
            folderConfigurationService.ConfigurationDeleted.Subscribe(configuration =>
            {
                if (_hiDriveSyncTasks.TryRemove(configuration.Id, out var hiDriveSyncTask))
                {
                    Log.Information("Folder {folder} removed!", configuration);
                    hiDriveSyncTask.Stop();
                }
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("Starting HiDriveSyncService!");
            ExecuteAsync(_cts.Token);
            return Task.CompletedTask;
        }

        private void ExecuteAsync(CancellationToken token)
        {
            foreach (var hiDriveSyncTask in _hiDriveSyncTasks)
            {
                hiDriveSyncTask.Value.Activate(token);

                //_tasks.TryAdd(hiDriveSyncTask.Key, Task.Factory.StartNew(() => hiDriveSyncTask.Value.Start(token), TaskCreationOptions.LongRunning));
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Stopping HiDriveSyncService!");
            // Stop called without start
            //if (_tasks.Count == 0)
            //{
            //    return;
            //}

            // Signal cancellation to the executing method
            _cts.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(Task.WhenAll(_hiDriveSyncTasks.Values.Where(task => task.IsRunning).Select(task => task.WaitForShutdown())), Task.Delay(-1, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        public HiDriveSyncTask GetTask(Guid configurationId)
        {
            if (_hiDriveSyncTasks.TryGetValue(configurationId, out var hiDriveSyncTask))
            {
                return hiDriveSyncTask;
            }

            return null;
        }
    }
}