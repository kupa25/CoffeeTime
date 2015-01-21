using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
                cloudStorage.Values.Remove("userName");
                LoginPanel.Visibility = Visibility.Visible;
                CofeePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                App.UserName = GetValueFromCloud("userName");
                LoginPanel.Visibility = Visibility.Collapsed;
                CofeePanel.Visibility = Visibility.Visible;
            }
        }

        private static string GetValueFromCloud(string p)
        {
            return cloudStorage.Values.ContainsKey(p) ? (string)cloudStorage.Values[p] : string.Empty;
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
            JToken jsonResourceSets = json["resourceSets"];
            JToken jsonAddress = null;
            JToken jsonResource = null;

            if (jsonResourceSets.HasValues)
            {
                jsonResource = jsonResourceSets[0]["resources"];
            }
            if (jsonResource.HasValues)
            {
                jsonAddress = jsonResource[0]["address"];
            }

            if (jsonAddress == null)
            {
                return;
            }

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
        /// TODO: Make it useful
        /// </summary>
        private async void CreateGeofence()
        {
            GeofenceMonitor.Current.Geofences.Clear();

            BasicGeoposition basicGeoposition = new BasicGeoposition();
            Geoposition geoposition = await _geolocator.GetGeopositionAsync();
            Geofence geofence;

            basicGeoposition.Latitude = geoposition.Coordinate.Latitude;
            basicGeoposition.Longitude = geoposition.Coordinate.Longitude;
            basicGeoposition.Altitude = (double)geoposition.Coordinate.Altitude;
            double radius = 10.0;

            Geocircle geocircle = new Geocircle(basicGeoposition, radius);

            // want to listen for enter geofence, exit geofence and remove geofence events
            // you can select a subset of these event states
            MonitoredGeofenceStates mask = 0;

            mask |= MonitoredGeofenceStates.Entered;
            mask |= MonitoredGeofenceStates.Exited;
            mask |= MonitoredGeofenceStates.Removed;

            // setting up how long you need to be in geofence for enter event to fire
            TimeSpan dwellTime = new TimeSpan(1, 0, 0);

            // setting up how long the geofence should be active
            TimeSpan duration = new TimeSpan(0, 10, 0);

            // setting up the start time of the geofence
            DateTimeOffset startTime = DateTimeOffset.Now;

            geofence = new Geofence("Test", geocircle, mask, true);

            GeofenceMonitor.Current.Geofences.Add(geofence);
        }

        #endregion

        #region UI Events
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            cloudStorage.Values.Remove("userName");
            ToggleLoginPanel();
        }

        private async void btnCoffee_Click(object sender, RoutedEventArgs e)
        {
            CreateGeofence();
            CoffeeTime.CoffeeTimePush.UploadChannel(App.UpdateTags());
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            //Set the name
            cloudStorage.Values.Add("userName", TxtUserName.Text);
            TxtUserName.Text = string.Empty;

            ToggleLoginPanel();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(About)))
            {
                throw new Exception("Failed to navigate");
            }
        }

        #endregion

        void _geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            //Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
            //{
            //    progressRing.IsActive = true;
            //});
            

            var lat = args.Position.Coordinate.Latitude;
            var longitude = args.Position.Coordinate.Longitude;

            CreateAndUpdateTags(lat, longitude);

            //Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
            //{
            //    progressRing.IsActive = false;
            //});
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
    }
}
