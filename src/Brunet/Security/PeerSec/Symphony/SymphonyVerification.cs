/*
Copyright (C) 2009 David Wolinsky <davidiw@ufl.edu>, University of Florida

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

using Brunet.Connections;
using Brunet.Messaging;
using Brunet.Transport;
using Brunet.Symphony;
using Mono.Security.X509;

namespace Brunet.Security {
  public class SymphonyVerification : ICertificateVerification {
    protected ConnectionTable _ct;

    public SymphonyVerification(ConnectionTable ct)
    {
      _ct = ct;
    }

    public bool Verify(X509Certificate certificate, ISender sender)
    {
      Address addr = null;
      AHSender ahsender = sender as AHSender;
      if(ahsender != null) {
        addr = ahsender.Destination;
      } else {
        Edge edge = sender as Edge;
        if(edge != null) {
          Connection con = _ct.GetConnection(edge);
          if(con != null) {
            addr = con.Address;
          }
        }
      }

      if(addr == null) {
        return true;
      }
      return CertificateHandler.Verify(certificate, addr.ToString());
    }
  }
}
