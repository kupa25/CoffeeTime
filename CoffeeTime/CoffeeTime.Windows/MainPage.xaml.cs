using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Web.Http;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CoffeeTime
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static ApplicationDataContainer cloudStorage = ApplicationData.Current.RoamingSettings;
        private Geolocator _geolocator = null;

        public MainPage()
        {
            this.InitializeComponent();

            ToggleLoginPanel();
            _geolocator = new Geolocator();
            _geolocator.DesiredAccuracy = PositionAccuracy.High;
            _geolocator.MovementThreshold = 10;
        }

        #region Helper methods
        private void ToggleLoginPanel()
        {
            if (string.IsNullOrEmpty(GetValueFromCloud("userName")))
            {
                App.UserName = GetValueFromCloud("userName"));
                LoginPanel.Visibility = Visibility.Visible;
                CofeePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                CofeePanel.Visibility = Visibility.Visible;
            }
        }

        private static string GetValueFromCloud(string p)
        {
            return cloudStorage.Values.ContainsKey(p) ? (string)cloudStorage.Values[p] : string.Empty;
        }

        #endregion


        #region UI_Events
        private void BtnSetName_Click(object sender, RoutedEventArgs e)
        {
            //Set the name
            cloudStorage.Values.Add("userName", TxtUserName.Text);
            TxtUserName.Text = string.Empty;

            ToggleLoginPanel();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            cloudStorage.Values.Remove("userName");
            ToggleLoginPanel();
        }

        private async void BtnCoffeeTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                progressRing.IsActive = true;
                // Carry out the operation
                Geoposition pos = await _geolocator.GetGeopositionAsync().AsTask();

                var latitude = pos.Coordinate.Point.Position.Latitude.ToString();
                var longitute = pos.Coordinate.Point.Position.Longitude.ToString();
                var accuracy = pos.Coordinate.Accuracy.ToString();
                var positionSource = pos.Coordinate.PositionSource.ToString();

                //using the bing api from here

                var client = new HttpClient();
                var url = string.Format("http://dev.virtualearth.net/REST/v1/Locations/{0},{1}?o=json&key={2}",
                    latitude,
                    longitute,
                    "Apt41Xkk3z4iDILoiNw1a2jsu_waWdAlm2knrzsQTC3-1pQbZ80yLZR6uNZ75jIC");

                var response = await client.GetAsync(new Uri(url));
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(jsonString);
                JToken jsonAddress = json["resourceSets"][0]["resources"][0]["address"];
                var address = new Address 
                {
                    PostalAddress = (string)jsonAddress["postalCode"],
                    Locality = (string)jsonAddress["locality"],
                    County = (string)jsonAddress["adminDistrict2"],
                    State = (string)jsonAddress["adminDistrict"],
                    Country = (string)jsonAddress["countryRegion"],
                };
                progressRing.IsActive = false;

                App.address = address;
                CoffeeTime.CoffeeTimePush.UploadChannel(App.UpdateTags());
            }
            catch(Exception ex)
            {

            }
        }

        #endregion
    }
}
