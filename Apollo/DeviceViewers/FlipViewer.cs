﻿using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class FlipViewer: UserControl {
        public static readonly string DeviceIdentifier = "flip";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Flip _flip;
        ComboBox ComboBox;
        CheckBox Bypass;

        public FlipViewer(Flip flip) {
            InitializeComponent();

            _flip = flip;

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _flip.Mode;
            Mode_Changed(null, null);
            
            Bypass = this.Get<CheckBox>("Bypass");
            Bypass.IsChecked = _flip.Bypass;
            Bypass_Changed(null, EventArgs.Empty);
        }

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _flip.Mode = (string)ComboBox.SelectedItem;

        private void Bypass_Changed(object sender, EventArgs e) => _flip.Bypass = Bypass.IsChecked.Value;
    }
}