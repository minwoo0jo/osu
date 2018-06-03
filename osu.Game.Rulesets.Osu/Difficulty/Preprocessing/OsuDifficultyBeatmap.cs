// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    /// <summary>
    /// An enumerable container wrapping <see cref="OsuHitObject"/> input as <see cref="OsuDifficultyHitObject"/>
    /// which contains extra data required for difficulty calculation.
    /// </summary>
    public class OsuDifficultyBeatmap : IEnumerable<OsuDifficultyHitObject>
    {
        private readonly IEnumerator<OsuDifficultyHitObject> difficultyObjects;
        private readonly Queue<OsuDifficultyHitObject> onScreen = new Queue<OsuDifficultyHitObject>();

        /// <summary>
        /// Creates an enumerator, which preprocesses a list of <see cref="OsuHitObject"/>s recieved as input, wrapping them as
        /// <see cref="OsuDifficultyHitObject"/> which contains extra data required for difficulty calculation.
        /// </summary>
        public OsuDifficultyBeatmap(List<OsuHitObject> objects, double timeRate)
        {
            // Sort OsuHitObjects by StartTime - they are not correctly ordered in some cases.
            // This should probably happen before the objects reach the difficulty calculator.
            objects.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            difficultyObjects = createDifficultyObjectEnumerator(objects, timeRate);
        }

        /// <summary>
        /// Returns an enumerator that enumerates all <see cref="OsuDifficultyHitObject"/>s in the <see cref="OsuDifficultyBeatmap"/>.
        /// The inner loop adds objects that appear on screen into a queue until we need to hit the next object.
        /// The outer loop returns objects from this queue one at a time, only after they had to be hit, and should no longer be on screen.
        /// This means that we can loop through every object that is on screen at the time when a new one appears,
        /// allowing us to determine a reading strain for the object that just appeared.
        /// </summary>
        public IEnumerator<OsuDifficultyHitObject> GetEnumerator()
        {
            while (true)
            {
                // Add upcoming objects to the queue until we have at least one object that had been hit and can be dequeued.
                // This means there is always at least one object in the queue unless we reached the end of the map.
                do
                {
                    if (!difficultyObjects.MoveNext())
                        break; // New objects can't be added anymore, but we still need to dequeue and return the ones already on screen.

                    OsuDifficultyHitObject latest = difficultyObjects.Current;
                    latest.TrueDensity = 0;
                    // Calculate flow values here
                    foreach (OsuDifficultyHitObject h in onScreen)
                    {
                        // ReSharper disable once PossibleNullReferenceException (resharper not smart enough to understand IEnumerator.MoveNext())
                        h.TimeUntilHit -= latest.DeltaTime;
                        // Calculate reading strain here
                        // Not every note needs to be processed separately to be played properly
                        // Stacks will not add to density
                        // Most linear patterns are processed as a single object
                        if (h.TimeUntilHit > 0)
                        {
                            latest.TrueDensity++;
                        }
                    }

                    onScreen.Enqueue(latest);
                }
                while (onScreen.Peek().TimeUntilHit > 0); // Keep adding new objects on screen while there is still time before we have to hit the next one.

                if (onScreen.Count == 0) break; // We have reached the end of the map and enumerated all the objects.
                yield return onScreen.Dequeue(); // Remove and return objects one by one that had to be hit before the latest one appeared.
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        private IEnumerator<OsuDifficultyHitObject> createDifficultyObjectEnumerator(List<OsuHitObject> objects, double timeRate)
        {
            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < objects.Count; i++)
            {
                if (i == 1)
                {
                    // Duplicate the first hitobjects of the map to form the first triangle
                    yield return new OsuDifficultyHitObject(objects[i], objects[i - 1], objects[i - 1], timeRate);
                    continue;
                }
                yield return new OsuDifficultyHitObject(objects[i], objects[i - 1], objects[i - 2], timeRate);
            }
        }
    }
}
