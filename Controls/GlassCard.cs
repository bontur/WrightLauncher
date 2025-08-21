using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WrightLauncher.Controls
{
    public class GlassCard : ContentControl
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GlassCard),
                new PropertyMetadata(new CornerRadius(8)));

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register(nameof(GlowColor), typeof(Brush), typeof(GlassCard),
                new PropertyMetadata(new SolidColorBrush(Colors.Purple)));

        public static readonly DependencyProperty IsGlowingProperty =
            DependencyProperty.Register(nameof(IsGlowing), typeof(bool), typeof(GlassCard),
                new PropertyMetadata(false));

        static GlassCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassCard),
                new FrameworkPropertyMetadata(typeof(GlassCard)));
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush GlowColor
        {
            get => (Brush)GetValue(GlowColorProperty);
            set => SetValue(GlowColorProperty, value);
        }

        public bool IsGlowing
        {
            get => (bool)GetValue(IsGlowingProperty);
            set => SetValue(IsGlowingProperty, value);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            IsGlowing = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsGlowing = false;
        }
    }
}

