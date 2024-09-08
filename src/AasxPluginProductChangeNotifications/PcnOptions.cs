/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;

namespace AasxPluginProductChangeNotifications
{
    public class PcnOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class PcnOptions : AasxPluginLookupOptionsBase
    {
        public List<PcnOptionsRecord> Records = new List<PcnOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static PcnOptions CreateDefault()
        {
            var defs = new DefinitionsMTP.ModuleTypePackage();

            var rec1 = new PcnOptionsRecord();
            rec1.AllowSubmodelSemanticId = new[] { 
                new Aas.Key(Aas.KeyTypes.Submodel, "0173-10029#01-XFB001#001") }.ToList();

            var opt = new PcnOptions();
            opt.Records.Add(rec1);

            return opt;
        }
    }
}
