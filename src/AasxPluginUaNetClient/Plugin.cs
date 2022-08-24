/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>,
Author: Michael Hoffmeister.

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

For OPC Content:

This code is based on:
https://github.com/OPCFoundation/UA-.NETStandard-Samples/tree/master/Samples/NetCoreConsoleClient

 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    // ReSharper disable once UnusedType.Global
    public class AasxPlugin : IAasxPluginInterface
    {
        private LogInstance Log = new LogInstance();

        public string GetPluginName()
        {
            return "AasxPluginOpcUaClient";
        }

        public void InitPlugin(string[] args)
        {
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(new AasxPluginActionDescriptionBase("create-client",
                "Creates a OPC UA client and returns as plain object. Arguments: (string _endpointURL, "
                + "bool _autoAccept, int _stopTimeout, string _userName, string _password)."));
            res.Add(new AasxPluginActionDescriptionBase("read-sme-value",
                "Reads a value and returns as plain object. Arguments: (UASampleClient client, string nodeName, "
                + "int index)."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense =
                    "This application uses the OPC Foundation .NET Standard stack. See: OPC REDISTRIBUTABLES "
                    + "Agreement of Use." + Environment.NewLine +
                    "The OPC UA Example Code of OPC UA Standard is licensed under the MIT license (MIT).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "create-client")
            {
                // OPC Copyright
                MessageBox.Show(
                    "Copyright (c) 2018-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, " +
                    "author: Andreas Orzelski\n\n" +
                    "Portions copyright (c) by OPC Foundation, Inc. and licensed under the Reciprocal Community " +
                    "License (RCL)\n" + "see https://opcfoundation.org/license/rcl.html",
                    "Plugin Notice"
                    );

                // check for arguments
                if (args == null || args.Length != 5 || !(args[0] is string && args[1] is bool && args[2] is int
                    && args[3] is string && args[4] is string))
                {
                    Log.Info("create-client() call with wrong arguments. Expected: (string _endpointURL, "
                        + "bool _autoAccept, int _stopTimeout, string _userName, string _password)");
                    return null;
                }

                // re-establish arguments
                var _endpointURL = args[0] as string;
                var _autoAccept = (bool)args[1];
                var _stopTimeout = (int)args[2];
                var _userName = args[3] as string;
                var _password = args[4] as string;

                // make client
                var client = new SampleClient.UASampleClient(_endpointURL, _autoAccept, _stopTimeout,
                    _userName, _password);
                client.ConsoleSampleClient().Wait();

                // return as plain object
                var res = new AasxPluginResultBaseObject();
                res.strType = "UASampleClient";
                res.obj = client;
                return res;
            }

            if (action == "read-sme-value")
            {
                // check for arguments
                if (args == null || args.Length != 3 || !(args[0] is SampleClient.UASampleClient
                    && args[1] is string && args[2] is int))
                {
                    Log.Info("read-sme-value() call with wrong arguments. Expected: (UASampleClient client, "
                        + "string nodeName, int index)");
                    return null;
                }

                // re-establish arguments
                var client = args[0] as SampleClient.UASampleClient;
                var nodeName = args[1] as string;
                var Namespace = (int)args[2];

                // make the call
                var value = client?.ReadSubmodelElementValue(nodeName, Namespace);

                // return as plain object
                var res = new AasxPluginResultBaseObject();
                res.strType = "value object";
                res.obj = value;
                return res;
            }

            return null;
        }
    }
}
