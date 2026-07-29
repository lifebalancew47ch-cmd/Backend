using LifeBalance.Notifications.Application.Interfaces;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Messaging;

namespace LifeBalance.Notifications.Infrastructure.Services;

public class FirebasePushProvider : IPushNotificationProvider
{
    private readonly ILogger<FirebasePushProvider> _logger;

    public FirebasePushProvider(ILogger<FirebasePushProvider> logger)
    {
        _logger = logger;
    }

    public async Task<PushResult> SendToDeviceAsync(string deviceToken, string title, string body, string? payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                return new PushResult { Success = false, ErrorMessage = "Device token is empty", Provider = "Firebase" };
            }

            var message = new Message
            {
                Fid = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                }
            };

            if (!string.IsNullOrEmpty(payload))
            {
                message.Data = new Dictionary<string, string> { { "payload", payload } };
            }

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation("Firebase push sent, response: {Response}", response);

            return new PushResult { Success = true, Provider = "Firebase" };
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase messaging error");
            return new PushResult { Success = false, ErrorMessage = ex.Message, Provider = "Firebase" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending Firebase push");
            return new PushResult { Success = false, ErrorMessage = ex.Message, Provider = "Firebase" };
        }
    }

    public async Task<List<PushResult>> SendToDevicesAsync(List<string> deviceTokens, string title, string body, string? payload)
    {
        var results = new List<PushResult>();

        if (deviceTokens.Count == 0)
        {
            results.Add(new PushResult { Success = false, ErrorMessage = "No device tokens provided", Provider = "Firebase" });
            return results;
        }

        try
        {
            var messages = deviceTokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(token => new Message
                {
                    Fid = token,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = !string.IsNullOrEmpty(payload)
                        ? new Dictionary<string, string> { { "payload", payload } }
                        : null
                })
                .ToList();

            if (messages.Count == 0)
            {
                results.Add(new PushResult { Success = false, ErrorMessage = "No valid device tokens", Provider = "Firebase" });
                return results;
            }

            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);

            for (int i = 0; i < response.Responses.Count; i++)
            {
                var resp = response.Responses[i];
                results.Add(new PushResult
                {
                    Success = resp.IsSuccess,
                    ErrorMessage = resp.Exception?.Message,
                    Provider = "Firebase"
                });

                if (!resp.IsSuccess)
                {
                    _logger.LogWarning("Firebase send failed: {Error}", resp.Exception?.Message);
                }
            }

            _logger.LogInformation("Firebase batch push: {Success}/{Total} sent successfully",
                results.Count(r => r.Success), results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase batch send failed");
            results.AddRange(deviceTokens.Select(t => new PushResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = "Firebase"
            }));
        }

        return results;
    }
}
