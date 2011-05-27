#if BRUNET_NUNIT
using System;
using Brunet;
using Brunet.Connections;
using Brunet.Messaging.Mock;
using Brunet.Services;
using Brunet.Security.PeerSec.Symphony;
using Brunet.Symphony;
using Brunet.Util;
using Brunet.Simulator.Tasks;
using Brunet.Simulator.Transport;
using NUnit.Framework;

namespace Brunet.Simulator {
  [TestFixture]
  public class TestSimulator {
    private Simulator _sim;
    [TearDown]
    public void Cleanup()
    {
      if(_sim != null) {
        _sim.Disconnect();
        _sim = null;
      }
    }

    static readonly int fifteen_mins = (int) ((new TimeSpan(0, 15, 0)).Ticks / TimeSpan.TicksPerMillisecond);

    [Test]
    /// <summary>First half builds the ring, second half tests the connection handler...</summary>
    public void RingTest() {
      Parameters p = new Parameters("Test", "Test");
      string[] args = "-b=.2 -c --secure_senders -s=50".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      Simulator sim = new Simulator(p);
      _sim = sim;
      Assert.IsTrue(sim.Complete(true), "Simulation failed to complete the ring");

      SimpleTimer.RunSteps(fifteen_mins, false);
      var nm0 = sim.TakenIDs.Values[0];
      int idx = 1;
      NodeMapping nm1 = null;
      do {
        nm1 = sim.TakenIDs.Values[idx++];
      } while(Simulator.AreConnected(nm0.Node, nm1.Node) && idx < sim.TakenIDs.Count);

      Assert.IsFalse(Simulator.AreConnected(nm0.Node, nm1.Node), "Sanity check");
      var ptype = new PType("chtest");
      var ch0 = new ConnectionHandler(ptype, (StructuredNode) nm0.Node);
      var ch1 = new ConnectionHandler(ptype, (StructuredNode) nm1.Node);
      ConnectionHandlerTest(nm0.Node, nm1.Node, ch0, ch1);

      SimpleTimer.RunSteps(fifteen_mins * 2, false);

      Assert.IsFalse(Simulator.AreConnected(nm0.Node, nm1.Node), "Sanity check0");
      ptype = new PType("chtest1");
      ch0 = new SecureConnectionHandler(ptype, (StructuredNode) nm0.Node, nm0.Sso);
      ch1 = new SecureConnectionHandler(ptype, (StructuredNode) nm1.Node, nm1.Sso);
      ConnectionHandlerTest(nm0.Node, nm1.Node, ch0, ch1);
    }

    [Test]
    public void SecureRingTest() {
      Parameters p = new Parameters("Test", "Test");
      string[] args = "-b=.2 -c --secure_edges -s=25".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      Simulator sim = new Simulator(p);
      _sim = sim;
      Assert.IsTrue(sim.Complete(true), "Simulation failed to complete the ring");
      var nm0 = sim.TakenIDs.Values[0];
      int idx = 1;
      NodeMapping nm1 = null;
      do {
        nm1 = sim.TakenIDs.Values[idx++];
      } while(Simulator.AreConnected(nm0.Node, nm1.Node) && idx < sim.TakenIDs.Count);
      Assert.IsFalse(Simulator.AreConnected(nm0.Node, nm1.Node), "Sanity check");
      var ptype = new PType("chtest");
      var ch0 = new ConnectionHandler(ptype, (StructuredNode) nm0.Node);
      var ch1 = new ConnectionHandler(ptype, (StructuredNode) nm1.Node);
      ConnectionHandlerTest(nm0.Node, nm1.Node, ch0, ch1);
    }

    protected void ConnectionHandlerTest(Node node0, Node node1,
        ConnectionHandler ch0, ConnectionHandler ch1)
    {
      var mdh0 = new MockDataHandler();
      var mdh1 = new MockDataHandler();
      MemBlock zero = MemBlock.Reference(new byte[] {0});
      EventHandler cb = delegate(object o, EventArgs ea) {
        Assert.AreEqual(o, zero, "Zero");
      };

      mdh0.HandleDataCallback += cb;
      mdh1.HandleDataCallback += cb;
      ch0.Subscribe(mdh0, null);
      ch1.Subscribe(mdh1, null);

      Assert.AreEqual(mdh0.Count, 0, "MDH0 0");
      Assert.AreEqual(mdh1.Count, 0, "MDH1 0");
      ch0.ConnectTo(node1.Address);
      Assert.IsTrue(AreConnected(node0, node1), "ConnectionHandler ConnectTo");
      SimpleTimer.RunSteps(fifteen_mins * 2, false);
      Assert.IsFalse(Simulator.AreConnected(node0, node1));
      ch0.Send(node1.Address, zero);
      SimpleTimer.RunSteps(fifteen_mins / 60, false);
      Assert.AreEqual(mdh0.Count, 0, "MDH0 1");
      Assert.AreEqual(mdh1.Count, 0, "MDH1 1");
      Assert.IsTrue(AreConnected(node0, node1), "ConnectionHandler ConnectTo0");
      SimpleTimer.RunSteps(fifteen_mins / 3, false);
      ch0.Send(node1.Address, zero);
      SimpleTimer.RunSteps(fifteen_mins / 60, false);
      Assert.AreEqual(mdh0.Count, 0, "MDH0 2");
      Assert.AreEqual(mdh1.Count, 1, "MDH1 2");
      Assert.IsTrue(Simulator.AreConnected(node0, node1), "Continuous 0");
      SimpleTimer.RunSteps(fifteen_mins / 3, false);
      ch0.Send(node1.Address, zero);
      SimpleTimer.RunSteps(fifteen_mins / 60, false);
      Assert.AreEqual(mdh0.Count, 0, "MDH0 3");
      Assert.AreEqual(mdh1.Count, 2, "MDH1 3");
      Assert.IsTrue(Simulator.AreConnected(node0, node1), "Continuous 1");
      SimpleTimer.RunSteps(fifteen_mins / 3, false);
      ch0.Send(node1.Address, zero);
      SimpleTimer.RunSteps(fifteen_mins / 60, false);
      Assert.AreEqual(mdh0.Count, 0, "MDH0 4");
      Assert.AreEqual(mdh1.Count, 3, "MDH1 4");
      Assert.IsTrue(Simulator.AreConnected(node0, node1), "Continuous 2");
      SimpleTimer.RunSteps(fifteen_mins / 3, false);
      ch1.Send(node0.Address, zero);
      SimpleTimer.RunSteps(fifteen_mins / 60, false);
      Assert.AreEqual(mdh0.Count, 1, "MDH0 5");
      Assert.AreEqual(mdh1.Count, 3, "MDH1 5");
      Assert.IsTrue(Simulator.AreConnected(node0, node1), "Continuous 3");
      SimpleTimer.RunSteps(fifteen_mins * 2, false);
      Assert.IsFalse(Simulator.AreConnected(node0, node1), "Dead");
      Assert.AreEqual(mdh0.Count, 1, "MDH0 6");
      Assert.AreEqual(mdh1.Count, 3, "MDH1 6");
    }

//    [Test]
    public void CompleteTheDtlsRing() {
      Parameters p = new Parameters("Test", "Test");
      string[] args = "-b=.2 --dtls -c --secure_edges -s=25".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      Simulator sim = new Simulator(p);
      Assert.IsTrue(sim.Complete(true), "Simulation failed to complete the ring");
    }

    [Test]
    public void CompleteTheSubring() {
      SubringParameters p = new SubringParameters();
      string[] args = "-b=.2 -c --secure_edges -s=25 --subring=10".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      SubringSimulator sim = new SubringSimulator(p);
      _sim = sim;
      Assert.IsTrue(sim.Complete(true), "Simulation failed to complete the ring");
    }

    [Test]
    public void TestNatTraversal() {
      Parameters p = new Parameters("Test", "Test");
      string[] args = "-c -s=100".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      Simulator sim = new Simulator(p);
      _sim = sim;
      Assert.IsTrue(sim.Complete(true), "Simulation failed to complete the ring");
      SimpleTimer.RunSteps(1000000, false);

      TestNat(sim, NatTypes.Cone, NatTypes.Disabled, false);
      TestNat(sim, NatTypes.RestrictedCone, NatTypes.Disabled, false);
      TestNat(sim, NatTypes.Symmetric, NatTypes.Disabled, true);
      TestNat(sim, NatTypes.Symmetric, NatTypes.Disabled, NatTypes.RestrictedCone, NatTypes.Disabled, false);
      TestNat(sim, NatTypes.Symmetric, NatTypes.OutgoingOnly, true);
    }

    private void TestNat(Simulator sim, NatTypes type0, NatTypes type1, bool relay)
    {
      TestNat(sim, type0, type1, type0, type1, relay);
    }

    private void TestNat(Simulator sim, NatTypes n0type0, NatTypes n0type1,
        NatTypes n1type0, NatTypes n1type1, bool relay)
    {
      string fail_s = String.Format("{0}/{1} and {2}/{3}", n0type0, n0type1,
          n1type0, n1type1);
      Node node0 = null;
      Node node1 = null;
      while(true) {
        node0 = NatFactory.AddNode(sim, n0type0, n0type1, relay);
        node1 = NatFactory.AddNode(sim, n1type0, n1type1, relay);

        Assert.IsTrue(sim.Complete(true), fail_s + " nodes are connected to the overlay");
        if(!Simulator.AreConnected(node0, node1)) {
          break;
        }
      }

      ManagedConnectionOverlord mco = new ManagedConnectionOverlord(node0);
      mco.Start();
      node0.AddConnectionOverlord(mco);
      mco.Set(node1.Address);

      Assert.IsTrue(AreConnected(node0, node1), fail_s + " nodes were unable to connect.");
    }

    [Test]
    public void Relays() {
      Parameters p = new Parameters("Test", "Test");
      string[] args = "-s=100".Split(' ');
      Assert.AreNotEqual(-1, p.Parse(args), "Unable to parse" + p.ErrorMessage);
      RelayOverlapSimulator sim = new RelayOverlapSimulator(p);
      _sim = sim;

      Address addr1 = null, addr2 = null;
      Node node1 = null, node2 = null;
      while(true) {
        sim.AddDisconnectedPair(out addr1, out addr2, sim.NCEnable);
        sim.Complete(true);

        node1 = (sim.Nodes[addr1] as NodeMapping).Node as Node;
        node2 = (sim.Nodes[addr2] as NodeMapping).Node as Node;

        if(!Simulator.AreConnected(node1, node2)) {
          break;
        }
      }

      ManagedConnectionOverlord mco = new ManagedConnectionOverlord(node1);
      mco.Start();
      node1.AddConnectionOverlord(mco);
      mco.Set(addr2);
      Assert.IsTrue(AreConnected(node1, node2));

      foreach(Connection con in node1.ConnectionTable.GetConnections(Relay.OverlapConnectionOverlord.STRUC_OVERLAP)) {
        con.State.Edge.Close();
      }
      foreach(Connection con in node2.ConnectionTable.GetConnections(Relay.OverlapConnectionOverlord.STRUC_OVERLAP)) {
        con.State.Edge.Close();
      }

      Assert.IsTrue(Simulator.AreConnected(node1, node2));
    }

    protected bool AreConnected(Node node0, Node node1)
    {
      Task connected = new AreConnected(node0, node1, null);
      connected.Start();
      connected.Run(120);
      return connected.Done;
    }
  }
}
#endif
