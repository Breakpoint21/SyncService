using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kyrodan.HiDrive;
using Kyrodan.HiDrive.Authentication;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NCrontab;
using Serilog;
using SyncService.ObjectModel.Folder;
using SyncService.Options;
using SyncService.Services.Account;

namespace SyncService.Services.Sync
{
    public class HiDriveSyncTask
    {
        private FolderConfiguration _folderConfiguration;
        private readonly IHiDriveClient _hiDriveClient;
        private readonly AccountService _accountService;
        private CrontabSchedule _schedule;
        private DateTime _nextOccurrence;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _runningTask;
        private Task _processTask;
        
        public HiDriveSyncTask(FolderConfiguration folderConfiguration, IHiDriveClient hiDriveClient,
            AccountService accountService)
        {
            _folderConfiguration = folderConfiguration;
            _hiDriveClient = hiDriveClient;
            _accountService = accountService;
        }

        public bool IsRunning => _processTask != null && !_processTask.IsCompleted;

        public async Task WaitForShutdown() => await _runningTask;

        public void Activate(CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _schedule = CrontabSchedule.Parse(_folderConfiguration.Schedule);
            _nextOccurrence = _schedule.GetNextOccurrence(DateTime.Now);

            _runningTask = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        private async Task Run()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now > _nextOccurrence)
                {
                    if(_processTask == null) _processTask = Process(_cancellationTokenSource.Token);
                    try
                    {
                        await _processTask;
                    }
                    finally
                    {
                        _processTask = null;
                    }
                    _nextOccurrence = _schedule.GetNextOccurrence(DateTime.Now);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
            }
        }

        public void StartNow()
        {
            if (_processTask == null)
            {
                _processTask = Process(_cancellationTokenSource.Token);
                _nextOccurrence = DateTime.Now.AddSeconds(-1);
            }
        }
        
        private async Task Process(CancellationToken token)
        {
            try
            {
                Log.Verbose("Start HiDrive Sync run for {@folder}", _folderConfiguration);
                
                var hiDriveSyncExecutor = new HiDriveSyncExecutor(_hiDriveClient);
                await _hiDriveClient.Authenticator.AuthenticateByRefreshTokenAsync(_accountService.Accounts.HiDriveAccount.RefreshToken);

                var syncExecutionInfo = new HiDriveSyncExecutionInfo
                {
                    Name = _folderConfiguration.Label,
                    SourcePath = _folderConfiguration.SourcePath,
                    DestinationPath = _folderConfiguration.DestinationPath,
                    LogPath = _folderConfiguration.LogPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncService", "Log"),
                    SyncContentOnly = false
                };

                var syncResult = await hiDriveSyncExecutor.Sync(syncExecutionInfo, token);
                if (_folderConfiguration.NotificationConfiguration.SendEmail)
                {
                    await SendEmail(syncResult);
                }
            }
            catch (AuthenticationException exception)
            {
                Log.Error(exception, "Authentication for account {@account} failed", _accountService.Accounts.HiDriveAccount);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failure while executing sync!");
                if (_folderConfiguration.NotificationConfiguration.SendEmail)
                {
                    await SendFailureEmail(exception);
                }
            }
        }

        public void Update(FolderConfiguration folderConfiguration)
        {
            Log.Debug("Update HiDrive Sync task for {@folder}", _folderConfiguration);
            _schedule = CrontabSchedule.Parse(folderConfiguration.Schedule);
            _nextOccurrence = _schedule.GetNextOccurrence(DateTime.Now);
            Log.Debug("Next Occurrence is now: {@NextOccurence}", _nextOccurrence);

            _folderConfiguration = folderConfiguration;
        }

        public void Stop()
        {
            Log.Information("Stop HiDrive Sync task for {@folder}", _folderConfiguration);
            _cancellationTokenSource?.Cancel();
        }

        private async Task SendEmail(SyncResult syncResult)
        {
            var smtpAccount = _accountService.Accounts.SmtpAccounts.FirstOrDefault(account =>
                account.Id == _folderConfiguration.NotificationConfiguration.EmailConfigurationId);

            if (smtpAccount != null)
            {
                var body = new StringBuilder();
                body.AppendLine(syncResult.TaskName);
                body.AppendLine($"State: {syncResult.State}");
                body.AppendLine();
                body.AppendLine($"Started at: {syncResult.Start}");
                body.AppendLine($"Finished at: {syncResult.End}");
                body.AppendLine();
                body.AppendLine($"Source: {_folderConfiguration.SourcePath}");
                body.AppendLine($"Destination: {_folderConfiguration.DestinationPath}");
                body.AppendLine();
                if (syncResult.Exception != null)
                {
                    body.AppendLine($"Exception: {syncResult.Exception}");
                    body.AppendLine();
                }
                else
                {
                    body.AppendLine("Summary:");
                    body.AppendLine($"Total:\t{syncResult.Items.Count}");
                    body.AppendLine($"Added:\t{syncResult.Items.Count(result => result.Action == SyncAction.Added && result.State == SyncState.Successful)}");
                    body.AppendLine($"Updated:\t{syncResult.Items.Count(result => result.Action == SyncAction.Updated && result.State == SyncState.Successful)}");
                    body.AppendLine($"Unchanged:\t{syncResult.Items.Count(result => result.Action == SyncAction.None && result.State == SyncState.Successful)}");
                    body.AppendLine($"Failed:\t{syncResult.Items.Count(result => result.State == SyncState.Failed)}");
                    
                }
                body.AppendLine("Items:");
                foreach (var resultItem in syncResult.Items)
                {
                    body.AppendLine(
                        $"\t{resultItem.Name} {resultItem.Action} {resultItem.State} {resultItem.Exception}");
                }

                try
                {
                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress("SyncService", "noreply@syncservice.com"));
                    mimeMessage.To.Add(new MailboxAddress(smtpAccount.EmailTo));
                    mimeMessage.Subject = $"[Sync] {syncResult.TaskName} - {syncResult.State}";
                    mimeMessage.Body = new TextPart("plain") {Text = body.ToString()};

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync(smtpAccount.Server, smtpAccount.Port);
                        await client.AuthenticateAsync(smtpAccount.Username, smtpAccount.Password);
                        await client.SendAsync(mimeMessage);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failure while sending email");
                }
            }
        }

        private async Task SendFailureEmail(Exception exception)
        {
            var smtpAccount = _accountService.Accounts.SmtpAccounts.FirstOrDefault(account =>
                account.Id == _folderConfiguration.NotificationConfiguration.EmailConfigurationId);

            if (smtpAccount != null)
            {
                var body = new StringBuilder();
                body.AppendLine(_folderConfiguration.Label);
                body.AppendLine();
                body.AppendLine($"Exception Detail: {exception}");

                try
                {
                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress("SyncService", "noreply@syncservice.com"));
                    mimeMessage.To.Add(new MailboxAddress(smtpAccount.EmailTo));
                    mimeMessage.Subject = $"[Sync] {_folderConfiguration.Label} - FAILURE";
                    mimeMessage.Body = new TextPart("plain") { Text = body.ToString() };

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync(smtpAccount.Server, smtpAccount.Port);
                        await client.AuthenticateAsync(smtpAccount.Username, smtpAccount.Password);
                        await client.SendAsync(mimeMessage);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failure while sending failure email");
                }
            }
        }
    }
}