// Copyright (C) 2010 David Wolinsky <davidiw@ufl.edu>, University of Florida
// For license, see the file LICENSE in the root directory of this software.

using System;

namespace Brunet.Simulator.Tasks {
  public class OnDemandConnection : ConnectionTask {

    public OnDemandConnection(NodeMapping node0, NodeMapping node1,
        bool secure, EventHandler finished) :
      base(node0, node1, secure, finished)
    {
    }

    override public void Start()
    {
      base.Start();
      EstablishConnection();
    }

    override protected void SecurityAssociationEstablished()
    {
      Finished();
    }

    override protected void AreConnectedHandler()
    {
      if(Secure) {
        if(Node0.Sso == null || Node1.Sso == null) {
          throw new Exception("SecurityOverlord undefined");
        }
        CreateSecurityAssociation();
      } else {
        Finished();
      }
    }
  }
}
