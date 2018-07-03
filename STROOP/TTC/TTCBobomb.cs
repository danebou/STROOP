﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace STROOP.Ttc
{
    /** A bob-omb is the black bomb enemy. There are
    *  two of them in TTC, near the start of the course.
    *  
    *  A bob-omb calls RNG every frame to determine whether
    *  it should blink its eyes. If it does blink, then it blinks
    *  for 16 frames, during which it won't call RNG.
    *  
    *  A bob-omb will only update when Mario is within 4000
    *  units of the bob-omb. Note that this is not synonymous
    *  with whether the bob-omb is visible or not, which is
    *  dictated by a radius smaller than 4000. In other words,
    *  for certain distances (like 3500), the bob-omb will
    *  not be visible, but will still update and call RNG
    *  just as normal.
    */
    public class TtcBobomb : TtcObject
    {

        //how deep into the blink the bob-omb is
        //this variable is 0 when the bob-omb is not blinking
        public int blinkingTimer;

        //whether Mario is within 4000 units of the bob-omb
        public bool withinMarioRange;

        public TtcBobomb()
        {
            blinkingTimer = 0;
            withinMarioRange = true;
        }

        public override void update()
        {
            //don't update at all if not within mario range
            if (!withinMarioRange) return;

            if (blinkingTimer > 0)
            { //currently blinking
                blinkingTimer = (blinkingTimer + 1) % 16;
            }
            else
            { //not currently blinking
                if (pollRNG() <= 655)
                {
                    blinkingTimer++;
                }
            }
        }

        /** Change whether Mario is within the bob-omb's
	     *  4000 unit radius.
	     */
        public void setWithinMarioRange(bool b)
        {
            withinMarioRange = b;
        }

        public override string ToString()
        {
            return id + OPENER + blinkingTimer + CLOSER;
        }

    }

}