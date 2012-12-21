
/*
Copyright (C) 2012  David Wolinsky <davidiw@ufl.edu>, University of Florida
                    Pierre St Juste <ptony82@ufl.edu>, University of Florida

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using Brunet;
using Brunet.Util;
using Brunet.Applications;
using Brunet.Security.PeerSec.Symphony;

namespace Ipop.Managed {

  public class Program {

    public static void Main(string[] args) {
      NodeConfig node_config = Utils.ReadConfig<NodeConfig>("brunet.config");
      IpopConfig ipop_config = Utils.ReadConfig<IpopConfig>("ipop.config");

      RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
      if(!File.Exists(node_config.Security.KeyPath) ||
        node_config.NodeAddress == null) {
        node_config.NodeAddress = Utils.GenerateAHAddress().ToString();
        Utils.WriteConfig("brunet.config", node_config);

        byte[] data = rsa.ExportCspBlob(true);
        FileStream file = File.Open(node_config.Security.KeyPath, FileMode.Create);
        file.Write(data, 0, data.Length);
        file.Close();

      }
      else if(File.Exists(node_config.Security.KeyPath)) {
        FileStream file = File.Open(node_config.Security.KeyPath, FileMode.Open);
        byte[] blob = new byte[file.Length];
        file.Read(blob, 0, (int)file.Length);
        file.Close();

        rsa.ImportCspBlob(blob);
      }

      ManagedIpopNode node = new ManagedIpopNode(node_config, ipop_config);
      node.Marad.InitCert(rsa);
      Thread nodeThread = new Thread(new ThreadStart(node.Run));
      nodeThread.Start();

      while (true) {
        string input = Console.ReadLine();
        if (input.StartsWith("svpn.")) {
          string[] inputs = input.Trim().Split(' ');
          string result = node.Marad.HandleRequest(inputs);
          Console.WriteLine("svpn: " + result);
        }
      }
    }

  }

}

