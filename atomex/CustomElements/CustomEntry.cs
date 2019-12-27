using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomEntry : Entry
    {
        public CustomEntry()
        {
            this.Focused += OnFocused;
        }

        private void OnFocused(object sender, FocusEventArgs e)
        {
            
        }
    }
}
