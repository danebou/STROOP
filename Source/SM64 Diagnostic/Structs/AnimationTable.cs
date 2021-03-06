﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public class AnimationTable
    {
        public struct AnimationReference
        {
            public int AnimationValue;
            public string AnimationName;

            public override int GetHashCode()
            {
                return AnimationValue;
            }
        }

        Dictionary<int, AnimationReference> _table = new Dictionary<int, AnimationReference>();

        public AnimationTable()
        {
        }

        public void Add(AnimationReference animationRef)
        {
            _table.Add(animationRef.AnimationValue, animationRef);
        }

        public string GetAnimationName(int animation)
        {
            if (!_table.ContainsKey(animation))
                return "Unknown Animation";

            return _table[animation].AnimationName;
        }
    }
}
