using System;
using System.Numerics;
using ImGuiNET;

namespace ZoomiesPlugin.Renderers
{
    public class ClassicRenderer
    {
        // Maximum yalms value
        private float maxYalms;
        // Redline starting value
        private float redlineStart;
        // Colors for the gauge
        private readonly uint dialColor;
        private readonly uint needleColor;
        private readonly uint markingsColor;
        private readonly uint textColor;
        private readonly uint backgroundColor;

        public ClassicRenderer()
        {
            // Set default values
            maxYalms = 20.0f;
            redlineStart = 16.0f;

            // Set colors (using RGBA format)
            backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.05f, 0.05f, 0.05f, 1.0f));
            dialColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
            needleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            markingsColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
        }

        // Set the maximum speed for the gauge
        public void SetMaxYalms(float newMax)
        {
            maxYalms = Math.Max(newMax, 5.0f); // Ensure a minimum value
        }

        // Set the redline starting point
        public void SetRedlineStart(float newStart)
        {
            redlineStart = Math.Clamp(newStart, 0.0f, maxYalms);
        }

        // Check if the close button was clicked - always return false now
        public bool WasCloseButtonClicked()
        {
            return false;
        }

        // Main rendering method
        public void Render(float displayYalms)
        {
            // Get window properties
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            Vector2 center = new Vector2(
                windowPos.X + windowSize.X / 2,
                windowPos.Y + windowSize.Y / 2
            );

            // Calculate radius (a bit smaller than half the window width)
            float radius = Math.Min(windowSize.X, windowSize.Y) * 0.4f;

            // Get the draw list for custom rendering
            var drawList = ImGui.GetWindowDrawList();

            // Make the window draggable
            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.InvisibleButton("##draggable", windowSize);

            // Handle dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Vector2 delta = ImGui.GetIO().MouseDelta;
                ImGui.SetWindowPos(new Vector2(windowPos.X + delta.X, windowPos.Y + delta.Y));
            }

            // Draw gauge background
            drawList.AddCircleFilled(center, radius + 20, backgroundColor);
            drawList.AddCircle(center, radius + 10, dialColor, 100, 2.0f);
            drawList.AddCircleFilled(center, radius, dialColor);

            // Draw redline
            DrawRedline(drawList, center, radius, redlineStart, maxYalms);

            // Draw markings and numbers
            DrawSpeedMarkings(drawList, center, radius);

            // Draw the needle
            DrawNeedle(drawList, center, radius, displayYalms / maxYalms);

            // Draw digital readout
            DrawDigitalReadout(drawList, center, radius, displayYalms);
        }

        // Draw the redline (danger zone)
        private void DrawRedline(ImDrawListPtr drawList, Vector2 center, float radius, float startValue, float endValue)
        {
            // Calculate percentages of the max speed
            float startPercent = startValue / maxYalms;
            float endPercent = endValue / maxYalms;

            // Clamp values
            startPercent = Math.Clamp(startPercent, 0.0f, 1.0f);
            endPercent = Math.Clamp(endPercent, 0.0f, 1.0f);

            // Calculate angles for the redline arc (150° to 30° range)
            float startAngle = 150 * (float)Math.PI / 180;
            float endAngle = 30 * (float)Math.PI / 180;
            float totalAngle = (endAngle - startAngle + 2 * (float)Math.PI) % (2 * (float)Math.PI);

            float redStartAngle = startAngle + totalAngle * startPercent;
            float redEndAngle = startAngle + totalAngle * endPercent;

            // Calculate segment count
            int segments = (int)(40 * (endPercent - startPercent));
            segments = Math.Max(segments, 12);

            // Calculate arc thickness
            float innerRadius = radius - 5;
            float outerRadius = radius + 5;

            // Red color for the danger zone
            uint redlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.1f, 0.1f, 0.7f));

            // Draw arc segments
            for (int i = 0; i < segments; i++)
            {
                float angle1 = redStartAngle + (redEndAngle - redStartAngle) * i / segments;
                float angle2 = redStartAngle + (redEndAngle - redStartAngle) * (i + 1) / segments;

                float innerStartX = center.X + innerRadius * (float)Math.Cos(angle1);
                float innerStartY = center.Y + innerRadius * (float)Math.Sin(angle1);
                float outerStartX = center.X + outerRadius * (float)Math.Cos(angle1);
                float outerStartY = center.Y + outerRadius * (float)Math.Sin(angle1);

                float innerEndX = center.X + innerRadius * (float)Math.Cos(angle2);
                float innerEndY = center.Y + innerRadius * (float)Math.Sin(angle2);
                float outerEndX = center.X + outerRadius * (float)Math.Cos(angle2);
                float outerEndY = center.Y + outerRadius * (float)Math.Sin(angle2);

                // Draw segment as a quad
                drawList.AddQuadFilled(
                    new Vector2(innerStartX, innerStartY),
                    new Vector2(outerStartX, outerStartY),
                    new Vector2(outerEndX, outerEndY),
                    new Vector2(innerEndX, innerEndY),
                    redlineColor
                );
            }
        }

        // Draw the speed markings and numbers
        private void DrawSpeedMarkings(ImDrawListPtr drawList, Vector2 center, float radius)
        {
            // Start angle (150 degrees) - left side
            float startAngle = 150 * (float)Math.PI / 180;
            // End angle (30 degrees) - right side
            float endAngle = 30 * (float)Math.PI / 180;
            // Total angle sweep (moving clockwise)
            float totalAngle = (endAngle - startAngle + 2 * (float)Math.PI) % (2 * (float)Math.PI);

            // Number of major markings (including 0)
            int majorMarkings = 5;
            // Number of minor markings between each major marking
            int minorMarkingsPerMajor = 4;

            // Redline color
            uint redlineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.1f, 0.1f, 1.0f));

            // Draw major markings and numbers
            for (int i = 0; i <= majorMarkings; i++)
            {
                float speed = (maxYalms / majorMarkings) * i;
                float angle = startAngle + (totalAngle * i / majorMarkings);

                // Calculate point on the dial
                float outerX = center.X + (radius - 2) * (float)Math.Cos(angle);
                float outerY = center.Y + (radius - 2) * (float)Math.Sin(angle);
                float innerX = center.X + (radius - 15) * (float)Math.Cos(angle);
                float innerY = center.Y + (radius - 15) * (float)Math.Sin(angle);

                // Determine if in redline zone
                bool isInRedZone = speed >= redlineStart && speed <= maxYalms;
                uint markingColor = isInRedZone ? redlineColor : markingsColor;

                // Draw marking line
                drawList.AddLine(new Vector2(innerX, innerY), new Vector2(outerX, outerY), markingColor, 2.0f);

                // Add number
                float textX = center.X + (radius - 30) * (float)Math.Cos(angle);
                float textY = center.Y + (radius - 30) * (float)Math.Sin(angle);
                drawList.AddText(new Vector2(textX - 10, textY - 10), markingColor, speed.ToString("0"));

                // Draw minor markings between major markings
                if (i < majorMarkings)
                {
                    for (int j = 1; j <= minorMarkingsPerMajor; j++)
                    {
                        float minorSpeed = speed + (maxYalms / majorMarkings) * (j / (float)minorMarkingsPerMajor);
                        bool minorInRedZone = minorSpeed >= redlineStart && minorSpeed <= maxYalms;
                        uint minorMarkingColor = minorInRedZone ? redlineColor : markingsColor;

                        float minorAngle = startAngle + (totalAngle * (i + (float)j / minorMarkingsPerMajor) / majorMarkings);
                        float minorOuterX = center.X + (radius - 2) * (float)Math.Cos(minorAngle);
                        float minorOuterY = center.Y + (radius - 2) * (float)Math.Sin(minorAngle);
                        float minorInnerX = center.X + (radius - 10) * (float)Math.Cos(minorAngle);
                        float minorInnerY = center.Y + (radius - 10) * (float)Math.Sin(minorAngle);

                        // Draw minor marking
                        drawList.AddLine(
                            new Vector2(minorInnerX, minorInnerY),
                            new Vector2(minorOuterX, minorOuterY),
                            minorMarkingColor, 1.0f
                        );
                    }
                }
            }
        }

        // Draw the needle (with close button functionality removed)
        private void DrawNeedle(ImDrawListPtr drawList, Vector2 center, float radius, float speedFraction)
        {
            // Clamp the speed fraction
            speedFraction = Math.Clamp(speedFraction, 0.0f, 1.0f);

            // Calculate the angle (150° to 30°, clockwise)
            float startAngle = 150 * (float)Math.PI / 180;
            float endAngle = 30 * (float)Math.PI / 180;
            float totalAngle = (endAngle - startAngle + 2 * (float)Math.PI) % (2 * (float)Math.PI);
            float needleAngle = startAngle + totalAngle * speedFraction;

            // Calculate needle endpoint
            float needleLength = radius - 20;
            float tipX = center.X + needleLength * (float)Math.Cos(needleAngle);
            float tipY = center.Y + needleLength * (float)Math.Sin(needleAngle);

            // Draw the needle
            drawList.AddLine(center, new Vector2(tipX, tipY), needleColor, 2.0f);

            // Draw center hub (without close button functionality)
            drawList.AddCircleFilled(center, 10, needleColor);

            // Inner circle
            drawList.AddCircleFilled(center, 6, dialColor);
        }

        // Draw the digital readout
        private void DrawDigitalReadout(ImDrawListPtr drawList, Vector2 center, float radius, float yalms)
        {
            // Position below the center
            Vector2 textPos = new Vector2(center.X - 40, center.Y + radius / 2);

            // Draw background
            drawList.AddRectFilled(
                new Vector2(textPos.X - 5, textPos.Y - 5),
                new Vector2(textPos.X + 85, textPos.Y + 25),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f))
            );

            // Format with yalms per second label
            string speedText = $"{yalms:F1} yalms/s";

            // Draw text
            drawList.AddText(textPos, textColor, speedText);
        }
    }
}
