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

using System.Diagnostics;
using System.Threading;

namespace Brunet.Util {
  public class ProtocolLog {
    private static object _sync = new object();
    public static EventBooleanSwitch ConsoleLogEnable =
        new EventBooleanSwitch("ConsoleLogEnable", "Log for unknown!");
    public static EventBooleanSwitch Connections =
        new EventBooleanSwitch("Connections", "Logs connections");
    public static EventBooleanSwitch ConnectionTableLocks = 
        new EventBooleanSwitch("ConnectionTableLocks", "Logs locks in the ConnectionTable");
    public static EventBooleanSwitch EdgeClose =
        new EventBooleanSwitch("EdgeClose", "The reason why an edge was closed.");
    public static EventBooleanSwitch Exceptions =
        new EventBooleanSwitch("Exceptions", "Logs exceptions");
    public static EventBooleanSwitch LinkDebug =
        new EventBooleanSwitch("LinkDebug", "Log for Link");
    public static EventBooleanSwitch MapReduce =
        new EventBooleanSwitch("MapReduce", "Log map-reduce computations");
    public static EventBooleanSwitch Monitor =
        new EventBooleanSwitch("Monitor", "Log the system monitor");
    public static EventBooleanSwitch NodeLog =
        new EventBooleanSwitch("NodeLog", "Log for node");
    public static EventBooleanSwitch NatHandler =
        new EventBooleanSwitch("NatHandler", "Log for NatHandler");
    public static EventBooleanSwitch Pathing =
        new EventBooleanSwitch("Pathing", "Log for pathing");
    public static EventBooleanSwitch PolicyBasedCO =
        new EventBooleanSwitch("PolicyBasedCO", "On demand connections.");
    public static EventBooleanSwitch RelayEdge =
        new EventBooleanSwitch("RelayEdge", "Log for RelayEdge");
    public static EventBooleanSwitch SCO =
        new EventBooleanSwitch("SCO", "Log for SCO");
    public static EventBooleanSwitch Security =
        new EventBooleanSwitch("Security", "Security logging.");
    public static EventBooleanSwitch SecurityExceptions =
        new EventBooleanSwitch("SecurityExceptions", "Security Handling Exception logging.");
    public static EventBooleanSwitch UdpEdge =
        new EventBooleanSwitch("UdpEdge", "Log for UdpEdge and UdpEdgeListener");

    public static bool CTL_enabled = false;

    public static void Enable() {
      if(!ConsoleLogEnable.Enabled) {
        return;
      }
      lock(_sync) {
        if(!CTL_enabled) {
          Trace.Listeners.Add(new ConsoleTraceListener(true));
          CTL_enabled = true;
        }
      }
    }

    /**
     * According to documentation using the other WriteIf actually calls write.
     * I don't know if that really makes much sense, but as a safe keep, let's
     * use this WriteIf, shame there is no C# inlining :(
     */
    public static void WriteIf(BooleanSwitch bs, string msg) {
#if TRACE
      if(bs.Enabled) {
        Trace.WriteLine(bs.DisplayName + ":  " + Thread.CurrentThread.Name + ":  " + msg);
      }
#elif BRUNET_NUNIT
      if(bs.Enabled) {
        System.Console.WriteLine(msg);
      }
#endif
    }

    public static void Write(BooleanSwitch bs, string msg) {
#if TRACE
      Trace.WriteLine(bs.DisplayName + ":  " + Thread.CurrentThread.Name + ":  " + msg);
#elif BRUNET_NUNIT
      System.Console.WriteLine(msg);
#endif
    }
  }
}
