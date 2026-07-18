using ApeRadar.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System;
using System.Collections.Generic;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using ApeRadar.Utils.Converters;

namespace ApeRadar.Utils
{
    static internal class ChartUtils
    {

        public static RectangularSection[] GetWinrateChartSections(Battlefield battlefield, int chartType)
        {
            List<RectangularSection> result = new();
            List<double[]> sections = battlefield.GetSectionsForPlot(chartType);
            if (sections.Count <= 1)
            {
                return result.ToArray();
            }
            for (int i = 0; i < sections.Count; i++)
            {
                result.Add(new RectangularSection
                {
                    Xi = sections[i][0],
                    Xj = sections[i][1],
                    Fill = new SolidColorPaint
                    {
                        Color = i % 2 == 0
                            ? ThemeManager.GetSkColor("AppChartSectionEvenColor", new SKColor(240, 240, 240))
                            : ThemeManager.GetSkColor("AppChartSectionOddColor", SKColors.White)
                    }
                });
            }
            return result.ToArray();
        }

        public static ISeries[] GetWinrateChartSeries(Battlefield battlefield, int chartType)
        {
            List<List<Player?>> playerListForPlot = battlefield.GetPlayersForPlot(chartType);
            return chartType switch
            {
                0 => GetWinrateChartColumnSeries(playerListForPlot),
                1 => GetWinrateChartColumnSeries(playerListForPlot),
                2 => GetWinrateChartColumnSeries(playerListForPlot),
                3 => GetWinrateChartColumnSeries(playerListForPlot),
                4 => GetWinrateChartLineSeries(playerListForPlot),
                5 => GetWinrateChartLineSeries(playerListForPlot),
                _ => GetWinrateChartLineSeries(playerListForPlot),
            };
        }

        private static ISeries[] GetWinrateChartLineSeries(List<List<Player?>> playerList)
        {
            return new ISeries[]
            {
                new LineSeries<Player?>
                {
                    Name = "Allies",
                    Values = playerList[0],
                    Mapping = MapPlayerWinrate,
                    Stroke = new SolidColorPaint(new SKColor(71,227,165)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 12,
                    GeometryFill = new SolidColorPaint(new SKColor(71,227,165)),
                    GeometryStroke = null,
                    YToolTipLabelFormatter = FormatPlayerTooltip
                },
                new LineSeries<Player?>
                {
                    Name = "Enemies",
                    Values = playerList[1],
                    Mapping = MapPlayerWinrate,
                    Stroke = new SolidColorPaint(new SKColor(255,66,0)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 12,
                    GeometryFill = new SolidColorPaint(new SKColor(255,66,0)),
                    GeometryStroke = null,
                    YToolTipLabelFormatter = FormatPlayerTooltip,
                }
            };
        }

        private static ISeries[] GetWinrateChartColumnSeries(List<List<Player?>> playerList)
        {
            return new ISeries[]
            {
                new ColumnSeries<Player?>
                {
                    Values = playerList[0],
                    Mapping = MapPlayerWinrate,
                    Stroke = null,
                    MaxBarWidth = 8,
                    Fill = new SolidColorPaint(new SKColor(71,227,165)),
                    YToolTipLabelFormatter = FormatPlayerTooltip,
                },
                new ColumnSeries<Player?>
                {
                    Values = playerList[1],
                    Mapping = MapPlayerWinrate,
                    Stroke = null,
                    MaxBarWidth = 8,
                    Fill = new SolidColorPaint(new SKColor(255,66,0)),
                    YToolTipLabelFormatter = FormatPlayerTooltip,
                },
            };
        }

        public static ISeries[] GetKDEChartSeries(Battlefield battlefield)
        {
            List<ObservablePoint[]> KDEListForPlot = battlefield.GetPlayersKDEForPlot();

            ISeries[] chartSeriesWinrateKDE = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Name = "Allies",
                    Values = KDEListForPlot[0],
                    Stroke = new SolidColorPaint(new SKColor(71,227,165)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    IsHoverable = false
                },
                new LineSeries<ObservablePoint>
                {
                    Name = "Enemies",
                    Values = KDEListForPlot[1],
                    Stroke = new SolidColorPaint(new SKColor(255,66,0)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    IsHoverable = false
                }
            };
            return chartSeriesWinrateKDE;
        }

        private static Coordinate MapPlayerWinrate(Player? player, int _)
        {
            double winrate = Properties.Settings.Default.WinrateTypeUsed == 0
                ? player!.AccountWinrate
                : player!.WeightedWinrate;

            // LiveCharts uses the first value as X and the second as Y. Keep
            // winrate on the value axis; PlotXPosition only controls ordering.
            return new Coordinate(player.PlotXPosition, winrate);
        }

        private static string FormatPlayerTooltip<TVisual, TLabel>(LiveChartsCore.Kernel.ChartPoint<Player?, TVisual, TLabel> chartPoint)
        {
            Player player = chartPoint.Model!;
            return $"{player.ClanTag} {player.Name}\r\n{new ShipTierConverter().Convert(player.ShipTier, null, null, null)} {player.ShipName}\r\n{chartPoint.Coordinate.PrimaryValue:p2}";
        }
    }
}
