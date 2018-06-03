// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : Skill
    {
        protected override double SkillMultiplier => 26.25;
        protected override double StrainDecayBase => 0.15;

        protected override double StrainValueOf(OsuDifficultyHitObject current)
        {
            double distance = Math.Pow(current.Distance, 0.99);
            double time = current.DeltaTime;

            // Any 1/4 note above 150 BPM will receive a buff if the angle is 90 degrees or below
            if (current.JumpAngle <= 90 && current.JumpAngle >= 0 && distance > 39)
            {
                time *= 0.67 + Math.Min(time, 100) / 300;
            }
            // Any jump with an angle of above 120 degrees will scale harder with distance
            else if (current.JumpAngle > 120)
            {
                distance += Math.Pow(current.Distance, 1.4) * ((current.JumpAngle - 120) / 480);
            }
            double aimValue = distance / time;

            return aimValue;
        }
    }
}
