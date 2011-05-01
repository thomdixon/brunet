// Copyright (C) 2010 David Wolinsky <davidiw@ufl.edu>, University of Florida
// For license, see the file LICENSE in the root directory of this software.

using Brunet.Concurrent;
using Brunet.Messaging;
using Brunet.Symphony;
using Brunet.Security.PeerSec.Symphony;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brunet.Simulator.Tasks {
  public class Rtt : Task {
    public readonly Node Node0;
    public readonly Node Node1;

    public Rtt(Node node0, Node node1, EventHandler finished) : base(finished)
    {
      Node0 = node0;
      Node1 = node1;
    }

    protected void Callback(object o, EventArgs ea)
    {
      Channel q = o as Channel;
      try {
        RpcResult res = (RpcResult) q.Dequeue();
        int result = (int) res.Result;
        if(result != 0) {
          throw new Exception(res.Result.ToString());
        }
      } catch(Exception e) {
        Console.WriteLine(e);
      }

      Finished();
    }

    override public void Start()
    {
      base.Start();
      var sender = new AHExactSender(Node0, Node1.Address);
      Channel q = new Channel(1);
      q.CloseEvent += Callback;
      Node0.Rpc.Invoke(sender, q, "sys:link.Ping", 0);
    }

    override public string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Rtt State: ");
      if(Done) {
        sb.Append("Complete");
        sb.Append("\n\tTime taken: ");
        sb.Append(TimeTaken);
      } else {
        sb.Append("In progress.");
      }
      return sb.ToString();
    }
  }
}
