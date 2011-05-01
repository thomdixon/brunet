// Copyright (C) 2010 David Wolinsky <davidiw@ufl.edu>, University of Florida
// For license, see the file LICENSE in the root directory of this software.

using System;
using System.Text;
using Brunet.Connections;
using Brunet.Security;
using Brunet.Symphony;
using Brunet.Util;

namespace Brunet.Simulator.Tasks {
  abstract public class ConnectionTask : Task {
    public readonly NodeMapping Node0;
    public readonly NodeMapping Node1;
    public readonly bool Secure;
    public bool Successful { get { return _successful; } }
    protected bool _successful;
    protected AreConnected _are_connected;

    public ConnectionTask(NodeMapping node0, NodeMapping node1,
        bool secure, EventHandler finished) : base(finished)
    {
      Node0 = node0;
      Node1 = node1;
      Secure = secure;
      _successful = false;
    }

    abstract protected void SecurityAssociationEstablished();
    abstract protected void AreConnectedHandler();

    protected void CreateSecurityAssociation()
    {
      var sender = new AHExactSender(Node0.Node, Node1.Node.Address);
      var sa = Node0.Sso.CreateSecurityAssociation(sender);

      SecurityAssociation.StateChangeHandler callback =
        delegate(SecurityAssociation in_sa, SecurityAssociation.States state)
      {
        if(state == SecurityAssociation.States.Active) {
          SecurityAssociationEstablished();
        } else if(state == SecurityAssociation.States.Closed) {
          Finished();
        } else {
          Console.WriteLine(state);
        }
      };

      if(sa.State != SecurityAssociation.States.Waiting) {
        callback(sa, sa.State);
      } else {
        sa.StateChangeEvent += callback;
      }
    }

    override protected void Finished()
    {
      if(_are_connected != null) {
        _successful = _are_connected.Done;
      }
      base.Finished();
    }

    protected void EstablishConnection()
    {
      var node0 = Node0.Node;
      var node1 = Node1.Node;
      ManagedConnectionOverlord mco = new ManagedConnectionOverlord(node0);
      mco.Start();
      node0.AddConnectionOverlord(mco);
      mco.Set(node1.Address);

      EventHandler eh = delegate(object obj, EventArgs ea) { AreConnectedHandler(); };
      _are_connected = new AreConnected(node0, node1, eh);
      _are_connected.Start();
    }

    override public string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Connection State: ");
      if(!Done) {
        sb.Append("In progress.");
        return sb.ToString();
      }

      sb.Append("Complete");
      sb.Append("\n\tTook: " + _time_taken);
      sb.Append("\n\tSuccess: " + _successful);
      return sb.ToString();
    }
  }
}
