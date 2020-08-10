﻿//MIT License
//
//Copyright (c) 2020 Pixel Precision LLC
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

// <summary>
// An implementation of MIDIOutput for Android.
// </summary>
using UnityEngine;

#if UNITY_ANDROID

public class MIDIOutputAnd : MIDIOutput
{
    protected class ConnectionListener : PxPre.AndWrap.OnDeviceOpenedListener
    {
        MIDIOutputAnd and;
        PxPre.AndWrap.MidiDeviceInfo deviceInfo;

        public ConnectionListener(MIDIOutputAnd and, PxPre.AndWrap.MidiDeviceInfo deviceInfo)
        { 
            this.and = and;
            this.deviceInfo = deviceInfo;
        }

        public override void _impl_OnDeviceOpened(PxPre.AndWrap.MidiDevice device)
        {
            if(this.and == null)
                return;

            this.and._OnConnect(this, device, this.deviceInfo);
        }
    }

    MIDIMgr mgr;
    PxPre.AndWrap.MidiDeviceInfo deviceInfo;
    PxPre.AndWrap.PortInfo portInfo;

    PxPre.AndWrap.MidiDevice deviceConnection;
    PxPre.AndWrap.MidiInputPort inputConnection;

    string cachedDeviceName;
    string cachedManufacturer;
    string cachedProduct;

    public MIDIOutputAnd(
        MIDIMgr mgr,
        PxPre.AndWrap.MidiDeviceInfo deviceInfo, 
        PxPre.AndWrap.PortInfo portInfo)
    { 
        this.mgr = mgr;
        this.deviceInfo = deviceInfo;
        this.portInfo = portInfo;

        PxPre.AndWrap.Bundle bundle = deviceInfo.getProperties();
        this.cachedDeviceName   = bundle.getString(PxPre.AndWrap.MidiDeviceInfo.PROPERTY_NAME);
        this.cachedManufacturer = bundle.getString(PxPre.AndWrap.MidiDeviceInfo.PROPERTY_MANUFACTURER);
        this.cachedProduct      = bundle.getString(PxPre.AndWrap.MidiDeviceInfo.PROPERTY_PRODUCT);
    }

    public bool Equivalent(MIDIOutput output)
    {
        if(output == null)
            return false;

        MIDIOutputAnd inputAnd = output as MIDIOutputAnd;
        if(inputAnd == null)
            return false;

        if(this.portInfo == inputAnd.portInfo)
            return true;

        if(this.portInfo.getPortNumber() != inputAnd.portInfo.getPortNumber())
            return false;

        if(this.portInfo.getType() != inputAnd.portInfo.getType())
            return false;

        return true;
    }

    public bool Activate()
    { 
        if(this.deviceConnection != null)
            return false;

        PxPre.AndWrap.MidiManager midiMgr = 
            new PxPre.AndWrap.MidiManager(null);

        ConnectionListener conListener = new ConnectionListener(this, this.deviceInfo);

        midiMgr.openDevice(
            this.deviceInfo, 
            conListener, 
            new PxPre.AndWrap.Handler(null, null));

        // Activate won't do anything, we'll have to wait for the asyncronous callback
        // to come back before we can acknowledge a change.
        return false;
    }

    public bool Deactivate()
    { 
        if(this.deviceConnection == null)
            return false;

        AndroidMIDIDeviceRecord.Inst().RemoveOutput(this.deviceInfo);

        this.inputConnection.close();
        this.deviceConnection.close();
        this.deviceConnection = null;
        this.inputConnection = null;

        return true;
    }

    public string DeviceName()
    { 
        return this.cachedDeviceName;
    }

    public string Product()
    { 
        return this.cachedProduct;
    }

    public string Manufacture()
    { 
        return this.cachedManufacturer;
    }

    public int Port()
    { 
        return this.portInfo.getPortNumber();
    }

    public bool SendKeyPress(MIDIMgr mgr, int midinote, int channel, sbyte velocity)
    { 
        if(this.inputConnection == null)
            return false;

        channel = Mathf.Clamp(channel, 1, 16);

        sbyte[] buffer = new sbyte[32];
        int numBytes = 0;
        buffer[numBytes++] = (sbyte)(0x90 + (channel - 1)); // note on
        buffer[numBytes++] = (sbyte)midinote; // pitch is middle C
        buffer[numBytes++] = velocity; // max velocity
        int offset = 0;
        // post is non-blocking
        this.inputConnection.send(buffer, offset, numBytes);
        return true;
    }

    public bool SendKeyRelease(MIDIMgr mgr, int midinote, int channel)
    { 
        if(this.inputConnection == null)
            return false;

         channel = Mathf.Clamp(channel, 1, 16);

        sbyte[] buffer = new sbyte[32];
        int numBytes = 0;
        buffer[numBytes++] = (sbyte)(0x80 + (channel - 1)); // note off
        buffer[numBytes++] = (sbyte)midinote; // pitch is middle C
        buffer[numBytes++] = (sbyte)0;
        int offset = 0;
        // post is non-blocking
        this.inputConnection.send(buffer, offset, numBytes);
        return true;
    }

    void _OnConnect(ConnectionListener cl, PxPre.AndWrap.MidiDevice device, PxPre.AndWrap.MidiDeviceInfo deviceInfo)
    { 
        this.inputConnection = device.openInputPort(this.portInfo.getPortNumber());

        if(this.inputConnection == null)
        { 
            device.close();
            AndroidMIDIDeviceRecord.Inst().RemoveOutput(deviceInfo);

            return;
        }
        AndroidMIDIDeviceRecord.Inst().AddOutput(deviceInfo);

        this.deviceConnection = device;
        this.mgr._OnMIDIOutputConnected(this);
    }
}

#endif