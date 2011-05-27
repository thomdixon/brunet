/*
Copyright (C) 2011 David Wolinsky <davidiw@ufl.edu>, University of Florida

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

using Brunet.Collections;
using Brunet.Connections;
using Brunet.Messaging;
using Brunet.Services;
using Brunet.Symphony;
using Brunet.Transport;
using Brunet.Util;

using System;
using System.Collections.Generic;

namespace Brunet.Security.PeerSec.Symphony {
  /// <summary>Provides a wrapper around sender objects, obtaining Edges if
  /// available, otherwise overlay senders.</summary>
  public class SecureConnectionHandler : ConnectionHandler {
    protected readonly SymphonySecurityOverlord _so;
    protected readonly Dictionary<SecurityAssociation, bool> _registered;

    public SecureConnectionHandler(PType ptype, StructuredNode node,
        SymphonySecurityOverlord so) : base(ptype, node)
    {
      _so = so;
      _registered = new Dictionary<SecurityAssociation, bool>();
    }

    override public void HandleData(MemBlock data, ISender return_path, object state)
    {
      SecurityAssociation sa = return_path as SecurityAssociation;
      if(sa == null) {
        ProtocolLog.WriteIf(ProtocolLog.Exceptions, String.Format(
              "Insecure sender {0} sent ptype {1}", return_path, _ptype));
        return;
      }
      base.HandleData(data, return_path, state);
    }

    override protected Address SenderToAddress(ISender sender)
    {
      SecurityAssociation sa = sender as SecurityAssociation;
      if(sa == null) {
        return null;
      }
      return base.SenderToAddress(sa.Sender);
    }

    override protected bool TryGetSender(Address dst, out ISender sender)
    {
      sender = null;
      var edge = GetConnection(dst);
      if(edge == null) {
        return false;
      }
      SecurityAssociation sa = null;
      if(GetSecureSender(edge, out sa)) {
        sender = sa;
        return true;
      }
      return false;
    }

    override protected void ValidConnection(Connection con)
    {
      con.StateChangeEvent += ConStateChange;
      SecurityAssociation sa;
      GetSecureSender(con, out sa);
    }
  
    override protected void ValidDisconnection(Connection con)
    {
      ISender sender;
      if(!_address_to_sender.TryGetValue(con.Address, out sender)) {
        sender = _so.CheckForSecurityAssociation(con.State.Edge);
        if(sender == null) {
          return;
        }
      }
      SecurityAssociation sa = sender as SecurityAssociation;
      if(sa != null) {
        sa.Close("Connection closed...");
      }
    }

    protected bool GetSecureSender(Connection con, out SecurityAssociation sa)
    {
      sa = _so.CreateSecurityAssociation(con.State.Edge);
      bool ready = false;
      lock(_address_to_sender) {
        if(!_registered.ContainsKey(sa)) {
          _registered[sa] = true;
          sa.StateChangeEvent += SAStateChange;
        }

        if(sa.State == SecurityAssociation.States.Active ||
            sa.State == SecurityAssociation.States.Updating)
        {
          AddConnection(con.Address, sa);
          ready = true;
        }
      }
      return ready;
    }

    protected void ConStateChange(Connection con,
        Pair<Connections.ConnectionState, Connections.ConnectionState> cs)
    {
      if(cs.First.Edge.Equals(cs.Second.Edge)) {
        return;
      }

      if(!_ondemand.ConnectionDesired(con.Address)) {
        return;
      }
      SecurityAssociation sa;
      GetSecureSender(con, out sa);
    }

    protected void SAStateChange(SecurityAssociation sa,
        SecurityAssociation.States state)
    {
      Address addr = SenderToAddress(sa);
      if(addr == null) {
        return;
      }

      if(state == SecurityAssociation.States.Active) {
        AddConnection(addr, sa);
      } else if(state == SecurityAssociation.States.Closed) {
        lock(_address_to_sender) {
          RemoveConnection(addr, sa);
          _registered.Remove(sa);
        }
      }
    }
  }
}
