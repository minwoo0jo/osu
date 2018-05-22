// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Linq;
using osu.Game.Rulesets.Osu.Objects;
using OpenTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    /// <summary>
    /// A wrapper around <see cref="OsuHitObject"/> extending it with additional data required for difficulty calculation.
    /// </summary>
    public class OsuDifficultyHitObject
    {
        private const int normalized_radius = 52;

        /// <summary>
        /// The <see cref="OsuHitObject"/> this <see cref="OsuDifficultyHitObject"/> refers to.
        /// </summary>
        public OsuHitObject BaseObject { get; }

        /// <summary>
        /// Normalized distance from the <see cref="OsuHitObject.StackedPosition"/> of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double Distance { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the StartTime of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double DeltaTime { get; private set; }

        /// <summary>
        /// Inner angle formed by the last three <see cref="OsuDifficultyHitObject"/> in degrees.
        /// </summary>
        public double JumpAngle { get; private set; }

        /// <summary>
        /// Number of milliseconds until the <see cref="OsuDifficultyHitObject"/> has to be hit.
        /// </summary>
        public double TimeUntilHit { get; set; }

        /// <summary>
        /// Number of objects already on the screen when the <see cref "OsuDifficultyHitObject"> appears
        /// </summary>
        public int TrueDensity { get; set; }

        private readonly OsuHitObject lastObject;
        private readonly double timeRate;

        /// <summary>
        /// Initializes the object calculating extra data required for difficulty calculation.
        /// </summary>
        public OsuDifficultyHitObject(OsuHitObject currentObject, OsuHitObject lastObject, OsuHitObject lastLastObject, double timeRate)
        {
            this.lastObject = lastObject;
            this.timeRate = timeRate;

            BaseObject = currentObject;

            setDistances();
            setTimingValues();
            // Calculate angle here
            OsuHitObject[] triangle = new OsuHitObject[] { currentObject, lastObject, lastLastObject };
            setAngle(triangle);
        }

        private void setDistances()
        {
            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            double scalingFactor = normalized_radius / BaseObject.Radius;
            if (BaseObject.Radius < 30)
            {
                double smallCircleBonus = Math.Min(30 - BaseObject.Radius, 5) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            Vector2 lastCursorPosition = lastObject.StackedPosition;
            float lastTravelDistance = 0;

            var lastSlider = lastObject as Slider;
            if (lastSlider != null)
            {
                computeSliderCursorPosition(lastSlider);
                lastCursorPosition = lastSlider.LazyEndPosition ?? lastCursorPosition;
                lastTravelDistance = lastSlider.LazyTravelDistance;
            }

            Distance = (lastTravelDistance + (BaseObject.StackedPosition - lastCursorPosition).Length) * scalingFactor;
        }

        private void setTimingValues()
        {
            // Every timing inverval is hard capped at the equivalent of 375 BPM streaming speed as a safety measure.
            DeltaTime = Math.Max(50, (BaseObject.StartTime - lastObject.StartTime) / timeRate);
            TimeUntilHit = BaseObject.TimePreempt;
        }

        private void computeSliderCursorPosition(Slider slider)
        {
            if (slider.LazyEndPosition != null)
                return;
            slider.LazyEndPosition = slider.StackedPosition;

            float approxFollowCircleRadius = (float)(slider.Radius * 3);
            var computeVertex = new Action<double>(t =>
            {
                // ReSharper disable once PossibleInvalidOperationException (bugged in current r# version)
                var diff = slider.StackedPositionAt(t) - slider.LazyEndPosition.Value;
                float dist = diff.Length;

                if (dist > approxFollowCircleRadius)
                {
                    // The cursor would be outside the follow circle, we need to move it
                    diff.Normalize(); // Obtain direction of diff
                    dist -= approxFollowCircleRadius;
                    slider.LazyEndPosition += diff * dist;
                    slider.LazyTravelDistance += dist;
                }
            });

            // Skip the head circle
            var scoringTimes = slider.NestedHitObjects.Skip(1).Select(t => t.StartTime);
            foreach (var time in scoringTimes)
                computeVertex(time);
            computeVertex(slider.EndTime);
        }

        private void calculateAngle(OsuHitObject[] t)
        {
            Vector2 v1 = new Vector2(t[2].X - t[1].X, t[2].Y - t[1].Y);
            Vector2 v2 = new Vector2(t[0].X - t[1].X, t[0].Y - t[1].Y);
            double angle = Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X);
            //Converting values in range (-2pi, pi) to (0, 180)
            angle = Math.Abs(angle * (180.0 / Math.PI));
            angle = (angle > 180) ? (angle - 180) : angle;
            JumpAngle = angle;
        }
    }
}
