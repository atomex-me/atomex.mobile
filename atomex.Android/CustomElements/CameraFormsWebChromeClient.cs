using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Xamarin.Essentials;
using Xamarin.Forms.Platform.Android;

namespace atomex.Droid.CustomElements
{
    public class CameraFormsWebChromeClient : FormsWebChromeClient
    {
        string _photoPath;
        public override bool OnShowFileChooser(Android.Webkit.WebView webView, Android.Webkit.IValueCallback filePathCallback, FileChooserParams fileChooserParams)
        {

            AlertDialog.Builder alertDialog = new AlertDialog.Builder(MainActivity.Instance);
            alertDialog.SetTitle("Take picture or choose a file");
            alertDialog.SetNeutralButton("Take picture", async (sender, alertArgs) =>
            {
                try
                {
                    var photo = await MediaPicker.CapturePhotoAsync();
                    var uri = await LoadPhotoAsync(photo);
                    filePathCallback.OnReceiveValue(uri);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
                }
            });
            alertDialog.SetNegativeButton("Choose picture", async (sender, alertArgs) =>
            {
                try
                {
                    var photo = await MediaPicker.PickPhotoAsync();
                    var uri = await LoadPhotoAsync(photo);
                    filePathCallback.OnReceiveValue(uri);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PickPhotoAsync THREW: {ex.Message}");
                }
            });
            alertDialog.SetPositiveButton("Cancel", (sender, alertArgs) =>
            {
                filePathCallback.OnReceiveValue(null);
            });
            Dialog dialog = alertDialog.Create();
            dialog.Show();
            return true;
        }

        async Task<Android.Net.Uri[]> LoadPhotoAsync(FileResult photo)
        {
            // cancelled
            if (photo == null)
            {
                _photoPath = null;
                return null;
            }
            // save the file into local storage
            var newFile = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
            using (var stream = await photo.OpenReadAsync())
            using (var newStream = File.OpenWrite(newFile))
                await stream.CopyToAsync(newStream);
            _photoPath = newFile;
            Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(_photoPath));
            return new Android.Net.Uri[] { uri };
        }
    }
}
