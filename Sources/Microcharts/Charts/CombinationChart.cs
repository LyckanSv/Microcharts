using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microcharts
{
    public class CombinationChart : AxisBasedChart
    {
        private Dictionary<ChartSerie, List<SKPoint>> pointsPerSerie = new Dictionary<ChartSerie, List<SKPoint>>();
        public CombinationChart() : base()
        {

        }

        #region Properties
        public byte BarAreaAlpha { get; set; } = DefaultValues.BarAreaAlpha;

        public float MinBarHeight { get; set; } = DefaultValues.MinBarHeight;

        public float PointSize { get; set; } = 14;

        public PointMode PointMode { get; set; } = PointMode.Circle;

        public float LineSize { get; set; } = 3;
        public LineMode LineMode { get; set; } = LineMode.Spline;
        #endregion

        public override void DrawContent(SKCanvas canvas, int width, int height)
        {
            pointsPerSerie.Clear();
            foreach (var s in Series)
                pointsPerSerie.Add(s, new List<SKPoint>());
            base.DrawContent(canvas, width, height);

        }

        protected override void DrawBar(ChartSerie serie, SKCanvas canvas, float headerHeight, float itemX, SKSize itemSize, SKSize barSize, float origin, float barX, float barY, SKColor color)
        {
            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = color,
            })
            {

                if ((serie as ChartSerieCombinate)?.Type == ChartSerieCombinateType.Bar)
                {
                    (SKPoint location, SKSize size) = GetBarDrawingProperties(headerHeight, itemSize, barSize, origin, barX, barY);
                    var rect = SKRect.Create(location, size);
                    canvas.DrawRect(rect, paint);
                }
                else
                {
                    var point = new SKPoint(barX - (itemSize.Width / 2) + (barSize.Width / 2), barY);
                    canvas.DrawPoint(point, color, PointSize, PointMode);
                    pointsPerSerie[serie].Add(point);
                }
            }
        }

        protected override void OnDrawContentEnd(SKCanvas canvas, SKSize itemSize, float origin, Dictionary<ChartEntry, SKRect> valueLabelSizes)
        {
            base.OnDrawContentEnd(canvas, itemSize, origin, valueLabelSizes);
            DrawSeriesLine(canvas, itemSize);
        }

        protected override void DrawBarArea(SKCanvas canvas, float headerHeight, SKSize itemSize, SKSize barSize, SKColor color, float origin, float value, float barX, float barY)
        {
            if (BarAreaAlpha > 0)
            {
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color.WithAlpha((byte)(this.BarAreaAlpha * this.AnimationProgress)),
                })
                {
                    var max = value > 0 ? headerHeight : headerHeight + itemSize.Height;
                    var height = Math.Abs(max - barY);
                    var y = Math.Min(max, barY);
                    canvas.DrawRect(SKRect.Create(barX - (itemSize.Width / 2), y, barSize.Width, height), paint);
                }
            }
        }

        private (SKPoint location, SKSize size) GetBarDrawingProperties(float headerHeight, SKSize itemSize, SKSize barSize, float origin, float barX, float barY)
        {
            var x = barX - (itemSize.Width / 2);
            var y = Math.Min(origin, barY);
            var height = Math.Max(MinBarHeight, Math.Abs(origin - barY));
            if (height < MinBarHeight)
            {
                height = MinBarHeight;
                if (y + height > Margin + itemSize.Height)
                {
                    y = headerHeight + itemSize.Height - height;
                }
            }

            return (new SKPoint(x, y), new SKSize(barSize.Width, height));
        }

        private void DrawSeriesLine(SKCanvas canvas, SKSize itemSize)
        {
            if (pointsPerSerie.Any() && LineMode != LineMode.None)
            {
                foreach (var s in Series)
                {
                    if (pointsPerSerie[s].Count > 1)
                    {
                        var points = pointsPerSerie[s].ToArray();
                        using (var paint = new SKPaint
                        {
                            Style = SKPaintStyle.Stroke,
                            Color = s.Color ?? SKColors.White,
                            StrokeWidth = LineSize,
                            IsAntialias = true,
                        })
                        {
                            if (s.Color == null)
                                using (var shader = CreateXGradient(points, s.Entries, s.Color))
                                    paint.Shader = shader;

                            var path = new SKPath();
                            path.MoveTo(points.First());
                            var last = (LineMode == LineMode.Spline) ? points.Length - 1 : points.Length;
                            for (int i = 0; i < last; i++)
                            {
                                if (LineMode == LineMode.Spline)
                                {
                                    var cubicInfo = CalculateCubicInfo(points, i, itemSize);
                                    path.CubicTo(cubicInfo.control, cubicInfo.nextControl, cubicInfo.nextPoint);
                                }
                                else if (LineMode == LineMode.Straight)
                                {
                                    path.LineTo(points[i]);
                                }
                            }

                            canvas.DrawPath(path, paint);
                        }
                    }
                }
            }
        }

        private (SKPoint control, SKPoint nextPoint, SKPoint nextControl) CalculateCubicInfo(SKPoint[] points, int i, SKSize itemSize)
        {
            var point = points[i];
            var nextPoint = points[i + 1];
            var controlOffset = new SKPoint(itemSize.Width * 0.8f, 0);
            var currentControl = point + controlOffset;
            var nextControl = nextPoint - controlOffset;
            return (currentControl, nextPoint, nextControl);
        }

        private SKShader CreateXGradient(SKPoint[] points, IEnumerable<ChartEntry> entries, SKColor? serieColor, byte alpha = 255)
        {
            var startX = points.First().X;
            var endX = points.Last().X;
            var rangeX = endX - startX;

            return SKShader.CreateLinearGradient(
                new SKPoint(startX, 0),
                new SKPoint(endX, 0),
                entries.Select(x => serieColor?.WithAlpha(alpha) ?? x.Color.WithAlpha(alpha)).ToArray(),
                null,
                SKShaderTileMode.Clamp);
        }
    }
}
