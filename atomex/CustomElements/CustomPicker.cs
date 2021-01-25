using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomPicker : Picker
    {
        public static readonly BindableProperty PaddingProperty =
                      BindableProperty.Create(
                          nameof(Padding),
                          typeof(Thickness),
                          typeof(CustomPicker),
                          new Thickness());

        public Thickness Padding
        {
            get { return (Thickness)this.GetValue(PaddingProperty); }
            set { this.SetValue(PaddingProperty, value); }
        }
    }
}
