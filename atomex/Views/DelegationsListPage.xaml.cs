﻿using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationsListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public DelegationsListPage()
        {
            InitializeComponent();
        }

        public DelegationsListPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
            string selectedColorName = Application.Current.RequestedTheme == OSAppTheme.Dark
                ? "ListViewSelectedBackgroundColorDark"
                : "ListViewSelectedBackgroundColor";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Frame selectedItem = (Frame)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;

            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
        }
    }
}
