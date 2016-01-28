using Android.App;
using Android.OS;
using Playground.Core;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Playground.Droid
{
    [Activity(Label = "Playground.Droid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}