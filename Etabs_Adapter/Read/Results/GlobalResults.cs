﻿/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Common;
#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS.Elements;
using BH.Engine.ETABS;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Requests;
using BH.oM.Geometry;
using BH.Engine.Geometry;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(GlobalResultRequest request)
        {
            switch (request.ResultType)
            {
                case GlobalResultType.Reactions:
                    return GetGlobalReactions(request.Cases);
                case GlobalResultType.ModalDynamics:
                    return GetModalParticipationMassRatios(request.Cases);
                default:
                    Engine.Reflection.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        private List<GlobalReactions> GetGlobalReactions(IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();
            List<GlobalReactions> globalReactions = new List<GlobalReactions>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] fx = null; double[] fy = null; double[] fz = null;
            double[] mx = null; double[] my = null; double[] mz = null;
            double gx = 0; double gy = 0; double gz = 0;

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            m_model.Results.BaseReact(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, ref gx, ref gy, ref gz);

            for (int i = 0; i < resultCount; i++)
            {
                GlobalReactions g = new GlobalReactions()
                {
                    ResultCase = loadcaseNames[i],
                    FX = fx[i],
                    FY = fy[i],
                    FZ = fz[i],
                    MX = mx[i],
                    MY = my[i],
                    MZ = mz[i],
                    TimeStep = stepNum[i]
                };

                globalReactions.Add(g);
            }

            return globalReactions;
        }

        /***************************************************/

        private List<ModalDynamics> GetModalParticipationMassRatios(IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            List<ModalDynamics> partRatios = new List<ModalDynamics>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] period = null;
            double[] ux = null; double[] uy = null; double[] uz = null;
            double[] sumUx = null; double[] sumUy = null; double[] sumUz = null;
            double[] rx = null; double[] ry = null; double[] rz = null;
            double[] sumRx = null; double[] sumRy = null; double[] sumRz = null;

            int res = m_model.Results.ModalParticipatingMassRatios(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum,
                ref period, ref ux, ref uy, ref uz, ref sumUx, ref sumUy, ref sumUz, ref rx, ref ry, ref rz, ref sumRx, ref sumRy, ref sumRz);

            if (res != 0) Engine.Reflection.Compute.RecordError("Could not extract Modal information.");


            // Although API documentation says that StepNumber should correspond to the Mode Number, testing shows that StepNumber is always 0.
            string previousModalCase = "";
            int modeNumber = 1; //makes up for stepnumber always = 0
            for (int i = 0; i < resultCount; i++)
            {
                if (loadcaseNames[i] != previousModalCase)
                    modeNumber = 1;

                ModalDynamics mod = new ModalDynamics()
                {
                    ResultCase = loadcaseNames[i],
                    ModeNumber = modeNumber,
                    Frequency = 1 / period[i],
                    MassRatioX = ux[i],
                    MassRatioY = uy[i],
                    MassRatioZ = uz[i],
                    InertiaRatioX = rx[i],
                    InertiaRatioY = ry[i],
                    InertiaRatioZ = rz[i]
                };

                modeNumber += 1;
                previousModalCase = loadcaseNames[i];

                partRatios.Add(mod);
            }

            return partRatios;
        }

        /***************************************************/

    }
}
