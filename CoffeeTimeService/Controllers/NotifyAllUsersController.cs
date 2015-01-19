using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure.Mobile.Service;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.ServiceBus.Notifications;

namespace CoffeeTimeService.Controllers
{
    public class NotifyAllUsersController : ApiController
    {
        public ApiServices Services { get; set; }

        // GET api/NotifyAllUsers
        public string Get()
        {
            Services.Log.Info("Hello from custom controller!");
            return "Hello";
        }

        // The following call is for illustration purpose only. The function
        // body should be moved to a controller in your app where you want
        // to send a notification.
        public async Task<bool> Post(JObject data)
        {
            try
            {
                //Store
                NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(
                    "Endpoint=sb://coffeetimehub-ns.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=Ub5tkbFmBMOL3LrXO2fCh6gY8GgQ4Lx/SmRqqOrd01w=",
                    "coffeetimehub");

                var zipcode = string.Empty;
                var message = string.Empty;
                if ( data["zipcode"] != null)
                {
                    zipcode = data["zipcode"].ToString();
                }

                var username = string.Empty;
                if (string.IsNullOrEmpty(data["UserName"].ToString()))
                {
                    message = "Wanna go for coffee";
                }
                else
                {
                    username = data["UserName"].ToString();
                    message = "Join " + username + " for a cup of coffee";
                }
                
                if (!string.IsNullOrEmpty(zipcode))
	            {
                    message += " at " + zipcode;
                }

                var toast = string.Format("<toast><visual><binding template=\"ToastText01\"><text id=\"1\">{0}</text></binding></visual></toast>", message);

                if (string.IsNullOrEmpty(zipcode))
                {
                    await hub.SendWindowsNativeNotificationAsync(toast);
                }
                else
                {
                    await hub.SendWindowsNativeNotificationAsync(toast, "PostalAddress:" + zipcode);
                }

                return true;
            }
            catch (Exception e)
            {
                Services.Log.Error(e.ToString());
            }
            return false;
        }
    }
}
