using System;
using SAHC;

//var udp = SocketUdp.Create();
//udp.Start();
var a = new HammingCode(54);
a.Decode(a.Encode("Hello world!"));

