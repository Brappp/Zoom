using System;
using System.Numerics;
using ImGuiNET;
using Dalamud.Interface.Utility.Raii;

namespace ZoomiesPlugin.Windows
{
    public class NyanCatRenderer : IDisposable
    {
        // Maximum yalms value
        private float maxYalms;
        // Colors for the rainbow trail
        private readonly uint[] rainbowColors;
        // Cat position and size
        private Vector2 catSize;
        // Animation timer
        private float animationTimer;
        // Frame counter for animation
        private int frameCounter;
        // Stores whether the user clicked the close button
        private bool closeButtonClicked;
        // Trail segments
        private const int MaxTrailSegments = 50;
        private int trailSegments;
        // Path to the Nyan Cat image
        private string nyanCatImagePath;
        // Previous speed for fading trail effect
        private float previousDisplayYalms;
        // Trail fade timer
        private float trailFadeTimer;

        public NyanCatRenderer()
        {
            // Set default values
            maxYalms = 20.0f;
            closeButtonClicked = false;
            animationTimer = 0f;
            frameCounter = 0;
            previousDisplayYalms = 0f;
            trailFadeTimer = 0f;

            // Increase the cat size for better visibility
            catSize = new Vector2(160, 80);  // Much larger for better visibility

            // Initialize rainbow colors (6 classic rainbow colors)
            rainbowColors = new uint[6];
            rainbowColors[0] = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.8f)); // Red
            rainbowColors[1] = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.8f)); // Orange
            rainbowColors[2] = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 0.8f)); // Yellow
            rainbowColors[3] = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.8f)); // Green
            rainbowColors[4] = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.5f, 1.0f, 0.8f)); // Blue
            rainbowColors[5] = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.0f, 1.0f, 0.8f)); // Purple

            // Get the path to the nyan.png file
            string pluginPath = Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!;
            nyanCatImagePath = System.IO.Path.Combine(pluginPath, "nyan.png");
        }

        // Set the maximum speed for scaling
        public void SetMaxYalms(float newMax)
        {
            maxYalms = Math.Max(newMax, 5.0f); // Ensure a minimum value
        }

        // Check if the close button was clicked
        public bool WasCloseButtonClicked()
        {
            // Reset state after reading
            bool wasClicked = closeButtonClicked;
            closeButtonClicked = false;
            return wasClicked;
        }

        // Main rendering method
        public void Render(float displayYalms)
        {
            // Get window properties
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            // Update animation timer
            animationTimer += ImGui.GetIO().DeltaTime * 5.0f;
            if (animationTimer >= 1.0f)
            {
                animationTimer = 0f;
                frameCounter = (frameCounter + 1) % 6; // 6 frames of animation
            }

            // Calculate trail segments based on speed and fade when not moving
            float speedRatio = Math.Clamp(displayYalms / maxYalms, 0.0f, 1.0f);

            // Handle trail fade when stopping
            if (displayYalms < 0.5f)
            {
                // If we're nearly stopped, start fading the trail
                trailFadeTimer += ImGui.GetIO().DeltaTime * 2.0f; // Adjust fade speed here
                trailFadeTimer = Math.Min(trailFadeTimer, 1.0f);
            }
            else
            {
                // We're moving, reset fade timer
                trailFadeTimer = 0f;
            }

            // Calculate trail segments with fade factor
            float fadeFactor = 1.0f - trailFadeTimer;
            trailSegments = (int)(speedRatio * MaxTrailSegments * fadeFactor);
            trailSegments = Math.Max(0, Math.Min(trailSegments, MaxTrailSegments));

            // Store current speed for next frame
            previousDisplayYalms = displayYalms;

            // Get the draw list for custom rendering
            var drawList = ImGui.GetWindowDrawList();

            // Make the window draggable
            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.InvisibleButton("##draggable", windowSize);
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Vector2 delta = ImGui.GetIO().MouseDelta;
                ImGui.SetWindowPos(new Vector2(windowPos.X + delta.X, windowPos.Y + delta.Y));
            }

            // Draw background (transparent)
            drawList.AddRectFilled(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.0f))
            );

            // Set cat position at right side of window
            Vector2 catPos = new Vector2(
                windowPos.X + windowSize.X - catSize.X - 20,
                windowPos.Y + windowSize.Y / 2 - catSize.Y / 2
            );

            // Draw rainbow trail
            DrawRainbowTrail(drawList, catPos, trailSegments);

            // Draw cat (using image or fallback to enhanced drawing)
            DrawCat(drawList, catPos, displayYalms);

            // Draw close button
            Vector2 closeButtonPos = new Vector2(windowPos.X + windowSize.X - 20, windowPos.Y + 20);
            DrawCloseButton(drawList, closeButtonPos, 10);
        }

        // Draw the rainbow trail
        private void DrawRainbowTrail(ImDrawListPtr drawList, Vector2 catPos, int segments)
        {
            // Adjust segment dimensions
            float segmentWidth = 15.0f;
            // Make the rainbow trail shorter in height - about 60% of cat height
            float totalTrailHeight = catSize.Y * 0.6f;
            float segmentHeight = totalTrailHeight / 6.0f;

            // Position trail to align with cat's middle
            float yOffset = catSize.Y / 2 - totalTrailHeight / 2;

            // Make trail overlap more with the poptart
            // For the drawn cat, we need to overlap with the toast part
            float poptartPosition = catPos.X + 10; // Position where the poptart/toast starts
            float actualTrailStartX = poptartPosition + 15; // More overlap into the poptart/toast

            // Draw trail segments from right to left
            for (int i = 0; i < segments; i++)
            {
                float xPos = actualTrailStartX - ((i + 1) * segmentWidth);

                // Skip if outside window
                if (xPos < ImGui.GetWindowPos().X - segmentWidth)
                    continue;

                // Add animation offset based on frame counter (reduced for better alignment)
                float animOffset = (float)Math.Sin(animationTimer * Math.PI + i * 0.2f) * 1.5f;

                // Draw six rainbow stripes (one for each color)
                for (int j = 0; j < 6; j++)
                {
                    float yPos = catPos.Y + yOffset + (j * segmentHeight) + animOffset;

                    drawList.AddRectFilled(
                        new Vector2(xPos, yPos),
                        new Vector2(xPos + segmentWidth, yPos + segmentHeight),
                        rainbowColors[j]
                    );
                }
            }
        }

        // Draw the cat using the loaded image or fallback to enhanced drawing
        private void DrawCat(ImDrawListPtr drawList, Vector2 catPos, float displayYalms)
        {
            // Animate the cat slightly up and down only when moving
            float bounce = 0f;
            if (displayYalms > 0.5f)
            {
                // Scale bounce based on speed (faster = more bounce)
                float bounceScale = Math.Min(displayYalms / 5.0f, 1.0f);
                bounce = (float)Math.Sin(animationTimer * Math.PI * 2) * 2.0f * bounceScale;
            }
            Vector2 adjustedPos = new Vector2(catPos.X, catPos.Y + bounce);

            // Load the texture using the sample plugin approach
            var texture = Plugin.TextureProvider.GetFromFile(nyanCatImagePath).GetWrapOrDefault();

            // If texture is available, use it
            if (texture != null)
            {
                // Calculate aspect ratio to maintain proportions
                float aspectRatio = (float)texture.Width / texture.Height;
                Vector2 drawSize = new Vector2(catSize.Y * aspectRatio, catSize.Y);

                // Draw the image directly
                drawList.AddImage(
                    texture.ImGuiHandle,
                    adjustedPos,
                    new Vector2(adjustedPos.X + drawSize.X, adjustedPos.Y + drawSize.Y)
                );
            }
            else
            {
                // Use fallback without excessive logging
                DrawEnhancedNyanCat(drawList, catPos, displayYalms);
            }
        }

        // Enhanced drawn version of Nyan Cat as a fallback
        private void DrawEnhancedNyanCat(ImDrawListPtr drawList, Vector2 catPos, float displayYalms)
        {
            // Animate the cat slightly up and down only when moving
            float bounce = 0f;
            if (displayYalms > 0.5f)
            {
                // Scale bounce based on speed (faster = more bounce)
                float bounceScale = Math.Min(displayYalms / 5.0f, 1.0f);
                bounce = (float)Math.Sin(animationTimer * Math.PI * 2) * 2.0f * bounceScale;
            }
            Vector2 adjustedPos = new Vector2(catPos.X, catPos.Y + bounce);

            // Colors
            uint pinkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.6f, 0.8f, 1.0f));
            uint darkPinkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.4f, 0.6f, 1.0f));
            uint blackColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            uint whiteColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            uint tanColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.65f, 1.0f));

            // Draw cat body (main pink rectangle)
            drawList.AddRectFilled(
                adjustedPos,
                new Vector2(adjustedPos.X + catSize.X, adjustedPos.Y + catSize.Y),
                pinkColor
            );

            // Draw cat head outline (slightly darker pink)
            float headSize = catSize.Y * 0.9f;
            Vector2 headPos = new Vector2(
                adjustedPos.X + catSize.X - headSize - 5,
                adjustedPos.Y + (catSize.Y - headSize) / 2
            );
            drawList.AddRectFilled(
                headPos,
                new Vector2(headPos.X + headSize, headPos.Y + headSize),
                darkPinkColor
            );

            // Draw cat head (pink)
            float innerHeadSize = headSize - 2;
            Vector2 innerHeadPos = new Vector2(
                headPos.X + 1,
                headPos.Y + 1
            );
            drawList.AddRectFilled(
                innerHeadPos,
                new Vector2(innerHeadPos.X + innerHeadSize, innerHeadPos.Y + innerHeadSize),
                pinkColor
            );

            // Draw eyes
            float eyeSize = headSize * 0.15f;
            Vector2 leftEyePos = new Vector2(headPos.X + headSize * 0.25f, headPos.Y + headSize * 0.3f);
            Vector2 rightEyePos = new Vector2(headPos.X + headSize * 0.25f, headPos.Y + headSize * 0.7f);

            // Draw eyes (black)
            drawList.AddCircleFilled(leftEyePos, eyeSize, blackColor);
            drawList.AddCircleFilled(rightEyePos, eyeSize, blackColor);

            // Draw eye highlights (white)
            drawList.AddCircleFilled(
                new Vector2(leftEyePos.X - eyeSize * 0.3f, leftEyePos.Y - eyeSize * 0.3f),
                eyeSize * 0.4f,
                whiteColor
            );
            drawList.AddCircleFilled(
                new Vector2(rightEyePos.X - eyeSize * 0.3f, rightEyePos.Y - eyeSize * 0.3f),
                eyeSize * 0.4f,
                whiteColor
            );

            // Draw mouth
            Vector2 mouthStart = new Vector2(headPos.X + headSize * 0.6f, headPos.Y + headSize * 0.5f);
            drawList.AddBezierCubic(
                mouthStart,
                new Vector2(mouthStart.X + headSize * 0.2f, mouthStart.Y - headSize * 0.1f),
                new Vector2(mouthStart.X + headSize * 0.3f, mouthStart.Y + headSize * 0.1f),
                new Vector2(mouthStart.X + headSize * 0.4f, mouthStart.Y),
                blackColor,
                2.0f
            );

            // Draw toast-like tan body
            float toastWidth = catSize.X * 0.65f; // Make toast slightly wider
            float toastHeight = catSize.Y * 0.75f; // Make toast slightly taller
            Vector2 toastPos = new Vector2(
                adjustedPos.X + 10, // Position toast a bit to the right for better trail connection
                adjustedPos.Y + (catSize.Y - toastHeight) / 2
            );
            drawList.AddRectFilled(
                toastPos,
                new Vector2(toastPos.X + toastWidth, toastPos.Y + toastHeight),
                tanColor
            );

            // Leg animation based on frame counter
            float legOffset = (frameCounter % 2 == 0) ? 2.0f : -2.0f;

            // Draw legs (small rectangles)
            float legWidth = 8.0f;
            float legHeight = 6.0f;
            float legSpacing = toastHeight / 3;

            // Front legs
            for (int i = 0; i < 2; i++)
            {
                float yPos = toastPos.Y + legSpacing * (i + 1) - legHeight / 2;
                float xOffset = (i % 2 == 0) ? legOffset : -legOffset;

                drawList.AddRectFilled(
                    new Vector2(toastPos.X - legWidth + xOffset, yPos),
                    new Vector2(toastPos.X + xOffset, yPos + legHeight),
                    pinkColor
                );
            }

            // Back legs
            for (int i = 0; i < 2; i++)
            {
                float yPos = toastPos.Y + legSpacing * (i + 1) - legHeight / 2;
                float xOffset = (i % 2 == 0) ? -legOffset : legOffset;

                drawList.AddRectFilled(
                    new Vector2(toastPos.X + toastWidth + xOffset, yPos),
                    new Vector2(toastPos.X + toastWidth + legWidth + xOffset, yPos + legHeight),
                    pinkColor
                );
            }

            // Draw tail
            Vector2 tailStart = new Vector2(toastPos.X + toastWidth + 2, toastPos.Y + toastHeight / 2);
            Vector2 tailEnd = new Vector2(tailStart.X + catSize.X * 0.15f, tailStart.Y + (float)Math.Sin(animationTimer * Math.PI * 3) * 5.0f);

            drawList.AddBezierCubic(
                tailStart,
                new Vector2(tailStart.X + 10, tailStart.Y - 10),
                new Vector2(tailEnd.X - 10, tailEnd.Y + 10),
                tailEnd,
                pinkColor,
                4.0f
            );
        }

        // Draw a small close button
        private void DrawCloseButton(ImDrawListPtr drawList, Vector2 pos, float radius)
        {
            // Check if mouse is over button
            Vector2 mousePos = ImGui.GetIO().MousePos;
            bool isHovering = Math.Pow(mousePos.X - pos.X, 2) + Math.Pow(mousePos.Y - pos.Y, 2) <= radius * radius;

            // Draw button circle
            uint buttonColor = isHovering ?
                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.3f, 0.3f, 1.0f)) :
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.1f, 0.1f, 0.8f));

            drawList.AddCircleFilled(pos, radius, buttonColor);

            // Draw X
            float xSize = radius * 0.7f;
            drawList.AddLine(
                new Vector2(pos.X - xSize / 2, pos.Y - xSize / 2),
                new Vector2(pos.X + xSize / 2, pos.Y + xSize / 2),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.8f)),
                1.5f
            );
            drawList.AddLine(
                new Vector2(pos.X + xSize / 2, pos.Y - xSize / 2),
                new Vector2(pos.X - xSize / 2, pos.Y + xSize / 2),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.8f)),
                1.5f
            );

            // Handle click
            if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                closeButtonClicked = true;
            }
        }

        // Dispose method
        public void Dispose()
        {
            // No resources to dispose - texture is handled by the Plugin class
        }
    }
}
