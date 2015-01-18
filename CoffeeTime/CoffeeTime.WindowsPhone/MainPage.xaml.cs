using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
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
using Windows.Web.Http;

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
            this.NavigationCacheMode = NavigationCacheMode.Required;
            ToggleLoginPanel();

            _geolocator = new Geolocator();
            _geolocator.DesiredAccuracy = PositionAccuracy.High;
            _geolocator.MovementThreshold = 10;
            _geolocator.PositionChanged += _geolocator_PositionChanged;
        }

        #region Helper methods
        private void ToggleLoginPanel()
        {
            if (string.IsNullOrEmpty(GetValueFromCloud("userName")))
            {
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

        void _geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            progressRing.IsActive = true;

            var lat = args.Position.Coordinate.Latitude;
            var longitude = args.Position.Coordinate.Longitude;

            CreateAndUpdateTags(lat, longitude);
            progressRing.IsActive = false;
        }

        private async void CreateAndUpdateTags(double latitude, double longitute)
        {
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

            App.address = address;
            App.UpdateTags();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async void btnCoffee_Click(object sender, RoutedEventArgs e)
        {
            CoffeeTime.CoffeeTimePush.UploadChannel(App.UpdateTags());
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            //Set the name
            cloudStorage.Values.Add("userName", TxtUserName.Text);
            TxtUserName.Text = string.Empty;

            ToggleLoginPanel();
        }
    }
}
