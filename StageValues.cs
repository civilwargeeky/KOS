﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    public class StageValues : SpecialValue
    {
        Vessel vessel;

        public StageValues(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override object GetSuffix(string suffixName)
        {
            return GetResourceOfCurrentStage(suffixName);
        }

        private object GetResourceOfCurrentStage(String resourceName)
        {
            var activeEngines = VesselUtils.GetListOfActivatedEngines(vessel);
            return Utils.ProspectForResource(resourceName, activeEngines);
        }
    }
}
