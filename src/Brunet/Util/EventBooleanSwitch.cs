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
  /// <summary>Delegate called when a switch occurs.</summary>
  public delegate void SwitchNotify();

  /// <summary> Provides BooleanSwitch with an event whenever the
  /// OnSwitchSettingChanged method is called.</summary>
  public class EventBooleanSwitch : BooleanSwitch {
    public EventBooleanSwitch(string displayName, string description,
        string defaultSwitchValue) :
      base(displayName, description, defaultSwitchValue)
    {
    }

    public EventBooleanSwitch(string displayName, string description) :
      base(displayName, description)
    {
    }

    public event SwitchNotify SwitchedSetting;

    override protected void OnSwitchSettingChanged()
    {
      base.OnSwitchSettingChanged();
      if(SwitchedSetting != null) {
        SwitchedSetting();
      }
    }
  }
}
