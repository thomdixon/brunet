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

using Brunet.Connections;
using Brunet.Messaging;
using Brunet.Symphony;
using Brunet.Transport;
using Brunet.Util;

using System;
using System.Collections.Generic;

namespace Brunet.Services {
  /// <summary>Provides a wrapper around sender objects, obtaining Edges if
  /// available, otherwise overlay senders.</summary>
  public class ConnectionHandler : SimpleSource, IDataHandler {
    protected readonly Dictionary<Connection, ConSenderWrapper> _con_to_csw;
    protected readonly Dictionary<Address, ISender> _address_to_sender;
    protected readonly Dictionary<ISender, Address> _sender_to_address;
    protected readonly StructuredNode _node;
    protected readonly PType _ptype;
    protected readonly MemBlock _ptype_mb;
    protected readonly OnDemandConnectionOverlord _ondemand;
    public enum ConnectionState {
      Offline,
      Online,
    };
    public delegate void ConnectionEvent(Address addr, ConnectionState cs);
    public event ConnectionEvent ConnectionReady;
    
    protected class ConSenderWrapper : ISender {
      public readonly Connection Con;

      public ConSenderWrapper(Connection con)
      {
        Con = con;
      }

      public void Send(ICopyable data)
      {
        Con.State.Edge.Send(data);
      }

      public string ToUri()
      {
        throw new NotImplementedException();
      }
    }

    public ConnectionHandler(PType ptype, StructuredNode node)
    {
      _node = node;
      _ondemand = new OnDemandConnectionOverlord(node);
      _node.AddConnectionOverlord(_ondemand);
      _ptype = ptype;
      _ptype_mb = ptype.ToMemBlock();
      _address_to_sender = new Dictionary<Address, ISender>();
      _sender_to_address = new Dictionary<ISender, Address>();
      _con_to_csw = new Dictionary<Connection, ConSenderWrapper>();

      node.GetTypeSource(_ptype).Subscribe(this, null);
      node.ConnectionTable.ConnectionEvent += HandleConnection;
      node.ConnectionTable.DisconnectionEvent += HandleDisconnection;
    }

    public void ConnectTo(Address dst)
    {
      _ondemand.Set(dst);
    }

    public bool ContainsAddress(Address addr)
    {
      if(addr == null) {
        return false;
      }

      ISender sender;
      return _address_to_sender.ContainsKey(addr) || TryGetSender(addr, out sender);
    }

    public Address GetAddress(ISender sender)
    {
      Address addr = null;
      if(!_sender_to_address.TryGetValue(sender, out addr)) {
        addr = SenderToAddress(sender);
        if(addr != null) {
          AddConnection(addr, sender);
        }
      }
      return addr;
    }

    public ISender GetSender(Address addr)
    {
      if(addr == null) {
        return null;
      }
      ISender sender = null;
      _address_to_sender.TryGetValue(addr, out sender);
      return sender;
    }

    virtual public void HandleData(MemBlock data, ISender return_path, object state)
    {
      Address addr = GetAddress(return_path);
      if(addr == null) {
        ProtocolLog.WriteIf(ProtocolLog.ConnectionHandlerLog,
            String.Format("Unable to obtain an address for: {0}", return_path));
        return;
      }
      _ondemand.Set(addr);

      try {
        _sub.Handle(data, return_path);
      } catch {
        string d_s = data.GetString(System.Text.Encoding.ASCII);
        ProtocolLog.WriteIf(ProtocolLog.Exceptions, String.Format(
              "Error handling packet from {0}, containing {1}",
              return_path, d_s));
      }
    }

    public bool Send(Address dst, MemBlock packet)
    {
      _ondemand.Set(dst);
      ISender sender = null;
      if(_address_to_sender.TryGetValue(dst, out sender) ||
          TryGetSender(dst, out sender))
      {
        try {
          sender.Send(new CopyList(_ptype_mb, packet));
          return true;
        } catch {
        }
      } else {
        ProtocolLog.WriteIf(ProtocolLog.ConnectionHandlerLog,
            String.Format("Unable to a destination for address: {0}", dst));
      }
      return false;
    }

    /// <summary>
    virtual protected Address SenderToAddress(ISender sender)
    {
      Connection con = sender as Connection;
      if(con == null) {
        Edge edge = sender as Edge;
        if(edge == null) {
          return null;
        }
        con = _node.ConnectionTable.GetConnection(edge);
        if(con == null) {
          return null;
        }
      }
      return con.Address;
    }

    ///<summary>Try to find a sender, if it exists, add to the dictionaries</summary>
    virtual protected bool TryGetSender(Address dst, out ISender sender)
    {
      // Let's see if we have a connection
      Connection con = GetConnection(dst);
      if(con == null) {
        sender = null;
        return false;
      }

      sender = new ConSenderWrapper(con);
      AddConnection(con.Address, sender);
      return true;
    }

    virtual protected void ValidConnection(Connection con)
    {
      AddConnection(con.Address, new ConSenderWrapper(con));
    }
  
    virtual protected void ValidDisconnection(Connection con)
    {
      RemoveConnection(con.Address);
    }

    /// <summary>Add to the dictionaries!</summary>
    protected void AddConnection(Address addr, ISender sender)
    {
      lock(_address_to_sender) {
        if(_address_to_sender.ContainsKey(addr)) {
          ISender to_remove = _address_to_sender[addr];
          _sender_to_address.Remove(to_remove);
        }
        _address_to_sender[addr] = sender;
        _sender_to_address[sender] = addr;
      }
      var ce = ConnectionReady;
      if(ce != null) {
        ce(addr, ConnectionState.Online);
      }
    }

    protected Connection GetConnection(Address addr)
    {
      ConnectionList cons = _node.ConnectionTable.GetConnections(ConnectionType.Structured);
      int index = cons.IndexOf(addr);
      if(index < 0) {
        return null;
      }
      return cons[index];
    }

    /// <summary>New edge...</summary>
    protected void HandleConnection(object ct, EventArgs ea)
    {
      ConnectionEventArgs cea = ea as ConnectionEventArgs;
      Connection con = cea.Connection;
      if(con.MainType != ConnectionType.Structured) {
        return;
      }

      if(!_ondemand.ConnectionDesired(con.Address)) {
        return;
      }
      ValidConnection(con);
    }

    ///<summary>Lost an edge...</summary>
    protected void HandleDisconnection(object ct, EventArgs ea)
    {
      ConnectionEventArgs cea = ea as ConnectionEventArgs;
      Connection con = cea.Connection;
      if(con.MainType != ConnectionType.Structured) {
        return;
      }

      ValidDisconnection(con);
    }

    ///<summary>Lost an edge, remove it from our connections</summary>
    protected void RemoveConnection(Address addr)
    {
      lock(_address_to_sender) {
        if(!_address_to_sender.ContainsKey(addr)) {
          return;
        }
        _sender_to_address.Remove(_address_to_sender[addr]);
        _address_to_sender.Remove(addr);
      }
      var ce = ConnectionReady;
      if(ce != null) {
        ce(addr, ConnectionState.Offline);
      }
    }
  }
}
