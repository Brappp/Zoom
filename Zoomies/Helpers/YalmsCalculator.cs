using System;
using System.Numerics;

namespace ZoomiesPlugin.Helpers
{
    public class YalmsCalculator
    {
        // Previous player position for speed calculation
        private Vector3 previousPosition;
        // Previous time measurement
        private DateTime previousTime;
        // Current calculated speed in yalms per second
        private float currentYalms;
        // Smoothed display speed for animation
        private float displayYalms;
        // Smoothing factor for needle movement
        private float damping;

        public YalmsCalculator()
        {
            // Initialize values
            previousPosition = Vector3.Zero;
            previousTime = DateTime.Now;
            currentYalms = 0.0f;
            displayYalms = 0.0f;
            damping = 0.1f; // Default damping value (lower = smoother)
        }

        // Get the currently displayed speed
        public float GetDisplayYalms()
        {
            return displayYalms;
        }

        // Get the current raw speed (for debug)
        public float GetCurrentYalms()
        {
            return currentYalms;
        }

        // Get the previous position (for debug)
        public Vector3 GetPreviousPosition()
        {
            return previousPosition;
        }

        // Get the previous time (for debug)
        public DateTime GetPreviousTime()
        {
            return previousTime;
        }

        // Set the damping factor for needle movement
        public void SetDamping(float newDamping)
        {
            damping = Math.Clamp(newDamping, 0.01f, 1.0f);
        }

        // Update the speed calculation
        public void Update(Vector3 currentPosition)
        {
            // If position hasn't been initialized, just store and return
            if (previousPosition == Vector3.Zero)
            {
                previousPosition = currentPosition;
                previousTime = DateTime.Now;
                return;
            }

            // Get current time for delta time calculation
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - previousTime).TotalSeconds;

            // Only update if enough time has passed to avoid division by very small numbers
            if (deltaTime > 0.01)
            {
                // Calculate horizontal distance only (X and Z axes, ignoring Y/vertical axis)
                float distanceTraveled = new Vector2(
                    currentPosition.X - previousPosition.X,
                    currentPosition.Z - previousPosition.Z
                ).Length();

                // Calculate speed in horizontal yalms per second
                currentYalms = distanceTraveled / (float)deltaTime;

                // Update previous values for next calculation
                previousPosition = currentPosition;
                previousTime = currentTime;
            }

            // Smoothly animate toward the current speed
            displayYalms = displayYalms + (currentYalms - displayYalms) * damping;
        }

        // Reset all values (used when player is not available)
        public void Reset()
        {
            currentYalms = 0.0f;
            displayYalms = 0.0f;
            previousPosition = Vector3.Zero;
            previousTime = DateTime.Now;
        }
    }
}
