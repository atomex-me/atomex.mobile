using System;
using System.Collections.Generic;
using System.Linq;
using atomex.Helpers;
using Microcharts;
using SkiaSharp;

namespace atomex.CustomElements
{
    public class CustomDonutChart : Chart
    {
        #region Properties

        /// <summary>
        /// Gets or sets the radius of the hole in the center of the chart.
        /// </summary>
        /// <value>The hole radius.</value>
        public float HoleRadius { get; set; } = 0;
        public string FontFamily { get; set; }
        public SKColor LabelColor { get; set; } = SKColor.Parse("#000000");

        #endregion

        #region Methods

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            this.DrawCaption(canvas, width, height);
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(width / 2, height / 2);
                var sumValue = this.Entries.Sum(x => Math.Abs(x.Value));
                var radius = (Math.Min(width, height) - (2 * Margin)) / 2;

                var start = 0.0f;
                for (int i = 0; i < this.Entries.Count(); i++)
                {
                    var entry = this.Entries.ElementAt(i);
                    var end = start + (Math.Abs(entry.Value) / sumValue);

                    // Sector
                    var path = RadialHelpers.CreateSectorPath(start, end, radius, radius * this.HoleRadius);
                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = entry.Color,
                        IsAntialias = true,
                    })
                    {
                        canvas.DrawPath(path, paint);
                    }

                    start = end;
                }
            }
        }

        private void DrawCaption(SKCanvas canvas, int width, int height)
        {
            //SKFontManager.Default.

            var sumValue = this.Entries.Sum(x => Math.Abs(x.Value));
            var rightValues = new List<Entry>();
            var leftValues = new List<Entry>();

            int i = 0;
            var current = 0.0f;
            while (i < this.Entries.Count() && (current < sumValue / 2))
            {
                var entry = this.Entries.ElementAt(i);
                rightValues.Add(entry);
                current += Math.Abs(entry.Value);
                i++;
            }

            while (i < this.Entries.Count())
            {
                var entry = this.Entries.ElementAt(i);
                leftValues.Add(entry);
                i++;
            }

            leftValues.Reverse();

            this.CustomDrawCaptionElements(canvas, width, height, rightValues, false);
            this.CustomDrawCaptionElements(canvas, width, height, leftValues, true);
        }

        /// <summary>
        /// Draws caption elements on the right or left side of the chart.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="entries">The entries.</param>
        /// <param name="isLeft">If set to <c>true</c> is left.</param>
        protected void CustomDrawCaptionElements(SKCanvas canvas, int width, int height, List<Entry> entries, bool isLeft)
        {
            var margin = 2 * this.Margin;
            var availableHeight = height - (2 * margin);
            var x = isLeft ? this.Margin : (width - this.Margin - this.LabelTextSize);
            var ySpace = (availableHeight - this.LabelTextSize) / ((entries.Count <= 1) ? 1 : entries.Count - 1);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries.ElementAt(i);
                var y = margin + (i * ySpace);
                if (entries.Count <= 1)
                {
                    y += (availableHeight - this.LabelTextSize) / 2;
                }

                var hasLabel = !string.IsNullOrEmpty(entry.Label);
                var hasValueLabel = !string.IsNullOrEmpty(entry.ValueLabel);

                if (hasLabel || hasValueLabel)
                {
                    var hasOffset = hasLabel && hasValueLabel;
                    var captionMargin = this.LabelTextSize * 0.60f;
                    var space = hasOffset ? captionMargin : 0;
                    var captionX = isLeft ? this.Margin : width - this.Margin - this.LabelTextSize;

                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Fill,
                        Color = entry.Color,
                    })
                    {
                        var rect = SKRect.Create(captionX, y, this.LabelTextSize, this.LabelTextSize);
                        canvas.DrawRect(rect, paint);
                    }

                    if (isLeft)
                    {
                        captionX += this.LabelTextSize + captionMargin;
                    }
                    else
                    {
                        captionX -= captionMargin;
                    }

                    this.DrawCaptionLabels(canvas, entry.Label, entry.TextColor, entry.ValueLabel, entry.Color, this.LabelTextSize, new SKPoint(captionX, y + (this.LabelTextSize / 2)), isLeft ? SKTextAlign.Left : SKTextAlign.Right);
                }
            }
        }

        public void DrawCaptionLabels(SKCanvas canvas, string label, SKColor labelColor, string value, SKColor valueColor, float textSize, SKPoint point, SKTextAlign horizontalAlignment)
        {
            var hasLabel = !string.IsNullOrEmpty(label);
            var hasValueLabel = !string.IsNullOrEmpty(value);

            if (hasLabel || hasValueLabel)
            {
                var hasOffset = hasLabel && hasValueLabel;
                var captionMargin = textSize * 0.60f;
                var space = hasOffset ? captionMargin : 0;

                if (hasLabel)
                {
                    using (var paint = new SKPaint()
                    {
                        TextSize = textSize,
                        IsAntialias = true,
                        //Color = labelColor,
                        Color = LabelColor,
                        IsStroke = false,
                        TextAlign = horizontalAlignment,
                        Typeface = SKTypeface.FromFamilyName(FontFamily)
                    })
                    {
                        var bounds = new SKRect();
                        var text = label;
                        paint.MeasureText(text, ref bounds);

                        var y = point.Y - ((bounds.Top + bounds.Bottom) / 2) - space;

                        canvas.DrawText(text, point.X, y, paint);
                    }
                }

                if (hasValueLabel)
                {
                    using (var paint = new SKPaint()
                    {
                        TextSize = textSize,
                        IsAntialias = true,
                        FakeBoldText = true,
                        //Color = valueColor,
                        Color = LabelColor,
                        IsStroke = false,
                        TextAlign = horizontalAlignment,
                        Typeface = SKTypeface.FromFamilyName(FontFamily)
                    })
                    {
                        var bounds = new SKRect();
                        var text = value;
                        paint.MeasureText(text, ref bounds);

                        var y = point.Y - ((bounds.Top + bounds.Bottom) / 2) + space;

                        canvas.DrawText(text, point.X, y, paint);
                    }
                }
            }
        }

        #endregion  
    }
}