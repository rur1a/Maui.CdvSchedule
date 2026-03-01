using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Maui.CdvSchedule.App;

internal static class CdvNotificationPlatform
{
    public const string ChannelId = "cdv_main_channel";
    public const string ChannelName = "Main Channel";
    public const string ChannelDescription = "CDV schedule notifications";

    public const string ExtraNotificationId = "cdv_notification_id";
    public const string ExtraTitle = "cdv_notification_title";
    public const string ExtraBody = "cdv_notification_body";

    public static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
        {
            Description = ChannelDescription,
        };
        channel.EnableVibration(true);
        channel.EnableLights(true);

        manager.CreateNotificationChannel(channel);
    }

    public static int GetSmallIcon(Context context)
    {
        var customIcon = context.Resources?.GetIdentifier("notifications_icon", "drawable", context.PackageName) ?? 0;
        return customIcon != 0 ? customIcon : Resource.Mipmap.appicon;
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class CdvNotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        CdvNotificationPlatform.EnsureChannel(context);

        var id = intent.GetIntExtra(CdvNotificationPlatform.ExtraNotificationId, -1);
        if (id < 0)
        {
            return;
        }

        var title = intent.GetStringExtra(CdvNotificationPlatform.ExtraTitle) ?? string.Empty;
        var body = intent.GetStringExtra(CdvNotificationPlatform.ExtraBody) ?? string.Empty;

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName);
        PendingIntent? contentIntent = null;
        if (launchIntent is not null)
        {
            launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            contentIntent = PendingIntent.GetActivity(
                context,
                id + 10_000,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }

        var builder = new NotificationCompat.Builder(context, CdvNotificationPlatform.ChannelId)
            .SetSmallIcon(CdvNotificationPlatform.GetSmallIcon(context))
            .SetContentTitle(title)
            .SetContentText(body)
            .SetPriority((int)NotificationPriority.High)
            .SetAutoCancel(true)
            .SetVisibility((int)NotificationVisibility.Public);

        if (contentIntent is not null)
        {
            builder.SetContentIntent(contentIntent);
        }

        NotificationManagerCompat.From(context).Notify(id, builder.Build());
    }
}
