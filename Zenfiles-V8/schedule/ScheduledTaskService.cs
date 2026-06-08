using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using testing_scheduler_with_signalir.notitication;
using testing_scheduler_with_signalir.schedule;
using Zenfiles_V8.schedule;

namespace testing_scheduler_with_signalir.schedule
{
    public class ScheduledTaskService : BackgroundService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly pullAssignments _pullAssignments;
        public ScheduledTaskService(IHubContext<NotificationHub> hubContext, pullAssignments pullAssignments)
        {
            _hubContext = hubContext;
            _pullAssignments = pullAssignments;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
               
                var jobsToProcess = FileStorage.Load();

                foreach (var job in jobsToProcess.ToList())
                {
                    // Check if job has expired (older than 1 hour)
                    if (DateTime.UtcNow - job.CreatedAt > TimeSpan.FromHours(1))
                    {
                        jobsToProcess.Remove(job);
                        Console.WriteLine($"{job.Name} expired after 1 hour and was removed.");
                        continue;
                    }

                    try
                    {
                        var response = await _httpClient.GetAsync(job.Url, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            // ✅ Success → remove from list
                            jobsToProcess.Remove(job);
                            Console.WriteLine($"{job.Name} succeeded and was removed.");
                        }
                        else
                        {
                            // ❌ Failure → keep in list until expiration
                            Console.WriteLine($"{job.Name} failed with status {response.StatusCode}, will retry until 1 hour passes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // ❌ Exception → keep in list until expiration
                        Console.WriteLine($"{job.Name} failed with error: {ex.Message}, will retry until 1 hour passes.");
                    }
                }
                FileStorage.Save(jobsToProcess);
                #region process vaults and assignments
                {
                    var assignmentmodels = _pullAssignments.DoSomething();
                    // Example: send to specific user in group "test" with ID "user12345"
                    foreach (var job in assignmentmodels)
                    {
                        await _hubContext.Clients.Group($"{job.VaultGuid}/{job.UserID}")
                     .SendAsync("ReceiveMessage", $"Subject: New Assignment Available for AuctionMessage:\r\nHello,You have been assigned a new task titled {job.Title}.\r\nThis assignment is now available for you to auction.Please review the details and proceed with the auction process at your earliest convenience.Thank you,\r\n");
                        // Run every 5 minutes
                    }

                }
                #endregion

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

