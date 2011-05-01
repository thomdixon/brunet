// Copyright (C) 2010 David Wolinsky <davidiw@ufl.edu>, University of Florida
// For license, see the file LICENSE in the root directory of this software.

using System;

namespace Brunet.Simulator.Tasks {
  public class ChotaLikeConnection : ConnectionTask {
    protected Rtt _rtt;

    public ChotaLikeConnection(NodeMapping node0, NodeMapping node1,
        bool secure, EventHandler finished) :
      base(node0, node1, secure, finished)
    {
    }

    override public void Start()
    {
      base.Start();
      _rtt = new Rtt(Node0.Node, Node1.Node, RttHandler);
      _rtt.Start();
    }

    protected void RttHandler(object rtt_task, EventArgs eh)
    {
      if(!Secure) {
        EstablishConnection();
        return;
      }

      if(Node0.Sso == null || Node1.Sso == null) {
        throw new Exception("SecurityOverlord undefined");
      }

      CreateSecurityAssociation();
    }

    override protected void SecurityAssociationEstablished()
    {
      EstablishConnection();
    }

    override protected void AreConnectedHandler()
    {
      Finished();
    }
  }
}
