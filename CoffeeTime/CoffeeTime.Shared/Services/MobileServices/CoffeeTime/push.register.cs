using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using Windows.Networking.PushNotifications;

// http://go.microsoft.com/fwlink/?LinkId=290986&clcid=0x409

namespace CoffeeTime
{
    internal class CoffeeTimePush
    {
        static PushNotificationChannel channel;

        internal static async void UploadChannel(List<string> tags)
        {
            //channel = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //await App.CoffeeTimeClient.GetPush().RegisterNativeAsync(channel.Uri, tags);

            var postalTag = tags.Find(tg => tg.Contains("Postal"));
            var zipcode = postalTag.Substring(postalTag.IndexOf(":") + 1);

            var payload = new JObject(
                        new JProperty("zipcode", zipcode),
                        new JProperty("UserName", App.UserName);

            await App.CoffeeTimeClient.InvokeApiAsync("notifyAllUsers", payload);
        }

        internal static async void UpdateAzureTags(List<string> tags)
        {
            channel = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            await App.CoffeeTimeClient.GetPush().RegisterNativeAsync(channel.Uri, tags);
        }
    }
}