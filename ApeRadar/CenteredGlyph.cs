using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ApeRadar
{
    public sealed class CenteredGlyph : FrameworkElement
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CenteredGlyph),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
            typeof(CenteredGlyph),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
            typeof(CenteredGlyph),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
            typeof(CenteredGlyph),
            new FrameworkPropertyMetadata(30d, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (string.IsNullOrEmpty(Text) || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            Typeface typeface = new(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            FormattedText formattedText = new(
                Text,
                CultureInfo.CurrentUICulture,
                FlowDirection,
                typeface,
                FontSize,
                Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            Geometry geometry = formattedText.BuildGeometry(new Point());
            Rect bounds = geometry.Bounds;
            if (bounds.IsEmpty)
            {
                return;
            }

            double x = (ActualWidth - bounds.Width) / 2 - bounds.X;
            double y = (ActualHeight - bounds.Height) / 2 - bounds.Y;
            drawingContext.PushTransform(new TranslateTransform(x, y));
            drawingContext.DrawGeometry(Foreground, null, geometry);
            drawingContext.Pop();
        }
    }
}
