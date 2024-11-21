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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxPluginContactInformation
{
    /// <summary>
    /// Additional version specific information. None, yet.
    /// </summary>
    public class ContactInformationOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
    }

    public class ContactInformationOptions : AasxPluginLookupOptionsBase
    {
        public List<ContactInformationOptionsRecord> Records = new List<ContactInformationOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static ContactInformationOptions CreateDefault()
        {
            var opt = new ContactInformationOptions();

            //
            //  basic record
            //

            var rec = new ContactInformationOptionsRecord();
            opt.Records.Add(rec);

            // V1.0
            rec.AllowSubmodelSemanticId.Add(
                AasxPredefinedConcepts.IdtaContactInformationV10.Static.SM_ContactInformations.GetSemanticKey());

            return opt;
        }
    }

}
