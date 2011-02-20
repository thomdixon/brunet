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

using Brunet;
using Brunet.Messaging;
using Brunet.Util;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Brunet.Applications {
  /**
  <summary>Provides an Rpc mechanism for enabling / disabling logs.</summary>
  */
  public class LogManager : IRpcHandler {
    public readonly Node Node;
    public readonly ConsoleTraceListener Console;
    public readonly RpcManager Rpc;
    public LogManager(Node node) {
      Node = node;
      Rpc = node.Rpc;
      Rpc.AddHandler("LogManager", this);
      Console = new ConsoleTraceListener(true);
    }

    public void HandleRpc(ISender caller, String method, IList arguments,
                          object request_state) {
      object result = new InvalidOperationException("Invalid method");
      if(method.Equals("Enable") || method.Equals("Disable")) {
        if(arguments.Count < 2) {
          Rpc.SendResult(request_state, new Exception("Not enough arguments."));
          return;
        }
       
        string option_type = arguments[0] as string;
        string option_name = arguments[1] as string;
        if(option_type == null || option_name == null) {
          Rpc.SendResult(request_state, new Exception("Expected a string."));
          return;
        }

        if(option_type.Equals("BooleanSwitch")) {
          BooleanSwitch bs;
          if(!TryGetBooleanSwitch(option_name, out bs)) {
            Rpc.SendResult(request_state, new Exception("No such BooleanSwitch."));
            return;
          }

          bs.Enabled = method.Equals("Enable");
          result = true;
        } else if(option_type.Equals("Trace") && option_name.Equals("Console")) {
          if(method.Equals("Enable")) {
            Trace.Listeners.Add(Console);
          } else {
            Trace.Listeners.Remove(Console);
          }
          result = true;
        } else {
          result = new InvalidOperationException("Invalid method");
        }
      }
      Rpc.SendResult(request_state, result);
    }

    public static bool TryGetBooleanSwitch(string name, out BooleanSwitch bs)
    {
      Type type = typeof(ProtocolLog);
      FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
      if(field == null) {
        bs = null;
        return false;
      }
      bs = field.GetValue(null) as BooleanSwitch;
      return bs != null;
    }
  }
}
