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
            
            if (current.JumpAngle <= 60 && distance > 39)
            {
                time *= 0.67 + Math.Max(time, 100) / 300;
            }
            else if (current.JumpAngle > 120)
            {
                distance += Math.Pow(current.Distance, 0.99) * ((current.JumpAngle - 120) / 60);
            }
            double aimValue = distance / time;

            return aimValue;
        }
    }
}
