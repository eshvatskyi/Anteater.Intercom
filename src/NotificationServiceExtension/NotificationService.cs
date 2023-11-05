using UserNotifications;

namespace Anteater.Intercom;

[Register("NotificationService")]
public class NotificationService : UNNotificationServiceExtension
{
    protected NotificationService(IntPtr handle) : base(handle) { }

    public override void DidReceiveNotificationRequest(UNNotificationRequest request, Action<UNNotificationContent> contentHandler)
    {
        Console.WriteLine($"[MAUI] notification with content received.");

        var content = (UNMutableNotificationContent)request.Content.MutableCopy();

        var userDefaults = new NSUserDefaults("group.com.anteater.intercom", NSUserDefaultsType.SuiteName);

        var attachmentUri = userDefaults.URLForKey("snapshotUrl");

        if (attachmentUri is null)
        {
            contentHandler(content);
            return;
        }

        Console.WriteLine($"[MAUI] download starting {attachmentUri}");

        var attachment = DownloadAttachment(attachmentUri).GetAwaiter().GetResult();

        if (attachment is null)
        {
            contentHandler(content);
            return;
        }

        content.Attachments = new[] { attachment };

        contentHandler(content);
    }

    private static async Task<UNNotificationAttachment> DownloadAttachment(Uri snapshotUrl)
    {
        var request = await NSUrlSession.SharedSession.CreateDownloadTaskAsync(snapshotUrl);

        Console.WriteLine($"[MAUI] download complete {request.Location}");

        var attachmentUrl = GetLocalFileUrl(request.Response as NSHttpUrlResponse);

        if (attachmentUrl is null)
        {
            return null;
        }

        if (!NSFileManager.DefaultManager.Move(request.Location, attachmentUrl, out _))
        {
            return null;
        }

        return UNNotificationAttachment.FromIdentifier("", attachmentUrl, (NSDictionary)null, out _);
    }

    private static NSUrl GetLocalFileUrl(NSHttpUrlResponse response)
    {
        if (response is null)
        {
            return null;
        }

        var contentType = response.GetHttpHeaderValue("Content-Type");

        Console.WriteLine($"[MAUI] response content-type: {contentType}");

        if (contentType?.Split('/') is not ["image", var imageType])
        {
            return null;
        }

        var fileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.{imageType}";

        var cachePath = NSSearchPath
            .GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User, true)
            .FirstOrDefault();

        if (cachePath is null)
        {
            return null;
        }

        Console.WriteLine($"[MAUI] cache path: {cachePath} file: {fileName}");

        return NSUrl.CreateFileUrl(Path.Combine(cachePath, fileName), false, null);
    }
}
